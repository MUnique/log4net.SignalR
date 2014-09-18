#region Using directives

using System;
using log4net.Appender;
using log4net.Core;
using Microsoft.AspNet.SignalR.Client;

#endregion


namespace log4net.SignalR
{
    public class SignalrAppender : AppenderSkeleton
    {
        private IHubProxy proxyConnection;
        
        private HubConnection hubConnection;

        public SignalrAppender()
        {
            System.Diagnostics.Debug.WriteLine("Instantiating");
        }

        public string ProxyUrl { get; set; }

        protected override void Append(LoggingEvent loggingEvent)
        {
            // LoggingEvent may be used beyond the lifetime of the Append()
            // so we must fix any volatile data in the event
            loggingEvent.Fix = FixFlags.All;

            var formattedEvent = RenderLoggingEvent(loggingEvent);

            var logEntry = new LogEntry(formattedEvent, new JsonLoggingEventData(loggingEvent));

            if (!string.IsNullOrEmpty(this.ProxyUrl))
            {
                this.SendLogEntryOverProxy(logEntry);
            }
            else
            {
                this.SendLogEntryOverGlobalHost(logEntry);
            }
        }
        
        protected override void OnClose()
        {
            base.OnClose();
            if (this.hubConnection != null)
            {
                this.hubConnection.Dispose();
                this.hubConnection = null;
            }
        }
        
        private void EnsureConnection()
        {
            if (this.hubConnection == null)
            {
                this.hubConnection = new HubConnection(this.ProxyUrl);
                this.proxyConnection = this.hubConnection.CreateHubProxy(typeof(SignalrAppenderHub).Name);
            }
            
            if (this.hubConnection.State == ConnectionState.Disconnected)
            {
                this.hubConnection.Start();
            }
        }
        
        private void SendLogEntryOverGlobalHost(LogEntry entry)
        {
            try
            {
                var hubContext = Microsoft.AspNet.SignalR.GlobalHost.ConnectionManager.GetHubContext(typeof(SignalrAppenderHub).Name);
                hubContext.Clients.Group(SignalrAppenderHub.Log4NetGroup).onLoggedEvent(entry.FormattedEvent, entry.LoggingEvent);
            }
            catch (Exception e)
            {
                LogManager.GetLogger(string.Empty).Warn("SendLogEntryOverGlobalHost Failed:", e);
            }
        }

        private void SendLogEntryOverProxy(LogEntry entry)
        {
            try
            {
                this.EnsureConnection();
                if (proxyConnection != null && this.hubConnection.State == ConnectionState.Connected)
                {
                    this.proxyConnection.Invoke("OnMessageLogged", entry);
                }
            }
            catch (Exception e)
            {
                LogManager.GetLogger(string.Empty).Warn("OnMessageLogged Failed:", e);
            }
        }
    }


    public class LogEntry
    {
        public LogEntry(string formttedEvent, JsonLoggingEventData loggingEvent)
        {
            FormattedEvent = formttedEvent;
            LoggingEvent = loggingEvent;
        }

        public string FormattedEvent { get; set; }
        public JsonLoggingEventData LoggingEvent { get; set; }
    }
}
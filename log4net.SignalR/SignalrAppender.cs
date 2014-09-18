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
            this.HubName = typeof(SignalrAppenderHub).Name;
            this.GroupName = SignalrAppenderHub.DefaultGroup;
        }

        public string ProxyUrl { get; set; }
        
        public string HubName { get; set; }
        
        public string GroupName { get; set; }
        
        protected override void Append(LoggingEvent loggingEvent)
        {
            // LoggingEvent may be used beyond the lifetime of the Append()
            // so we must fix any volatile data in the event
            loggingEvent.Fix = FixFlags.All;

            var formattedEvent = RenderLoggingEvent(loggingEvent);

            var logEntry = new LogEntry(formattedEvent, new JsonLoggingEventData(loggingEvent));

            if (string.IsNullOrEmpty(this.ProxyUrl))
            {
                this.SendLogEntryOverGlobalHost(logEntry);
            }
            else
            {
                this.SendLogEntryOverProxy(logEntry);
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
                this.proxyConnection = this.hubConnection.CreateHubProxy(this.HubName);
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
                var hubContext = Microsoft.AspNet.SignalR.GlobalHost.ConnectionManager.GetHubContext(this.HubName);
                hubContext.Clients.Group(this.GroupName).onLoggedEvent(entry.FormattedEvent, entry.LoggingEvent);
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
                    this.proxyConnection.Invoke("OnMessageLogged", entry, this.GroupName);
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
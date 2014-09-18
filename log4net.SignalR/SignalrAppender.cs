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
        private string _proxyUrl = "";
        private IHubProxy proxyConnection;

        public SignalrAppender()
        {
            System.Diagnostics.Debug.WriteLine("Instantiating");
        }

        public string ProxyUrl
        {
            get { return _proxyUrl; }
            set
            {
                if (value != "")
                {
                    HubConnection connection = new HubConnection(value);
                    proxyConnection = connection.CreateHubProxy(typeof(SignalrAppenderHub).Name);
                    connection.Start().Wait();
                }
                else
                {
                    proxyConnection = null;
                }
                _proxyUrl = value;
            }
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            // LoggingEvent may be used beyond the lifetime of the Append()
            // so we must fix any volatile data in the event
            loggingEvent.Fix = FixFlags.All;

            var formattedEvent = RenderLoggingEvent(loggingEvent);

            var logEntry = new LogEntry(formattedEvent, new JsonLoggingEventData(loggingEvent));

            if (proxyConnection != null)
            {
                ProxyOnMessageLogged(logEntry);
            }
            else
            {
                this.SendLogEntryOverGlobalHost(logEntry);
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
        
        private void ProxyOnMessageLogged(LogEntry entry)
        {
            try
            {
                proxyConnection.Invoke("OnMessageLogged", entry);
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
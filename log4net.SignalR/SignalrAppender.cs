#region Using directives

using System;
using System.Threading;
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

        private long currentId;

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
            var id = Interlocked.Increment(ref this.currentId);
            var logEntry = new LogEntry(id, formattedEvent, new JsonLoggingEventData(loggingEvent));

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
                SignalrAppenderHub.SendOnMessageLoggedByGlobalHost(entry, this.HubName, this.GroupName);
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
                    this.proxyConnection.Invoke(nameof(SignalrAppenderHub.OnMessageLogged), entry, this.GroupName);
                }
            }
            catch (Exception e)
            {
                LogManager.GetLogger(string.Empty).Warn("OnMessageLogged Failed:", e);
            }
        }
    }

    /// <summary>
    /// A log entry which is sent to the client.
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogEntry"/> class.
        /// </summary>
        /// <param name="id">The unique identifier which is a sequence number.</param>
        /// <param name="formttedEvent">The formtted event as configured in the settings.</param>
        /// <param name="loggingEvent">The logging event data.</param>
        public LogEntry(long id, string formttedEvent, JsonLoggingEventData loggingEvent)
        {
            this.Id = id;
            this.FormattedEvent = formttedEvent;
            this.LoggingEvent = loggingEvent;
        }

        /// <summary>
        /// Gets or sets the unique identifier (sequence number).
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the event as formatted text as configured in the settings.
        /// </summary>
        public string FormattedEvent { get; set; }

        /// <summary>
        /// Gets or sets the logging event data.
        /// </summary>
        public JsonLoggingEventData LoggingEvent { get; set; }
    }
}
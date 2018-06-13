#region Using directives

using System;
using Microsoft.AspNet.SignalR;

#endregion


namespace log4net.SignalR
{
    /// <summary>
    /// Client interface for the <see cref="SignalrAppenderHubBase{TClient}"/>.
    /// </summary>
    public interface ISignalrAppenderHubClient
    {
        void OnLoggedEvent(string formattedEvent, JsonLoggingEventData entry, long id);
    }

    public class OnMessageLoggedEventArgs : EventArgs
    {
        public OnMessageLoggedEventArgs(string hubName, string groupName, LogEntry entry)
        {
            HubName = hubName;
            GroupName = groupName;
            Entry = entry;
        }

        public string HubName { get; }

        public string GroupName { get; }

        public LogEntry Entry { get; }
    }

    public class SignalrAppenderHub : SignalrAppenderHubBase<ISignalrAppenderHubClient>
    {
        public static event EventHandler<OnMessageLoggedEventArgs> OnMessageLoggedByGlobalHost;
        public static void SendOnMessageLoggedByGlobalHost(LogEntry entry, string hubName, string groupName = DefaultGroup)
        {
            var hubContext = Microsoft.AspNet.SignalR.GlobalHost.ConnectionManager.GetHubContext<ISignalrAppenderHubClient>(hubName);
            hubContext.Clients.Group(groupName).OnLoggedEvent(entry.FormattedEvent, entry.LoggingEvent, entry.Id);
            OnMessageLoggedByGlobalHost?.Invoke(hubContext, new OnMessageLoggedEventArgs(hubName, groupName, entry));
        }
    }

    public abstract class SignalrAppenderHubBase<TClient> : Hub<TClient>
        where TClient: class, ISignalrAppenderHubClient
    {
        public const string DefaultGroup = "Log4NetGroup";

        public void Listen()
        {
            this.Listen(DefaultGroup);
        }
        
        public virtual void Listen(string groupName)
        {
            this.Groups.Add(Context.ConnectionId, groupName);
        }

        public void OnMessageLogged(LogEntry e)
        {
            this.OnMessageLogged(e, DefaultGroup);
        }
        
        public virtual void OnMessageLogged(LogEntry e, string groupName)
        {
            this.Clients.Group(groupName).OnLoggedEvent(e.FormattedEvent, e.LoggingEvent, e.Id);
        }
    }
}
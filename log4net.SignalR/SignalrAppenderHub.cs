#region Using directives

using Microsoft.AspNet.SignalR;

#endregion


namespace log4net.SignalR
{
    public class SignalrAppenderHub : Hub
    {
        public static string DefaultGroup
        {
            get
            {
                return "Log4NetGroup";
            }
        }

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
            this.Clients.Group(groupName).onLoggedEvent(e.FormattedEvent, e.LoggingEvent);
        }
    }
}
# log4net.SignalR


**log4net.SignalR** is a [Log4Net Appender](http://logging.apache.org/log4net/release/manual/introduction.html#appenders) 
that forwards Log4Net events to a SignalR hub. From this hub it gets forwarded to its registered SignalR clients, e.g. to a JavaScript client in the browser.
It uses the [SignalR](https://github.com/SignalR/SignalR) async signaling library to stream these events in real-time over a persistent connection between the server and client.

The main use case for log4net.SignalR is building a log viewer on your website that gives easy visibility to diagnostic information and errors logged on the server.


## Getting started

Getting started is easy. You can also check out the bundled MvcExample project to see how it works.

### Add log4net.SignalR.dll

Add the compiled log4net.SignalR.dll assembly or the source files to your project. 
The easiest way is to use the nuget package, because it adds all the required dependencies itself. The package name is munique.log4net.signalr.appender and can be added with the following command in the Package Manager Console:

* `Tools --> NuGet Package Manager --> Package Manager Console`
* Run ``Install-Package munique.log4net.signalr.appender``

### Configure log4net.SignalR as a Log4Net appender
#### Self Hosted Hub
Configure log4net.SignalR as a Log4Net appender to a self-hosted hub (for example, messages logged from within a web application) by adding this to your log4net configuration (usually web.config):

```xml
<configSections>
  <section name="log4net" type="log4net.Config.Log4NetConfigurationSectionHandler, log4net" />
</configSections>

<log4net debug="true">
    <appender name="SignalrAppender" type="log4net.SignalR.SignalrAppender, log4net.SignalR">
        <layout type="log4net.Layout.PatternLayout">
            <conversionPattern value="%date %-5level - %message%newline" />
        </layout>
    </appender>
    <root>
        <appender-ref ref="SignalrAppender" />
    </root>
</log4net>
```

#### Remotely hosted hub
Configure log4net.SignalR as a Log4Net appender to a remotely hosted hub, e.g. messages logged from a console application, but displayed in a web application (usually log4net.config or app.config):

```xml
<configSections>
  <section name="log4net" type="log4net.Config.Log4NetConfigurationSectionHandler, log4net" />
</configSections>

<log4net debug="true">
    <appender name="SignalrAppender" type="log4net.SignalR.SignalrAppender, log4net.SignalR">
		<proxyUrl>http://localhost/</proxyUrl> <!-- Note: This should point to the root of your Web Application, not the hub itself -->
        <layout type="log4net.Layout.PatternLayout">
            <conversionPattern value="%date %-5level - %message%newline" />
        </layout>
    </appender>
    <root>
        <appender-ref ref="SignalrAppender" />
    </root>
</log4net>
```


### Usage Example: Set up a page to listen for events

Add some jQuery to your web page to listen out for events raised on the server. Once the SignalrAppender is set up, all events logged on the server using Log4Net will be transmitted to the hub and a browser can listen to it by executing a JavaScript function.

Here we're adding each event's details to an HTML table, but you can use the `onLoggedEvent` callback and the log details passed in the `loggedEvent` parameter object to do anything you like.

```javascript
    $(function () {
        var log4net = $.connection.signalrAppenderHub;

        log4net.client.onLoggedEvent = function (formattedEvent, loggedEvent, id) {
            var dateCell = $("<td>").css("white-space", "nowrap").text(loggedEvent.TimeStamp);
            var levelCell = $("<td>").text(loggedEvent.Level);
            var detailsCell = $("<td>").text(loggedEvent.Message);
            var row = $("<tr>").append(dateCell, levelCell, detailsCell);
            $('#log-table tbody').append(row);
        };

        $.connection.hub.logging = true; // turn signalr console logging on/off

        $.connection.hub.start(function() {
            log4net.server.listen();
        });
    });
```

The parameters of `onLoggedEvent` are simply:
  * formattedEvent: The log entry formatted as string as configured in your log4net configuration
  * loggedEvent: An object which contains details about the logged event, such as `Message`, `Level`, `TimeStamp` or `ExceptionString`.
  * id: Sequentially ordered identifier. May be helpful if you want to cache log messages and later retrieve them by a client which connects after the event happend.

### Hub Name and Group
Additionally you can specifiy a hub name, and a group name. This way you can use more than one SignalR appender, e.g. to provide different appenders for different parts of your application or to provide only specific log messages to a specific group of clients.

The hub name is the name of the appender type. The default name is "SignalrAppenderHub". You can inherit from it in your hosting application if you need additional hubs:

```c#
public class MyHub : log4net.SignalR.SignalrAppenderHub 
{
}
```

The default group name is "Log4NetGroup". E.g. we set it to "MyGroup" in the configuration as well as the hub name:

```xml
<configSections>
  <section name="log4net" type="log4net.Config.Log4NetConfigurationSectionHandler, log4net" />
</configSections>

<log4net debug="true">
    <appender name="SignalrAppender" type="log4net.SignalR.SignalrAppender, log4net.SignalR">
        <HubName>MyHub</HubName>
        <GroupName>MyGroup</GroupName>
        <layout type="log4net.Layout.PatternLayout">
            <conversionPattern value="%date %-5level - %message%newline" />
        </layout>
    </appender>
    <root>
        <appender-ref ref="SignalrAppender" />
    </root>
</log4net>
```

To listen to the hub of this appender, we have to specify the hub name and the group accordingly in the SignalR client, e.g. Javascript:


```javascript
    $(function () {
        var log4net = $.connection.myHub; // <-- HubName goes here

        log4net.client.onLoggedEvent = function (loggedEvent) {
            var dateCell = $("<td>").css("white-space", "nowrap").text(loggedEvent.TimeStamp);
            var levelCell = $("<td>").text(loggedEvent.Level);
            var detailsCell = $("<td>").text(loggedEvent.Message);
            var row = $("<tr>").append(dateCell, levelCell, detailsCell);
            $('#log-table tbody').append(row);
        };

        $.connection.hub.logging = true; // turn signalr console logging on/off

        $.connection.hub.start(function() {
            log4net.server.listen('MyGroup'); // <- GroupName goes here
        });
    });
```

#### To test the MVC example
Open up two browser windows/tabs. Keep one tab on the initial page. Use the other tab instance to navigate. You will see the log4net messages accumulate in the first tab.

### A note about running the SignalrAppenderHub within an ASP.Net web application
If you are hosting the hub from within an ASP.Net application (which you most likely are), you should also make sure that you also tell IIS to map the proper URLs for the hubs.  You should add the following statement somewhere in your Global.asax.cs.

```C#
app.MapSignalR(new HubConfiguration {
                EnableDetailedErrors = true, //Do this to help debugging, set to false in production
                EnableJSONP = true, //Do this if any of your SignalR hubs will be called by a proxy hub (like an appender in an external process)
                EnableJavaScriptProxies = true
            });
```
##License
log4net.SignalR is open source under the [The MIT License (MIT)](http://www.opensource.org/licenses/mit-license.php)

# netcore在浏览器显示系统日志
注：本示例参考 [**johnwas**](https://github.com/johnwas) 的代码来实现<br>
在项目部署运行时若系统报错，通常只能通过查看系统日志文件的方式来排查代码报错；这是一个非常不便的事情，通常需要登录服务器并找到系统日志文件，才能打开日志查看具体的日志信息；就算将日志记录到数据库或者elasticserach，查看起来也非常不便；若系统报错，直接打开浏览器就能看到报错信息，并确认报错的代码位置，这将非常有用非常酷。我们将实现这样的功能，netcore项目在浏览器输出日志实际中的效果如下：
![image](https://img2022.cnblogs.com/blog/883152/202209/883152-20220928091844846-580200501.png)


在浏览器显示日志是根据[https://github.com/lavspent/Lavspent.BrowserLogger](https://github.com/lavspent/Lavspent.BrowserLogger)为基础进行改造的。

下载该项目并运行Test

![image-20220927101418043](https://img2022.cnblogs.com/blog/883152/202209/883152-20220928091946508-1724663859.png)


运行效果

![image](https://img2022.cnblogs.com/blog/883152/202209/883152-20220928092049017-1700726738.png)


日志显示界面地址为http://localhost:5000/con，刷新http://localhost:5000/api/values，日志界面接收日志信息并实时显示效果如图

![image](https://img2022.cnblogs.com/blog/883152/202209/883152-20220928092111773-758566171.png)




我们将在netcore项目中使用serilog并使用Lavspent.BrowserLogger将日志信息显示在浏览器上。

新建net6 webapi项目，并添加Serilog.AspNetCore包引用

![image](https://img2022.cnblogs.com/blog/883152/202209/883152-20220928092137266-302768084.png)


在program中添加代码使用serilog



```c#
builder.Host.UseSerilog((context, logger) => { 
    logger.WriteTo.Console();
    logger.WriteTo.File("Logs/log.txt");

});
```

在WeatherForecastController中添加代码输出日志

![image](https://img2022.cnblogs.com/blog/883152/202209/883152-20220928092208531-427720521.png)


控制台和日志输出了代码中的日志信息，serilog启用正常。

![image](https://img2022.cnblogs.com/blog/883152/202209/883152-20220928092225806-1837670232.png)




将下载的Lavspent.BrowserLogger类库添加到webapi项目所在的解决方案中

![image](https://img2022.cnblogs.com/blog/883152/202209/883152-20220928093254651-1014063314.png)


按照Lavspent.BrowserLogger使用说明
![image](https://img2022.cnblogs.com/blog/883152/202209/883152-20220928092239815-1508303293.png)


添加使用代码

![image](https://img2022.cnblogs.com/blog/883152/202209/883152-20220928092254657-275283870.png)


```c#
using Lavspent.BrowserLogger.Extensions;
using Lavspent.BrowserLogger.Options;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Host.UseSerilog((context, logger) => { 
    logger.WriteTo.Console();
    logger.WriteTo.File("Logs/log.txt");
});
builder.Services.Configure<BrowserLoggerOptions>(builder.Configuration.GetSection("BrowserLog"));
builder.Services.AddBrowserLogger();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.UseWebSockets();
app.UseBrowserLogger();

app.Run();

```



BrowserLoggerOptions选项从配置文件appsetting.json读取

```json
{
  "BrowserLog": {
    "LogLevel": {
      "Default": "Warning"
    },
    "ConsolePath": "con",
    "WebConsole": {
      "LogStreamUrl": "wss://localhost:44364/ls",  //注意：改成自己项目的端口,如果项目使用https前缀为wss,http前缀为ws
      "ShowClassName": false
    }
  },
  "AllowedHosts": "*"
}

```

集成完成后，通过swagger触发测试方法

![image](https://img2022.cnblogs.com/blog/883152/202209/883152-20220928092333766-210840049.png)


发现Browser Logger没有输出日志信息

![image](https://img2022.cnblogs.com/blog/883152/202209/883152-20220928092353196-1737935812.png)


发现是Serilog的使用问题，Serilog提供各种接收器（Sink）来处理日志输出到不同位置。在program中这选中代码F12。

![image](https://img2022.cnblogs.com/blog/883152/202209/883152-20220928092405711-1254029024.png)


Serilog提供了ConsoleSink、FileSink来处理将日志输出到控制台和输出到文件。

![image](https://img2022.cnblogs.com/blog/883152/202209/883152-20220928092437233-1452478553.png)


为了Serilog的日志信息输出到Browser Logger，我们需要自定义一个日志接收器。关于Serilog的接收器，可查看：https://github.com/serilog/serilog/wiki/Provided-Sinks；

如何自定义Serilog接收器，可查看：https://github.com/serilog/serilog/wiki/Developing-a-sink；

自定义Serilog接收器：

在类库项目中添加接收器类BrowserSink.cs;添加扩展类BrowserLoggerConfigurationExtensions.cs

![image](https://img2022.cnblogs.com/blog/883152/202209/883152-20220928092451712-2058971755.png)


代码如下：

```c#


using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using System;
using System.IO;
using System.Text;
using Lavspent.BrowserLogger;
using Lavspent.BrowserLogger.Models;
using System.Collections.Generic;
using System.Threading;

namespace Serilog.Sinks.Browser
{
    public class BrowserSink : ILogEventSink
    {
        readonly ITextFormatter _textFormatter;
        string _outputTemplate;

        public BrowserSink(
            ITextFormatter textFormatter,
            string outputTemplate)
        {            
            _textFormatter = textFormatter ?? throw new ArgumentNullException(nameof(textFormatter));
            _outputTemplate = outputTemplate;
        }

        private void RenderFullExceptionInfo(TextWriter textWriter, Exception exception)
        {
            Stack<Exception> se = new Stack<Exception>();
            while (exception != null)
            {
                se.Push(exception);
                exception = exception.InnerException;
            }
            while (se.TryPop(out exception ))
            {
                textWriter.Write("\n*** Exception Source:[{0}] ***\n\n{1}\n\n{2}\n", 
                    exception.Source, 
                    exception.Message, 
                    exception.StackTrace);
                
            }         
        }
        public void Emit(LogEvent logEvent)
        {
            if (BrowserLoggerService.Instance == null)
                return;

            using (TextWriter textWriter = new StringWriter())
            {

                _textFormatter.Format(logEvent, textWriter);

                LogEventPropertyValue ev;
                Exception exception = logEvent.Exception;
                if (exception != null)
                {
                    if (logEvent.Properties.TryGetValue("EventId", out ev))
                    {
                        RenderFullExceptionInfo(textWriter, exception);
                    }
                }
                BrowserLoggerService.Instance.Enqueue(new LogMessageEntry
                {
                    LogLevel = (Microsoft.Extensions.Logging.LogLevel)logEvent.Level,
                    TimeStampUtc = DateTime.UtcNow,
                   // ThreadId= Thread.CurrentThread.ManagedThreadId,
                    Name = "",
                    Message = textWriter.ToString()
                });
            }
        }
    }
}

```

```c#

using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using Serilog.Sinks.Browser;
using System;
namespace Serilog
{
    public static class BrowserLoggerConfigurationExtensions
    {
        static readonly object DefaultSyncRoot = new object();
        public const string DefaultOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} [sid:{CorrelationId}] {NewLine}{Exception}";
        // public const string DefaultOutputTemplate = "{Message:lj}{NewLine}{Exception}[sid:{sid}][db:{db}]";

        public static LoggerConfiguration Browser(
            this LoggerSinkConfiguration sinkConfiguration,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            string outputTemplate = DefaultOutputTemplate,
            IFormatProvider formatProvider = null,
            LoggingLevelSwitch levelSwitch = null)
        {
            if (sinkConfiguration is null) throw new ArgumentNullException(nameof(sinkConfiguration));
            var formatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);
            return sinkConfiguration.Sink(new BrowserSink(formatter, outputTemplate),
                restrictedToMinimumLevel,
                levelSwitch);
        }

    }
}

```

在program中启用新定义的BrowserSink接收器，在UseSerilog修改成如下：

```C#
builder.Host.UseSerilog((context, logger) => { 

    logger.WriteTo.Console();

    logger.WriteTo.File("Logs/log.txt");

    logger.WriteTo.Browser();
});
```

在swagger触发测试方法，这时候Browser Logger接收到了日志信息：

![image](https://img2022.cnblogs.com/blog/883152/202209/883152-20220928092520753-198169335.png)


我们在WeatherForecastController添加方法测试异常信息

```C#
 		/// <summary>
        /// 添加方法测试异常信息
        /// </summary>
        /// <returns></returns>
        [HttpGet("/TestError")]
        public IActionResult TestError()
        {
            string result = string.Empty;
            try
            {
                //数组Summaries的只有十个元素，超过数组边界，报错
                result = Summaries[20];
               
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            return Ok(result);
        }
```

启动项目，在swagger触发TestError，Browser Logger接收到了报错日志信息，并提示我们报错的代码位置是哪一行，这在系统运行的时候是很有帮助的，开发人员不用去数据库、或者服务器日志文件就能看到报错的信息。

![image](https://img2022.cnblogs.com/blog/883152/202209/883152-20220928092540023-1250949272.png)


但是报错信息还是不够显眼，报错信息如果能变成红色显示就能很快区分开来；而且页面会一直显示接收到的日志信息，当接收到报错信息最好能断开接收器，这样就能停留在报错信息的位置，并去排查错误了。基于此，对Default.html改造一下。

```html
<!DOCTYPE html>

<html lang="en" xmlns="http://www.w3.org/1999/xhtml">
<head>
    <meta charset="utf-8" />
    <title>Browser Logger</title>

    <!--link href="https://fonts.googleapis.com/css?family=Space+Mono&display=swap" rel="stylesheet" -->

    <style type="text/css">
        html, body {
            background: black;
            color: silver;
            font-family: Space Mono, Lucida Console, monospace;
            font-size: 13px;
            height: 100%;
            margin: 0;
            padding: 0;
            top: 0;
            width: 100%;
        }

        .wrapper {
            display: flex;
            flex-direction: column;
            height: 100%;
        }

        .header, .footer {
            font-size: 13px;
            padding: 10px;
        }

        .content {
            -ms-flex: 1;
            -o-flex: 1;
            -webkit-flex: 1;
            background: #1D1C1C;
            color: greenyellow;
            flex: 1;
            overflow: auto;
            padding: 5px 10px;
            white-space: pre;
        }

        div.table {
            display: table;
        }

        div.row {
            display: table-row;
        }

        div.cell {
            display: table-cell;
            padding-right: 10px;
            white-space: nowrap;
        }

        div.lastcell {
            display: table-cell;
        }

        div.line {
            color: gray;
            text-align: right;
        }

        span.trce {
            background: black;
            color: silver;
        }

        span.dbug {
            background: black;
            color: white;
        }

        span.info {
            background: greenyellow;
            color: black;
        }

        span.warn {
            background: orange;
            color: white;
        }

        span.fail {
            background: red;
            color: white;
        }

        span.crit {
            background: darkred;
            color: white;
        }

        span.unkn {
            background: white;
            color: darkred;
        }

        span.threadid {
            background: greenyellow;
            color: black;
        }

        .flash {
            animation: fade 200ms 5;
        }

        @keyframes fade {
            from {
                opacity: 1.0;
            }

            50% {
                opacity: 0.4;
            }

            to {
                opacity: 1.0;
            }
        }

        .ledText {
            color: white;
            font-weight: bold;
            line-height: 25px;
            text-transform: uppercase;
            vertical-align: middle;
            cursor: pointer;
        }

        .dot {
            background-color: silver;
            border-radius: 50%;
            display: inline-block;
            height: 10px;
            line-height: 25px;
            vertical-align: middle;
            width: 10px;
        }

        .online {
            background: yellowgreen;
        }

        .offline {
            background: red;
        }

        a {
            color: white;
            text-decoration: none;
        }

            a:hover {
                text-decoration: underline;
            }

            a:active {
                color: white;
            }

            a:visited {
                color: white;
            }
    </style>

    <script language="javascript" type="text/javascript">
        var dateFormat = function () {
            var token = /d{1,4}|m{1,4}|yy(?:yy)?|([HhMsTt])\1?|[LloSZ]|"[^"]*"|'[^']*'/g,
                timezone =
                    /\b(?:[PMCEA][SDP]T|(?:Pacific|Mountain|Central|Eastern|Atlantic) (?:Standard|Daylight|Prevailing) Time|(?:GMT|UTC)(?:[-+]\d{4})?)\b/g,
                timezoneClip = /[^-+\dA-Z]/g,
                pad = function (val, len) {
                    val = String(val);
                    len = len || 2;
                    while (val.length < len) val = "0" + val;
                    return val;
                };

            // Regexes and supporting functions are cached through closure
            return function (date, mask, utc) {
                var dF = dateFormat;

                // You can't provide utc if you skip other args (use the "UTC:" mask prefix)
                if (arguments.length == 1 &&
                    Object.prototype.toString.call(date) == "[object String]" &&
                    !/\d/.test(date)) {
                    mask = date;
                    date = undefined;
                }

                // Passing date through Date applies Date.parse, if necessary
                date = date ? new Date(date) : new Date;
                if (isNaN(date)) throw SyntaxError("invalid date");

                mask = String(dF.masks[mask] || mask || dF.masks["default"]);

                // Allow setting the utc argument via the mask
                if (mask.slice(0, 4) == "UTC:") {
                    mask = mask.slice(4);
                    utc = true;
                }

                var _ = utc ? "getUTC" : "get",
                    d = date[_ + "Date"](),
                    D = date[_ + "Day"](),
                    m = date[_ + "Month"](),
                    y = date[_ + "FullYear"](),
                    H = date[_ + "Hours"](),
                    M = date[_ + "Minutes"](),
                    s = date[_ + "Seconds"](),
                    L = date[_ + "Milliseconds"](),
                    o = utc ? 0 : date.getTimezoneOffset(),
                    flags = {
                        d: d,
                        dd: pad(d),
                        ddd: dF.i18n.dayNames[D],
                        dddd: dF.i18n.dayNames[D + 7],
                        m: m + 1,
                        mm: pad(m + 1),
                        mmm: dF.i18n.monthNames[m],
                        mmmm: dF.i18n.monthNames[m + 12],
                        yy: String(y).slice(2),
                        yyyy: y,
                        h: H % 12 || 12,
                        hh: pad(H % 12 || 12),
                        H: H,
                        HH: pad(H),
                        M: M,
                        MM: pad(M),
                        s: s,
                        ss: pad(s),
                        l: pad(L, 3),
                        L: pad(L > 99 ? Math.round(L / 10) : L),
                        t: H < 12 ? "a" : "p",
                        tt: H < 12 ? "am" : "pm",
                        T: H < 12 ? "A" : "P",
                        TT: H < 12 ? "AM" : "PM",
                        Z: utc ? "UTC" : (String(date).match(timezone) || [""]).pop().replace(timezoneClip, ""),
                        o: (o > 0 ? "-" : "+") + pad(Math.floor(Math.abs(o) / 60) * 100 + Math.abs(o) % 60, 4),
                        S: ["th", "st", "nd", "rd"][d % 10 > 3 ? 0 : (d % 100 - d % 10 != 10) * d % 10]
                    };

                return mask.replace(token,
                    function ($0) {
                        return $0 in flags ? flags[$0] : $0.slice(1, $0.length - 1);
                    });
            };
        }();


        // Some common format strings
        dateFormat.masks = {
            "default": "ddd mmm dd yyyy HH:MM:ss",
            shortDate: "m/d/yy",
            mediumDate: "mmm d, yyyy",
            longDate: "mmmm d, yyyy",
            fullDate: "dddd, mmmm d, yyyy",
            shortTime: "h:MM TT",
            mediumTime: "h:MM:ss TT",
            longTime: "h:MM:ss TT Z",
            isoDate: "yyyy-mm-dd",
            isoTime: "HH:MM:ss",
            isoDateTime: "yyyy-mm-dd'T'HH:MM:ss",
            isoUtcDateTime: "UTC:yyyy-mm-dd'T'HH:MM:ss'Z'"
        };

        // Internationalization strings
        dateFormat.i18n = {
            dayNames: [
                "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat",
                "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"
            ],
            monthNames: [
                "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec",
                "January", "February", "March", "April", "May", "June", "July", "August", "September", "October",
                "November", "December"
            ]
        };

        // For convenience...
        Date.prototype.format = function (mask, utc) {
            return dateFormat(this, mask, utc);
        };

        var line = 1;
        var wspath, websocket, config;
		
        function init(options) {
            var scheme = 'ws://'
            if (window.location.protocol == 'https:')
                scheme = 'wss://'

            wspath = scheme + window.location.host + '/ls';
            config = options;
            openWebsocket();
        }

        function openWebsocket() {
            var output = document.getElementById("output");
            websocket = new WebSocket(wspath);
            console.log(wspath);
            websocket.onopen = function (evt) { onOpen(evt, config); };
            websocket.onclose = function (evt) { onClose(evt, config); };
            websocket.onmessage = function (evt) { onMessage(evt, config, output); };
            websocket.onerror = function (evt) { onError(evt, config, output); };
        }

        function closeWebsocket() {
            websocket.close();
        }

        function openOrClose() {
            if (websocket.readyState == WebSocket.CLOSED) {
                openWebsocket();
            } else if (websocket.readyState == WebSocket.OPEN) {
                closeWebsocket();
            }
        }
        function clearLog() {
            var output = document.getElementById("output");
            output.innerHTML = '';
        }

        function setStatus(options, status) {
            var ledText = document.getElementById("ledText");
            var ledLamp = document.getElementById("ledLamp");
            if (status) {
                ledText.innerHTML = "Connected";
                ledLamp.classList.remove("offline");
                ledLamp.classList.add("online");
            } else {
                ledText.innerHTML = "Disconnected";
                ledLamp.classList.remove("online");
                ledLamp.classList.add("offline");
            }
            ledText.title = ""
        }

        function onOpen(evt, options) {
            setStatus(options, true);
        }

        function onClose(evt, options) {

            setStatus(options, false);
        }

        function replaceURLWithHTMLLinks(text) {
            var exp = /(\b(https?|ftp|file):\/\/[-A-Z0-9+&@#\/%?=~_|!:,.;]*[-A-Z0-9+&@#\/%=~_|])/i;
            return text.replace(exp, "<a target='blank' href='$1'>$1</a>");
        }

        function replaceSourceLine(text) {
            var exp = /(in [\w\/\.\\:]+\.cs:)/g;
            return text.replace(exp, "<font color='yellow'>$1</font>");
        }

        function getShortLogLevel(logLevel) {
            switch (logLevel) {
                case "Trace":
                    return "TRA";
                case "Debug":
                    return "DBG";
                case "Information":
                    return "INF";
                case "Warning":
                    return "WRN";
                case "Error":
                    return "ERR";
                case "Critical":
                    return "CRI";
                default:
                    return "UNK";
            }
        }


        var timeToStopFlash = new Date().getTime();

        function CheckFlash() {
            var curTime = new Date().getTime();
            if (curTime >= timeToStopFlash) {
                var ledLamp = document.getElementById("ledLamp");
                if (ledLamp)
                    ledLamp.classList.remove("flash");
            }
            setTimeout("CheckFlash()", 1500)
        }
        CheckFlash()

        function onMessage(evt, options, output) {
            timeToStopFlash = new Date().getTime() + 1500;
            var ledLamp = document.getElementById("ledLamp");
            if (!ledLamp.classList.contains("flash"))
                ledLamp.classList.add("flash");

            var data = JSON.parse(evt.data);

            var filter = document.getElementsByName("filter")[0].value;

            if (filter != "" && data.message.search(filter) == -1) {
                return
            }

            data.message = data.message.replace(/\\"/g, '"');
            data.message = replaceSourceLine(data.message);
            var message = '';

            if (options.showLineNumbers)
                message = message + '<div class="cell line">' + line + '&gt;</div>';

            var logLevel = getShortLogLevel(data.logLevel);
            message = message + '<div class="cell"><span class="' + logLevel + '">' + logLevel + '</div>';

            if (options.showTimeStamp) {
                var date = new Date(data.timeStampUtc);
                message = message +
                    '<div class="cell timestamp" title="' +
                    date.format("default") +
                    '">' +
                    date.format(options.dateFormatString) +
                    '</div>';
            }

            message = message + '<div class="threadid">[' + data.threadId + ']</div>';

            if (options.showClassName)
                message = message + '<div class="cell name">' + data.name + '</div>';

            data.message = message +
                '<div class="lastcell message" title="' +
                data.name +
                '">' +
                replaceURLWithHTMLLinks(data.message) +
                '</div>';

            writeToScreen(output, options, data);
            line++;
            if (!options.NewOnTop)
                output.scrollIntoView(false);
        }

        function onError(evt, options, output) {
            writeToScreen(output, options, evt.data);
        }

        function writeToScreen(output, options, data) {
            var row = document.createElement("div");
            row.innerHTML = data.message;
            row.className = "row";

            switch (data.logLevel) {
                case 'Critical':
                    row.style.color = 'orangered'
                    break;

                case 'Error':
                    row.style.color = 'orangered'
                    break;

                case 'Warning':
                    row.style.color = 'yellow'
                    break;
            }

            if (data.message.includes("- Exceptions:"))
                row.style.color = 'orangered'

            if (options.newOnTop)
                output.insertAdjacentElement('afterbegin', row);
            else {
                output.appendChild(row);
            }
        }
    </script>
</head>
<body>
    <div class="wrapper">
        <div class="header">
            <span>Logger state:</span>
            <span id="ledLamp" class="dot offline" hint=""></span>
            <span id="ledText" class="ledText" hint="" onclick="openOrClose()" title="">Unknown</span>
            //添加过滤输入框
            <span><input type="text" name="filter" style="width: 25%" /></span>
            <span id="clearLog" class="ledText" hint="" onclick="clearLog()" title="">Clear</span>
        </div>
        <div class="content">
            <div id="output"></div>
        </div>
    </div>
</body>
</html>
```

启动项目接着触发TestError，这时候我们看到报错信息已经变成了红色一目了然。

![image](https://img2022.cnblogs.com/blog/883152/202209/883152-20220928092610664-705284170.png)


点击CONNECTED,连接信息就会变成DISCONNETED,尝试触发测试方法，Browser Logger不再接收新的信息。

![image](https://img2022.cnblogs.com/blog/883152/202209/883152-20220928092632729-177710293.png)




模拟不同人员使用系统，我们只关注触发报错的用户日志信息。修改控制代码

![image](https://img2022.cnblogs.com/blog/883152/202209/883152-20220928092642592-1109749357.png)


在日志接收页面的过滤器中输入过滤关键字：456（模拟报错用户），点击GetWeatherForecast、TestError。

![image](https://img2022.cnblogs.com/blog/883152/202209/883152-20220928092654810-2077138927.png)


日志显示页面只显示包含[token:456]的报错信息。

真实项目中如果要设定一些日志的额外信息，可通Enrichment来设置，详细信息可查看：https://github.com/serilog/serilog/wiki/Enrichment。

博客园地址：[https://www.cnblogs.com/qmjblog/articles/16736898.html](https://www.cnblogs.com/qmjblog/articles/16736898.html)

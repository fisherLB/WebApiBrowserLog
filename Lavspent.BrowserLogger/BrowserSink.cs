

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

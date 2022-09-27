
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

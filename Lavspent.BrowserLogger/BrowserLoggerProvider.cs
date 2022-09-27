using Microsoft.Extensions.Logging;

namespace Lavspent.BrowserLogger
{
    public sealed class BrowserLoggerProvider : ILoggerProvider
    {
        private readonly BrowserLoggerService _browserLoggerService;

        public BrowserLoggerProvider(BrowserLoggerService browserLoggerService)
        {
            _browserLoggerService = browserLoggerService;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new BrowserLogger(categoryName, null, _browserLoggerService);
        }

        public void Dispose()
        {
        }
    }
}
using Microsoft.Extensions.Logging;

namespace Sys.Common.Logs
{
    public static class StaticLog
    {
        public static ILoggerFactory LoggerFactory { get; set; }

        public static ILogger CreateLogger<T>() => LoggerFactory.CreateLogger<T>();

        public static ILogger CreateLogger(string categoryName) => LoggerFactory.CreateLogger(categoryName);
    }
}
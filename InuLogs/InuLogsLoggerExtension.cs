using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using InuLogs.src;

namespace InuLogs
{
    public static class InuLogsLoggerExtension
    {
        public static ILoggingBuilder AddInuLogsLogger(this ILoggingBuilder builder, bool logCallerInfo = true, bool log = true)
        {
            builder.Services.AddSingleton<ILoggerProvider, InuLogsLoggerProvider>(_ => new InuLogsLoggerProvider(log, logCallerInfo));
            return builder;
        }
    }
}

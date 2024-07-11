using InuLogs.src.Managers;
using InuLogs.src.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace InuLogs.src
{
    internal class InuLogsExceptionLogger
    {
        private readonly RequestDelegate _next;
        public InuLogsExceptionLogger(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await LogException(ex, InuLogs.RequestLog);
                throw;
            }
        }
        public async Task LogException(Exception ex, RequestModel requestModel)
        {
            var inuExceptionLog = new InuExceptionLog();
            inuExceptionLog.EncounteredAt = DateTime.Now;
            inuExceptionLog.Message = ex.Message;
            inuExceptionLog.StackTrace = ex.StackTrace;
            inuExceptionLog.Source = ex.Source;
            inuExceptionLog.TypeOf = ex.GetType().ToString();
            inuExceptionLog.Path = requestModel?.Path;
            inuExceptionLog.Method = requestModel?.Method;
            inuExceptionLog.QueryString = requestModel?.QueryString;
            inuExceptionLog.RequestBody = requestModel?.RequestBody;

            //Insert
            await DynamicDBManager.InsertInuExceptionLog(inuExceptionLog);
        }
    }
}

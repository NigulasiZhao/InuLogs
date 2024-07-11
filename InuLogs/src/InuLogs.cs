using InuLogs.src.Enums;
using InuLogs.src.Helpers;
using InuLogs.src.Interfaces;
using InuLogs.src.Managers;
using InuLogs.src.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.IO;
using MySqlX.XDevAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Ubiety.Dns.Core.Common;

namespace InuLogs.src
{
    internal class InuLogs
    {
        public static RequestModel RequestLog;
        public static InuLogsSerializerEnum Serializer;
        private readonly RequestDelegate _next;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
        //private readonly IBroadcastHelper _broadcastHelper;
        private readonly InuLogsOptionsModel _options;

        public InuLogs(InuLogsOptionsModel options, RequestDelegate next/*, IBroadcastHelper broadcastHelper*/)
        {
            _next = next;
            _options = options;
            _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
            //_broadcastHelper = broadcastHelper;

            Serializer = options.Serializer;
            InuLogsConfigModel.UserName = _options.InuPageUsername;
            InuLogsConfigModel.Password = _options.InuPagePassword;
            InuLogsConfigModel.Blacklist = String.IsNullOrEmpty(_options.Blacklist) ? new string[] { } : _options.Blacklist.Replace(" ", string.Empty).Split(',');
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var requestPath = context.Request.Path.ToString();

            if (requestPath.StartsWith('/'))
                requestPath = requestPath.Remove(0, 1);

            if (!requestPath.Contains("ZLinupage") &&
                !requestPath.Contains("inulogs") &&
                !requestPath.Contains("ZLGstatics") &&
                !requestPath.Contains("favicon") &&
                !requestPath.Contains("zllogger") &&
                !ShouldBlacklist(requestPath))
            {
                //Request handling comes here
                var requestLog = await LogRequest(context);
                var responseLog = await LogResponse(context);

                var timeSpent = responseLog.FinishTime.Subtract(requestLog.StartTime);

                var inuLog = new InuLog
                {
                    IpAddress = context.Connection.RemoteIpAddress.ToString(),
                    ResponseStatus = responseLog.ResponseStatus,
                    QueryString = requestLog.QueryString,
                    Method = requestLog.Method,
                    Path = requestLog.Path,
                    Host = requestLog.Host,
                    RequestBody = requestLog.RequestBody,
                    ResponseBody = responseLog.ResponseBody,
                    TimeSpent = FormatTimeSpan(timeSpent),
                    RequestHeaders = requestLog.Headers,
                    ResponseHeaders = responseLog.Headers,
                    StartTime = requestLog.StartTime,
                    EndTime = responseLog.FinishTime,
                    Scheme = requestLog.Scheme,
                };

                await DynamicDBManager.InsertInuLog(inuLog);
                //await _broadcastHelper.BroadcastInuLog(inuLog);
            }
            else
            {
                await _next.Invoke(context);
            }
        }

        private async Task<RequestModel> LogRequest(HttpContext context)
        {
            var startTime = DateTime.Now;

            var requestBodyDto = new RequestModel()
            {
                RequestBody = string.Empty,
                Host = context.Request.Host.ToString(),
                Path = context.Request.Path.ToString(),
                Method = context.Request.Method.ToString(),
                QueryString = context.Request.QueryString.ToString(),
                StartTime = startTime,
                Scheme = context.Request.Scheme,
                // Headers = context.Request.Headers.Select(x => x.ToString()).Aggregate((a, b) => a + ": " + b),
                Headers = System.Text.Json.JsonSerializer.Serialize(context.Request.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)), new JsonSerializerOptions { WriteIndented = true })
            };


            if (context.Request.ContentLength > 1)
            {
                context.Request.EnableBuffering();
                await using var requestStream = _recyclableMemoryStreamManager.GetStream();
                await context.Request.Body.CopyToAsync(requestStream);
                requestBodyDto.RequestBody = GeneralHelper.ReadStreamInChunks(requestStream);
                context.Request.Body.Position = 0;
            }
            RequestLog = requestBodyDto;
            return requestBodyDto;
        }

        private async Task<ResponseModel> LogResponse(HttpContext context)
        {
            using (var originalBodyStream = context.Response.Body)
            {
                try
                {
                    using (var originalResponseBody = _recyclableMemoryStreamManager.GetStream())
                    {
                        context.Response.Body = originalResponseBody;
                        await _next(context);
                        context.Response.Body.Seek(0, SeekOrigin.Begin);
                        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
                        context.Response.Body.Seek(0, SeekOrigin.Begin);
                        var responseBodyDto = new ResponseModel
                        {
                            ResponseBody = responseBody,
                            ResponseStatus = context.Response.StatusCode,
                            FinishTime = DateTime.Now,
                            //Headers = context.Response.Headers.ContentLength > 0 ? context.Response.Headers.Select(x => x.ToString()).Aggregate((a, b) => a + ": " + b) : string.Empty,
                            Headers = context.Response.Headers.ContentLength > 0 ? System.Text.Json.JsonSerializer.Serialize(context.Response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)), new JsonSerializerOptions { WriteIndented = true }) : string.Empty
                        };
                        await originalResponseBody.CopyToAsync(originalBodyStream);
                        return responseBodyDto;
                    }
                }
                catch (OutOfMemoryException ex)
                {
                    return new ResponseModel
                    {
                        ResponseBody = "尝试读取响应体时发生OutOfMemoryException",
                        ResponseStatus = context.Response.StatusCode,
                        FinishTime = DateTime.Now,
                        Headers = context.Response.Headers.ContentLength > 0 ? context.Response.Headers.Select(x => x.ToString()).Aggregate((a, b) => a + ": " + b) : string.Empty,
                    };
                }
                finally
                {
                    context.Response.Body = originalBodyStream;
                }
            }
        }

        private bool ShouldBlacklist(string requestPath)
        {
            if (_options.UseRegexForBlacklisting)
            {
                for (int i = 0; i < InuLogsConfigModel.Blacklist.Length; i++)
                {
                    if (Regex.IsMatch(requestPath, InuLogsConfigModel.Blacklist[i], RegexOptions.IgnoreCase))
                        return true;
                }
                return false;
            }
            return InuLogsConfigModel.Blacklist.Contains(requestPath, StringComparer.OrdinalIgnoreCase);
        }

        public static string FormatTimeSpan(TimeSpan timeSpan)
        {
            var parts = new List<string>();

            if (timeSpan.Hours > 0)
            {
                parts.Add($"{timeSpan.Hours} hrs");
            }

            if (timeSpan.Minutes > 0)
            {
                parts.Add($"{timeSpan.Minutes} mins");
            }

            if (timeSpan.Seconds > 0)
            {
                parts.Add($"{timeSpan.Seconds} secs");
            }

            if (timeSpan.Milliseconds > 0)
            {
                parts.Add($"{timeSpan.Milliseconds} ms");
            }

            return string.Join(" ", parts);
        }
    }
}

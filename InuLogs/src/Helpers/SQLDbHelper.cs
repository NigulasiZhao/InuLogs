using Dapper;
using Dapper.Oracle;
using InuLogs.src.Data;
using InuLogs.src.Models;
using InuLogs.src.Utilities;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MongoDB.Bson.IO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace InuLogs.src.Helpers
{
    internal static class SQLDbHelper
    {
        public static async Task<Page<InuLog>> GetAllInuLogs(string searchString, string verbString, string statusCode, int pageNumber, int resultCode)
        {
            searchString = searchString?.ToLower();
            verbString = verbString?.ToLower();

            var query = @$"SELECT * FROM {Constants.InuLogTableName} ";

            if (!string.IsNullOrEmpty(searchString) || !string.IsNullOrEmpty(verbString) || !string.IsNullOrEmpty(statusCode) || resultCode == 0 || resultCode == 1)
                query += "WHERE 1=1 ";

            if (!string.IsNullOrEmpty(searchString))
            {
                if (GeneralHelper.IsPostgres())
                    query += $" AND (LOWER( {nameof(InuLog.Path)} ) LIKE '%{searchString}%' OR LOWER( {nameof(InuLog.Method)} ) LIKE '%{searchString}%' OR {nameof(InuLog.ResponseStatus)}::text LIKE '%{searchString}%' OR LOWER( {nameof(InuLog.RequestAndResponseInfo)} ) LIKE '%{searchString}%') ";
                else
                    query += $" AND (LOWER( {nameof(InuLog.Path)} ) LIKE '%{searchString}%' OR LOWER( {nameof(InuLog.Method)} ) LIKE '%{searchString}%' OR {nameof(InuLog.ResponseStatus)} LIKE '%{searchString}%' OR LOWER( {nameof(InuLog.RequestAndResponseInfo)} ) LIKE '%{searchString}%') ";
            }

            if (!string.IsNullOrEmpty(verbString))
            {
                query += $" AND LOWER( {nameof(InuLog.Method)} ) LIKE '%{verbString}%' ";
            }

            if (!string.IsNullOrEmpty(statusCode))
            {
                query += $" AND {nameof(InuLog.ResponseStatus)} = {statusCode} ";
            }
            if (resultCode == 0 || resultCode == 1)
            {
                query += $" AND {nameof(InuLog.ResultException)} = {resultCode}";
            }
            query += $" ORDER BY {nameof(InuLog.Id)} DESC";
            using (var connection = ExternalDbContext.CreateSQLConnection())
            {
                connection.Open();
                var logs = await connection.QueryAsync<InuLog>(query);
                foreach (var log in logs)
                {
                    RequestAndResponseInfoModel Info = Newtonsoft.Json.JsonConvert.DeserializeObject<RequestAndResponseInfoModel>(log.RequestAndResponseInfo);
                    log.QueryString = Info.QueryString;
                    log.ResponseHeaders = Info.ResponseHeaders;
                    log.ResponseBody = Info.ResponseBody;
                    log.RequestBody = Info.RequestBody;
                    log.RequestHeaders = Info.RequestHeaders;
                }
                connection.Close();
                return logs.ToPaginatedList(pageNumber);
            }
        }

        public static async Task InsertInuLog(InuLog log)
        {
            if (GeneralHelper.IsOracle())
            {
                var query = @$"INSERT INTO {Constants.InuLogTableName} (responseStatus,path,method,host,ipAddress,timeSpent,startTime,endTime,resultexception,scheme,requestandresponseinfo) " +
                "VALUES (:ResponseStatus,:Path,:Method,:Host,:IpAddress,:TimeSpent,:StartTime,:EndTime,:ResultException,:Scheme,:RequestAndResponseInfo)";
                var parameters = new OracleDynamicParameters();
                if (GeneralHelper.GetExceptionMessageKeyWords().Any(keyword => log.ResponseBody.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                {
                    parameters.Add("ResultException", 1, OracleMappingType.Int32, ParameterDirection.Input);
                }
                else
                {
                    parameters.Add("ResultException", 0, OracleMappingType.Int32, ParameterDirection.Input);
                }
                parameters.Add("ResponseStatus", log.ResponseStatus, OracleMappingType.Int32, ParameterDirection.Input);
                parameters.Add("Path", log.Path, OracleMappingType.Varchar2, ParameterDirection.Input);
                parameters.Add("Method", log.Method, OracleMappingType.Varchar2, ParameterDirection.Input);
                parameters.Add("Host", log.Host, OracleMappingType.Varchar2, ParameterDirection.Input);
                parameters.Add("IpAddress", log.IpAddress, OracleMappingType.Varchar2, ParameterDirection.Input);
                parameters.Add("TimeSpent", log.TimeSpent, OracleMappingType.Varchar2, ParameterDirection.Input);
                parameters.Add("StartTime", log.StartTime, OracleMappingType.Date, ParameterDirection.Input);
                parameters.Add("EndTime", log.EndTime, OracleMappingType.Date, ParameterDirection.Input);
                parameters.Add("Scheme", log.Scheme, OracleMappingType.Varchar2, ParameterDirection.Input);
                parameters.Add("RequestAndResponseInfo", log.RequestAndResponseInfo, OracleMappingType.Clob, ParameterDirection.Input);

                using (var connection = ExternalDbContext.CreateSQLConnection())
                {
                    connection.Open();
                    connection.Execute(query, parameters);
                    connection.Close();
                }
            }
            else
            {
                var query = @$"INSERT INTO {Constants.InuLogTableName} (responseStatus,path,method,host,ipAddress,timeSpent,startTime,endTime,resultexception,scheme,requestandresponseinfo) " +
                "VALUES (@ResponseStatus,@Path,@Method,@Host,@IpAddress,@TimeSpent,@StartTime,@EndTime,@ResultException,@Scheme,@RequestAndResponseInfo);";
                var parameters = new DynamicParameters();
                if (GeneralHelper.GetExceptionMessageKeyWords().Any(keyword => log.ResponseBody.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                {
                    parameters.Add("ResultException", 1, DbType.Int32);
                }
                else
                {
                    parameters.Add("ResultException", 0, DbType.Int32);
                }
                parameters.Add("ResponseStatus", log.ResponseStatus, DbType.Int32);
                parameters.Add("Path", log.Path, DbType.String);
                parameters.Add("Method", log.Method, DbType.String);
                parameters.Add("Host", log.Host, DbType.String);
                parameters.Add("IpAddress", log.IpAddress, DbType.String);
                parameters.Add("TimeSpent", log.TimeSpent, DbType.String);
                parameters.Add("Scheme", log.Scheme, DbType.String);
                parameters.Add("RequestAndResponseInfo", log.RequestAndResponseInfo, DbType.String);

                if (GeneralHelper.IsPostgres())
                {
                    parameters.Add("StartTime", log.StartTime, DbType.DateTime);
                    parameters.Add("EndTime", log.EndTime, DbType.DateTime);
                }
                else
                {
                    parameters.Add("StartTime", log.StartTime);
                    parameters.Add("EndTime", log.EndTime);
                }
                using (var connection = ExternalDbContext.CreateSQLConnection())
                {
                    connection.Open();
                    await connection.ExecuteAsync(query, parameters);
                    connection.Close();
                }
            }
        }


        public static async Task<Page<InuExceptionLog>> GetAllInuExceptionLogs(string searchString, int pageNumber)
        {
            var query = @$"SELECT * FROM {Constants.InuLogExceptionTableName} ";
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.ToLower();
                query += $"WHERE LOWER ( {nameof(InuExceptionLog.Source)} ) LIKE '%{searchString}%' OR LOWER ( {nameof(InuExceptionLog.Message)} ) LIKE '%{searchString}%' OR LOWER( {nameof(InuExceptionLog.StackTrace)} ) LIKE '%{searchString}%' ";
            }
            query += $"ORDER BY {nameof(InuExceptionLog.Id)} DESC";
            using (var connection = ExternalDbContext.CreateSQLConnection())
            {
                var logs = await connection.QueryAsync<InuExceptionLog>(query);
                return logs.ToPaginatedList(pageNumber);
            }
        }

        public static async Task InsertInuExceptionLog(InuExceptionLog log)
        {


            if (GeneralHelper.IsOracle())
            {
                var query = @$"INSERT INTO {Constants.InuLogExceptionTableName} (message,stackTrace,typeOf,source,path,method,queryString,requestBody,encounteredAt) " +
                "VALUES (:Message,:StackTrace,:TypeOf,:Source,:Path,:Method,:QueryString,:RequestBody,:EncounteredAt)";
                var parameters = new OracleDynamicParameters();
                parameters.Add("Message", log.Message, OracleMappingType.Clob, ParameterDirection.Input);
                parameters.Add("StackTrace", log.StackTrace, OracleMappingType.Clob, ParameterDirection.Input);
                parameters.Add("TypeOf", log.TypeOf, OracleMappingType.Clob, ParameterDirection.Input);
                parameters.Add("Source", log.Source, OracleMappingType.Clob, ParameterDirection.Input);
                parameters.Add("Path", log.Path, OracleMappingType.Clob, ParameterDirection.Input);
                parameters.Add("Method", log.Method, OracleMappingType.Varchar2, ParameterDirection.Input);
                parameters.Add("QueryString", log.QueryString, OracleMappingType.Clob, ParameterDirection.Input);
                parameters.Add("RequestBody", log.RequestBody, OracleMappingType.Clob, ParameterDirection.Input);
                parameters.Add("EncounteredAt", log.EncounteredAt, OracleMappingType.Date, ParameterDirection.Input);

                using (var connection = ExternalDbContext.CreateSQLConnection())
                {
                    await connection.ExecuteAsync(query, parameters);
                }
            }
            else
            {
                var query = @$"INSERT INTO {Constants.InuLogExceptionTableName} (message,stackTrace,typeOf,source,path,method,queryString,requestBody,encounteredAt) " +
                "VALUES (@Message,@StackTrace,@TypeOf,@Source,@Path,@Method,@QueryString,@RequestBody,@EncounteredAt);";
                var parameters = new DynamicParameters();
                parameters.Add("Message", log.Message, DbType.String);
                parameters.Add("StackTrace", log.StackTrace, DbType.String);
                parameters.Add("TypeOf", log.TypeOf, DbType.String);
                parameters.Add("Source", log.Source, DbType.String);
                parameters.Add("Path", log.Path, DbType.String);
                parameters.Add("Method", log.Method, DbType.String);
                parameters.Add("QueryString", log.QueryString, DbType.String);
                parameters.Add("RequestBody", log.RequestBody, DbType.String);
                if (GeneralHelper.IsPostgres())
                {
                    parameters.Add("EncounteredAt", log.EncounteredAt, DbType.DateTime);
                }
                else
                {
                    parameters.Add("EncounteredAt", log.EncounteredAt, DbType.DateTime);
                }
                using (var connection = ExternalDbContext.CreateSQLConnection())
                {
                    await connection.ExecuteAsync(query, parameters);
                }
            }
        }

        // LOGS OPERATION
        public static async Task<Page<InuLoggerModel>> GetAllLogs(string searchString, string logLevelString, int pageNumber)
        {
            var query = @$"SELECT * FROM {Constants.LogsTableName} ";

            if (!string.IsNullOrEmpty(searchString) || !string.IsNullOrEmpty(logLevelString))
                query += "WHERE ";

            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.ToLower();
                query += $"LOWER( {nameof(InuLoggerModel.CallingFrom)} ) LIKE '%{searchString}%' OR LOWER( {nameof(InuLoggerModel.CallingMethod)} ) LIKE '%{searchString}%' OR LOWER ( {nameof(InuLoggerModel.Message)} ) LIKE '%{searchString}%' OR {nameof(InuLoggerModel.EventId)} LIKE '%{searchString}%' " + (string.IsNullOrEmpty(logLevelString) ? "" : "AND ");
            }

            if (!string.IsNullOrEmpty(logLevelString))
            {
                logLevelString = logLevelString?.ToLower();
                query += $"LOWER( {nameof(InuLoggerModel.LogLevel)} ) LIKE '%{logLevelString}%' ";
            }
            query += $"ORDER BY {nameof(InuLoggerModel.Id)} DESC";

            using (var connection = ExternalDbContext.CreateSQLConnection())
            {
                connection.Open();
                var logs = await connection.QueryAsync<InuLoggerModel>(query);
                connection.Close();
                return logs.ToPaginatedList(pageNumber);
            }
        }

        public static async Task InsertLog(InuLoggerModel log)
        {
            if (GeneralHelper.IsOracle())
            {
                var query = @$"INSERT INTO {Constants.LogsTableName} (message,eventId,timestamp,callingFrom,callingMethod,lineNumber,logLevel) " +
                 "VALUES (:Message,:EventId,:Timestamp,:CallingFrom,:CallingMethod,:LineNumber,:LogLevel)";

                var parameters = new OracleDynamicParameters();
                parameters.Add("Message", log.Message, OracleMappingType.Clob, ParameterDirection.Input);
                parameters.Add("CallingFrom", log.CallingFrom, OracleMappingType.Clob, ParameterDirection.Input);
                parameters.Add("CallingMethod", log.CallingMethod, OracleMappingType.Varchar2, ParameterDirection.Input);
                parameters.Add("LineNumber", log.LineNumber, OracleMappingType.Int32, ParameterDirection.Input);
                parameters.Add("LogLevel", log.LogLevel, OracleMappingType.Varchar2, ParameterDirection.Input);
                parameters.Add("EventId", log.EventId, OracleMappingType.Varchar2, ParameterDirection.Input);
                parameters.Add("Timestamp", log.Timestamp, OracleMappingType.Date, ParameterDirection.Input);
                using (var connection = ExternalDbContext.CreateSQLConnection())
                {
                    await connection.ExecuteAsync(query, parameters);
                }
            }
            else
            {
                var query = @$"INSERT INTO {Constants.LogsTableName} (message,eventId,timestamp,callingFrom,callingMethod,lineNumber,logLevel) " +
                 "VALUES (@Message,@EventId,@Timestamp,@CallingFrom,@CallingMethod,@LineNumber,@LogLevel);";

                var parameters = new DynamicParameters();
                parameters.Add("Message", log.Message, DbType.String);
                parameters.Add("CallingFrom", log.CallingFrom, DbType.String);
                parameters.Add("CallingMethod", log.CallingMethod, DbType.String);
                parameters.Add("LineNumber", log.LineNumber, DbType.Int32);
                parameters.Add("LogLevel", log.LogLevel, DbType.String);
                parameters.Add("EventId", log.EventId, DbType.String);

                if (GeneralHelper.IsPostgres())
                {
                    parameters.Add("Timestamp", log.Timestamp, DbType.DateTime);
                }
                else
                {
                    parameters.Add("Timestamp", log.Timestamp, DbType.DateTime);
                }

                using (var connection = ExternalDbContext.CreateSQLConnection())
                {
                    await connection.ExecuteAsync(query, parameters);
                }
            }
        }




        public static async Task<bool> ClearLogs()
        {
            var inulogQuery = @$"truncate table {Constants.InuLogTableName}";
            var exQuery = @$"truncate table {Constants.InuLogExceptionTableName}";
            var logQuery = @$"truncate table {Constants.LogsTableName}";
            using (var connection = ExternalDbContext.CreateSQLConnection())
            {
                var inulogs = await connection.ExecuteAsync(inulogQuery);
                var exLogs = await connection.ExecuteAsync(exQuery);
                var logs = await connection.ExecuteAsync(logQuery);
                return inulogs > 1 && exLogs > 1 && logs > 1;
            }
        }
    }
}

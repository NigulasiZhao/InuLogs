using InuLogs.src.Data;
using InuLogs.src.Models;
using InuLogs.src.Utilities;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace InuLogs.src.Helpers
{
    internal class MongoDBHelper
    {
        public static MongoClient mongoClient = ExternalDbContext.CreateMongoDBConnection();
        static IMongoDatabase database = mongoClient.GetDatabase(InuLogsExternalDbConfig.MongoDbName);
        static IMongoCollection<InuLog> _inuLogs = database.GetCollection<InuLog>(Constants.InuLogTableName);
        static IMongoCollection<InuExceptionLog> _inuExLogs = database.GetCollection<InuExceptionLog>(Constants.InuLogExceptionTableName);
        static IMongoCollection<InuLoggerModel> _logs = database.GetCollection<InuLoggerModel>(Constants.LogsTableName);
        static IMongoCollection<Sequence> _counter = database.GetCollection<Sequence>(Constants.InuLogsMongoCounterTableName);


        public static Page<InuLog> GetAllInuLogs(string searchString, string verbString, string statusCode, int pageNumber)
        {
            searchString = searchString?.ToLower();
            var builder = Builders<InuLog>.Filter;
            var filter = builder.Empty;

            if (!string.IsNullOrEmpty(statusCode))
                filter &= builder.Eq(x => x.ResponseStatus, int.Parse(statusCode));

            if (!string.IsNullOrEmpty(verbString))
                filter &= builder.Eq(x => x.Method, verbString);

            if (!string.IsNullOrEmpty(searchString))
                filter &= builder.Where(l => l.Path.ToLower().Contains(searchString) || l.Method.ToLower().Contains(searchString) || (!string.IsNullOrEmpty(l.QueryString) && l.QueryString.ToLower().Contains(searchString)));

            var result = _inuLogs.Find(filter).SortByDescending(x => x.Id).ToPaginatedList(pageNumber);
            return result;
        }

        public static async Task InsertInuLog(InuLog log)
        {
            log.Id = GetSequenceId();
            await _inuLogs.InsertOneAsync(log);
        }

        public static async Task<bool> ClearInuLog()
        {
            var deleteResult = await _inuLogs.DeleteManyAsync(Builders<InuLog>.Filter.Empty);
            return deleteResult.IsAcknowledged;
        }

        public static Page<InuExceptionLog> GetAllInuExceptionLogs(string searchString, int pageNumber)
        {
            searchString = searchString?.ToLower();
            var builder = Builders<InuExceptionLog>.Filter;
            var filter = builder.Empty;

            if (!string.IsNullOrEmpty(searchString))
                filter &= builder.Where(l => l.Message.ToLower().Contains(searchString) || l.StackTrace.ToLower().Contains(searchString) || l.Source.ToLower().Contains(searchString));

            var result = _inuExLogs.Find(filter).SortByDescending(x => x.Id).ToPaginatedList(pageNumber);
            return result;
        }

        public static async Task InsertInuExceptionLog(InuExceptionLog log)
        {
            log.Id = GetSequenceId();
            await _inuExLogs.InsertOneAsync(log);
        }
        public static async Task<bool> ClearInuExceptionLog()
        {
            var deleteResult = await _inuExLogs.DeleteManyAsync(Builders<InuExceptionLog>.Filter.Empty);
            return deleteResult.IsAcknowledged;
        }


        //LOGS OPERATION
        public static async Task InsertLog(InuLoggerModel log)
        {
            log.Id = GetSequenceId();
            await _logs.InsertOneAsync(log);
        }
        public static async Task<bool> ClearLogs()
        {
            var deleteResult = await _logs.DeleteManyAsync(Builders<InuLoggerModel>.Filter.Empty);
            return deleteResult.IsAcknowledged;
        }
        public static Page<InuLoggerModel> GetAllLogs(string searchString, string logLevelString, int pageNumber)
        {
            searchString = searchString?.ToLower();
            var builder = Builders<InuLoggerModel>.Filter;
            var filter = builder.Empty;

            if (!string.IsNullOrEmpty(searchString))
                filter &= builder.Where(l => l.Message.ToLower().Contains(searchString) || l.CallingMethod.ToLower().Contains(searchString) || l.CallingFrom.ToLower().Contains(searchString) || (!string.IsNullOrEmpty(l.EventId) && l.EventId.ToLower().Contains(searchString)));

            if (!string.IsNullOrEmpty(logLevelString))
            {
                filter &= builder.Eq(l => l.LogLevel, logLevelString);
            }

            var result = _logs.Find(filter).SortByDescending(x => x.Id).ToPaginatedList(pageNumber);
            return result;
        }


        public static int GetSequenceId()
        {
            var filter = Builders<Sequence>.Filter.Eq(a => a._Id, "sequenceId");
            var update = Builders<Sequence>.Update.Inc(a => a.Value, 1);
            var sequence = _counter.FindOneAndUpdate(filter, update);

            return sequence.Value;
        }


        public static async Task<bool> ClearAllLogs()
        {
            var inuLogs = await ClearInuLog();
            var exLogs = await ClearInuExceptionLog();
            var logs = await ClearLogs();

            return inuLogs && exLogs && logs;
        }
    }
}

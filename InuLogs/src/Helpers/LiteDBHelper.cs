using InuLogs.src.Models;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace InuLogs.src.Helpers
{
    internal static class LiteDBHelper
    {
        public static LiteDatabase db = new LiteDatabase("inulogs.db");
        static ILiteCollection<InuLog> _inuLogs = db.GetCollection<InuLog>("InuLogs");
        static ILiteCollection<InuExceptionLog> _inuExLogs = db.GetCollection<InuExceptionLog>("InuExceptionLogs");
        static ILiteCollection<InuLoggerModel> _logs = db.GetCollection<InuLoggerModel>("Logs");


        public static Page<InuLog> GetAllInuLogs(string searchString, string verbString, string statusCode, int pageNumber)
        {
            var query = _inuLogs.Query();
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.ToLower();
                query.Where(l => l.Path.ToLower().Contains(searchString) || l.Method.ToLower().Contains(searchString) || l.ResponseStatus.ToString().Contains(searchString) || (!string.IsNullOrEmpty(l.QueryString) && l.QueryString.ToLower().Contains(searchString)));
            }

            if (!string.IsNullOrEmpty(verbString))
            {
                query.Where(l => l.Method.ToLower() == verbString.ToLower());
            }

            if (!string.IsNullOrEmpty(statusCode))
            {
                query.Where(l => l.ResponseStatus.ToString() == statusCode);
            }
            return query.OrderByDescending(x => x.Id).ToPaginatedList(pageNumber);
        }
        public static int InsertInuLog(InuLog log)
        {
            return _inuLogs.Insert(log);
        }

        public static int ClearInuLog()
        {
            return _inuLogs.DeleteAll();
        }


        public static Page<InuExceptionLog> GetAllInuExceptionLogs(string searchString, int pageNumber)
        {
            var query = _inuExLogs.Query();
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.ToLower();
                query.Where(l => l.Message.ToLower().Contains(searchString) || l.StackTrace.ToLower().Contains(searchString) || l.Source.ToLower().Contains(searchString));
            }
            return query.OrderByDescending(x => x.Id).ToPaginatedList(pageNumber);
        }

        public static int InsertInuExceptionLog(InuExceptionLog log)
        {
            return _inuExLogs.Insert(log);
        }
        public static int ClearInuExceptionLog()
        {
            return _inuExLogs.DeleteAll();
        }

        //LOGS OPERATION
        public static int InsertLog(InuLoggerModel log)
        {
            return _logs.Insert(log);
        }
        public static int ClearLogs()
        {
            return _logs.DeleteAll();
        }
        public static Page<InuLoggerModel> GetAllLogs(string searchString, string logLevelString, int pageNumber)
        {
            var query = _logs.Query();
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.ToLower();
                query.Where(l => l.Message.ToLower().Contains(searchString) || l.CallingMethod.ToLower().Contains(searchString) || l.CallingFrom.ToLower().Contains(searchString) || (!string.IsNullOrEmpty(l.EventId) && l.EventId.ToLower().Contains(searchString)));
            }
            if (!string.IsNullOrEmpty(logLevelString))
            {
                query.Where(l => l.LogLevel.ToLower() == logLevelString.ToLower());
            }
            return query.OrderByDescending(x => x.Id).ToPaginatedList(pageNumber);
        }

        // CLEAR ALL LOGS
        public static bool ClearAllLogs()
        {
            var inuLogs = ClearInuLog();
            var exLogs = ClearInuExceptionLog();
            var logs = ClearLogs();

            db.Rebuild();

            return inuLogs > 1 && exLogs > 1 && logs > 1;
        }
    }
}

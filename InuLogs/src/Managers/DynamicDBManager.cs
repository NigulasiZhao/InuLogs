using InuLogs.src.Helpers;
using InuLogs.src.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace InuLogs.src.Managers
{
    internal static class DynamicDBManager
    {
        internal enum TargetDbEnum
        {
            SqlDb = 0,
            LiteDb,
            MongoDb
        }
        private static string _connectionString = InuLogsExternalDbConfig.ConnectionString;


        private static TargetDbEnum GetTargetDbEnum
        {
            get
            {
                if (string.IsNullOrEmpty(_connectionString))
                {
                    return TargetDbEnum.LiteDb;
                }
                if (InuLogsDatabaseDriverOption.DatabaseDriverOption == Enums.InuLogsDbDriverEnum.Mongo)
                {
                    return TargetDbEnum.MongoDb;
                }
                return TargetDbEnum.SqlDb;
            }
        }

        public static async Task<bool> ClearLogs() =>
            GetTargetDbEnum switch
            {
                TargetDbEnum.SqlDb => await SQLDbHelper.ClearLogs(),
                TargetDbEnum.LiteDb => LiteDBHelper.ClearAllLogs(),
                TargetDbEnum.MongoDb => await MongoDBHelper.ClearAllLogs(),
                _ => throw new NotImplementedException()
            };

        public static async Task<Page<InuLog>> GetAllInuLogs(string searchString, string verbString, string statusCode, int pageNumber, int resultCode) =>
            GetTargetDbEnum switch
            {
                TargetDbEnum.SqlDb => await SQLDbHelper.GetAllInuLogs(searchString, verbString, statusCode, pageNumber, resultCode),
                TargetDbEnum.LiteDb => LiteDBHelper.GetAllInuLogs(searchString, verbString, statusCode, pageNumber),
                TargetDbEnum.MongoDb => MongoDBHelper.GetAllInuLogs(searchString, verbString, statusCode, pageNumber),
                _ => throw new NotImplementedException()
            };

        public static async Task InsertInuLog(InuLog log)
        {
            switch (GetTargetDbEnum)
            {
                case TargetDbEnum.SqlDb:
                    await SQLDbHelper.InsertInuLog(log);
                    break;
                case TargetDbEnum.LiteDb:
                    LiteDBHelper.InsertInuLog(log);
                    break;
                case TargetDbEnum.MongoDb:
                    await MongoDBHelper.InsertInuLog(log);
                    break;
            }
        }

        public static async Task<Page<InuExceptionLog>> GetAllInuExceptionLogs(string searchString, int pageNumber) =>
            GetTargetDbEnum switch
            {
                TargetDbEnum.SqlDb => await SQLDbHelper.GetAllInuExceptionLogs(searchString, pageNumber),
                TargetDbEnum.LiteDb => LiteDBHelper.GetAllInuExceptionLogs(searchString, pageNumber),
                TargetDbEnum.MongoDb => MongoDBHelper.GetAllInuExceptionLogs(searchString, pageNumber),
                _ => throw new NotImplementedException()
            };

        public static async Task InsertInuExceptionLog(InuExceptionLog log)
        {
            switch (GetTargetDbEnum)
            {
                case TargetDbEnum.SqlDb:
                    await SQLDbHelper.InsertInuExceptionLog(log);
                    break;
                case TargetDbEnum.LiteDb:
                    LiteDBHelper.InsertInuExceptionLog(log);
                    break;
                case TargetDbEnum.MongoDb:
                    await MongoDBHelper.InsertInuExceptionLog(log);
                    break;
            }
        }

        // LOG OPERATIONS
        public static async Task<Page<InuLoggerModel>> GetAllLogs(string searchString, string logLevelString, int pageNumber) =>
            GetTargetDbEnum switch
            {
                TargetDbEnum.SqlDb => await SQLDbHelper.GetAllLogs(searchString, logLevelString, pageNumber),
                TargetDbEnum.LiteDb => LiteDBHelper.GetAllLogs(searchString, logLevelString, pageNumber),
                TargetDbEnum.MongoDb => MongoDBHelper.GetAllLogs(searchString, logLevelString, pageNumber),
                _ => throw new NotImplementedException()
            };

        public static async Task InsertLog(InuLoggerModel log)
        {
            switch (GetTargetDbEnum)
            {
                case TargetDbEnum.SqlDb:
                    await SQLDbHelper.InsertLog(log);
                    break;
                case TargetDbEnum.LiteDb:
                    LiteDBHelper.InsertLog(log);
                    break;
                case TargetDbEnum.MongoDb:
                    await MongoDBHelper.InsertLog(log);
                    break;
            }
        }
    }
}

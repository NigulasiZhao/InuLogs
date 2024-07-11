using InuLogs.src.Models;
using MongoDB.Driver;
using MySql.Data.MySqlClient;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.Text;
using InuLogs.src.Enums;
using InuLogs.src.Exceptions;
using Dapper;
using InuLogs.src.Utilities;
using MongoDB.Bson;
using Oracle.ManagedDataAccess.Client;

namespace InuLogs.src.Data
{
    internal static class ExternalDbContext
    {
        private static string _connectionString = InuLogsExternalDbConfig.ConnectionString;

        public static IDbConnection CreateSQLConnection()
            => InuLogsDatabaseDriverOption.DatabaseDriverOption switch
            {
                InuLogsDbDriverEnum.MSSQL => CreateMSSQLConnection(),
                InuLogsDbDriverEnum.MySql => CreateMySQLConnection(),
                InuLogsDbDriverEnum.PostgreSql => CreatePostgresConnection(),
                InuLogsDbDriverEnum.Oracle => CreateOracleConnection(),
                _ => throw new NotSupportedException()
            };

        public static void Migrate() => BootstrapTables();

        public static void BootstrapTables()
        {
            var createInuTablesQuery = GetSqlQueryString();

            using (var connection = CreateSQLConnection())
            {
                try
                {
                    connection.Open();
                    _ = connection.Query(createInuTablesQuery);
                    connection.Close();
                }
                catch (SqlException ae)
                {
                    Debug.WriteLine(ae.Message.ToString());
                    throw ae;
                }
                catch (Exception ex)
                {
                    throw new InuLogsDatabaseException(ex.Message);
                }
            }

        }

        public static void MigrateNoSql()
        {
            try
            {
                var mongoClient = CreateMongoDBConnection();
                var database = mongoClient.GetDatabase(InuLogsExternalDbConfig.MongoDbName);
                _ = database.GetCollection<InuLog>(Constants.InuLogTableName);
                _ = database.GetCollection<InuExceptionLog>(Constants.InuLogExceptionTableName);
                _ = database.GetCollection<InuLoggerModel>(Constants.LogsTableName);

                //Seed counterDb
                var filter = new BsonDocument("name", Constants.InuLogsMongoCounterTableName);

                // Check if the collection exists
                var collections = database.ListCollections(new ListCollectionsOptions { Filter = filter });

                bool exists = collections.Any();
                var _counter = database.GetCollection<Sequence>(Constants.InuLogsMongoCounterTableName);

                if (!exists)
                {
                    var sequence = new Sequence
                    {
                        _Id = "sequenceId",
                        Value = 0
                    };
                    _counter.InsertOne(sequence);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message.ToString());
                throw new InuLogsDatabaseException(ex.Message);
            }
        }

        public static string GetSqlQueryString() =>
            InuLogsDatabaseDriverOption.DatabaseDriverOption switch
            {
                InuLogsDbDriverEnum.MSSQL => @$"
                                  IF OBJECT_ID('dbo.{Constants.InuLogTableName}', 'U') IS NULL CREATE TABLE {Constants.InuLogTableName} (
                                  id              INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                  responseBody    VARCHAR(max),
                                  responseStatus  int NOT NULL,
                                  requestBody     VARCHAR(max),
                                  queryString     VARCHAR(max),
                                  path            VARCHAR(max),
                                  requestHeaders  VARCHAR(max),
                                  responseHeaders VARCHAR(max),
                                  method          VARCHAR(30),
                                  host            VARCHAR(max),
                                  ipAddress       VARCHAR(30),
                                  timeSpent       VARCHAR(100),
                                  startTime       VARCHAR(100) NOT NULL,
                                  endTime         VARCHAR(100) NOT NULL,
                                  resultException  int,
                                  scheme       VARCHAR(100)
                            );
                                IF OBJECT_ID('dbo.{Constants.InuLogExceptionTableName}', 'U') IS NULL CREATE TABLE {Constants.InuLogExceptionTableName} (
                                id            INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                message       VARCHAR(max),
                                stackTrace    VARCHAR(max),
                                typeOf        VARCHAR(max),
                                source        VARCHAR(max),
                                path          VARCHAR(max),
                                method        VARCHAR(30),
                                queryString   VARCHAR(max),
                                requestBody   VARCHAR(max),
                                encounteredAt VARCHAR(100) NOT NULL
                             );
                                IF OBJECT_ID('dbo.{Constants.LogsTableName}', 'U') IS NULL CREATE TABLE {Constants.LogsTableName} (
                                id            INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                eventId       VARCHAR(100),
                                message       VARCHAR(max),
                                timestamp     VARCHAR(100) NOT NULL,
                                callingFrom   VARCHAR(100),
                                callingMethod VARCHAR(100),
                                lineNumber    INT,
                                logLevel      VARCHAR(30)
                             );
                        ",

                InuLogsDbDriverEnum.MySql => @$"
                             CREATE TABLE IF NOT EXISTS {Constants.InuLogTableName} (
                              id              INT NOT NULL PRIMARY KEY AUTO_INCREMENT,
                              responseBody    TEXT(65535),
                              responseStatus  INT NOT NULL,
                              requestBody     TEXT(65535),
                              queryString     VARCHAR(65535),
                              path            VARCHAR(65535),
                              requestHeaders  TEXT(65535),
                              responseHeaders VARCHAR(65535),
                              method          VARCHAR(30),
                              host            VARCHAR(65535),
                              ipAddress       VARCHAR(30),
                              timeSpent       VARCHAR(100),
                              startTime       VARCHAR(100) NOT NULL,
                              endTime         VARCHAR(100) NOT NULL,
                              resultException  int,
                              scheme       VARCHAR(100)
                            );
                           CREATE TABLE IF NOT EXISTS {Constants.InuLogExceptionTableName} (
                                id            INT NOT NULL PRIMARY KEY AUTO_INCREMENT,
                                message       TEXT(65535),
                                stackTrace    TEXT(65535),
                                typeOf        VARCHAR(65535),
                                source        TEXT(65535),
                                path          VARCHAR(65535),
                                method        VARCHAR(30),
                                queryString   VARCHAR(65535),
                                requestBody   TEXT(65535),
                                encounteredAt VARCHAR(100) NOT NULL
                             );
                           CREATE TABLE IF NOT EXISTS {Constants.LogsTableName} (
                                id            INT NOT NULL PRIMARY KEY AUTO_INCREMENT,
                                eventId       VARCHAR(100),
                                message       TEXT(65535),
                                timestamp     VARCHAR(100) NOT NULL,
                                callingFrom   VARCHAR(100),
                                callingMethod VARCHAR(100),
                                lineNumber    INT,
                                logLevel      VARCHAR(30)
                             );
                        ",

                InuLogsDbDriverEnum.PostgreSql => @$"
                             CREATE TABLE IF NOT EXISTS {Constants.InuLogTableName} (
                              id              SERIAL PRIMARY KEY,
                              responseBody    TEXT,
                              responseStatus  int NOT NULL,
                              requestBody     TEXT,
                              queryString     TEXT,
                              path            TEXT,
                              requestHeaders  TEXT,
                              responseHeaders TEXT,
                              method          VARCHAR(30),
                              host            VARCHAR,
                              ipAddress       VARCHAR(30),
                              timeSpent       VARCHAR,
                              startTime       TIMESTAMP with time zone NOT NULL,
                              endTime         TIMESTAMP with time zone NOT NULL,
                              resultException  int,
                              scheme       VARCHAR(100)
                            );
                           CREATE TABLE IF NOT EXISTS {Constants.InuLogExceptionTableName} (
                                id            SERIAL PRIMARY KEY,
                                message       TEXT,
                                stackTrace    TEXT,
                                typeOf        TEXT,
                                source        TEXT,
                                path          TEXT,
                                method        VARCHAR(30),
                                queryString   TEXT,
                                requestBody   TEXT,
                                encounteredAt TIMESTAMP with time zone NOT NULL
                             );
                           CREATE TABLE IF NOT EXISTS {Constants.LogsTableName} (
                                id            SERIAL PRIMARY KEY,
                                eventId       VARCHAR(100),
                                message       TEXT,
                                timestamp     TIMESTAMP with time zone NOT NULL,
                                callingFrom   TEXT,
                                callingMethod VARCHAR(100),
                                lineNumber    INTEGER,
                                logLevel      VARCHAR(30)
                             );
                        ",
                InuLogsDbDriverEnum.Oracle => @$"
BEGIN
                            BEGIN
                               EXECUTE IMMEDIATE 'CREATE TABLE {Constants.InuLogTableName} (
                                  ID              NUMBER PRIMARY KEY,
                                  RESPONSEBODY    CLOB,
                                  RESPONSESTATUS  NUMBER NOT NULL,
                                  REQUESTBODY     CLOB,
                                  QUERYSTRING     CLOB,
                                  PATH            CLOB,
                                  REQUESTHEADERS  CLOB,
                                  RESPONSEHEADERS CLOB,
                                  METHOD          VARCHAR2(30),
                                  HOST            CLOB,
                                  IPADDRESS       VARCHAR2(30),
                                  TIMESPENT       VARCHAR2(100),
                                  STARTTIME       DATE NOT NULL,
                                  ENDTIME         DATE NOT NULL,
                                  RESULTEXCEPTION NUMBER,
                                  SCHEME       VARCHAR2(100)
                               )';
                            EXCEPTION
                               WHEN OTHERS THEN
                                  IF SQLCODE != -955 THEN
                                     RAISE;
                                  END IF;
                            END;
                            
                            
                            BEGIN
                               EXECUTE IMMEDIATE 'CREATE TABLE {Constants.InuLogExceptionTableName} (
                                  ID            NUMBER PRIMARY KEY,
                                  MESSAGE       CLOB,
                                  STACKTRACE    CLOB,
                                  TYPEOF        CLOB,
                                  SOURCE        CLOB,
                                  PATH          CLOB,
                                  METHOD        VARCHAR2(30),
                                  QUERYSTRING   CLOB,
                                  REQUESTBODY   CLOB,
                                  ENCOUNTEREDAT DATE NOT NULL
                               )';
                            EXCEPTION
                               WHEN OTHERS THEN
                                  IF SQLCODE != -955 THEN
                                     RAISE;
                                  END IF;
                            END;
                            
                            
                            BEGIN
                               EXECUTE IMMEDIATE 'CREATE TABLE {Constants.LogsTableName} (
                                  ID            NUMBER PRIMARY KEY,
                                  EVENTID       VARCHAR2(100),
                                  MESSAGE       CLOB,
                                  TIMESTAMP     DATE NOT NULL,
                                  CALLINGFROM   CLOB,
                                  CALLINGMETHOD VARCHAR2(100),
                                  LINENUMBER    NUMBER,
                                  LOGLEVEL      VARCHAR2(30)
                               )';
                            EXCEPTION
                               WHEN OTHERS THEN
                                  IF SQLCODE != -955 THEN
                                     RAISE;
                                  END IF;
                            END;
                            
                            
                            -- 创建序列
                            BEGIN
                               EXECUTE IMMEDIATE 'CREATE SEQUENCE {Constants.InuLogTableName}Seq START WITH 1 INCREMENT BY 1';
                            EXCEPTION
                               WHEN OTHERS THEN
                                  IF SQLCODE != -955 THEN
                                     RAISE;
                                  END IF;
                            END;
                            
                            
                            BEGIN
                               EXECUTE IMMEDIATE 'CREATE SEQUENCE {Constants.InuLogExceptionTableName}Seq START WITH 1 INCREMENT BY 1';
                            EXCEPTION
                               WHEN OTHERS THEN
                                  IF SQLCODE != -955 THEN
                                     RAISE;
                                  END IF;
                            END;
                            
                            
                            BEGIN
                               EXECUTE IMMEDIATE 'CREATE SEQUENCE {Constants.LogsTableName}Seq START WITH 1 INCREMENT BY 1';
                            EXCEPTION
                               WHEN OTHERS THEN
                                  IF SQLCODE != -955 THEN
                                     RAISE;
                                  END IF;
                            END;
                            
                            
                            -- 创建触发器
                            BEGIN
                               EXECUTE IMMEDIATE '
                                  CREATE OR REPLACE TRIGGER trg_{Constants.InuLogTableName}_Id
                                  BEFORE INSERT ON {Constants.InuLogTableName}
                                  FOR EACH ROW
                                  BEGIN
                                     :new.id := {Constants.InuLogTableName}Seq.NEXTVAL;
                                  END;';
                            EXCEPTION
                               WHEN OTHERS THEN
                                  IF SQLCODE != -4080 THEN -- ORA-04080: trigger does not exist
                                     RAISE;
                                  END IF;
                            END;
                            
                            
                            BEGIN
                               EXECUTE IMMEDIATE '
                                  CREATE OR REPLACE TRIGGER trg_{Constants.InuLogExceptionTableName}_Id
                                  BEFORE INSERT ON {Constants.InuLogExceptionTableName}
                                  FOR EACH ROW
                                  BEGIN
                                     :new.id := {Constants.InuLogExceptionTableName}Seq.NEXTVAL;
                                  END;';
                            EXCEPTION
                               WHEN OTHERS THEN
                                  IF SQLCODE != -4080 THEN
                                     RAISE;
                                  END IF;
                            END;
                            
                            
                            BEGIN
                               EXECUTE IMMEDIATE '
                                  CREATE OR REPLACE TRIGGER trg_{Constants.LogsTableName}_Id
                                  BEFORE INSERT ON {Constants.LogsTableName}
                                  FOR EACH ROW
                                  BEGIN
                                     :new.id := {Constants.LogsTableName}Seq.NEXTVAL;
                                  END;';
                            EXCEPTION
                               WHEN OTHERS THEN
                                  IF SQLCODE != -4080 THEN
                                     RAISE;
                                  END IF;
                            END;
                            END;
                        ",
                _ => ""
            };

        public static NpgsqlConnection CreatePostgresConnection()
        {
            try
            {
                return new NpgsqlConnection(_connectionString);
            }
            catch (Exception ex)
            {
                throw new InuLogsDatabaseException(ex.Message);
            }
        }

        public static MySqlConnection CreateMySQLConnection()
        {
            try
            {
                return new MySqlConnection(_connectionString);
            }
            catch (Exception ex)
            {
                throw new InuLogsDatabaseException(ex.Message);
            }
        }

        public static SqlConnection CreateMSSQLConnection()
        {
            try
            {
                return new SqlConnection(_connectionString);
            }
            catch (Exception ex)
            {
                throw new InuLogsDatabaseException(ex.Message);
            }
        }
        public static OracleConnection CreateOracleConnection()
        {
            try
            {
                return new OracleConnection(_connectionString);
            }
            catch (Exception ex)
            {
                throw new InuLogsDatabaseException(ex.Message);
            }
        }
        public static MongoClient CreateMongoDBConnection()
        {
            try
            {
                return new MongoClient(_connectionString);
            }
            catch (Exception ex)
            {
                throw new InuLogsDatabaseException(ex.Message);
            }
        }
    }
}

using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using InuLogs.src.Models;
using InuLogs.src.Enums;

namespace InuLogs.src.Helpers
{
    internal static class GeneralHelper
    {
        public static string ReadStreamInChunks(Stream stream)
        {
            const int readChunkBufferLength = 4096;
            stream.Seek(0, SeekOrigin.Begin);
            using var textWriter = new StringWriter();
            using var reader = new StreamReader(stream);
            var readChunk = new char[readChunkBufferLength];
            int readChunkLength;
            do
            {
                readChunkLength = reader.ReadBlock(readChunk,
                                                   0,
                                                   readChunkBufferLength);
                textWriter.Write(readChunk, 0, readChunkLength);
            } while (readChunkLength > 0);
            return textWriter.ToString();
        }

        public static bool IsPostgres()
        {
            return !string.IsNullOrEmpty(InuLogsExternalDbConfig.ConnectionString) && InuLogsDatabaseDriverOption.DatabaseDriverOption == Enums.InuLogsDbDriverEnum.PostgreSql;
        }
        public static bool IsOracle()
        {
            return !string.IsNullOrEmpty(InuLogsExternalDbConfig.ConnectionString) && InuLogsDatabaseDriverOption.DatabaseDriverOption == Enums.InuLogsDbDriverEnum.Oracle;
        }
        public static List<string> GetExceptionMessageKeyWords()
        {
            return ExceptionMessageKeyWordsOption.ExceptionMessageKeyWords;
        }
        public static dynamic CamelCaseSerializer
            => InuLogs.Serializer switch
            {
                InuLogsSerializerEnum.Newtonsoft => new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                },
                _ => new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }
            };
        public static MemoryCacheEntryOptions cacheEntryOptions
        {
            get
            {
                return new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                    .SetPriority(CacheItemPriority.High);
            }
        }
    }
}

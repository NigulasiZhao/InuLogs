using InuLogs.src.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace InuLogs.src.Models
{
    public static class InuLogsConfigModel
    {
        public static string UserName { get; set; }
        public static string Password { get; set; }
        public static string[] Blacklist { get; set; }
    }
    public class InuLogsSettings
    {
        public bool IsAutoClear { get; set; }
        public InuLogsAutoClearScheduleEnum ClearTimeSchedule { get; set; } = InuLogsAutoClearScheduleEnum.Weekly;
        public string SetExternalDbConnString { get; set; } = string.Empty;
        public InuLogsDbDriverEnum DbDriverOption { get; set; }

        public List<string>ExceptionMessageKeyWords { get; set; } = new List<string>();
    }

    public static class InuLogsExternalDbConfig
    {
        public static string ConnectionString { get; set; } = string.Empty;
        public static string MongoDbName { get; set; } = "InuLogsDb";
    }
    public static class ExceptionMessageKeyWordsOption
    {
        public static List<string> ExceptionMessageKeyWords { get; set; }
    }
    public static class InuLogsDatabaseDriverOption
    {
        public static InuLogsDbDriverEnum DatabaseDriverOption { get; set; }
    }

    public static class AutoClearModel
    {
        public static bool IsAutoClear { get; set; }
        public static InuLogsAutoClearScheduleEnum ClearTimeSchedule { get; set; } = InuLogsAutoClearScheduleEnum.Weekly;
    }
}

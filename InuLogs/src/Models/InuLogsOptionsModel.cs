using InuLogs.src.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace InuLogs.src.Models
{
    public class InuLogsOptionsModel
    {
        public string InuPageUsername { get; set; }
        public string InuPagePassword { get; set; }
        public string Blacklist { get; set; }
        public string CorsPolicy { get; set; } = string.Empty;
        public bool UseOutputCache { get; set; }
        public bool UseRegexForBlacklisting { get; set; }
        public InuLogsSerializerEnum Serializer { get; set; } = InuLogsSerializerEnum.Default;
    }
}

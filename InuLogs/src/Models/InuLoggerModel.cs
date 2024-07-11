using System;
using System.Collections.Generic;
using System.Text;

namespace InuLogs.src.Models
{
    public class InuLoggerModel
    {
        public int Id { get; set; }
        public string EventId { get; set; } = string.Empty;
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public string CallingFrom { get; set; }
        public string CallingMethod { get; set; }
        public int LineNumber { get; set; }
        public string LogLevel { get; set; }

    }
}

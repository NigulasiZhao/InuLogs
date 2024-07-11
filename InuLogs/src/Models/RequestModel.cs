using System;
using System.Collections.Generic;
using System.Text;

namespace InuLogs.src.Models
{
    public class RequestModel
    {
        public string RequestBody { get; set; }
        public string QueryString { get; set; }
        public string Path { get; set; }
        public string Headers { get; set; }
        public string Method { get; set; }
        public string Host { get; set; }
        public string IpAddress { get; set; }
        public string Scheme { get; set; }
        public DateTime StartTime { get; set; }
    }
}

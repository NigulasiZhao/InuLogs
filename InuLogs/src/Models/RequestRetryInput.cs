using System;
using System.Collections.Generic;
using System.Text;

namespace InuLogs.src.Models
{
    public class RequestRetryInput
    {
        public string method { get; set; }
        public string url { get; set; }
        public string headers { get; set; }
        public string body { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace InuLogs.src.Exceptions
{
    internal class InuLogsDBDriverException : Exception
    {
        internal InuLogsDBDriverException(string message)
            : base(String.Format("InuLogs数据库异常: {0}", message))
        {

        }
    }
}

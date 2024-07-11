using System;
using System.Collections.Generic;
using System.Text;

namespace InuLogs.src.Exceptions
{
    internal class InuLogsAuthenticationException : Exception
    {
        internal InuLogsAuthenticationException() { }

        internal InuLogsAuthenticationException(string message)
            : base(String.Format("InuLogs身份验证异常: {0}", message))
        {

        }
    }
}

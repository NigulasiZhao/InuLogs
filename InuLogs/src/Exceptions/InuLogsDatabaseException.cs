using System;
using System.Collections.Generic;
using System.Text;

namespace InuLogs.src.Exceptions
{
    internal class InuLogsDatabaseException : Exception
    {
        internal InuLogsDatabaseException() { }

        internal InuLogsDatabaseException(string message)
            : base(String.Format("InuLogs数据库异常: {0} 确保你已经在.AddInuLogsServices()中传递了正确的数据库驱动程序选项或正确的连接字符串以及数据库连接字符串所需的所有参数", message))
        {

        }


    }
}

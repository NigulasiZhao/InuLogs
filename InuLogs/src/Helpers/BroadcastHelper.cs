using InuLogs.src.Hubs;
using InuLogs.src.Interfaces;
using InuLogs.src.Models;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace InuLogs.src.Helpers
{
    //internal class BroadcastHelper : IBroadcastHelper
    //{
    //    private readonly IHubContext<LoggerHub> _hubContext;
    //    public BroadcastHelper(IHubContext<LoggerHub> hubContext)
    //    {
    //        _hubContext = hubContext;
    //    }

    //    public async Task BroadcastInuLog(InuLog log)
    //    {
    //        var result = new { log = log, type = "rqLog" };
    //        await _hubContext.Clients.All.SendAsync("getLogs", result);
    //    }

    //    public async Task BroadcastLog(InuLoggerModel log)
    //    {
    //        var result = new { log = log, type = "log" };
    //        await _hubContext.Clients.All.SendAsync("getLogs", result);
    //    }

    //    public async Task BroadcastExLog(InuExceptionLog log)
    //    {
    //        var result = new { log = log, type = "exLog" };
    //        await _hubContext.Clients.All.SendAsync("getLogs", result);
    //    }
    //}
}

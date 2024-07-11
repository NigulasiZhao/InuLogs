using InuLogs.src.Enums;
using InuLogs.src.Managers;
using InuLogs.src.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InuLogs.src.Services
{
    internal class AutoLogClearerBackgroundService : BackgroundService
    {
        private bool isProcessing;
        private ILogger<AutoLogClearerBackgroundService> logger;
        private readonly IServiceProvider serviceProvider;

        public AutoLogClearerBackgroundService(ILogger<AutoLogClearerBackgroundService> logger, IServiceProvider serviceProvider)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (!isProcessing)
                {
                    isProcessing = true;
                }
                else
                {
                    return;
                }

                TimeSpan minute;
                var schedule = AutoClearModel.ClearTimeSchedule;

                switch (schedule)
                {
                    case InuLogsAutoClearScheduleEnum.Hourly:
                        minute = TimeSpan.FromMinutes(60);
                        break;
                    case InuLogsAutoClearScheduleEnum.Every6Hours:
                        minute = TimeSpan.FromHours(6);
                        break;
                    case InuLogsAutoClearScheduleEnum.Every12Hours:
                        minute = TimeSpan.FromHours(12);
                        break;
                    case InuLogsAutoClearScheduleEnum.Daily:
                        minute = TimeSpan.FromDays(1);
                        break;
                    case InuLogsAutoClearScheduleEnum.Weekly:
                        minute = TimeSpan.FromDays(7);
                        break;
                    case InuLogsAutoClearScheduleEnum.Monthly:
                        minute = TimeSpan.FromDays(30);
                        break;
                    case InuLogsAutoClearScheduleEnum.Quarterly:
                        minute = TimeSpan.FromDays(90);
                        break;
                    default:
                        minute = TimeSpan.FromDays(7);
                        break;

                }
                var start = DateTime.UtcNow;
                while (true)
                {
                    var remaining = (minute - (DateTime.UtcNow - start)).TotalMilliseconds;
                    if (remaining <= 0)
                        break;
                    if (remaining > Int16.MaxValue)
                        remaining = Int16.MaxValue;
                    await Task.Delay(TimeSpan.FromMilliseconds(remaining));
                }
                await DoWorkAsync();
                isProcessing = false;
            }
        }

        private async Task DoWorkAsync()
        {
            try
            {
                logger.LogInformation("日志清理后台服务正在启动");
                logger.LogInformation($"日志正在清理...");
                var result = await DynamicDBManager.ClearLogs();
                if (result)
                    logger.LogInformation($"日志清除成功!");
            }
            catch (Exception ex)
            {
                logger.LogError($"日志清理后台服务错误 : {ex.Message}");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("日志清理后台服务停止");
            return Task.CompletedTask;
        }
    }
}

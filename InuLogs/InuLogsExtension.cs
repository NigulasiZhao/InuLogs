using InuLogs.src.Data;
using InuLogs.src.Exceptions;
using InuLogs.src.Helpers;
using InuLogs.src.Hubs;
using InuLogs.src.Interfaces;
using InuLogs.src.Models;
using InuLogs.src.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace InuLogs
{
    public static class InuLogsExtension
    {
        public static readonly IFileProvider Provider = new EmbeddedFileProvider(
        typeof(InuLogsExtension).GetTypeInfo().Assembly,
        "InuLogs"
        );

        public static IServiceCollection AddInuLogServices(this IServiceCollection services, [Optional] Action<InuLogsSettings> configureOptions)
        {
            var options = new InuLogsSettings();
            if (configureOptions != null)
                configureOptions(options);

            AutoClearModel.IsAutoClear = options.IsAutoClear;
            AutoClearModel.ClearTimeSchedule = options.ClearTimeSchedule;
            InuLogsExternalDbConfig.ConnectionString = options.SetExternalDbConnString;
            InuLogsDatabaseDriverOption.DatabaseDriverOption = options.DbDriverOption;
            InuLogsExternalDbConfig.MongoDbName = Assembly.GetCallingAssembly().GetName().Name?.Replace('.', '_') + "_InuLogDB";
            ExceptionMessageKeyWordsOption.ExceptionMessageKeyWords = options.ExceptionMessageKeyWords;

            if (!string.IsNullOrEmpty(InuLogsExternalDbConfig.ConnectionString) && InuLogsDatabaseDriverOption.DatabaseDriverOption == 0)
                throw new InuLogsDBDriverException("缺少DB驱动程序选项:DbDriverOption是必需的在 .AddInuLogServices()");
            if (InuLogsDatabaseDriverOption.DatabaseDriverOption != 0 && string.IsNullOrEmpty(InuLogsExternalDbConfig.ConnectionString))
                throw new InuLogsDatabaseException("缺少连接字符串");

            services.AddDistributedMemoryCache();

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(5);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            //services.AddSignalR();
            services.AddMvcCore(x =>
            {
                x.EnableEndpointRouting = false;
            }).AddApplicationPart(typeof(InuLogsExtension).Assembly);

            //services.AddSingleton<IBroadcastHelper, BroadcastHelper>();
            services.AddSingleton<IMemoryCache, MemoryCache>();

            if (!string.IsNullOrEmpty(InuLogsExternalDbConfig.ConnectionString))
            {
                if (InuLogsDatabaseDriverOption.DatabaseDriverOption == src.Enums.InuLogsDbDriverEnum.Mongo)
                {
                    ExternalDbContext.MigrateNoSql();
                }
                else
                {
                    ExternalDbContext.Migrate();
                }
            }

            if (AutoClearModel.IsAutoClear)
                services.AddHostedService<AutoLogClearerBackgroundService>();

            return services;
        }

        public static IApplicationBuilder UseInuLogExceptionLogger(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<src.InuLogsExceptionLogger>();
        }

        public static IApplicationBuilder UseInuLog(this IApplicationBuilder app, Action<InuLogsOptionsModel> configureOptions)
        {
            //ServiceProviderFactory.BroadcastHelper = app.ApplicationServices.GetService<IBroadcastHelper>();
            var options = new InuLogsOptionsModel();
            configureOptions(options);
            if (string.IsNullOrEmpty(options.InuPageUsername))
            {
                throw new InuLogsAuthenticationException("参数Username必填 on .UseInuLog()");
            }
            else if (string.IsNullOrEmpty(options.InuPagePassword))
            {
                throw new InuLogsAuthenticationException("参数Password必填 on .UseInuLog()");
            }

            app.UseRouting();
            app.UseMiddleware<src.InuLogs>(options);


            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new EmbeddedFileProvider(
                    typeof(InuLogsExtension).GetTypeInfo().Assembly,
                  "InuLogs.src.InuPage"),

                RequestPath = new PathString("/ZLGstatics")
            });

            app.Build();

            app.UseAuthorization();

            app.UseSession();

            if (!string.IsNullOrEmpty(options.CorsPolicy))
                app.UseCors(options.CorsPolicy);

#if NET8_0_OR_GREATER
            if (options.UseOutputCache)
                app.UseOutputCache();
#endif

            return app.UseEndpoints(endpoints =>
            {
                //endpoints.MapHub<LoggerHub>("/zllogger");
                endpoints.MapControllerRoute(
                    name: "ZLinupage",
                    pattern: "ZLinupage/{action}",
                    defaults: new { controller = "InuPage", action = "Index" });
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapGet("inulogs", async context =>
                {
                    context.Response.ContentType = "text/html";
                    await context.Response.SendFileAsync(InuLogsExtension.GetFile());
                });
            });
        }


        public static IFileInfo GetFile()
        {
            return Provider.GetFileInfo("src.InuPage.index.html");
        }

        public static string GetFolder()
        {
            return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        }
    }
}

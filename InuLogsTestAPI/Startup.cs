using InuLogs;
using InuLogs.src.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InuLogsTestAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddInuLogServices(opt =>
            {
                opt.IsAutoClear = true;
                opt.SetExternalDbConnString = "Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)(HOST = 192.168.88.31)(PORT = 1521))(CONNECT_DATA =(SERVER = DEDICATED)(SERVICE_NAME = orclpdb)));User ID=productgis;Password=Hdkj1234#;Pooling=true;Max Pool Size=60;Min Pool Size=1;";
                //opt.SetExternalDbConnString = "User ID=productgis;Password=hdkj;Host=192.168.88.31;Port=5432;Database=geodata;Pooling=true;CommandTimeout=1200;";
                opt.DbDriverOption = InuLogsDbDriverEnum.Oracle;
                opt.ExceptionMessageKeyWords = new List<string> { "系统发生错误", "temperatureF" };
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseInuLogExceptionLogger();
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();
            app.UseInuLog(opt => { opt.InuPageUsername = "admin"; opt.InuPagePassword = "123"; });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}

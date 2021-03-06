﻿using System;
using System.IO;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;    
using Pomelo.Web.Middleware;
using Pomelo.Model.Database;
using Microsoft.CodeAnalysis.Options;
using Org.BouncyCastle.Utilities;
using System.Net.WebSockets;

namespace Pomelo.Web
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
            //跨域设置
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        builder.WithOrigins("*")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                    });
            });

            services.AddControllers();
            //services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            //Swagger
            services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Pomelo API", Version = "v1" });
                    var basePath = Path.GetDirectoryName(typeof(Program).Assembly.Location);//获取应用程序所在目录（绝对，不受工作目录影响，建议采用此方法获取路径）

                    c.IncludeXmlComments(Path.Combine(basePath, "Pomelo.Web.xml"));

                });
  


            //redis 服务
            var redisConnectionString = Configuration.GetSection("Redis").Get<RedisModel>();

            var csredis = new CSRedis.CSRedisClient($"{redisConnectionString.Host},password={redisConnectionString.Password},defaultDatabase={redisConnectionString.DefaultDatabase},prefix={redisConnectionString.Prefix}");
            RedisHelper.Initialization(csredis);
            services.AddSingleton<IDistributedCache>(new Microsoft.Extensions.Caching.Redis.CSRedisCache(RedisHelper.Instance));



            //后台任务
            //services.AddHostedService<DefaultJob>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseStaticFiles();


            app.UseMiddleware(typeof(ErrorHandlingMiddleware));

            //swagger
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pomelo API");
                c.RoutePrefix = "doc";
            });


            //app.UseHttpsRedirection();

            app.UseRouting();
            app.UseCors();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => endpoints.MapDefaultControllerRoute());


            //websocket服务   
            app.Map("/imserver/ws", SocketHandle.Map);

        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IISSample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // These two middleware are registered via an IStartupFilter in UseIISIntegration but you can configure them here.
            services.Configure<IISOptions>(options =>
            {
                options.AuthenticationDisplayName = "Windows Auth";
            });
            services.Configure<ForwardedHeadersOptions>(options =>
            {
            });
        }

        public void Configure(
            IApplicationBuilder app, ILoggerFactory loggerfactory)
        {
            var logger = loggerfactory.CreateLogger("Requests");

            app.Run(async (context) =>
            {
                logger.LogDebug("Received request: " + context.Request.Method + " " + context.Request.Path);

                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("Hello World - " + DateTimeOffset.Now + Environment.NewLine);
                await context.Response.WriteAsync(Environment.NewLine);

                await context.Response.WriteAsync("Address:" + Environment.NewLine);
                await context.Response.WriteAsync("Scheme: " + context.Request.Scheme + Environment.NewLine);
                await context.Response.WriteAsync("Host: " + context.Request.Headers["Host"] + Environment.NewLine);
                await context.Response.WriteAsync("PathBase: " + context.Request.PathBase.Value + Environment.NewLine);
                await context.Response.WriteAsync("Path: " + context.Request.Path.Value + Environment.NewLine);
                await context.Response.WriteAsync("Query: " + context.Request.QueryString.Value + Environment.NewLine);
                await context.Response.WriteAsync(Environment.NewLine);

                await context.Response.WriteAsync("Connection:" + Environment.NewLine);
                await context.Response.WriteAsync("RemoteIp: " + context.Connection.RemoteIpAddress + Environment.NewLine);
                await context.Response.WriteAsync("RemotePort: " + context.Connection.RemotePort + Environment.NewLine);
                await context.Response.WriteAsync("LocalIp: " + context.Connection.LocalIpAddress + Environment.NewLine);
                await context.Response.WriteAsync("LocalPort: " + context.Connection.LocalPort + Environment.NewLine);
                await context.Response.WriteAsync("ClientCert: " + context.Connection.ClientCertificate + Environment.NewLine);
                await context.Response.WriteAsync(Environment.NewLine);

                var domain = System.AppDomain.CurrentDomain;
                await context.Response.WriteAsync($"TargetFrameworkName={AppContext.TargetFrameworkName} BaseDirectory: {domain.BaseDirectory}");

                await context.Response.WriteAsync("{Environment.NewLine} User:{context.User.Identity.Name} {Environment.NewLine}");

                //IAuthenticationSchemeProvider authSchemeProvider
                //var scheme = await authSchemeProvider.GetSchemeAsync(IISDefaults.AuthenticationScheme);
                //await context.Response.WriteAsync("DisplayName: " + scheme?.DisplayName + Environment.NewLine);

                await context.Response.WriteAsync(Environment.NewLine);

                await context.Response.WriteAsync("Headers:" + Environment.NewLine);
                foreach (var header in context.Request.Headers)
                {
                    await context.Response.WriteAsync(header.Key + ": " + header.Value + Environment.NewLine);
                }
                await context.Response.WriteAsync(Environment.NewLine);

                await context.Response.WriteAsync("Environment Variables:" + Environment.NewLine);
                var vars = Environment.GetEnvironmentVariables();
                foreach (var key in vars.Keys.Cast<string>().OrderBy(key => key, StringComparer.OrdinalIgnoreCase))
                {
                    var value = vars[key];
                    await context.Response.WriteAsync(key + ": " + value + Environment.NewLine);
                }

                await context.Response.WriteAsync(Environment.NewLine);
                if (context.Features.Get<IHttpUpgradeFeature>() != null) {
                    await context.Response.WriteAsync("Websocket feature is enabled.");
                } else {
                    await context.Response.WriteAsync("Websocket feature is disabled.");
                }
            });
        }

        public static void Main(string[] args)
        {
            // return AssemblyLoadContext.Default.LoadFromAssemblyPath(fullPath);
            var dir = AppContext.BaseDirectory;

            Run();
        }

        /* #region Assembly System.Runtime, Version=4.2.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
        public event EventHandler Exit
        {
            add { AppContext.ProcessExit += value; }
            remove { AppContext.ProcessExit -= value; }
        } 
        */
        static void Run()
        { 
            var host = new WebHostBuilder()
                .ConfigureLogging((_, factory) =>
                {
                    factory.AddConsole();
                    factory.AddFilter("Console", level => level >= LogLevel.Trace); // .Debug);
                })
                .UseKestrel()
                .UseStartup<Startup>()
                .Build();

            var services = host.Services;
            var options = services.GetRequiredService<Microsoft.AspNetCore.Hosting.Internal.WebHostOptions>();
            if (options != null) {
                options.SuppressStatusMessages = false;
                options.DetailedErrors = true;
                options.CaptureStartupErrors = true;
            }

            host.Run();
        }
    }
}


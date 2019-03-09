// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IISSample
{
    public class Startup
    {
        public static Startup Instance;

        public void ConfigureServices(IServiceCollection services)
        {
            // These two middleware are registered via an IStartupFilter in UseIISIntegration but you can configure them here.
            services.Configure<IISOptions>(options => {
                options.AuthenticationDisplayName = "Windows Auth";
            });

            services.Configure<ForwardedHeadersOptions>(options => {
            });

            // Configure IAuthenticationSchemeProvider 
            Instance = this;
            IServiceCollection _applicationServiceCollection = services;
            // _applicationServiceCollection.AddSingleton<HostedServiceExecutor>();

            services = _applicationServiceCollection.AddTransient<IHttpContextFactory, HttpContextFactoryWrap>();
        }

        public void Configure(
            IApplicationBuilder app, ILoggerFactory loggerfactory)
            // IAuthenticationSchemeProvider authSchemeProvider)
        {
            Instance = this;
            var logger = loggerfactory.CreateLogger("Requests");

            app.Run(async (context) => {
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

                await context.Response.WriteAsync("User: " + context.User.Identity.Name + Environment.NewLine);

                var authSchemeProvider = app.ApplicationServices.GetService<IAuthenticationSchemeProvider>();
                if (authSchemeProvider != null) {
                    var scheme = await authSchemeProvider.GetSchemeAsync(IISDefaults.AuthenticationScheme);

                    await context.Response.WriteAsync("DisplayName: " + scheme?.DisplayName + Environment.NewLine);
                }
                else {
                    await context.Response.WriteAsync("No IAuthenticationSchemeProvider");
                }
                await context.Response.WriteAsync(Environment.NewLine);

                await context.Response.WriteAsync("Headers:" + Environment.NewLine);
                foreach (var header in context.Request.Headers) {
                    await context.Response.WriteAsync(header.Key + ": " + header.Value + Environment.NewLine);
                }
                await context.Response.WriteAsync(Environment.NewLine);

                await context.Response.WriteAsync("Environment Variables:" + Environment.NewLine);
                var vars = Environment.GetEnvironmentVariables();
                foreach (var key in vars.Keys.Cast<string>().OrderBy(key => key, StringComparer.OrdinalIgnoreCase)) {
                    var value = vars[key];
                    await context.Response.WriteAsync(key + ": " + value + Environment.NewLine);
                }

                await context.Response.WriteAsync(Environment.NewLine);
                if (context.Features.Get<IHttpUpgradeFeature>() != null) {
                    await context.Response.WriteAsync("Websocket feature is enabled.");
                }
                else {
                    await context.Response.WriteAsync("Websocket feature is disabled.");
                }
            });
        }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilderWrap()
                .ConfigureLogging((_, factory) => {
                    factory.AddConsole();
                    factory.AddFilter("Console", level => level >= LogLevel.Debug);
                })
                .UseKestrel()
                .UseStartup<Startup>()
                .Build();

            //var type = typeof(IAuthenticationSchemeProvider);
            //Could not resolve a service of type 'Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider'
            // C:\Users\akris\.nuget\packages\microsoft.aspnetcore.authentication.abstractions\2.2.0\lib\netstandard2.0\Microsoft.AspNetCore.Authentication.Abstractions.dll

            var task = Run(host);
            if (task.Exception != null) {
                Console.WriteLine($"Server fail {task.Exception}");
            }
            task.GetAwaiter().GetResult();
        }

        public static async Task Run(IWebHost host)
        {
            CancellationToken cancellationToken = CancellationToken.None;
            CancellationTokenSource src = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // var features = host.ServerFeatures;
            var services = host.Services;
            var options = services.GetRequiredService<Microsoft.AspNetCore.Hosting.Internal.WebHostOptions>();
            if (options != null) {
                options.SuppressStatusMessages = false;
                options.DetailedErrors = true;
                options.CaptureStartupErrors = true;
            }

            // _applicationServiceCollection.AddSingleton<HostedServiceExecutor>();
            // host.Run();
            // var task = host.RunAsync(cancellationToken);

            var task = host.StartAsync(cancellationToken);
            task.GetAwaiter().GetResult();

            var waiter = WaitShutdown(host, src);
            await waiter; // .GetAwaiter().GetResult();
        }

        public static async Task WaitShutdown(
            IWebHost host, // , CancellationToken token = default
            CancellationTokenSource src)
        {
            var token = src.Token;
            var applicationLifetime = host.Services.GetService<IApplicationLifetime>();

            token.Register(state => {
                ((IApplicationLifetime)state).StopApplication();
            },
            applicationLifetime);

            var waitForStop = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            applicationLifetime.ApplicationStopping.Register(obj => {
                var tcs = (TaskCompletionSource<object>)obj;
                tcs.TrySetResult(null);
            }, waitForStop);

            AttachCtrlcSigtermShutdown(src, null, "SigtermShutdown");

            var options = host.Services.GetService<WebHostOptions>();
            IApplicationBuilder appBuild = host.Services.GetService<IApplicationBuilder>();
            var features = appBuild?.ServerFeatures;
            var addr = features?.Get<IServerAddressesFeature>().Addresses.FirstOrDefault() ?? "";
            var root = options.WebRoot ?? AppDomain.CurrentDomain.BaseDirectory;
            Console.WriteLine($"WebRoot={root}");
            Console.WriteLine($@" started {options.ApplicationName} :" +
                 $" ContentRootPath={options.ContentRootPath} host {addr}");

            await waitForStop.Task;

            // WebHost will use its default ShutdownTimeout if none is specified.
            await host.StopAsync();
        }

        private static void AttachCtrlcSigtermShutdown(
            CancellationTokenSource cts, ManualResetEventSlim resetEvent, string shutdownMessage)
        {
            void Shutdown()
            {
                if (!cts.IsCancellationRequested)
                {
                    if (!string.IsNullOrEmpty(shutdownMessage))
                    {
                        Console.WriteLine(shutdownMessage);
                    }
                    try
                    {
                        cts.Cancel();
                    }
                    catch (ObjectDisposedException) { }
                }

                // Wait on the given reset event
                resetEvent?.Wait();
            };

            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) => Shutdown();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Shutdown();
                // Don't terminate the process immediately, wait for the Main thread to exit gracefully.
                eventArgs.Cancel = true;
            };
        }
    }
}

namespace Hosting { 
    using Microsoft.AspNetCore.Hosting.Internal;
    using Microsoft.AspNetCore.Hosting.Server;
    using Microsoft.Extensions.Options;
    using System.Diagnostics;
    using System.Reflection;
    using System.Threading.Tasks;

    public class WebHostBuilderWrap : WebHostBuilder, IWebHostBuilder
    {
        public WebHostBuilderWrap() : base()
        {
            _options2 = new WebHostOptions(_config, Assembly.GetEntryAssembly()?.GetName().Name);
        }

        private WebHostOptions _options2;

        public override IWebHost Build()
        {
            var host = base.Build();

            _options2 = _options;
            //_options.CaptureStartupErrors = true;
            //_options.SuppressStatusMessages = false;
            return host;
        }
    }

    public class HttpContextFactoryWrap : HttpContextFactory, IHttpContextFactory
    {
        public HttpContextFactoryWrap(IOptions<FormOptions> formOptions) : this(formOptions, null) { }

        public HttpContextFactoryWrap(
            IOptions<FormOptions> formOptions, IHttpContextAccessor httpContextAccessor)
            : base(formOptions, httpContextAccessor)
        {
        }

        public new HttpContext Create(IFeatureCollection featureCollection)
            => base.Create(featureCollection);
        // => throw new Exception(); 
        public new void Dispose(HttpContext httpContext)
            => base.Dispose(httpContext);
    }

    public class HostingApplicationWrap : HostingApplication, IHttpApplication<HostingApplication.Context>
    {
        public HostingApplicationWrap(
            RequestDelegate application,
            ILogger logger,
            DiagnosticListener diagnosticSource,
            IHttpContextFactory httpContextFactory)
          : base(application, logger, diagnosticSource, httpContextFactory)
        {
        }

        public override HostingApplication.Context CreateContext(IFeatureCollection contextFeatures)
            => base.CreateContext(contextFeatures);

        public override void DisposeContext(HostingApplication.Context context, Exception exception)
            => base.DisposeContext(context, exception);

        public override Task ProcessRequestAsync(HostingApplication.Context context)
            => base.ProcessRequestAsync(context);
    }
}


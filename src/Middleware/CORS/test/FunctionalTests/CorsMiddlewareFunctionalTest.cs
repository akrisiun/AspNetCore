// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace FunctionalTests
{
    public class CorsMiddlewareFunctionalTests : LoggedTest
    {
        public CorsMiddlewareFunctionalTests(ITestOutputHelper output)
            : base(output)
        {
            Output = output;
        }

        public ITestOutputHelper Output { get; }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Disabling this test on OSX until we have a resolution for https://github.com/aspnet/AspNetCore-Internal/issues/1619")]
        public async Task RunClientTests()
        {
            using (StartLog(out var loggerFactory))
            using (var deploymentResult = await CreateDeployments(loggerFactory))
            {
                ProcessStartInfo processStartInfo;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    processStartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd",
                        Arguments = "/c npm test --no-color --no-watchman",
                    };
                }
                else
                {
                    processStartInfo = new ProcessStartInfo
                    {
                        FileName = "npm",
                        Arguments = "test --no-watchman",
                    };
                }
                // Disallow the test from downloading \ installing chromium.
                processStartInfo.Environment["PUPPETEER_SKIP_CHROMIUM_DOWNLOAD"] = "true";

                // Act
                var result = await ProcessManager.RunProcessAsync(processStartInfo, loggerFactory.CreateLogger("ProcessManager"));

                // Assert
                Assert.Success(result);
                Assert.Contains("Test Suites: 1 passed, 1 total", result.Output);
            }
        }

        private static async Task<SamplesDeploymentResult> CreateDeployments(ILoggerFactory loggerFactory)
        {
            var solutionPath = TestPathUtilities.GetSolutionRootDirectory("Middleware");

            var runtimeFlavor = GetRuntimeFlavor();
            var applicationType = runtimeFlavor == RuntimeFlavor.Clr ? ApplicationType.Standalone : ApplicationType.Portable;

            var configuration =
#if RELEASE
                "Release";
#else
                "Debug";
#endif

            var destinationParameters = new DeploymentParameters
            {
                RuntimeFlavor = runtimeFlavor,
                ServerType = ServerType.Kestrel,
                ApplicationPath = Path.Combine(solutionPath, "CORS", "samples", "SampleDestination"),
                PublishApplicationBeforeDeployment = false,
                ApplicationType = applicationType,
                Configuration = configuration,
            };

            var destinationFactory = ApplicationDeployerFactory.Create(destinationParameters, loggerFactory);
            var destinationDeployment = await destinationFactory.DeployAsync();

            var originParameters = new DeploymentParameters
            {
                RuntimeFlavor = runtimeFlavor,
                ServerType = ServerType.Kestrel,
                ApplicationPath = Path.Combine(solutionPath, "CORS", "samples", "SampleOrigin"),
                PublishApplicationBeforeDeployment = false,
                ApplicationType = applicationType,
                Configuration = configuration,
            };

            var originFactory = ApplicationDeployerFactory.Create(originParameters, loggerFactory);
            var originDeployment = await originFactory.DeployAsync();

            return new SamplesDeploymentResult(originFactory, originDeployment, destinationFactory, destinationDeployment);
        }

        private static RuntimeFlavor GetRuntimeFlavor()
        {
#if NET461
                return RuntimeFlavor.Clr;
#elif NETCOREAPP2_2
            return RuntimeFlavor.CoreClr;
#else
#error Target frameworks need to be updated
#endif
        }

        private readonly struct SamplesDeploymentResult : IDisposable
        {
            public SamplesDeploymentResult(
                ApplicationDeployer originDeployer,
                DeploymentResult originResult,
                ApplicationDeployer destinationDeployer,
                DeploymentResult destinationResult)
            {
                OriginDeployer = originDeployer;
                OriginResult = originResult;
                DestinationDeployer = destinationDeployer;
                DestinationResult = destinationResult;
            }

            public ApplicationDeployer OriginDeployer { get; }

            public DeploymentResult OriginResult { get; }

            public ApplicationDeployer DestinationDeployer { get; }

            public DeploymentResult DestinationResult { get; }

            public void Dispose()
            {
                OriginDeployer.Dispose();
                DestinationDeployer.Dispose();
            }
        }
    }
}

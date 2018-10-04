using System;
using System.Diagnostics;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.ApplicationInsights.ServiceFabric;
using Microsoft.ApplicationInsights.ServiceFabric.Module;
using Microsoft.ApplicationInsights.DependencyCollector;
using Infrastructure.Telemetry;

namespace MyActor
{
    internal static class Program
    {
        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static void Main()
        {
            try
            {
                // This line registers an Actor Service to host your actor class with the Service Fabric runtime.
                // The contents of your ServiceManifest.xml and ApplicationManifest.xml files
                // are automatically populated when you build this project.
                // For more information, see https://aka.ms/servicefabricactorsplatform


                new ServiceRemotingDependencyTrackingTelemetryModule().Initialize(TelemetryConfiguration.Active);
                new ServiceRemotingRequestTrackingTelemetryModule().Initialize(TelemetryConfiguration.Active);

                TelemetryConfiguration.Active.InstrumentationKey = "b84236f0-5202-4b41-98d5-bde5b0675217";
                TelemetryConfiguration.Active.TelemetryChannel.DeveloperMode = true;
                TelemetryConfiguration.Active.TelemetryInitializers.Add(new HttpDependenciesParsingTelemetryInitializer());
                TelemetryDebugWriter.IsTracingDisabled = true;


                // this is the cause of dupe logging
                var dependencyTrackingTelemetryModule = new DependencyTrackingTelemetryModule();
                dependencyTrackingTelemetryModule.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("core.windows.net");
                dependencyTrackingTelemetryModule.Initialize(TelemetryConfiguration.Active);

                var builder = TelemetryConfiguration.Active.TelemetryProcessorChainBuilder;
                builder.Use((next) => new PollingTelemetryFilter(next));
                builder.Build();


                ActorRuntime.RegisterActorAsync<MyActor>(
                   (context, actorType) => 
                   {
                       FabricTelemetryInitializerExtension.SetServiceCallContext(context);
                       TelemetryConfiguration.Active.TelemetryInitializers.Add(FabricTelemetryInitializerExtension.CreateFabricTelemetryInitializer(context));
                       return new ActorService(context, actorType);
                   }).GetAwaiter().GetResult();

                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception e)
            {
                ActorEventSource.Current.ActorHostInitializationFailed(e.ToString());
                throw;
            }
        }
    }
}

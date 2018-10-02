using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Telemetry
{
    public class PollingTelemetryFilter : ITelemetryProcessor
    {
        private readonly ITelemetryProcessor _next;

        public PollingTelemetryFilter(ITelemetryProcessor next)
        {
            this._next = next;
        }

        public virtual void Process(ITelemetry item)
        {
            switch (item)
            {
                case RequestTelemetry requestTelemetry:

                    if (requestTelemetry.Name == "TryDequeueAsync")
                        return;

                    break;
                case DependencyTelemetry dependencyTelemetry:

                    if (dependencyTelemetry.Name == "TryDequeueAsync")
                        return;

                    if (dependencyTelemetry.Type == "Azure blob" && dependencyTelemetry.Name.StartsWith("HEAD "))
                        return;

                    if (dependencyTelemetry.Target == "127.0.0.1" && dependencyTelemetry.Type == "Http")
                        return;

                    break;
                default:
                    break;
            }

            this._next.Process(item);
        }
    }
}

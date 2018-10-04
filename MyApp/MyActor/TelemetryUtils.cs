using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyActor
{
    public class TelemetryWrappedException : Exception
    {
        public IDictionary<string, string> Properties { get; private set; }
        public IDictionary<string, double> Metrics { get; private set; }

        public TelemetryWrappedException(
            Exception innerException,
            IDictionary<string, string> properties = null,
            IDictionary<string, double> metrics = null)
            : base($"Telemetry wrapped exception: {innerException.Message}", innerException)
        {
            if (innerException == null) throw new ArgumentNullException(nameof(innerException));

            Properties = properties;
            Metrics = metrics;
        }
    }

    public static class TelemetryUtils
    {
        public static void UnwrapAndTrackException(
     this TelemetryClient telemetryClient,
     Exception exception,
     Func<IDictionary<string, string>> getProperties = null,
     Func<IDictionary<string, double>> getMetrics = null)
        {
            IDictionary<string, string> properties = null;
            IDictionary<string, double> metrics = null;

            if (getProperties != null)
                properties = getProperties();

            if (getMetrics != null)
                metrics = getMetrics();

            while (exception is TelemetryWrappedException)
            {
                var telemetryWrappedException = exception as TelemetryWrappedException;

                if (telemetryWrappedException.Properties != null)
                {
                    if (properties == null)
                        properties = new Dictionary<string, string>();

                    foreach (var pair in telemetryWrappedException.Properties)
                        properties[pair.Key] = pair.Value;
                }

                if (telemetryWrappedException.Metrics != null)
                {
                    if (metrics == null)
                        metrics = new Dictionary<string, double>();

                    foreach (var pair in telemetryWrappedException.Metrics)
                        metrics[pair.Key] = pair.Value;
                }

                exception = telemetryWrappedException.InnerException;
            }

            telemetryClient.TrackException(exception, properties, metrics);
        }
    }
}

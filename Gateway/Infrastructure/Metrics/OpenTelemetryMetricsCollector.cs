using Application.Abstractions.Metrics;
using System.Diagnostics;
using System.Diagnostics.Metrics;



namespace Infrastructure.Metrics
{
    public sealed class OpenTelemetryMetricsCollector : IMetricsCollector
    {
        private readonly Meter _meter;

        public OpenTelemetryMetricsCollector()
        {
            _meter = new Meter("ChatSystem.Gateway", "1.0.0");
        }

        public void IncrementCounter(string name, params KeyValuePair<string, object?>[] tags)
        {
            var counter = _meter.CreateCounter<long>(name);
            counter.Add(1, tags.ToArray());
        }

        public void DecrementCounter(string name, params KeyValuePair<string, object?>[] tags)
        {
            var counter = _meter.CreateCounter<long>(name);
            counter.Add(-1, tags.ToArray());
        }

        public void RecordHistogram(string name, double value, params KeyValuePair<string, object?>[] tags)
        {
            var histogram = _meter.CreateHistogram<double>(name);
            histogram.Record(value, tags.ToArray());
        }

        public void RecordGauge(string name, double value, params KeyValuePair<string, object?>[] tags)
        {
            _meter.CreateObservableGauge<double>(name, () =>
                new Measurement<double>(value, tags.Select(kv => new KeyValuePair<string, object?>(kv.Key, kv.Value)).ToArray())
            );
        }

        public IDisposable BeginScope(params KeyValuePair<string, object?>[] tags)
        {
            // Can integrate with ILogger.BeginScope or Activity.Current
            var activity = Activity.Current;
            if (activity != null)
            {
                foreach (var tag in tags)
                {
                    activity.SetTag(tag.Key, tag.Value);
                }
            }
            return new NoOpDisposable();
        }

        private class NoOpDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}

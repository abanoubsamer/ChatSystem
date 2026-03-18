using Application.Abstractions.Metrics;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Infrastructure.Metrics
{
    using Application.Abstractions.Metrics;
    using System.Collections.Frozen;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;

    namespace Infrastructure.Metrics
    {
        public sealed class OpenTelemetryMetricsCollector : IMetricsCollector, IDisposable
        {
            private readonly Meter _meter;

           
            private readonly FrozenDictionary<string, UpDownCounter<long>> _knownCounters;
            private readonly FrozenDictionary<string, Histogram<double>> _knownHistograms;

           
            private readonly ConcurrentDictionary<string, UpDownCounter<long>> _dynamicCounters = new();
            private readonly ConcurrentDictionary<string, Histogram<double>> _dynamicHistograms = new();
            private readonly ConcurrentDictionary<string, GaugeEntry> _gauges = new();

            public OpenTelemetryMetricsCollector()
            {
                _meter = new Meter("ChatSystem.Gateway", "1.0.0");

                _knownCounters = new Dictionary<string, UpDownCounter<long>>
                {
                    ["connections.active"] = _meter.CreateUpDownCounter<long>("connections.active"),
                    ["message.dispatched"] = _meter.CreateUpDownCounter<long>("message.dispatched"),
                    ["message.decompressed"] = _meter.CreateUpDownCounter<long>("message.decompressed"),
                    ["message.validation.errors"] = _meter.CreateUpDownCounter<long>("message.validation.errors"),
                    ["message.processing.errors"] = _meter.CreateUpDownCounter<long>("message.processing.errors"),
                    ["ratelimit.exceeded"] = _meter.CreateUpDownCounter<long>("ratelimit.exceeded"),

                }.ToFrozenDictionary();

                _knownHistograms = new Dictionary<string, Histogram<double>>
                {
                    ["message.processing.duration_ms"] = _meter.CreateHistogram<double>("message.processing.duration_ms"),
                }.ToFrozenDictionary();
            }

            // ─── Counter ─────────────────────────────────────────────────────────────

            /// <summary>Hot path — zero allocation للـ pre-warmed metrics</summary>
            public void IncrementCounter(string name)
            {
                GetOrCreateCounter(name).Add(1);
            }

            /// <summary>Hot path — TagList على الـ Stack، مفيش heap allocation</summary>
            public void IncrementCounter(string name, string tagKey, object? tagValue)
            {
                // ✅ TagList = stack-allocated struct, مش array على الـ heap
                var tags = new TagList { { tagKey, tagValue } };
                GetOrCreateCounter(name).Add(1, tags);
            }

            /// <summary>Hot path — 2 tags بدون params array</summary>
            public void IncrementCounter(string name,
                string key1, object? val1,
                string key2, object? val2)
            {
                var tags = new TagList { { key1, val1 }, { key2, val2 } };
                GetOrCreateCounter(name).Add(1, tags);
            }

            /// <summary>Slow path — للـ dynamic tags (3+)</summary>
            public void IncrementCounter(string name, params KeyValuePair<string, object?>[] tags)
            {
                var tagList = new TagList();
                foreach (var tag in tags)
                    tagList.Add(tag);

                GetOrCreateCounter(name).Add(1, tagList);
            }

            public void DecrementCounter(string name, string tagKey, object? tagValue)
            {
                var tags = new TagList { { tagKey, tagValue } };
                GetOrCreateCounter(name).Add(-1, tags);
            }

            public void DecrementCounter(string name, params KeyValuePair<string, object?>[] tags)
            {
                var tagList = new TagList();
                foreach (var tag in tags)
                    tagList.Add(tag);

                GetOrCreateCounter(name).Add(-1, tagList);
            }

            // ─── Histogram ───────────────────────────────────────────────────────────

            public void RecordHistogram(string name, double value, string tagKey, object? tagValue)
            {
                var tags = new TagList { { tagKey, tagValue } };
                GetOrCreateHistogram(name).Record(value, tags);
            }

            public void RecordHistogram(string name, double value,
                string key1, object? val1,
                string key2, object? val2)
            {
                var tags = new TagList { { key1, val1 }, { key2, val2 } };
                GetOrCreateHistogram(name).Record(value, tags);
            }

            public void RecordHistogram(string name, double value, params KeyValuePair<string, object?>[] tags)
            {
                var tagList = new TagList();
                foreach (var tag in tags)
                    tagList.Add(tag);

                GetOrCreateHistogram(name).Record(value, tagList);
            }

            // ─── Gauge ───────────────────────────────────────────────────────────────

            public void RecordGauge(string name, double value,
                params KeyValuePair<string, object?>[] tags)
            {
                _gauges.AddOrUpdate(
                    key: name,
                    addValueFactory: n =>
                    {
                        var entry = new GaugeEntry(value, tags);
                        _meter.CreateObservableGauge<double>(n, () =>
                            new Measurement<double>(entry.Value, entry.Tags));
                        return entry;
                    },
                    updateValueFactory: (_, existing) =>
                    {
                        existing.Update(value, tags);
                        return existing;
                    });
            }

            // ─── Scope ───────────────────────────────────────────────────────────────

            public IDisposable BeginScope(params KeyValuePair<string, object?>[] tags)
            {
                var activity = Activity.Current;
                if (activity is not null)
                {
                    foreach (var tag in tags)
                        activity.SetTag(tag.Key, tag.Value);
                }
                return NoOpDisposable.Instance; // ✅ static singleton — zero allocation
            }

            // ─── Dispose ─────────────────────────────────────────────────────────────

            public void Dispose() => _meter.Dispose();

            // ─── Private Helpers ─────────────────────────────────────────────────────

            [System.Runtime.CompilerServices.MethodImpl(
                System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            private UpDownCounter<long> GetOrCreateCounter(string name)
            {
                // ✅ FrozenDictionary lookup أول — أسرع path لأي metric معروف
                if (_knownCounters.TryGetValue(name, out var known))
                    return known;

                // Fallback للـ dynamic metrics
                return _dynamicCounters.GetOrAdd(name, n => _meter.CreateUpDownCounter<long>(n));
            }

            [System.Runtime.CompilerServices.MethodImpl(
                System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            private Histogram<double> GetOrCreateHistogram(string name)
            {
                if (_knownHistograms.TryGetValue(name, out var known))
                    return known;

                return _dynamicHistograms.GetOrAdd(name, n => _meter.CreateHistogram<double>(n));
            }

            // ─── Inner Types ─────────────────────────────────────────────────────────

            private sealed class GaugeEntry
            {
                private double _value;
                private KeyValuePair<string, object?>[] _tags;

                // ✅ Volatile بدل lock للـ simple read/write على الـ gauge
                public double Value => Volatile.Read(ref _value);
                public KeyValuePair<string, object?>[] Tags => Volatile.Read(ref _tags);

                public GaugeEntry(double value, KeyValuePair<string, object?>[] tags)
                {
                    _value = value;
                    _tags = tags;
                }

                public void Update(double value, KeyValuePair<string, object?>[] tags)
                {
                    Volatile.Write(ref _value, value);
                    Volatile.Write(ref _tags, tags);
                }
            }

            private sealed class NoOpDisposable : IDisposable
            {
                public static readonly NoOpDisposable Instance = new();
                private NoOpDisposable() { }
                public void Dispose() { }
            }
        }
    }
}
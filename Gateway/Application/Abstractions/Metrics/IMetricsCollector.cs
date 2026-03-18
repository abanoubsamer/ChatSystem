using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Metrics
{
    public interface IMetricsCollector
    {
        void IncrementCounter(string name);
        void IncrementCounter(string name, string tagKey, object? tagValue);
        void IncrementCounter(string name, string key1, object? val1, string key2, object? val2);
        void IncrementCounter(string name, params KeyValuePair<string, object?>[] tags);

        void DecrementCounter(string name, string tagKey, object? tagValue);
        void DecrementCounter(string name, params KeyValuePair<string, object?>[] tags);

        void RecordHistogram(string name, double value, string tagKey, object? tagValue);
        void RecordHistogram(string name, double value, string key1, object? val1, string key2, object? val2);
        void RecordHistogram(string name, double value, params KeyValuePair<string, object?>[] tags);

        void RecordGauge(string name, double value, params KeyValuePair<string, object?>[] tags);

        IDisposable BeginScope(params KeyValuePair<string, object?>[] tags);
    }
}

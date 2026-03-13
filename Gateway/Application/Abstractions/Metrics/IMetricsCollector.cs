using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Metrics
{
    public interface IMetricsCollector
    {
        void IncrementCounter(string name, params KeyValuePair<string, object?>[] tags);
        void DecrementCounter(string name, params KeyValuePair<string, object?>[] tags);
        void RecordHistogram(string name, double value, params KeyValuePair<string, object?>[] tags);
        void RecordGauge(string name, double value, params KeyValuePair<string, object?>[] tags);

        // Optional: Scoped metrics for connection tracking
        IDisposable BeginScope(params KeyValuePair<string, object?>[] tags);
    }
}

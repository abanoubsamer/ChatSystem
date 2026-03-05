using Application.Abstractions.Session;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Session
{
    /// <summary>
    /// In-memory ring timeout manager.
    /// Each call session gets a CancellationTokenSource that fires after the ring duration.
    /// </summary>
    public class RingTimeoutService : IRingTimeoutService
    {
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _timers = new();

        public void StartRingTimer(string sessionId, TimeSpan timeout, Func<Task> onTimeoutAsync)
        {
            // Cancel any existing timer for this session (safety guard)
            CancelRingTimer(sessionId);

            var cts = new CancellationTokenSource();
            _timers[sessionId] = cts;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(timeout, cts.Token);

                    // Timer completed without cancellation → no one answered
                    if (!cts.Token.IsCancellationRequested)
                    {
                        _timers.TryRemove(sessionId, out _);
                        await onTimeoutAsync();
                    }
                }
                catch (TaskCanceledException)
                {
                    // Timer was cancelled (call was answered or ended) — do nothing
                }
            });
        }

        public void CancelRingTimer(string sessionId)
        {
            if (_timers.TryRemove(sessionId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
            }
        }
    }
}

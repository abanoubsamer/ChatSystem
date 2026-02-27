using Application.Abstractions.Queue;
using Application.Dtos.Ack;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Queue
{
        public class AckBatchQueue 
    {
            private readonly Queue<Acked> _buffer = new();
            private IDisposable? _timer;
            private bool _isFlushing = false;
            private const int BATCH_SIZE = 100;
            private const int FLUSH_INTERVAL_MS = 200;
            private const int MAX_BUFFER_SIZE = 1000;

            private readonly Func<List<Acked>, Task> _onFlush;
            private readonly Func<Func<object?, Task>, object?, TimeSpan, TimeSpan, IDisposable> _registerTimer;
            private readonly ILogger _logger;

            public AckBatchQueue(
                Func<Func<object?, Task>, object?, TimeSpan, TimeSpan, IDisposable> registerTimer,
                Func<List<Acked>, Task> onFlush,
                ILogger logger)
            {
                _registerTimer = registerTimer;
                _onFlush = onFlush;
                _logger = logger;
            }

            public Task EnqueueAsync(Acked ack)
            {
                if (_buffer.Count >= MAX_BUFFER_SIZE)
                {
                    _logger.LogWarning("Buffer overflow — dropping ACK for receiver {ReceiverId}", ack.ReceiverId);
                    return Task.CompletedTask;
                }

                _buffer.Enqueue(ack);

                // ✅ Timer بيتعمل بس لما يجي أول ACK
                _timer ??= _registerTimer(
                    FlushAsync,
                    null,
                    TimeSpan.FromMilliseconds(FLUSH_INTERVAL_MS),
                    TimeSpan.FromMilliseconds(FLUSH_INTERVAL_MS));

                if (_buffer.Count >= BATCH_SIZE)
                    return FlushAsync(null);

                return Task.CompletedTask;
            }

            public async Task FlushAsync(object? _)
            {
                if (_buffer.Count == 0 || _isFlushing) return;

                _isFlushing = true;
                try
                {
                    var batch = new List<Acked>(_buffer.Count);
                    while (_buffer.TryDequeue(out var ack))
                        batch.Add(ack);

                    await _onFlush(batch);
                }
                finally
                {
                    _isFlushing = false;
                }
            }

            public async Task FlushAndDisposeAsync()
            {
                _timer?.Dispose();
                _timer = null;

                if (_buffer.Count > 0)
                    await FlushAsync(null);
            }
        }
    
}

using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Infrastructure.Extension
{
    public static class BatchChannelExtensions
    {
        public static async IAsyncEnumerable<IReadOnlyList<T>> ReadBatchesAsync<T>(
            this ChannelReader<T> reader,
            int maxBatchSize,
            TimeSpan maxWaitTime,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            var batch = new List<T>(maxBatchSize);

            while (!ct.IsCancellationRequested)
            {
                // 👇 استنى أول عنصر
                if (!await reader.WaitToReadAsync(ct).ConfigureAwait(false))
                    yield break;

                // اقرأ أول عنصر (لازم يكون موجود)
                while (reader.TryRead(out var item))
                {
                    batch.Add(item);
                    break;
                }

                // 👇 ابدأ timeout بعد أول عنصر
                var timeoutTask = Task.Delay(maxWaitTime, ct);

                while (batch.Count < maxBatchSize)
                {
                    var waitTask = reader.WaitToReadAsync(ct).AsTask();

                    var completed = await Task.WhenAny(waitTask, timeoutTask)
                                              .ConfigureAwait(false);

                    if (completed == timeoutTask)
                    {
                        // ⏰ الوقت خلص → نطلع الباتش
                        break;
                    }

                    // فيه عناصر جديدة
                    while (batch.Count < maxBatchSize && reader.TryRead(out var next))
                    {
                        batch.Add(next);
                    }
                }

                yield return batch;

                batch = new List<T>(maxBatchSize); // reset clean list
            }
        }
    }
}
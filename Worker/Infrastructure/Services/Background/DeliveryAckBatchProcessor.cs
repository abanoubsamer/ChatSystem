using Application.Abstractions.Queue;
using Application.Abstractions.Services.Ack;
using Application.Dtos.Ack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Runtime.CompilerServices;


namespace Infrastructure.Services.Background
{
    public class DeliveryAckBatchProcessor : BackgroundService
    {
       
        private readonly IQueue<Acked> _ackQueue;
        private readonly IServiceProvider _serviceProvider;

        // Batch configuration
        private const int BATCH_SIZE = 100;
        private const int BATCH_TIMEOUT_MS = 50; // 50ms max wait
        public DeliveryAckBatchProcessor(IServiceProvider serviceProvider,  IQueue<Acked> ackQueue)
        {
            _serviceProvider = serviceProvider;
            _ackQueue = ackQueue; 
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var ackService = scope.ServiceProvider.GetRequiredService<IAckServices>();

           
           
            await foreach (var batch in GetBatchesAsync(stoppingToken))
            {
                
                    //await ackService.DeliveryAckProcesss(batch, stoppingToken);
                  
            }
        }


        private async IAsyncEnumerable<List<Acked>> GetBatchesAsync(
             [EnumeratorCancellation] CancellationToken ct)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(BATCH_TIMEOUT_MS));

            while (!ct.IsCancellationRequested)
            {
                await timer.WaitForNextTickAsync(ct);

                
                var batch = await _ackQueue.TryReadBatchAsync(BATCH_SIZE, ct);

                if (batch.Count > 0)
                {
                    yield return batch;
                }
            }
        }


       
    }
}


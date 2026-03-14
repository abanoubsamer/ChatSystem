using Application.Abstractions.Compression;
using Application.Abstractions.Metrics;
using Application.Abstractions.Pipeline;
using Application.Messaging;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Pipeline.Middlewares
{
    
    public sealed class DecompressionMiddleware : IMessageMiddleware
    {
        private readonly IMessageCompressor _compressor;
        private readonly IMetricsCollector _metrics;
        private readonly ILogger<DecompressionMiddleware> _logger;

        public DecompressionMiddleware(
            IMessageCompressor compressor,
            IMetricsCollector metrics,
            ILogger<DecompressionMiddleware> logger)
        {
            _compressor = compressor;
            _metrics = metrics;
            _logger = logger;
        }

        public async Task InvokeAsync(
            MessageContext context,
            ReadOnlyMemory<byte> payload,
            MessageMiddlewareDelegate next,
            CancellationToken ct)
        {
            // Fast path — الغالبية العظمى من الـ messages مش compressed
            if (!_compressor.IsCompressed(payload.Span))
            {
                await next(context, payload, ct);
                return;
            }

            var decompressed = await _compressor.DecompressAsync(payload, ct);

            _logger.LogDebug(
                "Decompressed message | userId={UserId} | original={Original}b | decompressed={Decompressed}b",
                context.UserId, payload.Length, decompressed.Length);

            _metrics.IncrementCounter("message.decompressed",
                new KeyValuePair<string, object?>("user.id", context.UserId),
                new KeyValuePair<string, object?>("ratio", (double)decompressed.Length / payload.Length));

            // يمرر الـ payload الجديد (المفكوك) للـ middleware الجاي
            await next(context, decompressed, ct);
        }
    }
}

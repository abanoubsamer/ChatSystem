using Application.Abstractions.Compression;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Compression
{
    public sealed class GzipMessageCompressor : IMessageCompressor
    {
        public async Task<byte[]> CompressAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default)
        {
            using var output = new MemoryStream();
            await using (var gzip = new GZipStream(output, CompressionLevel.Fastest, true))
            {
                await gzip.WriteAsync(data, ct);
            }
            return output.ToArray();
        }

        public async Task<byte[]> DecompressAsync(ReadOnlyMemory<byte> compressedData, CancellationToken ct = default)
        {
            using var input = new MemoryStream(compressedData.ToArray());
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            await gzip.CopyToAsync(output, ct);
            return output.ToArray();
        }

        public bool IsCompressed(ReadOnlySpan<byte> data)
        {
            return data.Length >= 2 && data[0] == 0x1F && data[1] == 0x8B;
        }
    }
}

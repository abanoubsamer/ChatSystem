using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Compression
{
    public interface IMessageCompressor
    {
        /// <summary>
        /// Compresses data using the configured algorithm
        /// </summary>
        Task<byte[]> CompressAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Decompresses data using the configured algorithm
        /// </summary>
        Task<byte[]> DecompressAsync(ReadOnlyMemory<byte> compressedData, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if data is compressed based on magic bytes
        /// </summary>
        bool IsCompressed(ReadOnlySpan<byte> data);
    }
}

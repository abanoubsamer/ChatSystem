using MessagePack;
using MessagePack.Resolvers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Serialization
{
    public static class MessageSerializer
    {
        private static readonly MessagePackSerializerOptions _options =
            MessagePackSerializerOptions.Standard
                .WithResolver(ContractlessStandardResolver.Instance)
                .WithSecurity(MessagePackSecurity.TrustedData);
        public static byte[] Serialize<T>(T value)
        {
            return MessagePackSerializer.Serialize(value, _options);
        }

        // ✅ للـ synchronous deserialization
        public static T Deserialize<T>(ReadOnlyMemory<byte> data)
        {
            return MessagePackSerializer.Deserialize<T>(data, _options);
        }

        public static T Deserialize<T>(byte[] data)
        {
            return MessagePackSerializer.Deserialize<T>(data, _options);
        }

     
        public static async Task SerializeAsync<T>(Stream stream, T value, CancellationToken cancellationToken = default)
        {
            await MessagePackSerializer.SerializeAsync(stream, value, _options, cancellationToken);
        }

        public static async Task<T> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default)
        {
            return await MessagePackSerializer.DeserializeAsync<T>(stream, _options, cancellationToken);
        }

      
        public static object Deserialize(Type type, ReadOnlyMemory<byte> data)
        {
            return MessagePackSerializer.Deserialize(type, data, _options);
        }
    }
}

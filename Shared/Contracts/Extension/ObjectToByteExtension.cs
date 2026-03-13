using MessagePack;
using MessagePack.Resolvers;

namespace Contracts.Extension
{
    public static class ObjectToByteExtension
    {
        private static readonly MessagePackSerializerOptions _options =
            MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);

        public static byte[] ToByteArray(this object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj), "Object cannot be null");
            }

            return MessagePackSerializer.Serialize(obj, _options);
        }
    }
}

using MessagePack;
using MessagePack.Resolvers;

namespace Infrastructure.Extension
{
    public static class ObjectToByteExtension
    {
        // اعمله static readonly مرة واحدة بس
        private static readonly MessagePackSerializerOptions _options =
            MessagePackSerializerOptions.Standard
                .WithResolver(ContractlessStandardResolver.Instance);

        public static byte[] ToByteArray(this object obj)
        {
            return MessagePackSerializer.Serialize(obj, _options); 
        }
    }
}

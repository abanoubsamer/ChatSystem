using MessagePack;
using MessagePack.Resolvers;

namespace Infrastructure.Extension
{
    public static class ObjectToByteExtension
    {

        public static byte[] ToByteArray(this object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj), "Object cannot be null");
            }
          
          
            var options = MessagePackSerializerOptions.Standard
                              .WithResolver(ContractlessStandardResolver.Instance);
                             
            return MessagePackSerializer.Serialize(obj, options);
        }

       
        public static T FromByteArray<T>(this byte[] byteArray)
        {
            if (byteArray == null)
                throw new ArgumentNullException(nameof(byteArray), "Byte array cannot be null");

            var options = MessagePackSerializerOptions.Standard
                .WithResolver(ContractlessStandardResolver.Instance);

            return MessagePackSerializer.Deserialize<T>(byteArray, options);
        }
     
    }
}

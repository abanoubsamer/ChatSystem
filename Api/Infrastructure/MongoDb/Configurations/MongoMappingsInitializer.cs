using System.Reflection;

namespace Infrastructure.MongoDb.Configurations
{
    public static class MongoMappingsInitializer
    {
        public static void RegisterAll()
        {
            var types = typeof(MongoMappingsInitializer).Assembly.GetTypes()
                .Where(t => t.IsClass && t.Name.EndsWith("Mapping"));

            foreach (var type in types)
            {
                var method = type.GetMethod("Register", BindingFlags.Public | BindingFlags.Static);
                method?.Invoke(null, null);
            }
        }
    }
}

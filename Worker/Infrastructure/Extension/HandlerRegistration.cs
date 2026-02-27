
using Application.Abstractions.Handler.Ack;
using System.Reflection;



namespace Infrastructure.Extension
{
    public static class HandlerRegistration
    {
        public static Dictionary<string, IAckHandler> RegisterHandlers()
        {
            var handlers = new Dictionary<string, IAckHandler>();

            var handlerTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => typeof(IAckHandler).IsAssignableFrom(t) 
                    && !t.IsInterface && !t.IsAbstract);


            foreach (var type in handlerTypes)
            {
                var instance = (IAckHandler)Activator.CreateInstance(type);
                handlers[instance.ACK] = instance;
                Console.WriteLine($"Registered handler: {instance.ACK}");
            }

            return handlers;
        }
    }

}

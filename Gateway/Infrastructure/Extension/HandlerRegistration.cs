using Application.Abstractions.Handler.Methods;
using System.Reflection;



namespace Infrastructure.Extension
{
    public static class HandlerRegistration
    {
        public static Dictionary<string, IMethodHandler> RegisterHandlers()
        {
            var handlers = new Dictionary<string, IMethodHandler>();

            var handlerTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => typeof(IMethodHandler).IsAssignableFrom(t) 
                    && !t.IsInterface && !t.IsAbstract);


            foreach (var type in handlerTypes)
            {
                var instance = (IMethodHandler)Activator.CreateInstance(type);
                handlers[instance.MethodName] = instance;
                Console.WriteLine($"Registered handler: {instance.MethodName}");
            }

            return handlers;
        }
    }

}

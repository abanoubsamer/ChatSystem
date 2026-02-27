using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Consumers
{
    using Infrastructure.EventPipeline;
    using MassTransit;

    public class EventConsumer<TEvent> : IConsumer<TEvent> where TEvent : class
    {
        private readonly EventPipeline<TEvent> _pipeline;

        public EventConsumer(EventPipeline<TEvent> pipeline)
        {
            _pipeline = pipeline;
        }

        public Task Consume(ConsumeContext<TEvent> context)
            => _pipeline.ExecuteAsync(context.Message);
    }

}

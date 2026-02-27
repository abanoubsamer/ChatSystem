using Application.Abstractions.EventPipeline;


namespace Infrastructure.EventPipeline
{
    public class EventPipeline<TEvent>
    {
        private readonly IReadOnlyList<IEventPipelineStep<TEvent>> _steps;

        public EventPipeline(
            IEnumerable<IEventPipelineStep<TEvent>> steps)
        {
            _steps = steps.ToList();
        }

        public Task ExecuteAsync(TEvent evt)
        {
            var index = -1;

            Task Next()
            {
                index++;
                if (index < _steps.Count)
                    return _steps[index].HandleAsync(evt, Next);

                return Task.CompletedTask;
            }

            return Next();
        }
    }


}

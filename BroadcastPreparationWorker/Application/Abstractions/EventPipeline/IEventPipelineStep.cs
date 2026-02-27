using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.EventPipeline
{
    public interface IEventPipelineStep<TEvent>
    {
        Task HandleAsync(TEvent evt, Func<Task> next);
    }
}

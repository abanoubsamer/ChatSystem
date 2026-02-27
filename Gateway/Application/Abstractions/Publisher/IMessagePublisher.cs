using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Publisher
{
    public interface IMessagePublisher
    {
        Task PublishAsync<T>(T message);
        Task PublishBatchAsync(IEnumerable<object> events);
    }
}

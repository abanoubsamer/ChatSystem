using Application.Result;
using Domain.Models;


namespace Application.Abstractions.Repositories.Messages
{
    public interface IMessagesCommandRepository
    {
        public Task<Result<string>> AddNewMessageAsync(Message entity);
    }
}

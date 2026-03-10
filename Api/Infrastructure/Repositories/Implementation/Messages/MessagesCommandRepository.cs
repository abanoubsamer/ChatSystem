
using Application.Abstractions.Repositories.Messages;
using Application.Result;
using Domain.Models;
using Application.Abstractions.Repositories.GenaricRepo;
using Infrastructure.Repositories.GenaricRepo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories.Implementation.Messages
{
    public class MessagesCommandRepository : IMessagesCommandRepository
    {
        private readonly IGenaricRepository<Message> _repository;

        public MessagesCommandRepository(IGenaricRepository<Message> repository)
        {
            _repository = repository;
        }
        public async Task<Result<string>> AddNewMessageAsync(Message entity)
        {
            if (entity == null)
                return Result<string>.Fail("Message is null");
            try
            {
                await _repository.InsertAsync(entity);

                return Result<string>.Success("Message added successfully");
            }
            catch (Exception ex)
            {

                return Result<string>.Fail(ex.Message);
            }
        }
    }
}

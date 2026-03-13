using Application.Abstractions.Services;
using Application.Dtos.Stories;
using Contracts.Enums;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services
{
    public class StoryMediaService : IStoryMediaService
    {
        private readonly IConfiguration _configuration;
        private const string BaseUrl = "https://chatteststorage.blob.core.windows.net/stories";

        public StoryMediaService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

      

   
    }
}

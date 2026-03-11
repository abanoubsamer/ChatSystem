using Contracts.Enums;

namespace Application.Dtos.Stories
{
    public class UpdatePrivacySettingsRequest
    {
        public StoryPrivacy Privacy { get; set; }
        public List<string> HiddenFromUserIds { get; set; } = new List<string>();
        public List<string> AllowedUserIds { get; set; } = new List<string>();
    }
}

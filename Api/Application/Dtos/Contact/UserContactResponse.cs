namespace Application.Dtos.Contact
{
    public class UserContactResponse
    {
        public string ContactUserId { get; set; }
        public string ContactName { get; set; }
        public string ContactEmail { get; set; }
        public string? ContactAvater { get; set; }
        public bool IsBlocked { get; set; }
        public bool IsFavorite { get; set; }
        public DateTime AddedAt { get; set; }
    }
}

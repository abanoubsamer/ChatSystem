namespace Application.Dtos.Contact
{
    public class UserContactResponse
    {
        public string ContactUserId { get; set; }
        public string ContactName { get; set; }
        public bool IsBlocked { get; set; }
        public bool IsFavorite { get; set; }
        public DateTime AddedAt { get; set; }
    }
}

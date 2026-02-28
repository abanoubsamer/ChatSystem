namespace Application.Dtos.Contact
{
    public class UpdateContactDto
    {
        public string ContactUserId { get; set; }
        public string? ContactName { get; set; }
        public bool? IsBlocked { get; set; }
        public bool? IsFavorite { get; set; }
    }
}

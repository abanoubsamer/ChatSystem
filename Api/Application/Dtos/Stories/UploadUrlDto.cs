namespace Application.Dtos.Stories
{
    public class UploadUrlDto
    {
        public string UploadId { get; set; }
        public string PresignedUrl { get; set; }
        public string FinalMediaUrl { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}

namespace TaskManagementSystem.Models.DTOs
{
 
    public class AttachmentDto
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public DateTime UploadedAt { get; set; }
        public string UploadedBy { get; set; }
        public long FileSize { get; set; }  
    }
}
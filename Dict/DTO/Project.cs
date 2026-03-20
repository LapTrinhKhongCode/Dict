using Microsoft.AspNetCore.Mvc;

namespace Dict.DTO
{
    public class CreateProjectDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class UpdateProjectDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class ProjectDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int WorkspaceId { get; set; }
        public int CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public int MediaCount { get; set; }       // Số file PDF trong project
        public int VocabularyCount { get; set; }  // Số từ vựng
    }
}

namespace Dict.DTO
{
    public class MediaDtos
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string MimeType { get; set; }
        public long? SizeBytes { get; set; }
        public string StorageUrl { get; set; }
        public int OwnerId { get; set; }
        public string OwnerName { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
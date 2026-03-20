using Dict.DTO;
using Microsoft.AspNetCore.Mvc;

namespace Dict.Service.IService
{
    public interface IProjectService
    {
        Task<List<ProjectDto>> GetByWorkspaceAsync(int workspaceId, int userId);
        Task<ProjectDto> GetByIdAsync(int projectId, int userId);
        Task<ProjectDto> CreateAsync(int workspaceId, int userId, CreateProjectDto dto);
        Task<ProjectDto> UpdateAsync(int projectId, int userId, UpdateProjectDto dto);
        Task DeleteAsync(int projectId, int userId);

        // Media trong project
        Task<List<MediaDtos>> GetMediaAsync(int projectId, int userId);
        Task<MediaDtos> UploadMediaAsync(int projectId, int userId, IFormFile file);
        Task DeleteMediaAsync(int mediaId, int userId);
    }
}

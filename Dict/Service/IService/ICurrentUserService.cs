namespace Dict.Service.IService
{
    public interface ICurrentUserService
    {
        int UserId { get; }
        int WorkspaceId { get; }
    }
}
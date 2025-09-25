namespace Dict.Service.IService
{
    public interface ICommentService
    {
        Task<string?> GetCommentJson(string label);
    }
}

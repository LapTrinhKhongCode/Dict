namespace Dict.Service.IService;

    public interface IWordService
    {
        Task<string?> GetWordJson(string label);
    }


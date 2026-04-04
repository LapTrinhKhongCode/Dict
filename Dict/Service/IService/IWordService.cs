namespace Dict.Service.IService;

    public interface IWordService
    {
        Task<string?> GetWordJson(string label);
        Task UpsertCacheForLabelAsync(string label, string newJson, string category);
        Task<int> GetSearchMissCountAsync(string term);
        Task IncrementSearchMissAsync(string term);
}


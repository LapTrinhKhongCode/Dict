namespace Dict.Service.IService
{
    public interface IJsonBuilderService
    {
        Task<string> RebuildJsonForWordAsync(string label);    
        Task<string> RebuildJsonForKanjiAsync(string label);    
    }
}

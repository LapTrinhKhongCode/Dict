namespace Dict.Models
{
    public record SearchHitMessage(int EntryId);
    public record SearchMissMessage(string Term);
}

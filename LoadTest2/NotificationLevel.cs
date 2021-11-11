namespace ConsoleApp3
{
    public enum NotLevel
    {
        Emergency = 1,
        Alert = 2,
        Warning = 3,
        Info = 4,
    }

    public record NotificationLevel(int LevelId, string Code, string Name);
}

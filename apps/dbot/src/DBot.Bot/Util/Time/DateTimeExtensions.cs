namespace DBot.Bot.Util.Time;

public static class DateTimeExtensions
{
    public static string ToEmoji(this DateTime date)
    {
        return ClockFaceUtil.TimeToEmoji(date.Hour, date.Minute);
    }
}
namespace DBot.Bot.Util.Time;

public static class ClockFaceUtil
{
    private static readonly List<ClockFace> ClockFaces = new()
    {
        new ClockFace
        {
            Face = "🕛",
            Time = new List<string> { "12:00", "00:00" }
        },
        new ClockFace
        {
            Face = "🕧",
            Time = new List<string> { "12:30", "00:30" }
        },
        new ClockFace
        {
            Face = "🕐",
            Time = new List<string> { "13:00", "01:00" }
        },
        new ClockFace
        {
            Face = "🕜",
            Time = new List<string> { "13:30", "01:30" }
        },
        new ClockFace
        {
            Face = "🕑",
            Time = new List<string> { "14:00", "02:00" }
        },
        new ClockFace
        {
            Face = "🕝",
            Time = new List<string> { "14:30", "02:30" }
        },
        new ClockFace
        {
            Face = "🕒",
            Time = new List<string> { "15:00", "03:00" }
        },
        new ClockFace
        {
            Face = "🕞",
            Time = new List<string> { "15:30", "03:30" }
        },
        new ClockFace
        {
            Face = "🕓",
            Time = new List<string> { "16:00", "04:00" }
        },
        new ClockFace
        {
            Face = "🕟",
            Time = new List<string> { "16:30", "04:30" }
        },
        new ClockFace
        {
            Face = "🕔",
            Time = new List<string> { "17:00", "05:00" }
        },
        new ClockFace
        {
            Face = "🕠",
            Time = new List<string> { "17:30", "05:30" }
        },
        new ClockFace
        {
            Face = "🕕",
            Time = new List<string> { "18:00", "06:00" }
        },
        new ClockFace
        {
            Face = "🕡",
            Time = new List<string> { "18:30", "06:30" }
        },
        new ClockFace
        {
            Face = "🕖",
            Time = new List<string> { "19:00", "07:00" }
        },
        new ClockFace
        {
            Face = "🕢",
            Time = new List<string> { "19:30", "07:30" }
        },
        new ClockFace
        {
            Face = "🕗",
            Time = new List<string> { "20:00", "08:00" }
        },
        new ClockFace
        {
            Face = "🕣",
            Time = new List<string> { "20:30", "08:30" }
        },
        new ClockFace
        {
            Face = "🕘",
            Time = new List<string> { "21:00", "09:00" }
        },
        new ClockFace
        {
            Face = "🕤",
            Time = new List<string> { "21:30", "09:30" }
        },
        new ClockFace
        {
            Face = "🕙",
            Time = new List<string> { "22:00", "10:00" }
        },
        new ClockFace
        {
            Face = "🕥",
            Time = new List<string> { "22:30", "10:30" }
        },
        new ClockFace
        {
            Face = "🕚",
            Time = new List<string> { "23:00", "11:00" }
        },
        new ClockFace
        {
            Face = "🕦",
            Time = new List<string> { "23:30", "11:30" }
        }
    };

    public static string TimeToEmoji(int hours, int minutes)
    {
        var clockFace = ClockFaces
            .Find(cf => cf.Time.Any(t =>
            {
                var timeParts = t.Split(":");
                var hour = int.Parse(timeParts[0]);
                var minute = int.Parse(timeParts[1]);
                return ((minute == 30 && minutes is >= 15 and <= 45) ||
                        (minute == 0 && minutes is < 15 or > 45)) && hours == hour;
            }));

        return clockFace?.Face ?? ":face_with_peeking_eye:";
    }

    public static string DateToEmoji(DateTime date)
    {
        return TimeToEmoji(date.Hour, date.Minute);
    }
}
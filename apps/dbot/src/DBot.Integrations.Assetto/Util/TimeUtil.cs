using System.Globalization;

namespace DBot.Integrations.Assetto.Util;

public static class TimeUtil
{
    // Convert assetto timeofday to Minutes/Hour
    public static (int hours, int minutes, string formatted) ConvertTimeOfDayToHoursAndMinutes(int timeOfDay)
    {
        var hours = 0;
        var minutes = 0;

        if (timeOfDay >= -16) // PM
        {
            hours = 12 + (timeOfDay + 16) / 16;
            minutes = (timeOfDay + 16) % 16 * 15 / 4;
        }
        else // AM
        {
            hours = Math.Abs((-207 - timeOfDay) / 16);
            minutes = Math.Abs((-207 - timeOfDay) % 16 * 15 / 4);
        }

        var currentHour = hours.ToString().PadLeft(2, '0');
        var currentMinute = minutes.ToString(CultureInfo.CurrentCulture).PadLeft(2, '0');
        var formatted = $"{currentHour}:{currentMinute}";

        return (hours, minutes, formatted);
    }
}
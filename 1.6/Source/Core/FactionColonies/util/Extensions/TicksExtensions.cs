using RimWorld;

namespace FactionColonies.util
{
    public static class TicksExtensions
    {
        public static string ToTimeString(this int ticks)
        {
            string hours = $"{ticks / GenDate.TicksPerHour}";
            string fractionHours = $"{ticks % GenDate.TicksPerHour / ((float)GenDate.TicksPerHour)}";

            if (fractionHours.Length > 3)
            {
                fractionHours = fractionHours.Substring(2, 2);
            }
            else if (fractionHours.Length == 3)
            {
                fractionHours = fractionHours.Substring(2, 1) + "0";
            }

            while (hours.Length < 2) hours = "0" + hours;

            if (ticks < GenDate.TicksPerDay) return hours + "." + fractionHours + " hours";
            return GenDate.ToStringTicksToDays(ticks);
        }
    }
}

using System;
using Verse;

namespace FactionColonies
{
    /// <summary>
    /// Dynamic provider that shows the military cooldown formula
    /// with current stat values and dead pawn penalty.
    /// </summary>
    public class CodexProvider_MilitaryCooldown : ICodexDynamicProvider
    {
        public string GetDynamicContent(FactionFC faction)
        {
            bool deadPawnPenalty = FCSettings.deadPawnsIncreaseMilitaryCooldown;
            double baseDays = 3.0;

            string result = "FCCodexCooldownParams".Translate() + "\n\n";
            result += "FCCodexCooldownBase".Translate(baseDays) + "\n";
            result += "FCCodexCooldownDeadPawn".Translate(
                deadPawnPenalty ? "FCCodexEnabled".Translate().ToString() : "FCCodexDisabled".Translate().ToString()) + "\n\n";

            if (deadPawnPenalty)
            {
                // Base ticks per dead pawn, converted to hours
                double ticksPerDeath = 10000;
                double hoursPerDeath = ticksPerDeath / 2500.0;
                result += "FCCodexCooldownDeadPawnCalc".Translate(
                    Math.Round(hoursPerDeath, 1)) + "\n\n";

                // Example: 3 deaths
                double extraHours = 3 * hoursPerDeath;
                result += "FCCodexCooldownExample".Translate(
                    3,
                    Math.Round(extraHours, 1),
                    Math.Round(baseDays + extraHours / 24.0, 1));
            }

            return result;
        }
    }
}

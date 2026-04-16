using System;
using System.Linq;
using Verse;

namespace FactionColonies
{
    /// <summary>
    /// Dynamic provider that shows the prosperity target formula with live
    /// happiness/loyalty/unrest values for each settlement.
    /// </summary>
    public class CodexProvider_Prosperity : ICodexDynamicProvider
    {
        public string GetDynamicContent(FactionFC faction)
        {
            if (!faction.settlements.Any())
                return "FCCodexProsNoSettlements".Translate();

            string result = "FCCodexProsHeader".Translate() + "\n\n";

            foreach (WorldSettlementFC s in faction.settlements)
            {
                double target = s.GetProsperityTarget();
                double gain = s.GetProsperityGain();

                result += s.Name + ":\n";
                result += "  " + "FCCodexProsCurrent".Translate(Math.Round(s.prosperity, 1)) + "\n";
                result += "  " + "FCCodexProsTarget".Translate(
                    Math.Round(target, 1),
                    Math.Round(s.happiness, 1),
                    Math.Round(s.loyalty, 1),
                    Math.Round(100.0 - s.unrest, 1)) + "\n";
                result += "  " + "FCCodexProsDrift".Translate(Math.Round(gain, 2)) + "\n";
                result += "  " + "FCCodexProsProdMult".Translate(Math.Round(s.prosperity / 100.0, 2)) + "\n\n";
            }

            return result.TrimEnd();
        }
    }
}

using System;
using System.Linq;
using Verse;

namespace FactionColonies
{
    /// <summary>
    /// Dynamic provider that shows the current Empire Threat Level breakdown,
    /// including component contributions and the handicap cap.
    /// </summary>
    public class CodexProvider_ThreatLevel : ICodexDynamicProvider
    {
        public string GetDynamicContent(FactionFC faction)
        {
            if (!faction.settlements.Any())
                return "FCCodexETLNoSettlements".Translate();

            double avgLevel = faction.settlements.Average(s => (double)s.settlementLevel);
            int maxLevel = faction.settlements.Max(s => s.settlementLevel);
            double income = faction.income;
            int count = faction.settlements.Count;

            double avgFactor = (avgLevel - 1.0) * 0.2;
            double maxFactor = (maxLevel - 1.0) * 0.1;

            double etl = ThreatScalingUtil.ComputeEmpireThreatLevel(faction);
            double handicapCap = ThreatScalingUtil.ComputeHandicapCap(faction);

            string result = "FCCodexETLCurrent".Translate(Math.Round(etl, 2)) + "\n\n";
            result += "FCCodexETLBreakdown".Translate() + "\n";
            result += "  " + "FCCodexETLAvgLevel".Translate(Math.Round(avgLevel, 1), Math.Round(avgFactor * 0.35, 3)) + "\n";
            result += "  " + "FCCodexETLMaxLevel".Translate(maxLevel, Math.Round(maxFactor * 0.15, 3)) + "\n";
            result += "  " + "FCCodexETLIncome".Translate(Math.Round(income, 0)) + "\n";
            result += "  " + "FCCodexETLCount".Translate(count) + "\n\n";
            result += "FCCodexETLHandicapCap".Translate(Math.Round(handicapCap, 2)) + "\n";
            result += "FCCodexETLMaxSetting".Translate(FCSettings.maxThreatMultiplier);

            return result;
        }
    }
}

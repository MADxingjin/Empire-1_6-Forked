using System;
using System.Linq;
using Verse;
using RimWorld;

namespace FactionColonies.util
{
    /// <summary>
    /// Builds rich tooltip strings for UI elements, showing formulas
    /// and current values to help players understand game mechanics.
    /// </summary>
    public static class CodexTooltips
    {
        /// <summary>
        /// Enhanced tooltip for the faction-level prosperity stat in the overview panel.
        /// Explains what prosperity does and shows the average with per-settlement breakdown hint.
        /// </summary>
        public static string GetFactionProsperityTooltip(FactionFC faction)
        {
            string tip = "FCFactionProsperity".Translate() + "\n-----\n" + "FCFactionProsperityDesc".Translate();
            tip += "\n\n" + "FCCodexTipProsFormula".Translate();
            if (faction.settlements.Any())
            {
                double lowest = faction.settlements.Min(s => s.prosperity);
                double highest = faction.settlements.Max(s => s.prosperity);
                tip += "\n" + "FCCodexTipProsRange".Translate(
                    Math.Round(lowest, 0),
                    Math.Round(highest, 0));
            }
            return tip;
        }

        /// <summary>
        /// Enhanced tooltip for the faction-level happiness stat.
        /// </summary>
        public static string GetFactionHappinessTooltip(FactionFC faction)
        {
            string tip = "FCFactionHappiness".Translate() + "\n-----\n" + "FCFactionHappinessDesc".Translate();
            tip += "\n\n" + "FCCodexTipSocialDrift".Translate("FCHappiness".Translate());
            return tip;
        }

        /// <summary>
        /// Enhanced tooltip for the faction-level loyalty stat.
        /// </summary>
        public static string GetFactionLoyaltyTooltip(FactionFC faction)
        {
            string tip = "FCFactionLoyalty".Translate() + "\n-----\n" + "FCFactionLoyaltyDesc".Translate();
            tip += "\n\n" + "FCCodexTipSocialDrift".Translate("FCLoyality".Translate());
            return tip;
        }

        /// <summary>
        /// Enhanced tooltip for the faction-level unrest stat.
        /// </summary>
        public static string GetFactionUnrestTooltip(FactionFC faction)
        {
            string tip = "FCFactionUnrest".Translate() + "\n-----\n" + "FCFactionUnrestDesc".Translate();
            tip += "\n\n" + "FCCodexTipUnrestDrift".Translate();
            return tip;
        }

        /// <summary>
        /// Tooltip for the estimated profit display.
        /// Shows income vs upkeep breakdown.
        /// </summary>
        public static string GetProfitTooltip(FactionFC faction)
        {
            double income = Math.Round(faction.income, 0);
            double upkeep = Math.Round(faction.upkeep, 0);
            double profit = Math.Round(faction.profit, 0);

            string tip = "FCCodexTipProfitBreakdown".Translate(income, upkeep, profit);
            return tip;
        }

        /// <summary>
        /// Tooltip for the "Time till tax" display.
        /// Explains tax averaging and the current interval.
        /// </summary>
        public static string GetTaxTimerTooltip()
        {
            int days = FCSettings.timeBetweenTaxes / GenDate.TicksPerDay;
            string tip = "FCCodexTipTaxTimer".Translate(days);
            tip += "\n\n" + "FCCodexTipTaxAveraging".Translate();
            return tip;
        }

        /// <summary>
        /// Enhanced tooltip for the settlement military level stat.
        /// Appends targeting weight and ETL info to the existing military tooltip.
        /// </summary>
        public static string GetMilitaryTargetingInfo(WorldSettlementFC settlement)
        {
            int milLevel = settlement.settlementMilitaryLevel;
            string weight;
            if (milLevel <= 1) weight = "10";
            else if (milLevel <= 3) weight = "7";
            else if (milLevel <= 5) weight = "3";
            else weight = "1";

            FactionFC faction = FactionCache.FactionComp;
            double etl = 1.0;
            if (faction is object && faction.settlements.Any())
                etl = ThreatScalingUtil.ComputeEmpireThreatLevel(faction);

            return "\n\n" + "FCCodexTipMilTargeting".Translate(weight) +
                   "\n" + "FCCodexTipMilETL".Translate(Math.Round(etl, 2));
        }
    }
}

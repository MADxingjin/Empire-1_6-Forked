using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace FactionColonies
{
    public class FCPolicyBehavior_Egalitarian : FCPolicyBehavior
    {
        private Dictionary<int, TaxBreakData> taxBreaks = new Dictionary<int, TaxBreakData>();

        public override void OnSettlementCreated(FactionFC faction, WorldSettlementFC settlement)
        {
            settlement.happiness = 60;
        }

        public override double ModifyStat(FCStatDef stat, double currentValue, WorldSettlementFC settlement)
        {
            if (settlement == null) return currentValue;

            bool onTaxBreak = IsOnTaxBreak(settlement.Tile);

            // Happiness-based tax bonus
            if (stat == FCStatDefOf.taxBonusFlat)
            {
                double bonus = Math.Floor(settlement.happiness / 10);
                if (onTaxBreak) bonus -= 30;
                return currentValue + bonus;
            }

            // Tax break bonuses: +2 happiness, +2 prosperity
            if (onTaxBreak)
            {
                if (stat == FCStatDefOf.happinessGainedBase)
                    return currentValue + 2;
                if (stat == FCStatDefOf.prosperityBaseRecovery)
                    return currentValue + 2;
            }

            return currentValue;
        }

        public override string GetStatDescription(FCStatDef stat, WorldSettlementFC settlement)
        {
            if (settlement == null) return null;
            bool onTaxBreak = IsOnTaxBreak(settlement.Tile);

            if (stat == FCStatDefOf.taxBonusFlat)
            {
                double bonus = Math.Floor(settlement.happiness / 10);
                if (onTaxBreak) bonus -= 30;
                return TextUtil.ColorizeAdditiveBonus(bonus) + " - " + policy.def.LabelCap + "\n";
            }
            if (onTaxBreak)
            {
                if (stat == FCStatDefOf.happinessGainedBase)
                    return TextUtil.ColorizeAdditiveBonus(2) + " - " + policy.def.LabelCap + "\n";
                if (stat == FCStatDefOf.prosperityBaseRecovery)
                    return TextUtil.ColorizeAdditiveBonus(2) + " - " + policy.def.LabelCap + "\n";
            }
            return null;
        }

        public override IEnumerable<FloatMenuOption> GetSettlementActions(FactionFC faction, WorldSettlementFC settlement)
        {
            yield return new FloatMenuOption("FCGiveTaxBreak".Translate(), delegate
            {
                if (!IsOnTaxBreak(settlement.Tile))
                {
                    Find.WindowStack.Add(new FCWindow_Confirm(
                        "FCConfirmTaxBreak".Translate(),
                        () =>
                        {
                            var data = GetOrCreate(settlement.Tile);
                            data.startTick = Find.TickManager.TicksGame;
                            data.enabled = true;
                            settlement.InvalidateStatCache();
                            Messages.Message(
                                TranslatorFormattedStringExtensions.Translate("FCGivingTaxBreak", settlement.Name),
                                MessageTypeDefOf.NeutralEvent);
                        }));
                }
                else
                {
                    var data = GetOrCreate(settlement.Tile);
                    Messages.Message(
                        "FCAlreadyGivingTaxBreak".Translate(Math.Round(
                            (data.startTick + GenDate.TicksPerDay * 10 -
                                Find.TickManager.TicksGame) / (double)GenDate.TicksPerDay, 1)),
                        MessageTypeDefOf.RejectInput);
                }
            });
        }

        public override void Tick(FactionFC faction)
        {
            if (Find.TickManager.TicksGame % 250 != 0) return;
            int currentTick = Find.TickManager.TicksGame;
            foreach (var kvp in taxBreaks)
            {
                if (kvp.Value.enabled && (kvp.Value.startTick + GenDate.TicksPerDay * 10) <= currentTick)
                {
                    kvp.Value.enabled = false;
                    faction.ReturnSettlementByLocation(kvp.Key)?.InvalidateStatCache();
                }
            }
        }

        private bool IsOnTaxBreak(int tile) => taxBreaks.TryGetValue(tile, out var d) && d.enabled;

        private TaxBreakData GetOrCreate(int tile)
        {
            if (!taxBreaks.TryGetValue(tile, out var data))
            {
                data = new TaxBreakData();
                taxBreaks[tile] = data;
            }
            return data;
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref taxBreaks, "taxBreaks", LookMode.Value, LookMode.Deep);
            taxBreaks = taxBreaks ?? new Dictionary<int, TaxBreakData>();
        }

        // Debug accessors
        public int DebugTaxBreakCount() => taxBreaks.Count;
        public int DebugActiveTaxBreakCount() => taxBreaks.Count(kvp => kvp.Value.enabled);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FactionColonies
{
    public class FCPolicyExt_Egalitarian : FCPolicyModExtension
    {
        public override FCPolicyState CreateState() => new FCPolicyState_Egalitarian();

        public override void OnSettlementCreated(FactionFC faction, WorldSettlementFC settlement)
        {
            settlement.happiness = 60;
        }

        public override double ModifyTaxBonus(double bonus, WorldSettlementFC settlement)
        {
            bonus += Math.Floor(settlement.happiness / 10);
            var state = FactionCache.FactionComp.GetPolicyState<FCPolicyState_Egalitarian>();
            if (state != null && state.IsOnTaxBreak(settlement.Tile))
                bonus -= 30;
            return bonus;
        }

        public override double GetSettlementHappinessBonus(WorldSettlementFC settlement)
        {
            var state = FactionCache.FactionComp.GetPolicyState<FCPolicyState_Egalitarian>();
            return state != null && state.IsOnTaxBreak(settlement.Tile) ? 2 : 0;
        }

        public override double GetSettlementProsperityBonus(WorldSettlementFC settlement)
        {
            var state = FactionCache.FactionComp.GetPolicyState<FCPolicyState_Egalitarian>();
            return state != null && state.IsOnTaxBreak(settlement.Tile) ? 2 : 0;
        }

        public override IEnumerable<FloatMenuOption> GetSettlementActions(FactionFC faction, WorldSettlementFC settlement)
        {
            yield return new FloatMenuOption("FCGiveTaxBreak".Translate(), delegate
            {
                var state = faction.GetPolicyState<FCPolicyState_Egalitarian>();
                if (state == null) return;

                if (!state.IsOnTaxBreak(settlement.Tile))
                {
                    Find.WindowStack.Add(new FCWindow_Confirm(
                        "FCConfirmTaxBreak".Translate(),
                        () =>
                        {
                            var data = state.GetOrCreate(settlement.Tile);
                            data.startTick = Find.TickManager.TicksGame;
                            data.enabled = true;
                            Messages.Message(
                                TranslatorFormattedStringExtensions.Translate("FCGivingTaxBreak", settlement.Name),
                                MessageTypeDefOf.NeutralEvent);
                        }));
                }
                else
                {
                    var data = state.GetOrCreate(settlement.Tile);
                    Messages.Message(
                        "FCAlreadyGivingTaxBreak".Translate(Math.Round(
                            (data.startTick + GenDate.TicksPerDay * 10 -
                                Find.TickManager.TicksGame) / (double)GenDate.TicksPerDay, 1)),
                        MessageTypeDefOf.RejectInput);
                }
            });
        }

        public override void Tick(FactionFC faction, FCPolicy policy)
        {
            var state = policy.state as FCPolicyState_Egalitarian;
            if (state == null) return;

            foreach (var kvp in state.taxBreaks.ToList())
            {
                if (kvp.Value.enabled &&
                    (kvp.Value.startTick + GenDate.TicksPerDay * 10) <= Find.TickManager.TicksGame)
                {
                    kvp.Value.enabled = false;
                }
            }
        }
    }
}

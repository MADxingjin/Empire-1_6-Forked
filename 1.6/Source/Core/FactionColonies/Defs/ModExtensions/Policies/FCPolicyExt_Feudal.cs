using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using FactionColonies.util;

namespace FactionColonies
{
    public class FCPolicyExt_Feudal : FCPolicyModExtension
    {
        public override FCPolicyState CreateState() => new FCPolicyState_Feudal();

        public override void ModifyBattlePenalties(ref double prosperityLoss, ref double happinessLoss, ref double loyaltyLoss)
        {
            loyaltyLoss *= 2;
        }

        public override double ModifyTitheMultiplier(double mult) => mult * 1.2;

        public override void Tick(FactionFC faction, FCPolicy policy)
        {
            var state = policy.state as FCPolicyState_Feudal;
            if (state == null) return;

            if (!state.canUseMercenary &&
                (state.tickLastUsedMercenary + GenDate.TicksPerSeason) <= Find.TickManager.TicksGame)
            {
                state.canUseMercenary = true;
                Find.LetterStack.ReceiveLetter("FCActionAvailable".Translate(),
                    "FCActionMercenaryRefreshed".Translate(), LetterDefOf.PositiveEvent);
            }
        }

        public override IEnumerable<(TaggedString label, Action onClick)> GetMainTabActionButtons(FactionFC faction)
        {
            yield return ("FCRequestMercenary".Translate(), () =>
            {
                var state = faction.GetPolicyState<FCPolicyState_Feudal>();
                if (state == null) return;

                if (state.canUseMercenary)
                {
                    state.canUseMercenary = false;
                    state.tickLastUsedMercenary = Find.TickManager.TicksGame;

                    PawnGenerationRequest request = FCPawnGenerator.WorkerOrMilitaryRequest();
                    request.ColonistRelationChanceFactor = 20f;
                    Pawn pawn = PawnGenerator.GeneratePawn(request);

                    IncidentParms parms = new IncidentParms
                    {
                        target = Find.CurrentMap,
                        faction = FactionCache.PlayerColonyFaction,
                        points = 999,
                        raidArrivalModeForQuickMilitaryAid = true,
                        raidNeverFleeIndividual = true,
                        raidArrivalMode = PawnsArrivalModeDefOf.CenterDrop,
                        raidStrategy = RaidStrategyDefOf.ImmediateAttackFriendly
                    };
                    parms.raidArrivalModeForQuickMilitaryAid = true;

                    PawnsArrivalModeWorker_EdgeWalkIn worker = new PawnsArrivalModeWorker_EdgeWalkIn();
                    worker.TryResolveRaidSpawnCenter(parms);
                    worker.Arrive(new List<Pawn> { pawn }, parms);

                    Find.LetterStack.ReceiveLetter(
                        "FCMercenaryJoined".Translate(),
                        "FCMercenaryJoinedText".Translate(pawn.NameFullColored),
                        LetterDefOf.PositiveEvent,
                        new LookTargets(pawn));
                    pawn.SetFaction(Faction.OfPlayer);
                }
                else
                {
                    Messages.Message(
                        "FCActionMercenaryOnCooldown".Translate(
                            ((state.tickLastUsedMercenary + GenDate.TicksPerSeason) - Find.TickManager.TicksGame).ToTimeString()),
                        MessageTypeDefOf.RejectInput);
                }
            });
        }
    }
}

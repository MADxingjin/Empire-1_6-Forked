using System;
using RimWorld;
using Verse;

namespace FactionColonies
{
    public class FCPolicyExt_Pacifist : FCPolicyModExtension
    {
        public override FCPolicyState CreateState() => new FCPolicyState_Pacifist();

        public override bool BlocksAction(FCActionType action) =>
            action == FCActionType.CaptureSettlement ||
            action == FCActionType.RaidSettlement ||
            action == FCActionType.EnslaveSettlement ||
            action == FCActionType.DeployMilitary;

        public override bool SuppressMemberDeathPenalty() => true;

        public override int ModifyDeadPawnCooldownMultiplier(int multiplier) => multiplier + 2000;

        public override bool EnablesAction(FCActionType action) => action == FCActionType.SendDiplomat;

        public override bool HandleDiplomaticEnvoy(FactionFC faction, FCPolicy policy, Faction targetFaction)
        {
            var state = policy.state as FCPolicyState_Pacifist;
            int lastUsed = state?.tickLastUsedDiplomat ?? -1;

            if (Find.TickManager.TicksGame >= (lastUsed + GenDate.TicksPerDay * 5))
            {
                if (state != null)
                    state.tickLastUsedDiplomat = Find.TickManager.TicksGame;

                int random = Rand.Range(1, 10);
                if (random > 5)
                {
                    int relationImprovement = Rand.Range(5, 15);
                    targetFaction.TryAffectGoodwillWith(Find.FactionManager.OfPlayer, relationImprovement);
                    Find.LetterStack.ReceiveLetter("FCRelationImproved".Translate(),
                        "FCRelationImprovedText".Translate(targetFaction.Name, relationImprovement),
                        LetterDefOf.PositiveEvent);
                }
                else
                {
                    Find.LetterStack.ReceiveLetter("FCRelationNotImproved".Translate(),
                        "FCFailedToImproveRelationship".Translate(targetFaction.Name), LetterDefOf.NeutralEvent);
                }

                return true;
            }

            Messages.Message(
                "XDaysToSendDiplomat".Translate(Math.Round(
                    ((lastUsed + GenDate.TicksPerDay * 5) -
                     Find.TickManager.TicksGame).TicksToDays(), 1)), MessageTypeDefOf.RejectInput);
            return false;
        }
    }
}

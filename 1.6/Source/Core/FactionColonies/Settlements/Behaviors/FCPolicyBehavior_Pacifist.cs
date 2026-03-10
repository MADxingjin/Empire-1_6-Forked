using System;
using RimWorld;
using Verse;

namespace FactionColonies
{
    public class FCPolicyBehavior_Pacifist : FCPolicyBehavior
    {
        private CooldownAbility diplomatCooldown = new CooldownAbility
        {
            cooldownTicks = GenDate.TicksPerDay * 5
        };

        public override bool HandleDiplomaticEnvoy(FactionFC faction, Faction targetFaction)
        {
            if (!diplomatCooldown.IsReady)
            {
                Messages.Message(
                    "XDaysToSendDiplomat".Translate(Math.Round(diplomatCooldown.DaysRemaining, 1)),
                    MessageTypeDefOf.RejectInput);
                return false;
            }

            diplomatCooldown.Use();

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

        public override void ExposeData()
        {
            Scribe_Deep.Look(ref diplomatCooldown, "diplomatCooldown");
            diplomatCooldown = diplomatCooldown ?? new CooldownAbility { cooldownTicks = GenDate.TicksPerDay * 5 };
        }

        // Debug accessors
        public bool DebugCooldownReady() => diplomatCooldown.IsReady;
        public float DebugCooldownDays() => diplomatCooldown.DaysRemaining;
        public void DebugResetCooldown() => diplomatCooldown.tickLastUsed = -1;
    }
}

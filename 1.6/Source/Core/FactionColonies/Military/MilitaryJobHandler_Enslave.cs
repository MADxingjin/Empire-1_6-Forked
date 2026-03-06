using FactionColonies.util;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace FactionColonies
{
    public class MilitaryJobHandler_Enslave : MilitaryJobHandler
    {
        public override bool IsValidTarget(Faction targetFaction) => targetFaction?.def?.defName != "VFEI_Insect";

        public override void OnDeployed(WorldObjectComp_SettlementMilitary milComp, PlanetTile location, int timeToFinish, Faction enemy)
        {
            FactionFC factionfc = FactionCache.FactionComp;
            FCEvent evt = FCEventMaker.MakeEvent(FCEventDefOf.enslaveEnemySettlement);
            evt.customDescription = "settlementMilitaryForcesEnslave".Translate(milComp.WorldSettlement.Name, milComp.returnMilitaryTarget().Label);
            Find.LetterStack.ReceiveLetter("FCMilitaryAction".Translate(), "FCMilitarySentEnslave".Translate(milComp.WorldSettlement.Name, Find.WorldObjects.SettlementAt(location)), LetterDefOf.NeutralEvent);
            evt.DefineEvent(factionfc, milComp.WorldSettlement.Tile, timeToFinish);
        }

        public override bool OnResolved(WorldObjectComp_SettlementMilitary milComp)
        {
            FactionFC faction = FactionCache.FactionComp;
            int winner = SimulateBattleFc.FightBattle(
                militaryForce.createMilitaryForceFromSettlement(milComp.WorldSettlement, true),
                militaryForce.createMilitaryForceFromFaction(milComp.militaryEnemy, false));

            if (winner == 0)
            {
                faction.addExperienceToFactionLevel(5f);

                string text = "";

                int num = new IntRange(1, 3).RandomInRange;
                for (int i = 0; i <= num; i++)
                {
                    Pawn prisoner = PaymentUtil.generatePrisoner(milComp.militaryEnemy);
                    text += "PrisonerCaptureInfo".Translate(prisoner.Name.ToString(), milComp.WorldSettlement.Name) + "\n";
                    milComp.WorldSettlement.addPrisoner(prisoner);
                }

                Find.LetterStack.ReceiveLetter("RaidLoot".Translate(),
                    "RaidEnemySettlementSuccess".Translate(
                        Find.WorldObjects.SettlementAt(milComp.militaryLocation).LabelCap) + "\n" + text,
                    LetterDefOf.PositiveEvent, new LookTargets(Find.WorldObjects.SettlementAt(milComp.militaryLocation)));
            }
            else if (winner == 1)
            {
                Find.LetterStack.ReceiveLetter("RaidFailure".Translate(),
                    "RaidEnemySettlementFailure".Translate(
                        Find.WorldObjects.SettlementAt(milComp.militaryLocation).LabelCap), LetterDefOf.NegativeEvent,
                    new LookTargets(Find.WorldObjects.SettlementAt(milComp.militaryLocation)));
            }

            return winner == 0;
        }
    }
}

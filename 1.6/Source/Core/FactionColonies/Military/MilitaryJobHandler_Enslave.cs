using FactionColonies.util;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace FactionColonies
{
    public class MilitaryJobHandler_Enslave : MilitaryJobHandler
    {
        public override bool IsValidTarget(Faction targetFaction) => targetFaction?.def?.defName != "Insect";

        public override void OnDeployed(WorldObjectComp_SettlementMilitary milComp, PlanetTile location, int timeToFinish, Faction enemy)
        {
            FactionFC factionfc = FactionCache.FactionComp;
            FCEvent evt = FCEventMaker.MakeEvent(FCEventDefOf.enslaveEnemySettlement);
            evt.customDescription = "settlementMilitaryForcesEnslave".Translate(milComp.WorldSettlement.Name, milComp.ReturnMilitaryTarget().Label);
            Settlement target = Find.WorldObjects.SettlementAt(location);
            Find.LetterStack.ReceiveLetter("FCMilitaryAction".Translate(), "FCMilitarySentEnslave".Translate(milComp.WorldSettlement.Name, target?.LabelCap ?? (TaggedString)""), LetterDefOf.NeutralEvent);
            evt.DefineEvent(factionfc, milComp.WorldSettlement.Tile, timeToFinish);
        }

        public override BattleResult OnResolved(WorldObjectComp_SettlementMilitary milComp)
        {
            FactionFC faction = FactionCache.FactionComp;

            Settlement target = Find.WorldObjects.SettlementAt(milComp.militaryLocation);
            if (target == null)
            {
                LogUtil.Warning("Military enslave target at tile " + milComp.militaryLocation + " no longer exists");
                return new BattleResult();
            }

            BattleResult result = SimulateBattleFc.FightBattle(
                militaryForce.CreateMilitaryForceFromSettlement(milComp.WorldSettlement, true),
                militaryForce.CreateMilitaryForceFromFaction(milComp.militaryEnemy, false));

            if (result.AttackerVictory)
            {
                faction.AddExperienceToFactionLevel(5f);

                string text = "";

                int num = new IntRange(1, 3).RandomInRange;
                for (int i = 0; i <= num; i++)
                {
                    Pawn prisoner = PaymentUtil.GeneratePrisoner(milComp.militaryEnemy);
                    text += "PrisonerCaptureInfo".Translate(prisoner.Name.ToString(), milComp.WorldSettlement.Name) + "\n";
                    milComp.WorldSettlement.AddPrisoner(prisoner);
                }

                Find.LetterStack.ReceiveLetter("RaidLoot".Translate(),
                    "RaidEnemySettlementSuccess".Translate(
                        target.LabelCap) + "\n" + text,
                    LetterDefOf.PositiveEvent, new LookTargets(target));
            }
            else if (result.DefenderVictory)
            {
                Find.LetterStack.ReceiveLetter("RaidFailure".Translate(),
                    "RaidEnemySettlementFailure".Translate(
                        target.LabelCap), LetterDefOf.NegativeEvent,
                    new LookTargets(target));
            }

            return result;
        }
    }
}

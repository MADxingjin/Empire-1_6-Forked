using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace FactionColonies
{
    [HarmonyPatch(typeof(JobDriver_Goto), "TryExitMap")]
    public class Patch
    {
        static bool Prefix(ref JobDriver_Goto __instance)
        {
            Pawn pawn = __instance.pawn;
            if (!(pawn.Map?.Parent is WorldSettlementFC settlement)) return true;

            var military = settlement.MilitaryComp;
            if (military == null || !military.isUnderAttack) return true;

            // Allow supporting caravan pawns (player's own colonists) to exit
            foreach (var cs in military.supporting)
            {
                if (cs.pawns.Contains(pawn)) return true;
            }

            // Block all non-supporting defenders (mercenary or generated)
            if (military.defenders.Contains(pawn)) return false;

            return true;
        }
    }

    [HarmonyPatch(typeof(JobGiver_ExitMap), "TryGiveJob")]
    class Patch_JobGiver_ExitMap
    {
        static bool Prefix(Pawn pawn, ref Job __result)
        {
            if (!(pawn.Map?.Parent is WorldSettlementFC settlement)) return true;
            var military = settlement.MilitaryComp;
            if (military == null || !military.isUnderAttack) return true;
            if (military.defenders.Contains(pawn))
            {
                __result = null;
                return false;
            }
            return true;
        }
    }
}

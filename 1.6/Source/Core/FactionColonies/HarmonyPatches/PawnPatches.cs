using FactionColonies.util;
using HarmonyLib;
using Verse;

namespace FactionColonies
{
    [HarmonyPatch(typeof(Pawn), "Kill")]
    class MercenaryDied
    {
        static bool Prefix(Pawn __instance)
        {
            if (__instance.IsMercenary())
            {
                if (__instance.Faction != FactionCache.PlayerColonyFaction) __instance.SetFaction(FactionCache.PlayerColonyFaction);
                MercenarySquadFC squad = FactionCache.FactionComp.militaryCustomizationUtil.returnSquadFromUnit(__instance);
                if (squad != null)
                {
                    Mercenary merc = FactionCache.FactionComp.militaryCustomizationUtil.returnMercenaryFromUnit(__instance, squad);
                    if (merc != null)
                    {
                        if (squad.settlement != null)
                        {
                            if (FCSettings.deadPawnsIncreaseMilitaryCooldown)
                            {
                                squad.dead += 1;
                            }

                            squad.settlement.happiness -= 1;
                        }

                        squad.PassPawnToDeadMercenaries(merc);
                    }

                    squad.removeDroppedEquipment();
                }
                else
                {
                    LogUtil.Warning("Mercenary Errored out. Did not find squad.");
                }

                __instance.equipment?.DestroyAllEquipment();
                __instance.apparel?.DestroyAll();
                //__instance.Destroy();
                return true;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(DeathActionWorker_Simple), "PawnDied")]
    class MercenaryAnimalDied
    {
        static bool Prefix(Corpse corpse)
        {
            if (FactionCache.FactionComp.militaryCustomizationUtil.IsMercenaryPawn(corpse.InnerPawn))
            {
                //corpse.InnerPawn.SetFaction(FactionColonies.getPlayerColonyFaction());
                corpse.Destroy();
                return false;
            }

            return true;
        }
    }

    // [HarmonyPatch(typeof(JobGiver_AnimalFlee), "TryGiveJob")]
    class TryGiveJobFleeAnimal
    {
        static bool Prefix(Pawn pawn)
        {
            if (FactionCache.FactionComp.militaryCustomizationUtil.IsMercenaryPawn(pawn))
            {
                return false;
            }

            return true;
        }
    }

}

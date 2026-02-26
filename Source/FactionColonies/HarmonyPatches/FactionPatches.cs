using FactionColonies.util;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace FactionColonies
{//stops friendly faction from being a group source
    [HarmonyPatch(typeof(IncidentWorker_RaidFriendly), "TryResolveRaidFaction")]
    class RaidFriendlyStopSettlementFaction
    {
        static void Postfix(ref IncidentWorker_RaidFriendly __instance, ref bool __result, IncidentParms parms)
        {
            if (parms.faction == FactionCache.PlayerColonyFaction)
            {
                parms.faction = null;
                __result = false;
            }
        }
    }

    //Goodwill by distance to settlement
    [HarmonyPatch(typeof(SettlementProximityGoodwillUtility), "AppendProximityGoodwillOffsets")]
    class GoodwillPatch
    {
        static void Postfix(PlanetTile tile, List<Pair<Settlement, int>> outOffsets, bool ignoreIfAlreadyMinGoodwill, bool ignorePermanentlyHostile)
        {
            outOffsets.RemoveAll(pair => pair.First.Faction == FactionCache.PlayerColonyFaction);
        }
    }

    //CheckReachNaturalGoodwill()
    [HarmonyPatch(typeof(Faction), "CheckReachNaturalGoodwill")]
    class GoodwillPatchFunctionsGoodwillTendency
    {
        static bool Prefix(ref Faction __instance)
        {
            if (__instance == FactionCache.PlayerColonyFaction)
            {
                return false;
            }

            return true;
        }
    }

    //tryAffectGoodwillWith
    [HarmonyPatch(typeof(Faction), "TryAffectGoodwillWith")]
    class GoodwillPatchFunctionsGoodwillAffect
    {
        static bool Prefix(ref Faction __instance, Faction other, int goodwillChange, bool canSendMessage = true,
            bool canSendHostilityLetter = true, HistoryEventDef reason = null, GlobalTargetInfo? lookTarget = null)
        {
            if (__instance == FactionCache.PlayerColonyFaction && other == Find.FactionManager.OfPlayer)
            {
                if (reason == HistoryEventDefOf.RequestedTrader ||
                    reason == HistoryEventDefOf.GaveGift ||
                    reason == HistoryEventDefOf.Traded)
                {
                    return false;
                }

                return true;
            }

            return true;
        }
    }


    //Notify_MemberDied(Pawn member, DamageInfo? dinfo, bool wasWorldPawn, Map map)
    [HarmonyPatch(typeof(Faction), "Notify_MemberDied")]
    class GoodwillPatchFunctionsMemberDied
    {
        static bool Prefix(ref Faction __instance, Pawn member, DamageInfo? dinfo, bool wasWorldPawn, Map map)
        {
            if (member.Faction == FactionCache.PlayerColonyFaction && !wasWorldPawn &&
                !PawnGenerator.IsBeingGenerated(member) && map != null && map.IsPlayerHome &&
                !__instance.HostileTo(Faction.OfPlayer))
            {
                FactionFC faction = FactionCache.FactionComp;
                if (!faction.hasPolicy(FCPolicyDefOf.pacifist) && dinfo != null)
                {
                    if (dinfo.Value.Category == DamageInfo.SourceCategory.Collapse)
                    {
                        faction.GainUnrestForReason(new Message("DeathOfFactionPawn".Translate(), MessageTypeDefOf.PawnDeath), 5d);
                        faction.GainHappiness(-5d);
                    }
                    else if (dinfo.Value.Instigator?.Faction == Find.FactionManager.OfPlayer)
                    {
                        faction.GainUnrestForReason(new Message("DeathOfFactionPawn".Translate(), MessageTypeDefOf.PawnDeath), 5d);
                        faction.GainHappiness(-5d);
                    }
                }

                //return false to stop from continuing method
                return false;
            }

            return true;
        }
    }

    //Player traded
    [HarmonyPatch(typeof(Faction), "Notify_PlayerTraded")]
    class GoodwillPatchFunctionsPlayerTraded
    {
        static bool Prefix(ref Faction __instance, float marketValueSentByPlayer, Pawn playerNegotiator)
        {
            if (__instance == FactionCache.PlayerColonyFaction)
            {
                return false;
            }

            return true;
        }
    }

    //Player traded
    [HarmonyPatch(typeof(Faction), "Notify_MemberCaptured")]
    class GoodwillPatchFunctionsCapturedPawn
    {
        static bool Prefix(ref Faction __instance, Pawn member, Faction violator)
        {
            if (__instance == FactionCache.PlayerColonyFaction && violator == Faction.OfPlayer && !member.IsSlaveOfColony)
            {
                FactionFC faction = FactionCache.FactionComp;
                faction.GainUnrestForReason(new Message("CaptureOfFactionPawn".Translate(), MessageTypeDefOf.NegativeEvent), 15d);
                faction.GainHappiness(-10d);

                return false;
            }

            return true;
        }
    }

    //member exit map
 /*   [HarmonyPatch(typeof(Faction), "Notify_MemberExitedMap")]
    class GoodwillPatchFunctionsExitedMap
    {
        static bool Prefix(ref Faction __instance, Pawn member, bool free)
        {
            if (__instance.def.defName == "PColony")
            {
                return false;
            }

            return true;
        }
    }

    //member took damage
    [HarmonyPatch(typeof(Faction), "Notify_MemberTookDamage")]
    class GoodwillPatchFunctionsTookDamage
    {
        static bool Prefix(ref Faction __instance, Pawn member, DamageInfo dinfo)
        {
            if (__instance.def.defName == "PColony")
            {
                return false;
            }

            return true;
        }
    } */
} 

using FactionColonies.util;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;

namespace FactionColonies
{
    [HarmonyPatch(typeof(Pawn), "GetGizmos")]
    class PawnDraftGizmos
    {
        public static void Postfix(ref Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            // Early exit checks BEFORE any allocations - most pawns will exit here
            if (__result == null || __instance?.Faction == null || __instance.Map == null)
            {
                return;
            }

            WorldSettlementFC settlementFc = __instance.Map.Parent as WorldSettlementFC;
            if (settlementFc == null)
            {
                return;
            }

            Faction playerColonyFaction = FactionCache.PlayerColonyFaction;

            if (__instance.Faction == playerColonyFaction)
            {
                Pawn pawn = __instance;

                Command_Toggle draftColonists = new Command_Toggle
                {
                    hotKey = KeyBindingDefOf.Command_ColonistDraft,
                    isActive = () => false,
                    toggleAction = () =>
                    {
                        if (pawn.Faction == Faction.OfPlayer) return;
                        pawn.SetFaction(Faction.OfPlayer);
                        // SetFaction → AddAndRemoveDynamicComponents creates pawn.drafter for OfPlayer pawns
                        if (pawn.drafter != null)
                            pawn.drafter.Drafted = true;
                    },
                    defaultDesc = "CommandToggleDraftDesc".Translate(),
                    icon = TexCommand.Draft,
                    turnOnSound = SoundDefOf.DraftOn,
                    groupKey = 81729172,
                    defaultLabel = "CommandDraftLabel".Translate()
                };

                if (pawn.Downed)
                {
                    draftColonists.Disable("IsIncapped".Translate(pawn.LabelShort, pawn));
                }

                draftColonists.tutorTag = "Draft";
                __result = __result.Append(draftColonists);
                return;
            }

            if (__instance.Faction == Faction.OfPlayer && __instance.Drafted && settlementFc.MilitaryComp != null)
            {
                // Check if pawn is in a supporting caravan (avoid LINQ closure allocations)
                Pawn found = __instance;
                bool isSupporting = false;
                foreach (var caravan in settlementFc.MilitaryComp.supporting)
                {
                    if (caravan.pawns.Contains(found))
                    {
                        isSupporting = true;
                        break;
                    }
                }

                if (!isSupporting)
                {
                    // Only convert to list when we actually need to modify existing gizmos
                    List<Gizmo> output = __result.ToList();

                    foreach (Gizmo gizmo in output)
                    {
                        Command_Toggle action = gizmo as Command_Toggle;
                        if (action != null && action.hotKey == KeyBindingDefOf.Command_ColonistDraft)
                        {
                            action.toggleAction = () =>
                            {
                                found.SetFaction(FactionCache.PlayerColonyFaction);
                                // Re-add to defenders list and defense lord after undrafting
                                var milComp = settlementFc.MilitaryComp;
                                if (milComp != null && milComp.defenders.Any())
                                {
                                    if (!milComp.defenders.Contains(found))
                                        milComp.defenders.Add(found);

                                    var defenderLord = milComp.defenders[0].GetLord();
                                    if (defenderLord != null && !defenderLord.ownedPawns.Contains(found))
                                    {
                                        defenderLord.AddPawn(found);
                                        defenderLord.CurLordToil.UpdateAllDuties();
                                    }
                                }
                            };
                            break;
                        }
                    }

                    __result = output;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), "GetGizmos")]
    class PrisonerGizmosPatch
    {
        /// <summary>
        /// Checks if pawn is a valid prisoner that can be sent to settlements.
        /// Optimized to avoid expensive quest gizmo enumeration when possible.
        /// </summary>
        private static bool CanSendPrisoner(Pawn pawn)
        {
            // Fast checks first
            if (pawn.guest == null) return false;
            if (!pawn.guest.IsPrisoner) return false;
            if (!pawn.guest.PrisonerIsSecure) return false;

            // Only do expensive quest check if basic checks pass
            return QuestUtility.GetQuestRelatedGizmos(pawn).EnumerableNullOrEmpty();
        }

        /// <param name="prisoner"></param>
        /// <returns>A <c>Command_Action</c> that sends the selected <paramref name="prisoner"/> to an empire settlementFC.</returns>
        /// TODO: Replace this with needing to actually send the prisoner to the settlement via caravan or droppod? At the very least, the transfer shouldn't be instantaneous
        private static Command_Action SendPrisonerAction(Pawn prisoner) => new Command_Action
        {
            defaultLabel = "SendToSettlement".Translate(),
            defaultDesc = "",
            icon = TexLoad.iconMilitary,
            action = delegate
            {
                if (prisoner.Map.dangerWatcher.DangerRating != StoryDanger.None)
                {
                    Messages.Message("cantSendWithDangerLevel".Translate(prisoner.Map.dangerWatcher.DangerRating.ToString()), MessageTypeDefOf.RejectInput);
                    return;
                }

                List<FloatMenuOption> settlementList = FactionCache.FactionComp.settlements.Select(settlement => new FloatMenuOption("floatMenuOptionSendPrisonerToSettlement".Translate(settlement.Name, settlement.settlementLevel, settlement.prisonerList.Count()), delegate
                {
                    //disappear prisoner
                    TravelUtil.SendPrisoner(prisoner, settlement);

                    foreach (var bed in Find.Maps.Where(map => map.IsPlayerHome).SelectMany(map => map.listerBuildings.allBuildingsColonist).OfType<Building_Bed>().Where(bed => bed.OwnersForReading.Any(bedPawn => bedPawn == prisoner)))
                    {
                        bed.ForOwnerType = BedOwnerType.Colonist;
                        bed.ForOwnerType = BedOwnerType.Prisoner;
                    }
                })).ToList();

                Find.WindowStack.Add(new FloatMenu(settlementList));
            }
        };

        public static void Postfix(ref Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            // Early exit for non-prisoners (most common case) hmmmm
            if (__instance.guest == null || !__instance.guest.IsPrisoner)
            {
                return;
            }

            if (FactionCache.FactionComp is null) return;
            if (!FactionCache.FactionComp.IsActionAllowed(FCActionType.SendPrisoner)) return;
            if (!CanSendPrisoner(__instance)) return;

            __result = __result.Append(SendPrisonerAction(__instance));
        }
    }
}

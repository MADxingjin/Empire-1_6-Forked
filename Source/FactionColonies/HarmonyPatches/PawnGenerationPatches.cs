using FactionColonies.util;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace FactionColonies
{
    [HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", typeof(PawnGenerationRequest))]
    class PawnGenerationPatches
    {
        //TODO: LIKELY BUG: This will probably (inevitably) hijack the pawn creation for custom military units. Need to find a way to let those pawns respect the xenotype chosen by the player
        public static void Prefix(ref PawnGenerationRequest request)
        {
            if (!(request.Faction is null) && request.Faction == FactionCache.PlayerColonyFaction && request.KindDef?.IsHumanLikeRace() == true)
            {
                XenotypeFilter filter = FactionCache.FactionComp.xenotypeFilter;
                XenotypeDef chosenXenotype = null;
                CustomXenotype chosenCustomXenotype = null;

                filter.GetRandomXenotypeForRequest(request, out chosenXenotype, out chosenCustomXenotype);

                /* Modify the request to force our chosen xenotype */
                if (!(chosenXenotype is null))
                {
                    request.ForcedXenotype = chosenXenotype;
                    //Debug logging
                    LogUtil.Message($"GeneratePawn patch forced xenotype: {chosenXenotype.defName} for pawnKind: {request.KindDef.defName}");
                }
                else if (!(chosenCustomXenotype is null))
                {
                    request.ForcedCustomXenotype = chosenCustomXenotype;
                    //Debug logging
                    LogUtil.Message($"GeneratePawn patch forced custom xenotype: {chosenCustomXenotype.name} for pawnKind: {request.KindDef.defName}");
                }
                else
                {
                    //Debug Logging
                    LogUtil.Warning($"GeneratePawn patch failed to force a xenotype or custom xenotype for pawnKind: {request.KindDef.defName}");
                }
            }
        }
    }
}

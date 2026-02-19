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
    [HarmonyPatch(typeof(PawnGenerator), "GeneratePawn")]
    class PawnGenerationPatches
    {
        public static void Prefix(ref PawnGenerationRequest request)
        {
            if (request.Faction == FactionCache.PlayerColonyFaction && request.KindDef?.IsHumanLikeRace() == true)
            {
                //Debug logging
                LogUtil.Message("In GeneratePawn prefix for Empire faction humanlikes...");

                XenotypeFilter filter = FactionCache.FactionComp.xenotypeFilter;
                XenotypeDef chosenXenotype = null;
                CustomXenotype chosenCustomXenotype = null;

                filter.GetRandomXenotypeForRequest(request, out chosenXenotype, out chosenCustomXenotype);

                /* Modify the request to force our chosen xenotype */
                if (!(chosenXenotype is null))
                {
                    request.ForcedXenotype = chosenXenotype;
                    //Debug logging
                    LogUtil.Message($"GeneratePawn patch forced xenotype: {chosenXenotype.defName}");
                }
                else if (!(chosenCustomXenotype is null))
                {
                    request.ForcedCustomXenotype = chosenCustomXenotype;
                    //Debug logging
                    LogUtil.Message($"GeneratePawn patch forced custom xenotype: {chosenCustomXenotype.name}");
                }
            }
        }
    }
}

using RimWorld;
using System;
using Verse;

namespace FactionColonies.util
{
    class FCPawnGenerator
    {
        /// <summary>
        /// Generates a pawn with a specific forced xenotype that the PawnGenerationPatches prefix
        /// will respect (instead of overriding with the xenotype filter).
        /// Use this for designed military units where the player chose a specific xenotype.
        /// </summary>
        public static Pawn GenerateWithForcedXenotype(PawnGenerationRequest request)
        {
            PawnGenerationPatches.respectForcedXenotype = true;
            try
            {
                return PawnGenerator.GeneratePawn(request);
            }
            finally
            {
                PawnGenerationPatches.respectForcedXenotype = false;
            }
        }

        public static PawnGenerationRequest WorkerOrMilitaryRequest(PawnKindDef pawnKindDef = null, XenotypeDef xenotypeDef = null)
        {
            var kindDef = pawnKindDef;
            if (kindDef == null)
            {
                kindDef = PColonyPawnKindDefOf.PColony_Fighter;
            }

            // Get a safe age value
            float? fixedAge = null;
            try
            {
                fixedAge = kindDef.GetReasonableMercenaryAge();
            }
            catch (Exception ex)
            {
                LogUtil.Warning($"Failed to get reasonable age for {kindDef?.defName}: {ex.Message}");
                fixedAge = null;
            }

            return new PawnGenerationRequest(
                kind: kindDef,
                faction: FactionCache.PlayerColonyFaction,
                context: PawnGenerationContext.NonPlayer,
                tile: -1,
                forceGenerateNewPawn: false,
                allowDead: false,
                allowDowned: false,
                canGeneratePawnRelations: false,
                mustBeCapableOfViolence: true,
                colonistRelationChanceFactor: 0,
                forceAddFreeWarmLayerIfNeeded: false,
                allowGay: true,
                allowFood: true,
                allowAddictions: false,
                inhabitant: false,
                certainlyBeenInCryptosleep: false,
                forceRedressWorldPawnIfFormerColonist: false,
                worldPawnFactionDoesntMatter: true,
                biocodeWeaponChance: 0,
                extraPawnForExtraRelationChance: null,
                relationWithExtraPawnChanceFactor: 0,
                validatorPreGear: null,
                validatorPostGear: null,
                forcedTraits: null,
                prohibitedTraits: null,
                forcedXenotype: xenotypeDef,
                fixedBiologicalAge: fixedAge
            );
        }

        public static PawnGenerationRequest CivilianRequest(PawnKindDef pawnKindDef = null, XenotypeDef xenotypeDef = null)
        {
            var kindDef = pawnKindDef;
            if (kindDef == null)
            {
                kindDef = PColonyPawnKindDefOf.PColony_Villager;
            }

            float? fixedAge = null;
            try
            {
                fixedAge = kindDef.GetReasonableMercenaryAge();
            }
            catch (Exception ex)
            {
                LogUtil.Warning($"Failed to get reasonable age for {kindDef?.defName}: {ex.Message}");
                fixedAge = null;
            }

            return new PawnGenerationRequest(
                kind: kindDef,
                faction: FactionCache.PlayerColonyFaction,
                context: PawnGenerationContext.NonPlayer,
                tile: -1,
                forceGenerateNewPawn: false,
                allowDead: false,
                allowDowned: false,
                canGeneratePawnRelations: false,
                mustBeCapableOfViolence: false,
                colonistRelationChanceFactor: 0,
                forceAddFreeWarmLayerIfNeeded: false,
                allowGay: true,
                allowFood: true,
                allowAddictions: false,
                inhabitant: false,
                certainlyBeenInCryptosleep: false,
                forceRedressWorldPawnIfFormerColonist: false,
                worldPawnFactionDoesntMatter: false,
                biocodeWeaponChance: 0,
                extraPawnForExtraRelationChance: null,
                relationWithExtraPawnChanceFactor: 0,
                validatorPreGear: null,
                validatorPostGear: null,
                forcedTraits: null,
                prohibitedTraits: null,
                forcedXenotype: xenotypeDef,
                fixedBiologicalAge: fixedAge,
                fixedChronologicalAge: fixedAge
            );
        }

        public static PawnGenerationRequest AnimalRequest(PawnKindDef race)
        {
            return new PawnGenerationRequest(
                kind: race,
                faction: FactionCache.PlayerColonyFaction,
                context: PawnGenerationContext.NonPlayer,
                tile: -1,
                forceGenerateNewPawn: false,
                allowDead: false,
                allowDowned: false,
                canGeneratePawnRelations: false,
                mustBeCapableOfViolence: false,
                colonistRelationChanceFactor: 0,
                forceAddFreeWarmLayerIfNeeded: false,
                allowGay: true,
                allowFood: true,
                allowAddictions: false,
                inhabitant: false,
                certainlyBeenInCryptosleep: false,
                forceRedressWorldPawnIfFormerColonist: false,
                worldPawnFactionDoesntMatter: false,
                biocodeWeaponChance: 0,
                extraPawnForExtraRelationChance: null,
                relationWithExtraPawnChanceFactor: 0,
                validatorPreGear: null,
                validatorPostGear: null,
                forcedTraits: null,
                prohibitedTraits: null,
                fixedBiologicalAge: race.GetReasonableMercenaryAge()
            );
        }

        /// <summary>
        /// Generate a simple delivery pawn using the Empire's fighter template.
        /// </summary>
        public static PawnGenerationRequest SimpleDeliveryRequest()
        {
            return new PawnGenerationRequest(
                kind: PColonyPawnKindDefOf.PColony_Fighter,
                faction: FactionCache.PlayerColonyFaction,
                context: PawnGenerationContext.NonPlayer,
                tile: -1,
                forceGenerateNewPawn: false,
                allowDead: false,
                allowDowned: false,
                canGeneratePawnRelations: false,
                mustBeCapableOfViolence: true,
                colonistRelationChanceFactor: 0,
                forceAddFreeWarmLayerIfNeeded: false,
                allowGay: true,
                allowFood: true,
                allowAddictions: false,
                inhabitant: false,
                certainlyBeenInCryptosleep: false,
                forceRedressWorldPawnIfFormerColonist: false,
                worldPawnFactionDoesntMatter: false,
                biocodeWeaponChance: 0,
                extraPawnForExtraRelationChance: null,
                relationWithExtraPawnChanceFactor: 0,
                validatorPreGear: null,
                validatorPostGear: null,
                forcedTraits: null,
                prohibitedTraits: null
            );
        }
    }
}

using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace FactionColonies
{
    public class ResourceFilterExtension_Animals : ResourceFilterExtension
    {
        public override void SetFilter(ThingFilter filter, TechLevel techlevel)
        {
            List<PawnKindDef> allAnimalDefs = DefDatabase<PawnKindDef>.AllDefsListForReading;
            foreach (PawnKindDef def in allAnimalDefs)
            {
                if (def.IsAnimalAndAllowed())
                {
                    filter.SetAllow(def.race, true);
                }
            }
        }
        public override ThingSetMaker getThingSetMaker(out TechLevel tlevel)
        {
            tlevel = TechLevel.Undefined;
            return new ThingSetMaker_Animals();
        }
        public override List<Thing> generateSpecificThings(ThingDef thingDef, QualityCategory quality, ThingDef stuffDef, int quantity)
        {
            List<Thing> output = new List<Thing>();
            for(int i = 0; i < quantity; i++)
            {
                Pawn animalPawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(thingDef.race.AnyPawnKind, Faction.OfPlayer));
                if (!(animalPawn is null))
                {
                    output.Add(animalPawn);
                }
            }
            return output;
        }
    }
    /* ThingSetMaker_Animal is used by the Animal resource, so for organization purposes, I'm moving it here,
     * in the ResourceFilterExtension for animals */
    public class ThingSetMaker_Animals : ThingSetMaker
    {
        protected override void Generate(ThingSetMakerParams parms, List<Thing> outThings)
        {
            List<PawnKindDef> things = new List<PawnKindDef>();
            List<PawnKindDef> allAnimalDefs = DefDatabase<PawnKindDef>.AllDefsListForReading;

            foreach (PawnKindDef def in allAnimalDefs)
            {
                bool flag = def.race.race.Animal && def.RaceProps.IsFlesh &&
                            def.race.BaseMarketValue > parms.totalMarketValueRange.Value.min &&
                            def.race.tradeTags != null && !def.race.tradeTags.Contains("AnimalMonster") &&
                            !def.race.tradeTags.Contains("AnimalGenetic") &&
                            !def.race.tradeTags.Contains("AnimalAlpha");
                if (flag)
                {
                    things.Add(def);
                }
            }

        regen:
            PawnGenerationRequest request = new PawnGenerationRequest(kind: things.RandomElement<PawnKindDef>(),
                faction: Find.FactionManager.OfPlayer, context: PawnGenerationContext.NonPlayer, tile: -1,
                forceGenerateNewPawn: false, allowDead: false, allowDowned: false,
                canGeneratePawnRelations: true, mustBeCapableOfViolence: false, colonistRelationChanceFactor: 1f,
                forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowFood: true, allowAddictions: false,
                inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false,
                worldPawnFactionDoesntMatter: true, biocodeWeaponChance: 0, extraPawnForExtraRelationChance: null,
                relationWithExtraPawnChanceFactor: 1, validatorPreGear: null, validatorPostGear: null,
                forcedTraits: null, prohibitedTraits: null);
            Pawn pawn = PawnGenerator.GeneratePawn(request);


            if (pawn.MarketValue > parms.totalMarketValueRange.Value.max)
            {
                goto regen;
            }

            outThings.Add(pawn);
        }


        static List<PawnKindDef> allowedGeneratedList()
        {
            List<PawnKindDef> things = new List<PawnKindDef>();
            List<PawnKindDef> allAnimalDefs = DefDatabase<PawnKindDef>.AllDefsListForReading;

            foreach (PawnKindDef def in allAnimalDefs)
            {
                bool flag = def.race.race.Animal && def.RaceProps.IsFlesh && def.race.tradeTags != null &&
                            !def.race.tradeTags.Contains("AnimalMonster") &&
                            !def.race.tradeTags.Contains("AnimalGenetic") &&
                            !def.race.tradeTags.Contains("AnimalAlpha");
                if (flag)
                {
                    things.Add(def);
                }
            }

            return things;
        }

        protected override IEnumerable<ThingDef> AllGeneratableThingsDebugSub(ThingSetMakerParams parms)
        {
            List<ThingDef> list = new List<ThingDef>();
            foreach (PawnKindDef def in ThingSetMaker_Animals.allowedGeneratedList())
            {
                list.Add((def.race));
            }

            return list;
        }
    }
}

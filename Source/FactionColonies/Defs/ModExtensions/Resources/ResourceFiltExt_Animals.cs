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
        public override List<Thing> generateSpecificThings(ThingDef thingDef, int quantity, QualityCategory quality = QualityCategory.Normal, ThingDef stuffDef = null)
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
        private const int MAX_ATTEMPTS = 1000;
        private const int MAX_ATTEMPTS_FEW = 100;
        protected override void Generate(ThingSetMakerParams parms, List<Thing> outThings)
        {
            List<PawnKindDef> things = new List<PawnKindDef>();
            List<PawnKindDef> allAnimalDefs = DefDatabase<PawnKindDef>.AllDefsListForReading;

            float totalValue = 0;
            foreach (PawnKindDef def in allAnimalDefs)
            {
                if (parms.filter.Allows(def.race) &&
                    def.race.race.Animal && def.RaceProps.IsFlesh &&
                    def.race.BaseMarketValue >= parms.totalMarketValueRange.Value.min &&
                    def.race.tradeTags != null &&
                    !def.race.tradeTags.Contains("AnimalMonster") &&
                    !def.race.tradeTags.Contains("AnimalGenetic") &&
                    !def.race.tradeTags.Contains("AnimalAlpha"))
                {
                    things.Add(def);
                }
            }
            if (things.Count == 0)
            {
                LogUtil.Warning($"Attempted to generate things in ThingSetMaker_Animals, but no PawnKindDefs satisfied the criteria");
                return;
            }

            Pawn pawn;
            int totalAttempts = 0;
            int minCount = parms.countRange?.min ?? 0;
            int maxCount = parms.countRange?.max ?? MAX_ATTEMPTS;
            do
            {
                int attempts = 0;
                do
                {
                    PawnGenerationRequest request = new PawnGenerationRequest(kind: things.RandomElement(),
                                                                              faction: Find.FactionManager.OfPlayer,
                                                                              allowAddictions: false,
                                                                              worldPawnFactionDoesntMatter: true);
                    pawn = PawnGenerator.GeneratePawn(request);
                    attempts++;
                }
                while (pawn.MarketValue + totalValue > parms.totalMarketValueRange.Value.max && attempts < MAX_ATTEMPTS_FEW);

                if (attempts >= MAX_ATTEMPTS_FEW)
                {
                    LogUtil.Warning($"ThingSetMaker_Animals: Attempted to generate valid animal pawn {MAX_ATTEMPTS_FEW} times, but failed. Moving on");
                }
                else
                {
                    totalValue += pawn.MarketValue;
                    outThings.Add(pawn);
                }
                totalAttempts++;
            }
            while ((outThings.Count < minCount || totalValue < parms.totalMarketValueRange.Value.min) && // If we don't have enough things or value, keep going
                   !(outThings.Count >= maxCount || totalValue >= parms.totalMarketValueRange.Value.max) && // If we have too many things or too much value, stop
                   totalAttempts < MAX_ATTEMPTS); // Stop at max attempts
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
            foreach (PawnKindDef def in allowedGeneratedList())
            {
                list.Add((def.race));
            }

            return list;
        }
    }
}

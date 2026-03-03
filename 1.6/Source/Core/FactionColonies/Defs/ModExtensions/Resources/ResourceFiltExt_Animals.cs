using FactionColonies.util;
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
            foreach (PawnKindDef def in FactionCache.AllAnimalKindDefs)
            {
                filter.SetAllow(def.race, true);
            }
        }
        public override ThingSetMaker getThingSetMaker(out TechLevel tlevel)
        {
            tlevel = TechLevel.Undefined;
            return new ThingSetMaker_Animals();
        }
        public override List<Thing> generateSpecificThings(ThingDef thingDef, int quantity, QualityCategory quality = QualityCategory.Normal, ThingDef stuffDef = null)
        {
            // Only handle animal race ThingDefs. For regular items (animal products),
            // return null so the generic path in ResourceFC.cs handles them.
            if (thingDef.race == null)
            {
                return null;
            }

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
            // Build animal pawn pool
            List<PawnKindDef> animalDefs = new List<PawnKindDef>();
            foreach (PawnKindDef def in FactionCache.AllAnimalKindDefs)
            {
                if (parms.filter.Allows(def.race))
                {
                    animalDefs.Add(def);
                }
            }

            // Build animal product pool (non-race ThingDefs allowed by the filter)
            List<ThingDef> productDefs = new List<ThingDef>();
            foreach (ThingDef def in parms.filter.AllowedThingDefs)
            {
                if (def.race == null)
                {
                    productDefs.Add(def);
                }
            }

            int totalOptions = animalDefs.Count + productDefs.Count;
            if (totalOptions == 0)
            {
                LogUtil.Warning($"Attempted to generate things in ThingSetMaker_Animals, but no defs satisfied the criteria");
                return;
            }

            float totalValue = 0;
            int totalAttempts = 0;
            int minCount = parms.countRange?.min ?? 0;
            int maxCount = parms.countRange?.max ?? MAX_ATTEMPTS;
            float maxBudget = parms.totalMarketValueRange.Value.max;

            do
            {
                // Randomly decide: animal or product, weighted by pool size
                bool pickAnimal = animalDefs.Count > 0
                    && (productDefs.Count == 0 || Rand.Range(0, totalOptions) < animalDefs.Count);

                if (pickAnimal)
                {
                    // Generate animal pawn
                    Pawn pawn = null;
                    int attempts = 0;
                    do
                    {
                        PawnGenerationRequest request = new PawnGenerationRequest(kind: animalDefs.RandomElement(),
                                                                                  faction: Find.FactionManager.OfPlayer,
                                                                                  allowAddictions: false,
                                                                                  worldPawnFactionDoesntMatter: true);
                        pawn = PawnGenerator.GeneratePawn(request);
                        attempts++;
                    }
                    while (pawn.MarketValue + totalValue > maxBudget && attempts < MAX_ATTEMPTS_FEW);

                    if (attempts >= MAX_ATTEMPTS_FEW)
                    {
                        LogUtil.Warning($"ThingSetMaker_Animals: Attempted to generate valid animal pawn {MAX_ATTEMPTS_FEW} times, but failed. Moving on");
                    }
                    else
                    {
                        totalValue += pawn.MarketValue;
                        outThings.Add(pawn);
                    }
                }
                else if (productDefs.Count > 0)
                {
                    // Generate animal product
                    ThingDef productDef = productDefs.RandomElement();
                    float remainingBudget = maxBudget - totalValue;

                    if (productDef.BaseMarketValue <= remainingBudget)
                    {
                        int stackCount = Math.Max(1, Math.Min(
                            (int)(remainingBudget / productDef.BaseMarketValue),
                            productDef.stackLimit));

                        Thing thing = ThingMaker.MakeThing(productDef);
                        thing.stackCount = stackCount;
                        totalValue += productDef.BaseMarketValue * stackCount;
                        outThings.Add(thing);
                    }
                }

                totalAttempts++;
            }
            while ((outThings.Count < minCount || totalValue < parms.totalMarketValueRange.Value.min) && // If we don't have enough things or value, keep going
                   !(outThings.Count >= maxCount || totalValue >= maxBudget) && // If we have too many things or too much value, stop
                   totalAttempts < MAX_ATTEMPTS); // Stop at max attempts
        }

        protected override IEnumerable<ThingDef> AllGeneratableThingsDebugSub(ThingSetMakerParams parms)
        {
            List<ThingDef> list = new List<ThingDef>();

            // Add animal race ThingDefs
            foreach (PawnKindDef def in FactionCache.AllAnimalKindDefs)
            {
                list.Add(def.race);
            }

            // Add non-race ThingDefs (animal products) from the filter
            if (parms.filter != null)
            {
                foreach (ThingDef def in parms.filter.AllowedThingDefs)
                {
                    if (def.race == null)
                    {
                        list.Add(def);
                    }
                }
            }

            return list;
        }
    }
}

using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace FactionColonies
{
    public class ResourceFilterExtension_Animals : ResourceFilterExtension
    {
        public override void SetFilter(ThingFilter filter, TechLevel techlevel, ResourceFC resource = null)
        {
            foreach (PawnKindDef def in FactionCache.AllAnimalKindDefs)
            {
                filter.SetAllow(def.race, true);
            }
        }
        public override ThingSetMaker GetThingSetMaker(out TechLevel tlevel, ResourceFC resource = null)
        {
            tlevel = TechLevel.Undefined;
            return new ThingSetMaker_Animals();
        }
        public override List<Thing> GenerateSpecificThings(ThingDef thingDef, int quantity, QualityCategory quality = QualityCategory.Normal, ThingDef stuffDef = null, ResourceFC resource = null)
        {
            // Only handle animal race ThingDefs. For regular items (animal products),
            // return null so the generic path in ResourceFC.cs handles them.
            if (thingDef.race == null)
            {
                return null;
            }

            List<Thing> output = new List<Thing>();
            for (int i = 0; i < quantity; i++)
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
        private const int MAX_ATTEMPTS = 200;
        private const int MAX_ATTEMPTS_FEW = 20;
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

            // Pre-compute cheapest animal market value to allow early budget-exhaustion exit.
            // Uses race.BaseMarketValue as a cheap estimate to avoid generating pawns just to check price.
            float cheapestAnimalValue = animalDefs.Count > 0
                ? animalDefs.Min(def => def.race.BaseMarketValue)
                : float.MaxValue;
            float cheapestProductValue = productDefs.Count > 0
                ? productDefs.Min(def => def.BaseMarketValue)
                : float.MaxValue;

            float totalValue = 0;
            int totalAttempts = 0;
            int minCount = parms.countRange?.min ?? 0;
            int maxCount = parms.countRange?.max ?? MAX_ATTEMPTS;
            float maxBudget = parms.totalMarketValueRange.Value.max;

            do
            {
                float remainingBudget = maxBudget - totalValue;

                // Early exit: nothing affordable remains
                if (remainingBudget < cheapestAnimalValue && remainingBudget < cheapestProductValue)
                {
                    break;
                }

                // Randomly decide: animal or product, weighted by pool size
                // Skip animals if budget can't afford even the cheapest one
                bool canAffordAnimal = animalDefs.Count > 0 && remainingBudget >= cheapestAnimalValue;
                bool canAffordProduct = productDefs.Count > 0 && remainingBudget >= cheapestProductValue;

                bool pickAnimal = canAffordAnimal
                    && (!canAffordProduct || Rand.Range(0, totalOptions) < animalDefs.Count);

                if (pickAnimal)
                {
                    // Generate animal pawn, destroying any over-budget pawns to prevent leaks
                    Pawn pawn = null;
                    int attempts = 0;
                    do
                    {
                        if (pawn != null)
                        {
                            // Destroy the previous over-budget pawn before generating a new one
                            pawn.Destroy();
                            pawn = null;
                        }
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
                        // Destroy the last over-budget pawn
                        if (pawn != null)
                        {
                            pawn.Destroy();
                        }
                    }
                    else
                    {
                        totalValue += pawn.MarketValue;
                        outThings.Add(pawn);
                    }
                }
                else if (canAffordProduct)
                {
                    // Generate animal product
                    ThingDef productDef = productDefs.RandomElement();

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

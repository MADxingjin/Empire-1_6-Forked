using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace FactionColonies
{
    public class ColonyStockGenerator : StockGenerator
    {
        private StockGenerator parent;
        private FactionFC faction = FactionCache.FactionComp;

        public ColonyStockGenerator(StockGenerator parent)
        {
            this.parent = parent;
        }
        
        public override IEnumerable<Thing> GenerateThings(PlanetTile forTile, Faction faction = null)
        {
            return parent.GenerateThings(forTile, faction).Where(thing => HandlesThingDef(thing.def));
        }

        public override bool HandlesThingDef(ThingDef thingDef)
        {
            return thingDef.techLevel <= faction.techLevel ||
                // This used to be "thingDef is StockGenerator_Techprints", but visual studio flagged it with a warning, due to "StockGenerator_Techprints" never being of type ThingDef.
                // which is true. The StockGenerator type isn't a ThingDef. So why did that check exist in the first place?? The condition would never be satisfied!
                // I've replaced it with the following techprint check, since I *think* this is what the original programmer meant to do. This will allow this codepath to actually run,
                //   though, whereas it previously never did. So keep an eye on this...
                   (ThingCategoryDefOf.Techprints.ContainedInThisOrDescendant(thingDef) && (!(thingDef.tradeTags?.Contains("ExoticMisc") ?? true)) && parent.HandlesThingDef(thingDef));
        }


        public override IEnumerable<string> ConfigErrors(TraderKindDef parentDef)
        {
            return parent.ConfigErrors(parentDef);
        }
        
        public override void ResolveReferences(TraderKindDef trader)
        {
            parent.ResolveReferences(trader);
        }
    }
}
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FactionColonies
{
    /// <summary>
    /// Shared row class used by both TileMutatorResourceExtension and
    /// TileLandmarkResourceExtension. Represents a single per-resource bonus entry.
    /// </summary>
    public class TileResourceBonus
    {
        public ResourceTypeDef resource;
        public double additive = 0;
        public double multiplier = 1;
        public string label;
    }

    /// <summary>
    /// DefModExtension attached to vanilla TileMutatorDefs via XML patch. Declares per-resource
    /// additive / multiplier bonuses that get applied to settlements founded on a tile carrying
    /// the mutator. Consumed by ResourceTypeDef.GetMutatorAdditives / GetMutatorMultipliers
    /// (preview + runtime share the same scan).
    /// </summary>
    public class TileMutatorResourceExtension : DefModExtension
    {
        public List<TileResourceBonus> bonuses = new List<TileResourceBonus>();
    }
}

using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FactionColonies
{
    public class MutatorResourceBonus
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
        public List<MutatorResourceBonus> bonuses = new List<MutatorResourceBonus>();
    }
}

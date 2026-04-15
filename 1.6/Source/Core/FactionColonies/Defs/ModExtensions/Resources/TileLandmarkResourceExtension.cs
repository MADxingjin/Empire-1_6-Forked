using System.Collections.Generic;
using Verse;

namespace FactionColonies
{
    /// <summary>
    /// DefModExtension attached to vanilla LandmarkDefs via XML patch. Declares per-resource
    /// additive / multiplier bonuses that get applied to settlements founded on a tile carrying
    /// the landmark. Odyssey-only; the Tile.Landmark accessor returns null when Odyssey is
    /// inactive, so non-Odyssey installs silently no-op. Consumed by
    /// ResourceTypeDef.GetLandmarkAdditives / GetLandmarkMultipliers (preview + runtime share
    /// the same scan).
    /// </summary>
    public class TileLandmarkResourceExtension : DefModExtension
    {
        public List<TileResourceBonus> bonuses = new List<TileResourceBonus>();
    }
}

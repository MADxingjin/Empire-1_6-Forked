using RimWorld.Planet;
using Verse;

namespace FactionColonies
{
    /// <summary>
    /// This class allows resources to specify conditional additives/multipliers.
    /// </summary>
    public abstract class ResourceProductionExtension : DefModExtension
    {
        public string extName;
        public string extDesc;
        public virtual double GetAdditiveBonus(PlanetTile tile, WorldSettlementFC settlement = null)
        {
            return 0;
        }
        public virtual double GetMultiplierBonus(PlanetTile tile, WorldSettlementFC settlement = null)
        {
            return 1;
        }
    }
}

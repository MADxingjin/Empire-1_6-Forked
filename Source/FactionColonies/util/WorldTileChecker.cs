using System.Linq;
using System.Text;
using RimWorld.Planet;
using Verse;

namespace FactionColonies.util
{
    public static class WorldTileChecker
    {
        public static bool IsValidTileForNewSettlement(PlanetTile tile, WorldSettlementDef settlementdef, StringBuilder reason = null)
        {
            if (tile == -1)
            {
                reason?.Append("selectedInvalidTile".Translate());
                return false;
            }

            if (!(settlementdef.GetModExtension<SettlementTypeExtension>().tileIsValidForSettlement(tile, reason)))
            {
                return false;
            }

            return true;
        }
    }
}

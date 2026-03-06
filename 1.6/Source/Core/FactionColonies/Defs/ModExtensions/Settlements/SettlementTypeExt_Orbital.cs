using FactionColonies;
using FactionColonies.util;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace FactionColonies
{
    public class SettlementTypeExtension_Orbital : SettlementTypeExtension
    {
        public int constructionDays = 8;

        private static string[] spaceLocations;
        private static string[] spaceKeywords;

        private static string[] GetSpaceLocations()
        {
            if (spaceLocations == null)
            {
                spaceLocations = new string[]
                {
                    "FCOrbitalLocation1".Translate(),
                    "FCOrbitalLocation2".Translate(),
                    "FCOrbitalLocation3".Translate(),
                    "FCOrbitalLocation4".Translate(),
                    "FCOrbitalLocation5".Translate(),
                    "FCOrbitalLocation6".Translate(),
                    "FCOrbitalLocation7".Translate()
                };
            }
            return spaceLocations;
        }

        private static string[] GetSpaceKeywords()
        {
            if (spaceKeywords == null)
            {
                spaceKeywords = new string[]
                {
                    "FCOrbitalKeyword1".Translate(),
                    "FCOrbitalKeyword2".Translate(),
                    "FCOrbitalKeyword3".Translate(),
                    "FCOrbitalKeyword4".Translate(),
                    "FCOrbitalKeyword5".Translate(),
                    "FCOrbitalKeyword6".Translate(),
                    "FCOrbitalKeyword7".Translate(),
                    "FCOrbitalKeyword8".Translate(),
                    "FCOrbitalKeyword9".Translate(),
                    "FCOrbitalKeyword10".Translate()
                };
            }
            return spaceKeywords;
        }

        public static void InvalidateCache()
        {
            spaceLocations = null;
            spaceKeywords = null;
        }

        public override string getSettlementName(string fallback = "Settlement")
        {
            string[] keywords = GetSpaceKeywords();

            // Get the base name using the same logic as regular settlements
            string baseName = base.getSettlementName("Orbital");

            // Get a random space keyword
            string spaceKeyword = keywords[Rand.Range(0, keywords.Length)];

            // Combine base name with space keyword
            return $"{baseName} {spaceKeyword}";
        }
        public override int getCreationCost()
        {
            int baseCost = 5000;

            //TODO: reconsider how these are priced. "25% discount" for the advanced orbital makes no damn sense when we add a "premimum" anyways
            switch (parentDef.defName)
            {
                case "WorldSettlementDef_Orbital":
                    return baseCost;
                case "WorldSettlementDef_Orbital_Logistics":
                    return baseCost + 2000;
                case "WorldSettlementDef_Orbital_Advanced":
                    return (int)(baseCost * 0.75f) + 3000; // 25% discount + premium
                case "WorldSettlementDef_Orbital_Glitter":
                    return (int)(baseCost * 0.75f) + 5000;
                default:
                    return baseCost;
            }
        }
        public override bool tileIsValidForSettlement(PlanetTile tile, StringBuilder reason = null)
        {
            var worldGrid = Find.WorldGrid;
            var existingObjectTiles = Find.WorldObjects.AllWorldObjects.Select(wo => wo.Tile).ToHashSet();

            if (existingObjectTiles.Contains(tile))
            {
                reason?.Append("OrbitalTileOccupied".Translate());
                return false;
            }
            return true;
        }
        /// <summary>
        /// Takes a tile, and then returns the correct corresponding orbital tile.
        /// </summary>
        /// <param name="tile"></param>
        /// <returns></returns>
        public override PlanetTile getTileForSettlement(PlanetTile tile)
        {
            var worldGrid = Find.WorldGrid;
            if (tile.Layer == worldGrid.Orbit)
            {
                return tile;
            }
            else
            {
                return new PlanetTile(tile.tileId, worldGrid.Orbit);
            }
        }
        public override int getCreationTime(PlanetTile destination)
        {
            int baseDays = constructionDays;

            switch (parentDef.defName)
            {
                case "WorldSettlementDef_Orbital":
                    return baseDays * GenDate.TicksPerDay;
                case "WorldSettlementDef_Orbital_Logistics":
                    return (baseDays + 5) * GenDate.TicksPerDay;
                case "WorldSettlementDef_Orbital_Advanced":
                    return (int)((baseDays + 8) * 0.75f * GenDate.TicksPerDay); // 25% faster due to research
                case "WorldSettlementDef_Orbital_Glitter":
                    return (int)((baseDays + 12) * 0.75f * GenDate.TicksPerDay);
                default:
                    return baseDays * GenDate.TicksPerDay;
            }
        }
        public override string getLocationText(WorldSettlementFC settlement)
        {
            string[] locations = GetSpaceLocations();
            // Use the settlement's tileid to deterministically select a location text
            int locationIndex = Math.Abs(settlement.Tile.tileId) % locations.Length;
            return locations[locationIndex];
        }
        public override TaxDeliveryMode getTaxDeliveryMode(bool canUseShuttle, PlanetTile sourceTile)
        {
            // Force drop pods or shuttles for orbital platform settlements
            if (sourceTile != PlanetTile.Invalid)
            {
                if (ModsConfig.RoyaltyActive && canUseShuttle)
                {
                    return TaxDeliveryMode.Shuttle;
                }
                return TaxDeliveryMode.DropPod;
            }

            return base.getTaxDeliveryMode(canUseShuttle, sourceTile);
        }
    }
}

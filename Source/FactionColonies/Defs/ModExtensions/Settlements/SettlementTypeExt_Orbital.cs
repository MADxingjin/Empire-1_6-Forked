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
        // Space-themed location text for orbital platforms - use deterministic selection based on settlement ID
        // Techdebt - Language support for this would be nice
        //TODO: localization keys
        //       also this just seems like a really weird way of doing this. Find a better way
        private static string[] spaceLocations = {
                                    "Orbiting in deep space",
                                    "Stationed in low orbit",
                                    "Floating in the emptiness of space",
                                    "Anchored in orbit",
                                    "Positioned in low orbit",
                                    "Suspended above the surface",
                                    "Deployed in orbital space"
                                          };
        public override string getSettlementName(string fallback = "Settlement")
        {
            //TODO: these should really be translation keys
            // Space-themed keywords to append to generated names
            string[] spaceKeywords = { "Space Station", "Station", "Satellite", "Solar Base", "Orbital Hub", "Space Platform", "Cosmic Station", "Stellar Base", "Void Station", "Astral Platform" };

            // Get the base name using the same logic as regular settlements
            string baseName = base.getSettlementName("Orbital");

            // Get a random space keyword
            string spaceKeyword = spaceKeywords[Rand.Range(0, spaceKeywords.Length)];

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
            PlanetTile orbitalTile = new PlanetTile(tile.tileId, worldGrid.Orbit);

            if (existingObjectTiles.Contains(orbitalTile))
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
            // Use the settlement's tileid to deterministically select a location text
            int locationIndex = Math.Abs(settlement.Tile.tileId) % spaceLocations.Length;
            return spaceLocations[locationIndex];
        }
        public override TaxDeliveryMode getTaxDeliveryMode(bool canUseShuttle, PlanetTile sourceTile)
        {
            // Force drop pods for orbital platform settlements
            if (sourceTile != PlanetTile.Invalid)
            {
                return TaxDeliveryMode.DropPod;
            }

            return base.getTaxDeliveryMode(canUseShuttle, sourceTile);
        }
    }
}

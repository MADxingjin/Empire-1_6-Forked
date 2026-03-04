using FactionColonies.util;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static System.Collections.Specialized.BitVector32;

namespace FactionColonies
{
    /// <summary>
    /// This extension for WorldSettlementDefs allows for control over various aspects of creating a new settlement.
    /// </summary>
    public class SettlementTypeExtension : DefModExtension
    {
        protected WorldSettlementDef parentDef;
        protected FactionFC localfaction = null;
        protected FactionFC faction
        {
            get
            {
                if (localfaction == null)
                {
                    localfaction = FactionCache.FactionComp;
                }
                return localfaction;
            }
        }

        public override void ResolveReferences(Def parentDef)
        {
            base.ResolveReferences(parentDef);
            LogUtil.Message($"Calling ResolveReferences in SettlementTypeExtension for def {parentDef.defName}");
            if (parentDef is WorldSettlementDef wpd)
            {
                this.parentDef = wpd;
            }
            else
            {
                LogUtil.Error($"SettlementTypeExtension has non-WorldSettlementDef parent {parentDef.defName}! Setting to default");
                this.parentDef = WorldSettlementDefOf.WorldSettlementDef_Surface;
            }
        }
        /// <summary>
        /// Determines if the given tile is a valid location for a new settlement of this type.
        /// </summary>
        /// <param name="tile">The PlanetTile to check.</param>
        /// <param name="reason">A string stating the reason this tile is not valid.</param>
        /// <returns>TRUE if the given tile is valid for settlement. FALSE otherwise.</returns>
        public virtual bool tileIsValidForSettlement(PlanetTile tile, StringBuilder reason = null)
        {
            if (!TileFinder.IsValidTileForNewSettlement(tile, reason)) return false;

            foreach (WorldSettlementFC settlement in Find.WorldObjects.AllWorldObjects.Where(obj => obj.GetType() == typeof(WorldSettlementFC)))
            {
                if (Find.WorldGrid.IsNeighborOrSame(settlement.Tile, tile))
                {
                    reason?.Append("FactionBaseAdjacent".Translate());
                    return false;
                }
            }
            /* The default settlement type can't be built on impassable mountains. If you want to change this, then you
             * can define a new settlement type with its own SettlementTypeExtension, and then override this function
             */
            if (tile.Tile?.hilliness == Hilliness.Impassable)
            {
                reason?.Append("ImpassableMountains".Translate(parentDef.LabelCap));
                return false;
            }
            if (parentDef.allowedBiomes?.Count > 0)
            {
                bool foundAllowedBiome = false;
                foreach (BiomeDef biome in tile.Tile.Biomes)
                {
                    if (parentDef.allowedBiomes.Contains(biome))
                    {
                        foundAllowedBiome = true;
                        break;
                    }
                }
                if (!foundAllowedBiome)
                {
                    reason?.Append("NotAllowedBiome".Translate(parentDef.LabelCap));
                    return false;
                }
            }
            if (parentDef.blockedBiomes?.Count > 0)
            {
                foreach (BiomeDef biome in tile.Tile.Biomes)
                {
                    if (parentDef.blockedBiomes.Contains(biome))
                    {
                        reason?.Append("NotAllowedBiome".Translate(parentDef.LabelCap));
                        return false;
                    }
                }
            }
            return true;
        }
        /// <summary>
        /// Takes a tile, and then returns the correct corresponding tile for this settlement type.
        /// <para>Meant to be used for settlement types that live on planet layers other than the surface.</para>
        /// </summary>
        /// <param name="tile"></param>
        /// <returns></returns>
        public virtual PlanetTile getTileForSettlement(PlanetTile tile)
        {
            var worldGrid = Find.WorldGrid;
            if (tile.Layer == worldGrid.Surface)
            {
                return tile;
            }
            else
            {
                return new PlanetTile(tile.tileId, worldGrid.Surface);
            }
        }

        public virtual string getSettlementName(string fallback = "Settlement")
        {
            Faction pfaction = FactionCache.PlayerColonyFaction;
            if (pfaction?.def.settlementNameMaker == null)
            {
                return fallback;
            }

            List<String> used = new List<string>();
            List<Settlement> settlements = Find.WorldObjects.Settlements;
            foreach (Settlement found in settlements)
            {
                used.Add(found.Name);
            }

            return NameGenerator.GenerateName(pfaction.def.factionNameMaker, used, true);
        }

        /// <summary>
        /// Called at the very beginning of createPlayerColonySettlement(), before any code has run.
        /// </summary>
        public virtual void preCreation(ref PlanetTile tile, ref WorldSettlementDef settlementType)
        {
        }

        /// <summary>
        /// Called at the very end of createPlayerColonySettlement(), after all code has run (but before the letter notification is sent).
        /// </summary>
        public virtual void postCreation(WorldSettlementFC settlement)
        {
        }

        /// <summary>
        /// Determines how much it costs to found a new settlement of this type.
        /// </summary>
        /// <returns>The cost of a new settlement.</returns>
        public virtual int getCreationCost()
        {
            if (faction == null)
            {
                return (int)FCSettings.silverToCreateSettlement;
            }
            else
            {
                return (int)(FCSettings.silverToCreateSettlement + (500 * (faction.settlements.Count() + faction.settlementCaravansList.Count())));
            }
        }
        /// <summary>
        /// Determines how long it takes to create this settlement.
        /// </summary>
        /// <returns></returns>
        public virtual int getCreationTime(PlanetTile destination)
        {
            return TravelUtil.ReturnTicksToArrive(faction.capitalLocation, destination);
        }

        public virtual string getLocationText(WorldSettlementFC settlement)
        {
            return "Located".Translate() + " " + settlement.Tile.Tile.hilliness.GetLabel() + " " + "LandOf".Translate() + " " + settlement.Tile.Tile.PrimaryBiome.LabelCap.ToLower();
        }

        public virtual TaxDeliveryMode getTaxDeliveryMode(bool canUseShuttle, PlanetTile sourceTile)
        {
            if (FCSettings.forcedTaxDeliveryMode != default)
            {
                return FCSettings.forcedTaxDeliveryMode;
            }

            if (FactionCache.TechTransportPods.IsFinished)
            {
                if (ModsConfig.RoyaltyActive && canUseShuttle)
                {
                    return TaxDeliveryMode.Shuttle;
                }
                return TaxDeliveryMode.DropPod;
            }
            return TaxDeliveryMode.Caravan;
        }

        /// <summary>
        /// Returns a description of the settlement's current level for display in the settlement window.
        /// </summary>
        public virtual string getSettlementLevelDesc(int level)
        {
            switch (level)
            {
                case 1:
                    return "FCTownLevel1".Translate();
                case 2:
                    return "FCTownLevel2".Translate();
                case 3:
                case 4:
                    return "FCTownLevel3".Translate();
                case 5:
                case 6:
                    return "FCTownLevel4".Translate();
                default:
                    return "FCTownLevel5".Translate();
            }
        }

        /// <summary>
        /// Called after a settlement's level changes (upgrade or delevel) and stats have been updated.
        /// </summary>
        public virtual void onUpgrade(WorldSettlementFC settlement, int oldLevel, int newLevel)
        {
        }

        /// <summary>
        /// Called before a settlement is removed from the world.
        /// </summary>
        public virtual void preDestruction(WorldSettlementFC settlement)
        {
        }

        /// <summary>
        /// Called at the start of tax collection, after pre-tax preparation (cache invalidation, resource pruning).
        /// </summary>
        public virtual void preTax(WorldSettlementFC settlement)
        {
        }

        /// <summary>
        /// Called at the end of tax collection, after all calculations are complete.
        /// </summary>
        public virtual void postTax(WorldSettlementFC settlement, int silverAmount, List<Thing> titheThings)
        {
        }
    }
}

using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace FactionColonies
{
    public enum TileField
    {
        Temperature,
        Rainfall,
        Swampiness,
        Elevation,
        Pollution,
        AnimalDensity,
        PlantDensityFactor,
        FishPopulationFactor,
        RiverDistance,
        RoadCount,
        HasAnyRiver,
    }

    public class TileFieldCurve
    {
        public TileField field;
        public SimpleCurve curve;
        public float defaultValue = 0f;

        public double Evaluate(PlanetTile tile)
        {
            if (curve is null || curve.PointsCount == 0) return defaultValue;
            float raw = ReadField(tile);
            return curve.Evaluate(raw);
        }

        private float ReadField(PlanetTile tile)
        {
            Tile t = tile.Tile;
            if (t is null) return defaultValue;

            switch (field)
            {
                case TileField.Temperature:
                    return t.temperature;
                case TileField.Rainfall:
                    return t.rainfall;
                case TileField.Swampiness:
                    return t.swampiness;
                case TileField.Elevation:
                    return t.elevation;
                case TileField.Pollution:
                    return t.pollution;
                case TileField.AnimalDensity:
                    return t.AnimalDensity;
                case TileField.PlantDensityFactor:
                    return t.PlantDensityFactor;
                case TileField.FishPopulationFactor:
                    return t.FishPopulationFactor;
                case TileField.RiverDistance:
                {
                    SurfaceTile st = t as SurfaceTile;
                    return st is object ? st.riverDist : defaultValue;
                }
                case TileField.RoadCount:
                {
                    SurfaceTile st = t as SurfaceTile;
                    if (st is null) return 0f;
                    List<SurfaceTile.RoadLink> roads = st.Roads;
                    return roads is object ? roads.Count : 0f;
                }
                case TileField.HasAnyRiver:
                {
                    SurfaceTile st = t as SurfaceTile;
                    if (st is null) return 0f;
                    List<SurfaceTile.RiverLink> rivers = st.Rivers;
                    return (rivers is object && rivers.Count > 0) ? 1f : 0f;
                }
                default:
                    return defaultValue;
            }
        }
    }

    /// <summary>
    /// Concrete ResourceProductionExtension that maps raw tile fields through designer-authored
    /// SimpleCurves to produce per-resource additive / multiplier bonuses. Picked up automatically
    /// by ResourceFC.SetBaseResourceBonuses and the Create Colony preview via the existing
    /// ResourceTypeDef.GetExtensionAdditives / GetExtensionMultipliers paths.
    /// </summary>
    public class ResourceProductionExtension_TileField : ResourceProductionExtension
    {
        public List<TileFieldCurve> additives = new List<TileFieldCurve>();
        public List<TileFieldCurve> multipliers = new List<TileFieldCurve>();
        /// <summary>
        /// Planet layers this extension applies to. Empty = surface only.
        /// Mirrors the convention used by WorldSettlementDef.planetLayers.
        /// </summary>
        public List<PlanetLayerDef> planetLayers = new List<PlanetLayerDef>();

        public ResourceProductionExtension_TileField()
        {
            extName = "TileField";
            extDesc = "Tile environment";
        }

        private bool AppliesToTile(PlanetTile tile)
        {
            if (tile == PlanetTile.Invalid) return false;
            if (planetLayers.NullOrEmpty())
                return tile.Layer == Find.WorldGrid.Surface;
            return planetLayers.Contains(tile.LayerDef);
        }

        public override double GetAdditiveBonus(PlanetTile tile, WorldSettlementFC settlement = null)
        {
            if (!AppliesToTile(tile)) return 0;
            if (additives is null) return 0;
            double sum = 0;
            for (int i = 0; i < additives.Count; i++)
            {
                sum += additives[i].Evaluate(tile);
            }
            return sum;
        }

        public override double GetMultiplierBonus(PlanetTile tile, WorldSettlementFC settlement = null)
        {
            if (!AppliesToTile(tile)) return 1;
            if (multipliers is null) return 1;
            double mult = 1;
            for (int i = 0; i < multipliers.Count; i++)
            {
                mult *= multipliers[i].Evaluate(tile);
            }
            return mult;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public enum FCPolicyCategory : byte
    {
        Undefined = 0,
        Trait = 1,
        Core = 2,
        Tax = 3,
        Military = 4
    }

    public class FCPolicy : IExposable
    {
        public FCPolicy()
        {
        }

        public FCPolicy(FCPolicyDef def)
        {
            FactionFC faction = FactionCache.FactionComp;
            this.def = def;
            timeEnacted = Find.TickManager.TicksGame;

            // Create extension state and fire OnEnacted callbacks
            foreach (FCPolicyModExtension ext in def.PolicyExtensions)
            {
                if (state == null)
                    state = ext.CreateState();
                try
                {
                    ext.OnEnacted(faction, this);
                }
                catch (System.Exception e)
                {
                    LogUtil.Error($"FCPolicyModExtension.OnEnacted error for '{def.defName}': {e}");
                }
            }

            // Apply passive trait effects
            if (def.traitEffects != null && faction != null)
            {
                foreach (FCTraitEffectDef effect in def.traitEffects)
                    faction.addTrait(effect);
            }

            // Legacy enactment blocks removed — roadBuilders research unlock is now in
            // FCPolicyExt_RoadBuilders.OnEnacted, mercantile caravan init is in FCPolicyExt_Mercantile.OnEnacted.
        }

        public FCPolicyDef def;
        public int timeEnacted;
        public FCPolicyState state;

        public void ExposeData()
        {
            Scribe_Defs.Look(ref def, "def");
            Scribe_Values.Look(ref timeEnacted, "timeEnacted");
            Scribe_Deep.Look(ref state, "state");
        }


    }

    public class FCPolicyDef : Def, IExposable
    {
        public void ExposeData()
        {
            Scribe_Values.Look(ref factionLevelRequirement, "factionLevelRequirement");
            Scribe_Values.Look(ref techLevelRequirement, "techLevelRequirement");
            Scribe_Values.Look(ref desc, "desc");
            Scribe_Values.Look(ref category, "category");
            Scribe_Values.Look(ref cost, "cost");
            Scribe_Values.Look(ref type, "type");
            Scribe_Values.Look(ref techLevel, "techLevel");
            Scribe_Values.Look(ref enactDuration, "enactDuration");
            Scribe_Collections.Look(ref traits, "traits", LookMode.Value);
            Scribe_Collections.Look(ref positiveEffects, "positiveEffects", LookMode.Value);
            Scribe_Collections.Look(ref negativeEffects, "negativeEffects", LookMode.Value);
        }

        public string desc;
        public FCPolicyCategory category;
        public TechLevel techLevelRequirement;
        public int factionLevelRequirement;
        public List<string> positiveEffects;
        public List<string> negativeEffects;

        // Additional fields for XML compatibility
        public int cost;
        public string type;
        public TechLevel techLevel = TechLevel.Undefined;
        public int enactDuration;
        public List<string> traits = new List<string>();

        // Icon paths — set in XML, resolved lazily to textures
        public string iconPathLight;
        public string iconPathDark;

        // Passive stat effects applied to the faction when this policy/trait is enacted
        public List<FCTraitEffectDef> traitEffects = new List<FCTraitEffectDef>();

        // Policies/traits that are incompatible with this one (mutual exclusion in selection UI)
        public List<FCPolicyDef> incompatiblePolicies = new List<FCPolicyDef>();

        [Unsaved] private Texture2D resolvedIconLight;
        [Unsaved] private Texture2D resolvedIconDark;
        [Unsaved] private bool triedResolveLight;
        [Unsaved] private bool triedResolveDark;

        public Texture2D IconLight
        {
            get
            {
                if (!triedResolveLight)
                {
                    triedResolveLight = true;
                    if (!iconPathLight.NullOrEmpty())
                    {
                        resolvedIconLight = ContentFinder<Texture2D>.Get(iconPathLight, false);
                        if (resolvedIconLight == null)
                            LogUtil.Warning("Could not resolve light icon at '" + iconPathLight + "' for " + defName);
                    }
                }
                return resolvedIconLight;
            }
        }

        public Texture2D IconDark
        {
            get
            {
                if (!triedResolveDark)
                {
                    triedResolveDark = true;
                    if (!iconPathDark.NullOrEmpty())
                    {
                        resolvedIconDark = ContentFinder<Texture2D>.Get(iconPathDark, false);
                        if (resolvedIconDark == null)
                            LogUtil.Warning("Could not resolve dark icon at '" + iconPathDark + "' for " + defName);
                    }
                }
                return resolvedIconDark;
            }
        }

        /// <summary>
        /// Returns all FCPolicyModExtension instances attached to this def.
        /// </summary>
        public IEnumerable<FCPolicyModExtension> PolicyExtensions
        {
            get
            {
                if (modExtensions == null) yield break;
                foreach (DefModExtension ext in modExtensions)
                    if (ext is FCPolicyModExtension policyExt)
                        yield return policyExt;
            }
        }
        public bool HasNegativeEffects()
        {
            return negativeEffects != null && negativeEffects.Count > 0;
        }
        public bool HasPositiveEffects()
        {
            return positiveEffects != null && positiveEffects.Count > 0;
        }
        public string PolicyText()
        {
            return LabelCap + "\n\n" + CachedPolicyDesc();
        }
        public string PolicyDesc()
        {
            string str = "";

            if (HasPositiveEffects())
            {
                foreach (string positive in positiveEffects)
                {
                    str += positive.Colorize(Color.green) + "\n";
                }
            }

            if (HasPositiveEffects() && HasNegativeEffects())
            {
                str += "==========\n";
            }

            if (HasNegativeEffects())
            {
                foreach (string negative in negativeEffects)
                {
                    str += negative.Colorize(Color.red) + "\n";
                }
            }

            return str.Trim();
        }
        public string CachedPolicyDesc()
        {
            return FactionCache.FCPolicyDescs?[this] ?? PolicyDesc();
        }
    }

    [DefOf]
    public class FCPolicyDefOf
    {
        //Faction Traits
        public static FCPolicyDef empty;

        public static FCPolicyDef resilient;
        public static FCPolicyDef raiders;
        public static FCPolicyDef defenseInDepth;
        public static FCPolicyDef industrious;
        public static FCPolicyDef roadBuilders;
        public static FCPolicyDef mercantile;
        public static FCPolicyDef innovative;
        public static FCPolicyDef lucky;

        //Policies
        public static FCPolicyDef militaristic;
        public static FCPolicyDef pacifist;
        public static FCPolicyDef authoritarian;
        public static FCPolicyDef egalitarian;
        public static FCPolicyDef isolationist;
        public static FCPolicyDef expansionist;
        public static FCPolicyDef technocratic;
        public static FCPolicyDef feudal;
        public static FCPolicyDef slaver;

        static FCPolicyDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(FCPolicyDefOf));
        }
    }

}

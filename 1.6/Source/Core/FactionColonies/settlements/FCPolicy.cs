using System;
using System.Collections.Generic;
using FactionColonies.util;
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

            // Create behavior instance if this policy has procedural logic
            if (def.behaviorClass != null)
            {
                behavior = (FCPolicyBehavior)Activator.CreateInstance(def.behaviorClass);
                behavior.policy = this;
                try
                {
                    behavior.OnEnacted(faction);
                }
                catch (Exception e)
                {
                    LogUtil.Error($"FCPolicyBehavior.OnEnacted error for '{def.defName}': {e}");
                }
            }

        }

        public FCPolicyDef def;
        public int timeEnacted;
        public FCPolicyBehavior behavior;

        public void ExposeData()
        {
            Scribe_Defs.Look(ref def, "def");
            Scribe_Values.Look(ref timeEnacted, "timeEnacted");
            Scribe_Deep.Look(ref behavior, "behavior");

            if (Scribe.mode == LoadSaveMode.PostLoadInit && behavior != null)
                behavior.policy = this;
        }
    }

    public class FCPolicyDef : Def
    {
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
        // Icon paths — set in XML, resolved lazily to textures
        public string iconPathLight;
        public string iconPathDark;

        public List<FCStatModifier> statModifiers = new List<FCStatModifier>();
        public List<FCActionType> blockedActions = new List<FCActionType>();
        public List<FCActionType> enabledActions = new List<FCActionType>();
        public List<MilitaryJobDef> blockedMilitaryJobs = new List<MilitaryJobDef>();
        public List<MilitaryJobDef> enabledMilitaryJobs = new List<MilitaryJobDef>();
        public bool preventBuildingDestruction;
        public bool suppressMemberDeathPenalty;
        // Optional behavior class for policies that need procedural logic.
        // Must be a subclass of FCPolicyBehavior. Null for pure-XML policies.
        public Type behaviorClass;

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

            string statDesc = FCStatModifier.GetDescription(statModifiers);
            if (!statDesc.NullOrEmpty())
            {
                if (str.Length > 0) str += "\n";
                str += statDesc;
            }

            return str.Trim();
        }
        public string CachedPolicyDesc()
        {
            return FactionCache.FCPolicyDescs?[this] ?? PolicyDesc();
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string err in base.ConfigErrors())
                yield return err;
            foreach (string err in FCStatModifier.ConfigErrors(statModifiers, defName))
                yield return err;
            if (behaviorClass != null && !typeof(FCPolicyBehavior).IsAssignableFrom(behaviorClass))
                yield return defName + ": behaviorClass " + behaviorClass.Name + " is not a subclass of FCPolicyBehavior";
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

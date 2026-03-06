using FactionColonies;
using FactionColonies.util;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    /// <summary>
    /// Defines an interface to let classes specify additional tabs to add to the main tab window.
    /// </summary>
    public interface IMainTabWindowOverview
    {
        void PreOpenWindow(FactionFC faction);
        void OnTabSwitch();
        void DrawOverviewTab(Rect boundingBox);
        void PostCloseWindow();
        string TabName();
    }
    /// <summary>
    /// Defines an interface that WorldObjectComps can implement in order to add a new overview tab to the settlement window.
    /// <para>This must be implemented by a WorldObjectComp. It will not be invoked otherwise.</para>
    /// </summary>
    public interface ISettlementWindowOverview
    {
        void PreOpenWindow(WorldSettlementFC settlement);
        void OnTabSwitch();
        void DrawOverviewTab(Rect boundingBox);
        void PostCloseWindow();
        string OverviewTabName();
    }
    /// <summary>
    /// A simple interface that a WorldObjectComp -- attached to a WorldSettlementFC -- can implement to affect non-resource stats.
    /// <para>Results are cached alongside stat modifiers. When the comp's modifier values change, the comp must call
    /// <c>((WorldSettlementFC)parent).InvalidateStatCache()</c> to flush the cache.</para>
    /// </summary>
    public interface IStatModifierProvider
    {
        double GetStatModifier(FCStatDef stat);
        string GetStatModifierDesc(FCStatDef stat);
    }
    /// <summary>
    /// A WorldObjectComp interface for contributing dynamic, per-resource production bonuses.
    /// Unlike <see cref="IStatModifierProvider"/> (which operates at the stat level), this operates
    /// directly on <see cref="ResourceFC"/> instances, letting comps target specific resources.
    /// <para>Results are queried during production calculation (lazy-cached by ResourceFC's dirty flags).
    /// When values change, call <c>((WorldSettlementFC)parent).InvalidateResourceCaches()</c> to refresh.</para>
    /// </summary>
    public interface IResourceProductionModifier
    {
        /// <summary>
        /// Returns an additive production bonus for the given resource. Return 0 for no effect.
        /// </summary>
        double GetResourceAdditiveModifier(ResourceFC resource);
        /// <summary>
        /// Returns a multiplicative production modifier for the given resource. Return 1 for no effect.
        /// </summary>
        double GetResourceMultiplierModifier(ResourceFC resource);
        /// <summary>
        /// Returns a description of this comp's contribution for tooltip display.
        /// Return null or empty if not contributing to this resource.
        /// </summary>
        string GetResourceModifierDesc(ResourceFC resource);
    }
    /// <summary>
    /// Defines an interface to let classes hook into the tax system.
    /// </summary>
    public interface ITaxTickParticipant
    {
        void PreTaxResolution(FactionFC faction);
        void PostTaxResolution(FactionFC faction);
        /// <summary>
        /// Called at the start of tax collection, after pre-tax preparation (cache invalidation, resource pruning) and SettlementTypeExtension.preTax.
        /// </summary>
        void PreSettlementCreateTax(WorldSettlementFC settlement);
        /// <summary>
        /// Called at the end of tax collection, after all calculations are complete and SettlementTypeExtension.postTax.
        /// </summary>
        void PostSettlementCreateTax(WorldSettlementFC settlement, ref int silverAmount, List<Thing> titheThings);
    }
    /// <summary>
    /// Defines an interface to let classes hook into settlement creation and removal.
    /// </summary>
    public interface ISettlementLifecycleParticipant
    {
        /// <summary>
        /// Called after a settlement has been fully created, added to the world, and registered with the faction.
        /// </summary>
        void OnSettlementCreated(WorldSettlementFC settlement);
        /// <summary>
        /// Called when a settlement is being removed, before cleanup (military return, event removal) begins.
        /// </summary>
        void OnSettlementRemoved(WorldSettlementFC settlement);
        /// <summary>
        /// Called after a settlement has been upgraded or deleveled.
        /// </summary>
        void OnSettlementUpgraded(WorldSettlementFC settlement, int oldLevel, int newLevel);
    }
    /// <summary>
    /// Defines an interface to let classes hook into building construction and deconstruction.
    /// </summary>
    public interface IBuildingLifecycleParticipant
    {
        /// <summary>
        /// Called after a building has been fully constructed and its comps initialized.
        /// </summary>
        void OnBuildingConstructed(WorldSettlementFC settlement, BuildingFCDef building, int slot);
        /// <summary>
        /// Called before a building is deconstructed and its stat modifiers removed.
        /// </summary>
        void OnBuildingDeconstructed(WorldSettlementFC settlement, BuildingFCDef building, int slot);
    }
    /// <summary>
    /// Defines an interface to let classes hook into military deployment, recall, and battle resolution events.
    /// </summary>
    public interface IMilitaryEventParticipant
    {
        /// <summary>
        /// Called after a military squad has been deployed from a settlement.
        /// </summary>
        void OnSquadDeployed(WorldSettlementFC settlement, MilitaryJobDef job, bool isExtraSquad);
        /// <summary>
        /// Called when a military squad is recalled to its settlement.
        /// </summary>
        void OnSquadRecalled(WorldSettlementFC settlement);
        /// <summary>
        /// Called after a battle has been resolved, before the squad enters cooldown.
        /// </summary>
        void OnBattleResolved(WorldSettlementFC settlement, MilitaryJobDef job, bool victory);
    }
    /// <summary>
    /// Defines an interface to let classes hook into research project completion.
    /// </summary>
    public interface IResearchParticipant
    {
        void OnResearchCompleted(ResearchProjectDef project);
    }
    /// <summary>
    /// Defines an interface to let classes modify military forces before a battle is resolved.
    /// </summary>
    public interface IBattleModifier
    {
        /// <summary>
        /// Called before the battle loop begins. Modify the force's militaryLevel, militaryEfficiency,
        /// or forceRemaining to affect the outcome.
        /// </summary>
        void ModifyForce(militaryForce force, bool isAttacker);
    }
}

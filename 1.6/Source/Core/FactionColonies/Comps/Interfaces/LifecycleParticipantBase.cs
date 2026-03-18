using Verse;

namespace FactionColonies
{
    /// <summary>
    /// Convenience base class for <see cref="ILifecycleParticipant"/>.
    /// All methods are empty virtuals. Override only what you need.
    /// </summary>
    public abstract class LifecycleParticipantBase : ILifecycleParticipant
    {
        public virtual void OnSettlementCreated(WorldSettlementFC settlement) { }
        public virtual void OnSettlementRemoved(WorldSettlementFC settlement) { }
        public virtual void OnSettlementUpgraded(WorldSettlementFC settlement, int oldLevel, int newLevel) { }
        public virtual void OnSettlementTypeChanged(WorldSettlementFC settlement, WorldSettlementDef oldDef, WorldSettlementDef newDef) { }
        public virtual void OnBuildingConstructed(WorldSettlementFC settlement, BuildingFCDef building, int slot) { }
        public virtual void OnBuildingDeconstructed(WorldSettlementFC settlement, BuildingFCDef building, int slot) { }
        public virtual void OnSquadDeployed(WorldSettlementFC settlement, MilitaryJobDef job, bool isExtraSquad) { }
        public virtual void OnSquadRecalled(WorldSettlementFC settlement) { }
        public virtual void OnBattleResolved(WorldSettlementFC settlement, MilitaryJobDef job, bool victory, BattleResult result) { }
        public virtual void OnResearchCompleted(ResearchProjectDef project) { }
    }
}

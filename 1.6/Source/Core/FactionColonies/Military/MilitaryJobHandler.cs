using RimWorld;
using RimWorld.Planet;

namespace FactionColonies
{
    public abstract class MilitaryJobHandler
    {
        public MilitaryJobDef def;

        /// <summary>Called from SendMilitary. Create the FCEvent, send letters, etc.</summary>
        public abstract void OnDeployed(WorldObjectComp_SettlementMilitary milComp, PlanetTile location, int timeToFinish, Faction enemy);

        /// <summary>Called from ProcessMilitaryEvent when the event timer fires. Returns the BattleResult.</summary>
        public abstract BattleResult OnResolved(WorldObjectComp_SettlementMilitary milComp);

        /// <summary>Returns whether this job can target the given faction. Used to filter hostile menu options.</summary>
        public virtual bool IsValidTarget(Faction targetFaction) => true;

        /// <summary>
        /// If true, <see cref="WorldObjectComp_SettlementMilitary.ProcessMilitaryEvent"/> delegates
        /// resolution to <see cref="OnManualResolve"/> instead of calling <see cref="OnResolved"/>.
        /// The handler owns cooldown timing and lifecycle notification.
        /// </summary>
        public virtual bool ResolvesManually => false;

        /// <summary>
        /// Called instead of <see cref="OnResolved"/> when <see cref="ResolvesManually"/> is true.
        /// The handler generates a battle map and manages the async lifecycle.
        /// Must call <c>milComp.CooldownMilitaryFinal()</c> when the battle ends.
        /// </summary>
        public virtual void OnManualResolve(WorldObjectComp_SettlementMilitary milComp) { }
    }
}

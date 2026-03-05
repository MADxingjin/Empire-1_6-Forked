using System;
using System.Collections.Generic;
using FactionColonies.util;

namespace FactionColonies
{
    public static class BuildingLifecycleRegistry
    {
        private static readonly List<IBuildingLifecycleParticipant> _participants = new List<IBuildingLifecycleParticipant>();

        public static void Register(IBuildingLifecycleParticipant participant)
        {
            if (!_participants.Contains(participant)) _participants.Add(participant);
        }
        public static void Unregister(IBuildingLifecycleParticipant participant) => _participants.Remove(participant);
        public static IReadOnlyList<IBuildingLifecycleParticipant> Participants => _participants;

        public static void InvokeOnBuildingConstructed(WorldSettlementFC settlement, BuildingFCDef building, int slot)
        {
            foreach (IBuildingLifecycleParticipant participant in _participants)
            {
                try { participant.OnBuildingConstructed(settlement, building, slot); }
                catch (Exception e) { LogUtil.Error($"IBuildingLifecycleParticipant {participant.GetType().Name} threw in OnBuildingConstructed: {e}"); }
            }
        }

        public static void InvokeOnBuildingDeconstructed(WorldSettlementFC settlement, BuildingFCDef building, int slot)
        {
            foreach (IBuildingLifecycleParticipant participant in _participants)
            {
                try { participant.OnBuildingDeconstructed(settlement, building, slot); }
                catch (Exception e) { LogUtil.Error($"IBuildingLifecycleParticipant {participant.GetType().Name} threw in OnBuildingDeconstructed: {e}"); }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using FactionColonies.util;
using RimWorld;
using Verse;

namespace FactionColonies
{
    public static class ResearchRegistry
    {
        private static readonly List<IResearchParticipant> _participants = new List<IResearchParticipant>();

        public static void Register(IResearchParticipant participant)
        {
            if (!_participants.Contains(participant)) _participants.Add(participant);
        }
        public static void Unregister(IResearchParticipant participant) => _participants.Remove(participant);
        public static void ClearAll() => _participants.Clear();
        public static IReadOnlyList<IResearchParticipant> Participants => _participants;

        public static void InvokeOnResearchCompleted(ResearchProjectDef project)
        {
            foreach (IResearchParticipant participant in _participants)
            {
                try { participant.OnResearchCompleted(project); }
                catch (Exception e) { LogUtil.Error($"IResearchParticipant {participant.GetType().Name} threw in OnResearchCompleted: {e}"); }
            }
        }
    }
}

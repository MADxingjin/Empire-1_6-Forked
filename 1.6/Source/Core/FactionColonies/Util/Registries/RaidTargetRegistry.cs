using System;
using System.Collections.Generic;
using FactionColonies.util;
using RimWorld.Planet;

namespace FactionColonies
{
    public static class RaidTargetRegistry
    {
        private static readonly List<IRaidTarget> _targets = new List<IRaidTarget>();

        public static void Register(IRaidTarget target)
        {
            if (!_targets.Contains(target)) _targets.Add(target);
        }
        public static void Unregister(IRaidTarget target) => _targets.Remove(target);
        public static void ClearAll() => _targets.Clear();
        public static IReadOnlyList<IRaidTarget> Targets => _targets;

        /// <summary>
        /// Finds the <see cref="IRaidTarget"/> wrapping the given <see cref="WorldObject"/>, or null.
        /// </summary>
        public static IRaidTarget FindByWorldObject(WorldObject obj)
        {
            foreach (IRaidTarget target in _targets)
            {
                try
                {
                    if (target.WorldObject == obj) return target;
                }
                catch (Exception e)
                {
                    LogUtil.Error($"IRaidTarget {target.GetType().Name} threw in FindByWorldObject: {e}");
                }
            }
            return null;
        }
    }
}

using System;
using System.Collections.Generic;

namespace FactionColonies
{
    /// <summary>
    /// Allows submods to contribute additive or multiplicative modifiers to the Empire Threat Level (ETL).
    /// Follows the same pattern as <see cref="BattleModifierRegistry"/>.
    /// </summary>
    public static class ThreatScalingRegistry
    {
        private static readonly List<IThreatScalingContributor> _contributors = new List<IThreatScalingContributor>();

        public static void Register(IThreatScalingContributor contributor)
        {
            if (!_contributors.Contains(contributor)) _contributors.Add(contributor);
        }
        public static void Unregister(IThreatScalingContributor contributor) => _contributors.Remove(contributor);
        public static void ClearAll() => _contributors.Clear();
        public static IReadOnlyList<IThreatScalingContributor> Contributors => _contributors;

        public static double InvokeGetAdditiveContributions(FactionFC faction)
        {
            double total = 0;
            foreach (IThreatScalingContributor contributor in _contributors)
            {
                try { total += contributor.GetAdditiveContribution(faction); }
                catch (Exception e) { LogUtil.Error($"IThreatScalingContributor {contributor.GetType().Name} threw in GetAdditiveContribution: {e}"); }
            }
            return total;
        }

        public static double InvokeGetMultiplierContributions(FactionFC faction)
        {
            double total = 1.0;
            foreach (IThreatScalingContributor contributor in _contributors)
            {
                try { total *= contributor.GetMultiplicativeContribution(faction); }
                catch (Exception e) { LogUtil.Error($"IThreatScalingContributor {contributor.GetType().Name} threw in GetMultiplicativeContribution: {e}"); }
            }
            return total;
        }
    }
}

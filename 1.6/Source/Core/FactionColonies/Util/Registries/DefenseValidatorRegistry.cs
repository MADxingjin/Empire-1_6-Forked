using System;
using System.Collections.Generic;

namespace FactionColonies
{
    public static class DefenseValidatorRegistry
    {
        private static readonly List<IDefenseValidator> _validators = new List<IDefenseValidator>();

        public static void Register(IDefenseValidator validator)
        {
            if (!_validators.Contains(validator)) _validators.Add(validator);
        }
        public static void Unregister(IDefenseValidator validator) => _validators.Remove(validator);
        public static void ClearAll() => _validators.Clear();

        /// <summary>
        /// Returns true if all registered validators allow the defense assignment.
        /// </summary>
        public static bool CanDefend(WorldSettlementFC defender, WorldSettlementFC target)
        {
            foreach (IDefenseValidator validator in _validators)
            {
                try
                {
                    if (!validator.CanDefend(defender, target)) return false;
                }
                catch (Exception e)
                {
                    LogUtil.Error($"IDefenseValidator {validator.GetType().Name} threw in CanDefend: {e}");
                }
            }
            return true;
        }
    }
}

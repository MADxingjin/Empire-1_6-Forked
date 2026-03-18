using System;
using System.Collections.Generic;

namespace FactionColonies
{
    public static class BattleModifierRegistry
    {
        private static readonly List<IBattleModifier> _modifiers = new List<IBattleModifier>();

        public static void Register(IBattleModifier modifier)
        {
            if (!_modifiers.Contains(modifier)) _modifiers.Add(modifier);
        }
        public static void Unregister(IBattleModifier modifier) => _modifiers.Remove(modifier);
        public static void ClearAll() => _modifiers.Clear();
        public static IReadOnlyList<IBattleModifier> Modifiers => _modifiers;

        public static void InvokeModifyForce(militaryForce force, bool isAttacker)
        {
            foreach (IBattleModifier modifier in _modifiers)
            {
                try { modifier.ModifyForce(force, isAttacker); }
                catch (Exception e) { LogUtil.Error($"IBattleModifier {modifier.GetType().Name} threw in ModifyForce: {e}"); }
            }
        }
    }
}

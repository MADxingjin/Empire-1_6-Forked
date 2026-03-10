using System;
using System.Collections.Generic;
using FactionColonies.util;

namespace FactionColonies
{
    public class SilverPaymentContext
    {
        /// <summary>The amount of silver to charge. Modifiers can change this.</summary>
        public int Amount;
        /// <summary>Why silver is being charged. Match against PaymentUtil.Reason_* constants.</summary>
        public string Reason;
        /// <summary>The settlement this payment is associated with, if any.</summary>
        public WorldSettlementFC Settlement;

        public SilverPaymentContext(int amount, string reason, WorldSettlementFC settlement = null)
        {
            Amount = amount;
            Reason = reason;
            Settlement = settlement;
        }
    }

    public static class SilverPaymentRegistry
    {
        private static readonly List<ISilverPaymentModifier> _modifiers = new List<ISilverPaymentModifier>();

        public static void Register(ISilverPaymentModifier modifier)
        {
            if (!_modifiers.Contains(modifier)) _modifiers.Add(modifier);
        }
        public static void Unregister(ISilverPaymentModifier modifier) => _modifiers.Remove(modifier);
        public static void ClearAll() => _modifiers.Clear();
        public static IReadOnlyList<ISilverPaymentModifier> Modifiers => _modifiers;

        public static SilverPaymentContext InvokeModifiers(SilverPaymentContext context)
        {
            foreach (ISilverPaymentModifier modifier in _modifiers)
            {
                try { modifier.ModifyPayment(context); }
                catch (Exception e) { LogUtil.Error($"ISilverPaymentModifier {modifier.GetType().Name} threw in ModifyPayment: {e}"); }
            }
            return context;
        }
    }
}

using System;
using System.Collections.Generic;
using FactionColonies.util;
using Verse;

namespace FactionColonies
{
    public static class TaxTickRegistry
    {
        private static readonly List<ITaxTickParticipant> _taxers = new List<ITaxTickParticipant>();

        public static void Register(ITaxTickParticipant taxer)
        {
            if (!_taxers.Contains(taxer)) _taxers.Add(taxer);
        }
        public static void Unregister(ITaxTickParticipant taxer) => _taxers.Remove(taxer);
        public static void ClearAll() => _taxers.Clear();
        public static IReadOnlyList<ITaxTickParticipant> Taxers => _taxers;

        public static void InvokePreTaxResolution(FactionFC faction)
        {
            foreach (ITaxTickParticipant taxer in _taxers)
            {
                try { taxer.PreTaxResolution(faction); }
                catch (Exception e) { LogUtil.Error($"ITaxTickParticipant {taxer.GetType().Name} threw in PreTaxResolution: {e}"); }
            }
        }

        public static void InvokePostTaxResolution(FactionFC faction)
        {
            foreach (ITaxTickParticipant taxer in _taxers)
            {
                try { taxer.PostTaxResolution(faction); }
                catch (Exception e) { LogUtil.Error($"ITaxTickParticipant {taxer.GetType().Name} threw in PostTaxResolution: {e}"); }
            }
        }

        public static void InvokePreSettlementCreateTax(WorldSettlementFC settlement)
        {
            foreach (ITaxTickParticipant taxer in _taxers)
            {
                try { taxer.PreSettlementCreateTax(settlement); }
                catch (Exception e) { LogUtil.Error($"ITaxTickParticipant {taxer.GetType().Name} threw in PreSettlementCreateTax: {e}"); }
            }
        }

        public static void InvokePostSettlementCreateTax(WorldSettlementFC settlement, ref int silverAmount, List<Thing> titheThings)
        {
            foreach (ITaxTickParticipant taxer in _taxers)
            {
                try { taxer.PostSettlementCreateTax(settlement, ref silverAmount, titheThings); }
                catch (Exception e) { LogUtil.Error($"ITaxTickParticipant {taxer.GetType().Name} threw in PostSettlementCreateTax: {e}"); }
            }
        }
    }
}

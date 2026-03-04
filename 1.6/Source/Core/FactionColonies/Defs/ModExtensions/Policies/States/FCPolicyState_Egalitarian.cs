using System.Collections.Generic;
using Verse;

namespace FactionColonies
{
    public class FCPolicyState_Egalitarian : FCPolicyState
    {
        public Dictionary<int, TaxBreakData> taxBreaks = new Dictionary<int, TaxBreakData>();

        public TaxBreakData GetOrCreate(int tile)
        {
            if (!taxBreaks.TryGetValue(tile, out var data))
            {
                data = new TaxBreakData();
                taxBreaks[tile] = data;
            }
            return data;
        }

        public bool IsOnTaxBreak(int tile) => taxBreaks.TryGetValue(tile, out var d) && d.enabled;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref taxBreaks, "taxBreaks", LookMode.Value, LookMode.Deep);
        }
    }
}

using FactionColonies.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactionColonies
{
    /// <summary>
    /// A simple interface that a WorldObjectComp -- attached to a WorldSettlementFC -- can implement to affect non-resource stats.
    /// The implementer is responsible for their own caching.
    /// </summary>
    public interface IStatModifierProvider
    {
        double GetStatModifier(string field, Operation operation);
        string GetStatModifierDesc(string field, Operation operation);
    }
}

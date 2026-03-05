using FactionColonies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactionColonies
{
    public static class TaxTickRegistry
    {
        private static readonly List<ITaxTickParticipant> _taxers = new List<ITaxTickParticipant>();

        public static void Register(ITaxTickParticipant taxer) => _taxers.Add(taxer);
        public static void Unregister(ITaxTickParticipant taxer) => _taxers.Remove(taxer);
        public static IReadOnlyList<ITaxTickParticipant> Taxers => _taxers;
    }
}

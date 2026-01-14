using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactionColonies
{
    // Nothing for now, but we'll be adding resource specifications later, unique to Empire settlements. So might as well make the custom def now
    public class SettlementDef : WorldObjectDef
    {
        public List<ResourceTypeDef> resourceTypes = new List<ResourceTypeDef>();
    }
}

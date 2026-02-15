using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace FactionColonies
{
    /// <summary>
    /// Used to define filters for ResourceTypes that require more in-depth logic than Thing or ThingCategory allow/blocklists can provide.
    /// <para>NOTE: ResourceFilterExtension run *after* the thing and thingCategory allow/block lists have been applied.</para>
    /// </summary>
    public abstract class ResourceFilterExtension : DefModExtension
    {
        /// <summary>
        /// Sets the given filter for the given tech level.
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="techlevel"></param>
        public virtual void SetFilter(ThingFilter filter, TechLevel techlevel)
        {
        }
        /// <summary>
        /// Retrieves the ThingSetMaker associated with this resource.
        /// <para>Resources use ThingSetMaker_MarketValue by default. This function only needs to be specified if you want to use a different ThingSetMaker.</para>
        /// </summary>
        /// <param name="tlevel">Output parameter. This techlevel will be used if getThingSetMaker returns non-null.</param>
        /// <returns></returns>
        public virtual ThingSetMaker getThingSetMaker(out TechLevel tlevel)
        {
            tlevel = TechLevel.Undefined;
            return null;
        }

        public virtual List<Thing> generateSpecificThings(ThingDef thingDef, QualityCategory quality, ThingDef stuffDef, int quantity)
        {
            return null;
        }
    }
}

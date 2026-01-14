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
    /// NOTE: ResourceExtensions run *after* the thing and thingCategory allow/block lists have been applied.
    /// </summary>
    public abstract class ResourceExtension : DefModExtension
    {
        public virtual void SetFilter(ThingFilter filter)
        {
        }
    }
}

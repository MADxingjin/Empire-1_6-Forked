using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace FactionColonies
{
    /// <summary>
    /// Utility class for general-purpose functions (GENeral UTILities)
    /// </summary>
    public static class GenUtil
    {
        public static Type ReturnUnknownTypeFromName(string name)
        {
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = a.GetType(name);
                if (type != null)
                    return type;
            }

            return null;
        }
    }
}

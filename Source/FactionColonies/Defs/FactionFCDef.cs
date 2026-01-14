using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace FactionColonies
{
    //TODO: I don't think this class should actually be a def. We don't treat techLevel like a def, since it's actually modified during runtime
    //      and the stufffilter isn't actually used at all.
    //      techLevel should be moved into the Faction probably (maybe maybe a subclass to hold Empire-specific info)
    public class FactionFCDef : Def, IExposable
    {

        public FactionFCDef()
        {
        }

        public void ExposeData()
        {

            Scribe_Values.Look<TechLevel>(ref techLevel, "techLevel");
            Scribe_Deep.Look<ThingFilter>(ref apparelStuffFilter, "apparelStuffFilter");

        }

        public TechLevel techLevel = TechLevel.Undefined;
        public ThingFilter apparelStuffFilter = new ThingFilter();

        //public required research
    }



}

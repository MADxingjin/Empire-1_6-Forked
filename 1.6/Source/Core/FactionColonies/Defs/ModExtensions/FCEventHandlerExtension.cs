using System;
using FactionColonies.util;
using Verse;

namespace FactionColonies
{
    /// <summary>
    /// A DefModExtension for FCEventDef that provides a type-safe hook into event
    /// processing without Harmony patches.
    ///
    /// Usage in XML:
    ///   <modExtensions>
    ///     <li Class="MyMod.MyEventHandler"/>
    ///   </modExtensions>
    ///
    /// In C#, subclass this and override the virtual methods you need.
    /// </summary>
    public class FCEventHandlerExtension : DefModExtension
    {
        /// <summary>
        /// Called to resolve a custom event. Return true if handled (skips built-in resolution).
        /// Generic post-processing (loot, stat cleanup, cascading events, OnEventTriggered)
        /// still runs afterward regardless of return value.
        /// </summary>
        public virtual bool ResolveEvent(FCEvent evt, FactionFC faction)
        {
            return false;
        }

        /// <summary>
        /// Called after all standard event processing has completed (loot delivery,
        /// trait removal, prosperity changes, following events).
        /// </summary>
        public virtual void OnEventTriggered(FCEvent evt)
        {
        }

        /// <summary>
        /// Called during settlement removal for each active event that wasn't already
        /// handled by the core cleanup logic. Return true to cancel this event.
        /// </summary>
        public virtual bool ShouldCancelOnSettlementRemoval(FCEvent evt, WorldSettlementFC settlement)
        {
            return false;
        }
    }

    /// <summary>Internal handler for the deliveryArrival event.</summary>
    internal class FCEventHandlerExtension_DeliveryArrival : FCEventHandlerExtension
    {
        public override void OnEventTriggered(FCEvent evt)
        {
            DeliveryEvent.Action(evt);
        }
    }
}

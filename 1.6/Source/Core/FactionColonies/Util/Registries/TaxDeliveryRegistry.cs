using System;
using System.Collections.Generic;

namespace FactionColonies
{
    public class TaxDeliveryContext
    {
        /// <summary>The tax event being processed.</summary>
        public FCEvent Event;
        /// <summary>The source settlement, if resolvable.</summary>
        public WorldSettlementFC Settlement;
        /// <summary>Set to true by an interceptor to indicate it has redirected the event. Stops further interceptors.</summary>
        public bool Redirected = false;
        /// <summary>Set internally by <see cref="TaxDeliveryRegistry.InvokeTryDeliverGoods"/> when an interceptor consumes delivery.</summary>
        public bool Delivered = false;

        public TaxDeliveryContext(FCEvent evt, WorldSettlementFC settlement)
        {
            Event = evt;
            Settlement = settlement;
        }
    }

    public static class TaxDeliveryRegistry
    {
        private static readonly List<ITaxDeliveryInterceptor> _interceptors = new List<ITaxDeliveryInterceptor>();

        public static void Register(ITaxDeliveryInterceptor interceptor)
        {
            if (!_interceptors.Contains(interceptor)) _interceptors.Add(interceptor);
        }
        public static void Unregister(ITaxDeliveryInterceptor interceptor) => _interceptors.Remove(interceptor);
        public static void ClearAll() => _interceptors.Clear();
        public static IReadOnlyList<ITaxDeliveryInterceptor> Interceptors => _interceptors;

        /// <summary>
        /// Invokes <see cref="ITaxDeliveryInterceptor.OnTaxEventCreated"/> on all interceptors,
        /// stopping at the first one that sets <see cref="TaxDeliveryContext.Redirected"/> to true.
        /// </summary>
        public static void InvokeOnTaxEventCreated(TaxDeliveryContext context)
        {
            foreach (ITaxDeliveryInterceptor interceptor in _interceptors)
            {
                try
                {
                    interceptor.OnTaxEventCreated(context);
                    if (context.Redirected) return;
                }
                catch (Exception e)
                {
                    LogUtil.Error($"ITaxDeliveryInterceptor {interceptor.GetType().Name} threw in OnTaxEventCreated: {e}");
                }
            }
        }

        /// <summary>
        /// Invokes <see cref="ITaxDeliveryInterceptor.TryDeliverGoods"/> on all interceptors,
        /// stopping at the first one that returns true.
        /// Returns true if any interceptor consumed the delivery.
        /// </summary>
        public static bool InvokeTryDeliverGoods(TaxDeliveryContext context)
        {
            foreach (ITaxDeliveryInterceptor interceptor in _interceptors)
            {
                try
                {
                    if (interceptor.TryDeliverGoods(context))
                    {
                        context.Delivered = true;
                        return true;
                    }
                }
                catch (Exception e)
                {
                    LogUtil.Error($"ITaxDeliveryInterceptor {interceptor.GetType().Name} threw in TryDeliverGoods: {e}");
                }
            }
            return false;
        }
    }
}

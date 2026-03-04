using Verse;

namespace FactionColonies
{
    /// <summary>
    /// Base class for per-policy runtime state (cooldowns, timers, flags, etc.).
    /// Subclass this for any policy that needs persistent data beyond what lives on the def.
    ///
    /// State is stored on the FCPolicy instance and serialized via Scribe_Deep.
    /// Create instances by overriding FCPolicyModExtension.CreateState().
    ///
    /// Example:
    ///   public class FCPolicyState_Mercantile : FCPolicyState
    ///   {
    ///       public int nextCaravanTick;
    ///       public override void ExposeData()
    ///       {
    ///           Scribe_Values.Look(ref nextCaravanTick, "nextCaravanTick");
    ///       }
    ///   }
    /// </summary>
    public class FCPolicyState : IExposable
    {
        public virtual void ExposeData()
        {
        }
    }
}

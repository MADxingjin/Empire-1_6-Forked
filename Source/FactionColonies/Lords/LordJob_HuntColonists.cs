using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace FactionColonies
{
    public class LordJob_HuntColonists : LordJob
    {
        private bool delay;

        public LordJob_HuntColonists(bool delay)
        {
            this.delay = delay;
        }
        
        public override StateGraph CreateGraph()
        {
            StateGraph stateGraph = new StateGraph();

            LordToil lordToil = new LordToil_HuntColonists();
            stateGraph.AddToil(lordToil);

            if (!delay) return stateGraph;
            LordToil idleToil = new LordToil_IdleNearby();
            stateGraph.AddToil(idleToil);
            stateGraph.StartingToil = idleToil;
                
            Transition startAssault = new Transition(idleToil, lordToil);
            startAssault.AddTrigger(new Trigger_TicksPassed(500));
            stateGraph.AddTransition(startAssault);

            return stateGraph;
        }

        public override void Notify_PawnLost(Pawn pawn, PawnLostCondition condition)
        {
            FactionFC faction = FactionCache.FactionComp;
            //Check if a settlement battle ended
            WorldSettlementFC settlement = faction.returnSettlementByLocation(pawn.Tile);
            settlement?.MilitaryComp?.removeAttacker(pawn);
        }
    }
}
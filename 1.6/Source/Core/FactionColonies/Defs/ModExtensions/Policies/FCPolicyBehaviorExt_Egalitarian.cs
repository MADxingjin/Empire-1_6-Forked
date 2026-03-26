namespace FactionColonies
{
    public class FCPolicyBehaviorExt_Egalitarian : FCPolicyBehaviorExtension
    {
        public int startingHappiness = 60;
        public int taxBreakDurationDays = 10;
        public double taxBreakPenalty = 30;
        public int taxBreakHappinessBonus = 2;
        public int taxBreakProsperityBonus = 2;
        public double happinessDivisor = 10;
    }
}

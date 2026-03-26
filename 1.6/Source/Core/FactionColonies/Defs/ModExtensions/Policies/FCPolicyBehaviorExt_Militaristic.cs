namespace FactionColonies
{
    public class FCPolicyBehaviorExt_Militaristic : FCPolicyBehaviorExtension
    {
        public int extraSquadCooldownDays = 5;
        public float extraSquadCostFraction = 0.2f;
        public int militaryBuildingUpkeepDiscount = 100;
        public string autoPlaceBuildingDefName = "barracks";
    }
}

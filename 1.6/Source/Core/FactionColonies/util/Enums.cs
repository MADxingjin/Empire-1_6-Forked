using System.Collections.Generic;

namespace FactionColonies.util
{
    public enum MilitaryOrder
    {
        Undefined,
        DefendPoint,
        Hunt,
        RecoverWoundedAndLeave
    }

    public enum Operation
    {
        Addition,
        Multiplication
    }

    public enum PatchNoteType
    {
        Undefined,
        Hotfix,
        Patch,
        Minor,
        Major
    }

    public enum BattleMode
    {
        Auto,
        Manual,
        Hybrid
    }

    /// <summary>
    /// Hard-coded action gates that policies can block or enable via FCPolicyDef.blockedActions/enabledActions.
    /// Military job-level permissions are handled separately via MilitaryJobDef.defaultEnabled and
    /// FCPolicyDef.blockedMilitaryJobs/enabledMilitaryJobs.
    /// </summary>
    public enum FCActionType
    {
        DeployMilitary,
        SendDiplomat,
        DeployExtraSquad,
        BuildRoadsToAllies,
        UseFireSupport,
        SendPrisoner,
        SellPrisoner,
        DemolishBuilding,
        UpgradeSettlement,
        TradeWithSettlement
    }

    /// <summary>
    /// Centralizes the opt-in/opt-out distinction for FCActionType.
    /// Actions in the RequiresEnable set are opt-in (unavailable by default, require a policy/trait to enable).
    /// All other actions are opt-out (available by default, can be blocked by a policy/trait).
    /// </summary>
    public static class FCActionTypeUtil
    {
        private static readonly HashSet<FCActionType> requiresEnable = new HashSet<FCActionType>
        {
            FCActionType.SendDiplomat,
            FCActionType.DeployExtraSquad,
            FCActionType.BuildRoadsToAllies
        };

        public static bool RequiresEnable(FCActionType action) => requiresEnable.Contains(action);
    }
    

    public enum TaxDeliveryMode
    {
        None,
        TaxSpot,
        Caravan,
        DropPod,
        Shuttle
    }

    public enum EmpireDifficultyLevel
    {
        Peaceful = 0,
        CommunityBuilder = 1,
        AdventureStory = 2,
        StriveToSurvive = 3,
        BloodAndDust = 4,
        LosingIsFun = 5,
        Custom = 6
    }

    public enum TaxNotificationMode
    {
        All,        // Show both Letter and Message
        LetterOnly, // Only show Letter (blue notification)
        MessageOnly,// Only show Message (top-screen text)
        None        // Hide all tax delivery notifications
    }
    
    public enum FCPolicyCategory : byte
    {
        Undefined = 0,
        Trait = 1,
        Core = 2,
        Tax = 3,
        Military = 4,
        Social = 5,
        Doctrine = 6
    }

    public enum MilitaryWindowSlot
    {
        Units,
        Squads,
        FireSupport
    }
}

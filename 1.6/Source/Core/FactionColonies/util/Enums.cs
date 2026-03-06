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
        private static readonly System.Collections.Generic.HashSet<FCActionType> _requiresEnable = new System.Collections.Generic.HashSet<FCActionType>
        {
            FCActionType.SendDiplomat,
            FCActionType.DeployExtraSquad,
            FCActionType.BuildRoadsToAllies
        };

        public static bool RequiresEnable(FCActionType action) => _requiresEnable.Contains(action);
    }
}

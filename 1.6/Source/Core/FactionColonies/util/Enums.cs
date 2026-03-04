namespace FactionColonies.util
{
    public enum MilitaryOrder
    {
        Undefined,
        DefendPoint,
        Hunt,
        RecoverWoundedAndLeave
    }

    public enum MilitaryJob
    {
        Undefined,
        Cooldown,
        Deploy,
        RaidEnemySettlement,
        EnslaveEnemySettlement,
        CaptureEnemySettlement,
        DefendFriendlySettlement
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
    /// Action types that FCPolicyModExtension can block or enable.
    /// </summary>
    public enum FCActionType
    {
        CaptureSettlement,
        RaidSettlement,
        EnslaveSettlement,
        DeployMilitary,
        SendDiplomat,
        DeployExtraSquad,
        BuildRoadsToAllies
    }
}

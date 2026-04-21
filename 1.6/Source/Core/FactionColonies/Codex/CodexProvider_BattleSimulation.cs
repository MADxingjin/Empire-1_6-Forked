using System;
using Verse;

namespace FactionColonies
{
    /// <summary>
    /// Dynamic provider that shows the current battle simulation parameters:
    /// efficiency damping formula and defender advantage.
    /// </summary>
    public class CodexProvider_BattleSimulation : ICodexDynamicProvider
    {
        public string GetDynamicContent(FactionFC faction)
        {
            double damping = FCSettings.efficiencyDamping;
            double defAdv = FCSettings.defenderAdvantage;

            // Example: show how damping compresses a 2.0 efficiency
            double exampleRaw = 2.0;
            double dampened = 1.0 + (exampleRaw - 1.0) * damping;

            string result = "FCCodexBattleParams".Translate() + "\n\n";
            result += "FCCodexBattleDefAdv".Translate(Math.Round(defAdv, 2), Math.Round((defAdv - 1.0) * 100, 0)) + "\n";
            result += "FCCodexBattleDamping".Translate(Math.Round(damping, 2)) + "\n\n";
            result += "FCCodexBattleDampExample".Translate(
                Math.Round(exampleRaw, 1),
                Math.Round(dampened, 2));

            return result;
        }
    }
}

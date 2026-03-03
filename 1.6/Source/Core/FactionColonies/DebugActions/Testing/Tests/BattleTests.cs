using System;
using FactionColonies.util;

namespace FactionColonies
{
    public static class BattleTests
    {
        private class FixedRandProvider : IRandProvider
        {
            private readonly int _value;
            public FixedRandProvider(int value) => _value = value;
            public int Range(int min, int maxExclusive) => _value;
        }

        private class AlternatingRandProvider : IRandProvider
        {
            private readonly int _valueA;
            private readonly int _valueB;
            private bool _returnA = true;

            public AlternatingRandProvider(int a, int b)
            {
                _valueA = a;
                _valueB = b;
            }

            public int Range(int min, int maxExclusive)
            {
                if (_returnA) { _returnA = false; return _valueA; }
                _returnA = true;
                return _valueB;
            }
        }

        private static militaryForce CreateForce(double level, double efficiency, double remaining)
        {
            return new militaryForce
            {
                militaryLevel = level,
                militaryEfficiency = efficiency,
                forceRemaining = remaining
            };
        }

        [EmpireTest("Battle")]
        public static void FightRound_AttackerRollsHigher_DefenderLosesOne()
        {
            var mfa = CreateForce(5, 1.0, 5);
            var mfb = CreateForce(5, 1.0, 5);
            var rand = new AlternatingRandProvider(15, 5); // A rolls 15, B rolls 5

            SimulateBattleFc.FightRound(mfa, mfb, rand);

            TestAssert.AreEqual(5.0, mfa.forceRemaining, message: "Attacker should keep all forces");
            TestAssert.AreEqual(4.0, mfb.forceRemaining, message: "Defender should lose one");
        }

        [EmpireTest("Battle")]
        public static void FightRound_DefenderRollsHigher_AttackerLosesOne()
        {
            var mfa = CreateForce(5, 1.0, 5);
            var mfb = CreateForce(5, 1.0, 5);
            var rand = new AlternatingRandProvider(5, 15); // A rolls 5, B rolls 15

            SimulateBattleFc.FightRound(mfa, mfb, rand);

            TestAssert.AreEqual(4.0, mfa.forceRemaining, message: "Attacker should lose one");
            TestAssert.AreEqual(5.0, mfb.forceRemaining, message: "Defender should keep all forces");
        }

        [EmpireTest("Battle")]
        public static void FightRound_TiedRolls_AttackerLosesOne()
        {
            var mfa = CreateForce(5, 1.0, 5);
            var mfb = CreateForce(5, 1.0, 5);
            var rand = new FixedRandProvider(10); // Both roll 10

            SimulateBattleFc.FightRound(mfa, mfb, rand);

            // Tie goes to defender (attacker loses)
            TestAssert.AreEqual(4.0, mfa.forceRemaining);
            TestAssert.AreEqual(5.0, mfb.forceRemaining);
        }

        [EmpireTest("Battle")]
        public static void FightRound_HigherEfficiency_CompensatesLowerRoll()
        {
            var mfa = CreateForce(5, 2.0, 5); // 2x efficiency
            var mfb = CreateForce(5, 1.0, 5);
            // A rolls 5 * 2.0 = 10, B rolls 8 * 1.0 = 8 → A wins
            var rand = new AlternatingRandProvider(5, 8);

            SimulateBattleFc.FightRound(mfa, mfb, rand);

            TestAssert.AreEqual(5.0, mfa.forceRemaining);
            TestAssert.AreEqual(4.0, mfb.forceRemaining);
        }

        [EmpireTest("Battle")]
        public static void FightBattle_StrongerForceWins()
        {
            var mfa = CreateForce(10, 1.0, 10);
            var mfb = CreateForce(3, 1.0, 3);
            // A always rolls high, B always rolls low
            var rand = new AlternatingRandProvider(15, 2);

            int result = SimulateBattleFc.FightBattle(mfa, mfb, rand);

            TestAssert.AreEqual(0, result, message: "Result 0 = attacker wins");
            TestAssert.IsTrue(mfa.forceRemaining > 0, "Attacker should have forces remaining");
            TestAssert.LessThanOrEqual(mfb.forceRemaining, 0, "Defender should be eliminated");
        }

        [EmpireTest("Battle")]
        public static void FightBattle_DefenderWins_ReturnsOne()
        {
            var mfa = CreateForce(3, 1.0, 3);
            var mfb = CreateForce(10, 1.0, 10);
            // A always rolls low, B always rolls high
            var rand = new AlternatingRandProvider(2, 15);

            int result = SimulateBattleFc.FightBattle(mfa, mfb, rand);

            TestAssert.AreEqual(1, result, message: "Result 1 = defender wins");
        }
    }
}

using LudeonTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace FactionColonies
{
    public static class EmpireTestRunner
    {
        [DebugAction("Empire", "Run All Tests", allowedGameStates = AllowedGameStates.Playing)]
        public static void RunAll() => RunTests(null);

        [DebugAction("Empire", "Run Tests by Category", allowedGameStates = AllowedGameStates.Playing)]
        public static void RunByCategory()
        {
            var categories = DiscoverTests()
                .Select(t => t.attr.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            var options = new List<DebugMenuOption>();
            foreach (string cat in categories)
            {
                string local = cat;
                options.Add(new DebugMenuOption(local, DebugMenuOptionMode.Action, () => RunTests(local)));
            }
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
        }

        public static void RunTests(string category)
        {
            var tests = DiscoverTests();
            if (category != null)
                tests = tests.Where(t => t.attr.Category == category).ToList();

            int passed = 0, failed = 0, errors = 0, skipped = 0;
            var skipDetails = new List<string>();
            var failDetails = new List<string>();
            foreach (var (method, attr) in tests)
            {
                string testName = $"[{attr.Category}] {method.DeclaringType.Name}.{method.Name}";
                try
                {
                    method.Invoke(null, null);
                    passed++;
                    LogUtil.Message($"PASS: {testName}");
                }
                catch (TargetInvocationException tie) when (tie.InnerException is TestSkippedException tse)
                {
                    skipped++;
                    LogUtil.Message($"SKIP: {testName} -- {tse.Message}");
                    skipDetails.Add($"  SKIP: {testName} -- {tse.Message}");
                }
                catch (TargetInvocationException tie) when (tie.InnerException is TestFailedException tfe)
                {
                    failed++;
                    LogUtil.Error($"FAIL: {testName} -- {tfe.Message}");
                    failDetails.Add($"  FAIL: {testName} -- {tfe.Message}");
                }
                catch (Exception ex)
                {
                    errors++;
                    var inner = ex is TargetInvocationException t ? t.InnerException : ex;
                    LogUtil.Error($"ERROR: {testName} -- {inner.GetType().Name}: {inner.Message}");
                }
            }

            string label = category != null ? $"[{category}]" : "[All]";
            LogUtil.MessageForce($"Test results {label}: {passed} passed, {failed} failed, {errors} errors, {skipped} skipped (of {tests.Count} total)");
            if (failDetails.Count > 0)
            {
                LogUtil.MessageForce("Failed tests:\n" + string.Join("\n", failDetails));
            }
            if (skipDetails.Count > 0)
            {
                LogUtil.MessageForce("Skipped tests:\n" + string.Join("\n", skipDetails));
            }
        }

        private static List<(MethodInfo method, EmpireTestAttribute attr)> DiscoverTests()
        {
            return Assembly.GetExecutingAssembly()
                .GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .Select(m => (method: m, attr: m.GetCustomAttribute<EmpireTestAttribute>()))
                .Where(pair => pair.attr != null)
                .OrderBy(pair => pair.attr.Category)
                .ThenBy(pair => pair.method.Name)
                .ToList();
        }
    }
}

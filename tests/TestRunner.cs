using System;
using System.Collections.Generic;

namespace ReSizeWindow.Tests
{
    internal static class TestRunner
    {
        private static readonly List<string> Failures = new List<string>();

        private static int Main()
        {
            WindowMathTests.Run();
            SnappingTests.Run();
            TouchGroupTests.Run();
            StartMenuPolicyTests.Run();
            TilingLayoutTests.Run();
            TilingOrderTests.Run();

            foreach (string failure in Failures) Console.WriteLine("FAIL " + failure);
            Console.WriteLine(Failures.Count == 0 ? "All tests passed" : Failures.Count + " test(s) failed");
            return Failures.Count == 0 ? 0 : 1;
        }

        public static void AssertEqual<T>(string name, T expected, T actual)
        {
            if (!expected.Equals(actual)) Failures.Add(name + ": expected " + expected + ", got " + actual);
        }
    }
}

namespace ReSizeWindow.Tests
{
    internal static class TouchGroupTests
    {
        private static readonly Box Start = new Box(100, 100, 300, 300);

        public static void Run()
        {
            Find_DirectNeighbours_AreIncluded();
            Find_IsTransitive();
            Find_GapTolerance_AllowsTwoPixels();
            Find_OverlapTolerance_AllowsTwoPixels();
            Find_CornerOnlyContact_DoesNotTouch();
            Find_UnrelatedWindows_AreExcluded();
            Find_Stacked_Touches();
            Find_NoCandidates_ReturnsEmpty();
        }

        private static string Find(params Box[] candidates)
        {
            return string.Join(",", TouchGroup.Find(Start, candidates));
        }

        private static void Find_DirectNeighbours_AreIncluded()
        {
            AssertEqual("neighbours", "0,1", Find(new Box(300, 100, 500, 300), new Box(0, 100, 100, 300)));
        }

        private static void Find_IsTransitive()
        {
            var chain = new[] { new Box(500, 100, 700, 300), new Box(300, 100, 500, 300), new Box(700, 250, 900, 500) };
            AssertEqual("chain", "0,1,2", Find(chain));
        }

        private static void Find_GapTolerance_AllowsTwoPixels()
        {
            AssertEqual("gap 2", "0", Find(new Box(302, 100, 500, 300)));
            AssertEqual("gap 3", "", Find(new Box(303, 100, 500, 300)));
        }

        private static void Find_OverlapTolerance_AllowsTwoPixels()
        {
            AssertEqual("overlap 2", "0", Find(new Box(298, 100, 500, 300)));
            AssertEqual("overlap 3", "", Find(new Box(297, 100, 500, 300)));
        }

        private static void Find_CornerOnlyContact_DoesNotTouch()
        {
            AssertEqual("corner", "", Find(new Box(300, 300, 500, 500)));
        }

        private static void Find_UnrelatedWindows_AreExcluded()
        {
            AssertEqual("unrelated", "1", Find(new Box(600, 600, 800, 800), new Box(300, 150, 400, 250)));
        }

        private static void Find_Stacked_Touches()
        {
            AssertEqual("stacked", "0,1", Find(new Box(150, 300, 250, 500), new Box(100, 0, 300, 100)));
        }

        private static void Find_NoCandidates_ReturnsEmpty()
        {
            AssertEqual("empty", "", Find());
        }

        private static void AssertEqual<T>(string name, T expected, T actual)
        {
            TestRunner.AssertEqual("touch " + name, expected, actual);
        }
    }
}

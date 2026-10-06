namespace ReSizeWindow.Tests
{
    internal static class StartMenuPolicyTests
    {
        public static void Run()
        {
            PlainRelease_IsNotMasked();
            ReleaseAfterDrag_IsMaskedOnce();
            BlockLeftWin_MasksOnlyLeft();
            BlockLeftWin_IgnoredWhenDisabled();
            ReleaseAfterDrag_MaskedEvenWhenDisabled();
        }

        private static void PlainRelease_IsNotMasked()
        {
            var policy = new StartMenuPolicy();
            TestRunner.AssertEqual("plain left", false, policy.ShouldMaskRelease(true));
            TestRunner.AssertEqual("plain right", false, policy.ShouldMaskRelease(false));
        }

        private static void ReleaseAfterDrag_IsMaskedOnce()
        {
            var policy = new StartMenuPolicy();
            policy.MarkUsed();
            TestRunner.AssertEqual("after drag", true, policy.ShouldMaskRelease(false));
            TestRunner.AssertEqual("next plain release", false, policy.ShouldMaskRelease(false));
        }

        private static void BlockLeftWin_MasksOnlyLeft()
        {
            var policy = new StartMenuPolicy { BlockLeftWin = true };
            TestRunner.AssertEqual("blocked left", true, policy.ShouldMaskRelease(true));
            TestRunner.AssertEqual("blocked left again", true, policy.ShouldMaskRelease(true));
            TestRunner.AssertEqual("right unaffected", false, policy.ShouldMaskRelease(false));
        }

        private static void BlockLeftWin_IgnoredWhenDisabled()
        {
            var policy = new StartMenuPolicy { BlockLeftWin = true, Enabled = false };
            TestRunner.AssertEqual("paused left", false, policy.ShouldMaskRelease(true));
        }

        private static void ReleaseAfterDrag_MaskedEvenWhenDisabled()
        {
            var policy = new StartMenuPolicy();
            policy.MarkUsed();
            policy.Enabled = false;
            TestRunner.AssertEqual("paused mid-drag", true, policy.ShouldMaskRelease(true));
        }
    }
}

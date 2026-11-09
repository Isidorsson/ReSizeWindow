using System.Collections.Generic;

namespace ReSizeWindow.Tests
{
    internal static class TilingLayoutTests
    {
        private static readonly Box Landscape = new Box(0, 0, 1920, 1080);
        private static readonly Box Portrait = new Box(0, 0, 1080, 1920);

        public static void Run()
        {
            FitSplits_NoWindows_IsEmpty();
            FitSplits_PadsWithEvenAndTrims();
            Dwindle_One_FillsAreaMinusGap();
            Dwindle_Two_Landscape_SplitsVertically();
            Dwindle_Three_Landscape_AlternatesToHorizontal();
            Dwindle_Four_Landscape_AlternatesBackToVertical();
            Dwindle_Two_Portrait_SplitsHorizontally();
            Dwindle_ZeroGap_TilesWithoutOverlap();
            Dwindle_GapBetweenWindowsIsExact();
            Dwindle_OffsetArea_KeepsOrigin();

            Dwindle_UnevenSplit_GivesFirstWindowItsShare();
            ResizeSplits_FirstWindowRightEdge_MovesSplit();
            ResizeSplits_LastWindowLeftEdge_MovesSplit();
            ResizeSplits_InnerEdge_LeavesOtherSplits();
            ResizeSplits_TwoEdges_MoveBothSplits();
            ResizeSplits_OuterEdge_IsIgnored();
            ResizeSplits_ClampsExtremeDrag();

            Compensate_GrowsByInvisibleBorders();
            Compensate_NoBorders_IsIdentity();
        }

        private static void FitSplits_NoWindows_IsEmpty()
        {
            AssertEqual("fit 0", 0, TilingLayout.FitSplits(new List<double>(), 0).Count);
        }

        private static void FitSplits_PadsWithEvenAndTrims()
        {
            List<double> padded = TilingLayout.FitSplits(new List<double> { 0.3 }, 3);
            AssertEqual("fit pad count", 2, padded.Count);
            AssertEqual("fit keeps", 0.3, padded[0]);
            AssertEqual("fit pads even", 0.5, padded[1]);
            AssertEqual("fit trims", 1, TilingLayout.FitSplits(new List<double> { 0.3, 0.7, 0.6 }, 2).Count);
        }

        private static void Dwindle_One_FillsAreaMinusGap()
        {
            List<Box> boxes = Even(Landscape, 1, 8);
            AssertEqual("dwindle 1 count", 1, boxes.Count);
            AssertEqual("dwindle 1", new Box(8, 8, 1912, 1072), boxes[0]);
        }

        private static void Dwindle_Two_Landscape_SplitsVertically()
        {
            List<Box> boxes = Even(Landscape, 2, 8);
            AssertEqual("dwindle 2 first", new Box(8, 8, 956, 1072), boxes[0]);
            AssertEqual("dwindle 2 second", new Box(964, 8, 1912, 1072), boxes[1]);
        }

        private static void Dwindle_Three_Landscape_AlternatesToHorizontal()
        {
            List<Box> boxes = Even(Landscape, 3, 8);
            AssertEqual("dwindle 3 first", new Box(8, 8, 956, 1072), boxes[0]);
            AssertEqual("dwindle 3 second", new Box(964, 8, 1912, 536), boxes[1]);
            AssertEqual("dwindle 3 third", new Box(964, 544, 1912, 1072), boxes[2]);
        }

        private static void Dwindle_Four_Landscape_AlternatesBackToVertical()
        {
            List<Box> boxes = Even(Landscape, 4, 8);
            AssertEqual("dwindle 4 third", new Box(964, 544, 1434, 1072), boxes[2]);
            AssertEqual("dwindle 4 fourth", new Box(1442, 544, 1912, 1072), boxes[3]);
        }

        private static void Dwindle_Two_Portrait_SplitsHorizontally()
        {
            List<Box> boxes = Even(Portrait, 2, 8);
            AssertEqual("portrait first", new Box(8, 8, 1072, 956), boxes[0]);
            AssertEqual("portrait second", new Box(8, 964, 1072, 1912), boxes[1]);
        }

        private static void Dwindle_ZeroGap_TilesWithoutOverlap()
        {
            List<Box> boxes = Even(Landscape, 2, 0);
            AssertEqual("no gap first", new Box(0, 0, 960, 1080), boxes[0]);
            AssertEqual("no gap second", new Box(960, 0, 1920, 1080), boxes[1]);
        }

        private static void Dwindle_GapBetweenWindowsIsExact()
        {
            List<Box> boxes = Even(new Box(0, 0, 1001, 600), 2, 10);
            AssertEqual("gap between", 10, boxes[1].Left - boxes[0].Right);
            AssertEqual("gap right edge", 10, 1001 - boxes[1].Right);
        }

        private static void Dwindle_OffsetArea_KeepsOrigin()
        {
            List<Box> boxes = Even(new Box(1920, 40, 3840, 1120), 1, 8);
            AssertEqual("offset area", new Box(1928, 48, 3832, 1112), boxes[0]);
        }

        private static void Dwindle_UnevenSplit_GivesFirstWindowItsShare()
        {
            List<Box> boxes = TilingLayout.Dwindle(Tiles, new List<double> { 0.25 });
            AssertEqual("uneven first", new Box(8, 8, 482, 1072), boxes[0]);
            AssertEqual("uneven second", new Box(490, 8, 1912, 1072), boxes[1]);
        }

        private static void ResizeSplits_FirstWindowRightEdge_MovesSplit()
        {
            List<Box> boxes = Resized(2, 0, new Box(8, 8, 1000, 1072));
            AssertEqual("resize first right", 1000, boxes[0].Right);
            AssertEqual("resize first neighbour", 1008, boxes[1].Left);
        }

        private static void ResizeSplits_LastWindowLeftEdge_MovesSplit()
        {
            List<Box> boxes = Resized(2, 1, new Box(700, 8, 1912, 1072));
            AssertEqual("resize last neighbour", 692, boxes[0].Right);
            AssertEqual("resize last left", 700, boxes[1].Left);
        }

        // Three windows: one on the left, two stacked on the right.
        private static void ResizeSplits_InnerEdge_LeavesOtherSplits()
        {
            List<Box> boxes = Resized(3, 1, new Box(964, 8, 1912, 700));
            AssertEqual("inner left untouched", new Box(8, 8, 956, 1072), boxes[0]);
            AssertEqual("inner bottom", 700, boxes[1].Bottom);
            AssertEqual("inner neighbour", 708, boxes[2].Top);
        }

        private static void ResizeSplits_TwoEdges_MoveBothSplits()
        {
            List<Box> boxes = Resized(3, 2, new Box(900, 400, 1912, 1072));
            AssertEqual("two edges left", 892, boxes[0].Right);
            AssertEqual("two edges top", 392, boxes[1].Bottom);
            AssertEqual("two edges window", new Box(900, 400, 1912, 1072), boxes[2]);
        }

        private static void ResizeSplits_OuterEdge_IsIgnored()
        {
            List<Box> boxes = Resized(2, 0, new Box(50, 8, 956, 1072));
            AssertEqual("outer edge", new Box(8, 8, 956, 1072), boxes[0]);
        }

        private static void ResizeSplits_ClampsExtremeDrag()
        {
            List<Box> boxes = Resized(2, 0, new Box(8, 8, 1905, 1072));
            AssertEqual("clamped", 1714, boxes[0].Right);
        }

        private static readonly TileArea Tiles = new TileArea(Landscape, 8);

        private static List<Box> Resized(int count, int index, Box frame)
        {
            List<double> even = TilingLayout.FitSplits(new List<double>(), count);
            return TilingLayout.Dwindle(Tiles, TilingLayout.ResizeSplits(Tiles, even, index, frame));
        }

        private static List<Box> Even(Box area, int count, int gap)
        {
            return TilingLayout.Dwindle(new TileArea(area, gap), TilingLayout.FitSplits(new List<double>(), count));
        }

        private static void Compensate_GrowsByInvisibleBorders()
        {
            var window = new Box(100, 100, 400, 400);
            var visible = new Box(107, 100, 393, 393);
            AssertEqual("compensate", new Box(-7, 0, 207, 207), TilingLayout.Compensate(new Box(0, 0, 200, 200), visible, window));
        }

        private static void Compensate_NoBorders_IsIdentity()
        {
            var window = new Box(100, 100, 400, 400);
            AssertEqual("compensate none", new Box(0, 0, 200, 200), TilingLayout.Compensate(new Box(0, 0, 200, 200), window, window));
        }

        private static void AssertEqual<T>(string name, T expected, T actual)
        {
            TestRunner.AssertEqual(name, expected, actual);
        }
    }
}

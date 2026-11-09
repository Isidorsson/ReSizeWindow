namespace ReSizeWindow.Tests
{
    internal static class SnappingTests
    {
        private const int Threshold = 15;

        // Visible frame being dragged: 200x200 at (500,400).
        private static readonly Box Moving = new Box(500, 400, 700, 600);
        private static readonly Box[] NoBoxes = new Box[0];

        public static void Run()
        {
            Move_AdjacentToRightEdge_WithinThreshold_Snaps();
            Move_OutsideThreshold_StaysPut();
            Move_AdjacentToLeftEdge_SnapsRightEdge();
            Move_AlignedEdge_Snaps();
            Move_AlignedEdgeFarOnOtherAxis_DoesNotSnap();
            Move_AdjacentWithoutOverlapOnOtherAxis_DoesNotSnap();
            Move_AdjacentWithOverlapWithinThreshold_Snaps();
            Move_PicksSmallestCorrection();
            Move_AxesAreIndependent();
            Move_WorkAreaEdges_SnapInside();
            Move_KeepsSize();

            Resize_OnlyMovingEdgesSnap();
            Resize_MovingEdge_Snaps();
            Resize_RespectsMinimumSize();
            Resize_WorkAreaEdge_Snaps();
        }

        private static SnapContext Context(Box[] windows, Box[] workAreas)
        {
            return new SnapContext(windows, workAreas, Threshold, 100, 100);
        }

        private static SnapContext Windows(params Box[] windows)
        {
            return Context(windows, NoBoxes);
        }

        private static void Move_AdjacentToRightEdge_WithinThreshold_Snaps()
        {
            var other = new Box(200, 400, 488, 600);
            AssertEqual("adjacent right edge", new Box(488, 400, 688, 600), Snapping.SnapMove(Moving, Windows(other)));
        }

        private static void Move_OutsideThreshold_StaysPut()
        {
            var other = new Box(200, 400, 480, 600);
            AssertEqual("outside threshold", Moving, Snapping.SnapMove(Moving, Windows(other)));
        }

        private static void Move_AdjacentToLeftEdge_SnapsRightEdge()
        {
            var other = new Box(710, 400, 900, 600);
            AssertEqual("adjacent left edge", new Box(510, 400, 710, 600), Snapping.SnapMove(Moving, Windows(other)));
        }

        private static void Move_AlignedEdge_Snaps()
        {
            var stackedAbove = new Box(508, 100, 900, 390);
            AssertEqual("aligned left", new Box(508, 390, 708, 590), Snapping.SnapMove(Moving, Windows(stackedAbove)));
        }

        private static void Move_AlignedEdgeFarOnOtherAxis_DoesNotSnap()
        {
            var farAbove = new Box(508, 100, 900, 200);
            AssertEqual("aligned far", Moving, Snapping.SnapMove(Moving, Windows(farAbove)));
        }

        private static void Move_AdjacentWithoutOverlapOnOtherAxis_DoesNotSnap()
        {
            var farAbove = new Box(200, 0, 490, 100);
            AssertEqual("no overlap", Moving, Snapping.SnapMove(Moving, Windows(farAbove)));
        }

        private static void Move_AdjacentWithOverlapWithinThreshold_Snaps()
        {
            var justAbove = new Box(200, 300, 490, 390);
            AssertEqual("overlap within threshold", new Box(490, 390, 690, 590), Snapping.SnapMove(Moving, Windows(justAbove)));
        }

        private static void Move_PicksSmallestCorrection()
        {
            var nearLeft = new Box(100, 400, 485, 600);
            var nearRight = new Box(712, 400, 900, 600);
            AssertEqual("smallest wins", new Box(512, 400, 712, 600), Snapping.SnapMove(Moving, Windows(nearLeft, nearRight)));
        }

        private static void Move_AxesAreIndependent()
        {
            var other = new Box(200, 200, 490, 390);
            AssertEqual("both axes", new Box(490, 390, 690, 590), Snapping.SnapMove(Moving, Windows(other)));
        }

        private static void Move_WorkAreaEdges_SnapInside()
        {
            var context = Context(NoBoxes, new[] { new Box(0, 0, 1920, 1040) });
            AssertEqual("work area left/top", new Box(0, 0, 200, 200), Snapping.SnapMove(new Box(8, 5, 208, 205), context));
            AssertEqual("work area right/bottom", new Box(1720, 840, 1920, 1040), Snapping.SnapMove(new Box(1712, 835, 1912, 1035), context));
        }

        private static void Move_KeepsSize()
        {
            var other = new Box(200, 400, 488, 600);
            Box snapped = Snapping.SnapMove(Moving, Windows(other));
            AssertEqual("width", Moving.Width, snapped.Width);
            AssertEqual("height", Moving.Height, snapped.Height);
        }

        private static void Resize_OnlyMovingEdgesSnap()
        {
            var nearLeft = new Box(100, 400, 490, 600);
            AssertEqual("right resize ignores left", Moving, Snapping.SnapResize(Moving, Edges.Right, Windows(nearLeft)));
        }

        private static void Resize_MovingEdge_Snaps()
        {
            var nearRight = new Box(710, 400, 900, 600);
            AssertEqual("right edge snaps", new Box(500, 400, 710, 600),
                Snapping.SnapResize(Moving, Edges.Right, Windows(nearRight)));

            var side = new Box(710, 380, 900, 590);
            var below = new Box(500, 607, 600, 700);
            AssertEqual("corner snaps both axes", new Box(500, 400, 710, 607),
                Snapping.SnapResize(Moving, Edges.Right | Edges.Bottom, Windows(side, below)));
        }

        private static void Resize_RespectsMinimumSize()
        {
            var box = new Box(500, 400, 600, 600);
            var other = new Box(100, 400, 508, 600);
            AssertEqual("min width kept", box, Snapping.SnapResize(box, Edges.Left, Windows(other)));
        }

        private static void Resize_WorkAreaEdge_Snaps()
        {
            var context = Context(NoBoxes, new[] { new Box(0, 0, 1920, 1040) });
            AssertEqual("work area bottom", new Box(500, 400, 700, 1040),
                Snapping.SnapResize(new Box(500, 400, 700, 1030), Edges.Bottom, context));
        }

        private static void AssertEqual<T>(string name, T expected, T actual)
        {
            TestRunner.AssertEqual("snap " + name, expected, actual);
        }
    }
}

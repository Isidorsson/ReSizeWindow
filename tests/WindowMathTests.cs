namespace ReSizeWindow.Tests
{
    internal static class WindowMathTests
    {
        private static readonly Box Window = new Box(100, 100, 400, 400);

        public static void Run()
        {
            Offset_ShiftsAllSides();

            EdgesAt_OuterCells_PickEdgeOrCorner();
            EdgesAt_CenterCell_PicksNearestCorner();

            Resize_RightBottom_Grows();
            Resize_LeftTop_MovesOrigin();
            Resize_SingleEdge_LeavesOtherAxis();
            Resize_ClampsToMinimum_WithoutDrifting();

            Insets_ShrinkAndGrow_RoundTrip();

            Unmaximize_KeepsCursorRelativePosition();
            Unmaximize_ZeroWidth_CentersOnCursor();
        }

        private static void Offset_ShiftsAllSides()
        {
            AssertEqual("offset", new Box(110, 80, 410, 380), WindowMath.Offset(Window, 10, -20));
        }

        private static void EdgesAt_OuterCells_PickEdgeOrCorner()
        {
            AssertEqual("top-left", Edges.Left | Edges.Top, WindowMath.EdgesAt(Window, 110, 110));
            AssertEqual("top", Edges.Top, WindowMath.EdgesAt(Window, 250, 105));
            AssertEqual("top-right", Edges.Right | Edges.Top, WindowMath.EdgesAt(Window, 395, 110));
            AssertEqual("left", Edges.Left, WindowMath.EdgesAt(Window, 105, 250));
            AssertEqual("right", Edges.Right, WindowMath.EdgesAt(Window, 395, 250));
            AssertEqual("bottom-left", Edges.Left | Edges.Bottom, WindowMath.EdgesAt(Window, 105, 395));
            AssertEqual("bottom", Edges.Bottom, WindowMath.EdgesAt(Window, 250, 395));
            AssertEqual("bottom-right", Edges.Right | Edges.Bottom, WindowMath.EdgesAt(Window, 395, 395));
        }

        private static void EdgesAt_CenterCell_PicksNearestCorner()
        {
            AssertEqual("center upper-left", Edges.Left | Edges.Top, WindowMath.EdgesAt(Window, 240, 240));
            AssertEqual("center lower-right", Edges.Right | Edges.Bottom, WindowMath.EdgesAt(Window, 260, 260));
        }

        private static void Resize_RightBottom_Grows()
        {
            AssertEqual("grow br", new Box(100, 100, 450, 430),
                WindowMath.Resize(Window, Edges.Right | Edges.Bottom, 50, 30, 50, 50));
        }

        private static void Resize_LeftTop_MovesOrigin()
        {
            AssertEqual("grow tl", new Box(50, 70, 400, 400),
                WindowMath.Resize(Window, Edges.Left | Edges.Top, -50, -30, 50, 50));
        }

        private static void Resize_SingleEdge_LeavesOtherAxis()
        {
            AssertEqual("right only", new Box(100, 100, 420, 400),
                WindowMath.Resize(Window, Edges.Right, 20, 999, 50, 50));
        }

        private static void Resize_ClampsToMinimum_WithoutDrifting()
        {
            AssertEqual("clamp left", new Box(350, 100, 400, 400),
                WindowMath.Resize(Window, Edges.Left, 1000, 0, 50, 50));
            AssertEqual("clamp bottom", new Box(100, 100, 400, 150),
                WindowMath.Resize(Window, Edges.Bottom, 0, -1000, 50, 50));
        }

        private static void Insets_ShrinkAndGrow_RoundTrip()
        {
            var frame = new Box(107, 100, 393, 393);
            Insets insets = WindowMath.InsetsBetween(Window, frame);
            AssertEqual("insets shrink", frame, WindowMath.Shrink(Window, insets));
            AssertEqual("insets grow", Window, WindowMath.Grow(frame, insets));
            AssertEqual("insets measured", new Insets(7, 0, 7, 7), insets);
        }

        private static void Unmaximize_KeepsCursorRelativePosition()
        {
            var maximized = new Box(0, 0, 2000, 1000);
            AssertEqual("unmax 25%", new Box(300, 0, 1100, 600), WindowMath.Unmaximize(maximized, 800, 600, 500));
        }

        private static void Unmaximize_ZeroWidth_CentersOnCursor()
        {
            var degenerate = new Box(10, 0, 10, 0);
            AssertEqual("unmax degenerate", new Box(-90, 0, 110, 100), WindowMath.Unmaximize(degenerate, 200, 100, 10));
        }

        private static void AssertEqual<T>(string name, T expected, T actual)
        {
            TestRunner.AssertEqual(name, expected, actual);
        }
    }
}

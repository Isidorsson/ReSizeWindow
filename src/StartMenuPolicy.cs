namespace ReSizeWindow
{
    // Decides whether a Win key release should be kept from opening the Start menu.
    internal sealed class StartMenuPolicy
    {
        private bool usedDuringHold;

        public bool Enabled { get; set; }
        public bool BlockLeftWin { get; set; }

        public StartMenuPolicy()
        {
            Enabled = true;
        }

        public void MarkUsed()
        {
            usedDuringHold = true;
        }

        public bool ShouldMaskRelease(bool isLeftWin)
        {
            bool mask = usedDuringHold || (Enabled && BlockLeftWin && isLeftWin);
            usedDuringHold = false;
            return mask;
        }
    }
}

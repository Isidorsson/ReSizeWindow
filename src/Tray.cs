using System;
using System.Windows.Forms;

namespace ReSizeWindow
{
    internal sealed class Tray : IDisposable
    {
        private readonly NotifyIcon notifyIcon = new NotifyIcon();
        private readonly TrayIcons icons = new TrayIcons();
        private readonly ToolStripMenuItem enabledItem;
        private readonly DragController drag;
        private readonly StartMenuPolicy startMenu;
        private readonly AlwaysOnTop alwaysOnTop;
        private readonly Tiler tiler;

        public Tray(DragController drag, StartMenuPolicy startMenu, AlwaysOnTop alwaysOnTop, Tiler tiler)
        {
            this.drag = drag;
            this.startMenu = startMenu;
            this.alwaysOnTop = alwaysOnTop;
            this.tiler = tiler;
            enabledItem = new ToolStripMenuItem("Enabled", null, delegate { ToggleEnabled(); });

            var menu = new ContextMenuStrip();
            menu.Items.Add(enabledItem);
            menu.Items.Add(CreateAutoTilingItem());
            menu.Items.Add(CreateBlockLeftWinItem());
            menu.Items.Add(CreateAutostartItem());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, delegate { Application.Exit(); });

            notifyIcon.ContextMenuStrip = menu;
            notifyIcon.DoubleClick += delegate { ToggleEnabled(); };
            ShowState();
            UpdateTiling();
            notifyIcon.Visible = true;
            if (!alwaysOnTop.HotkeyRegistered) ShowHotkeyConflict();
        }

        public void Dispose()
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            icons.Dispose();
        }

        private void ToggleEnabled()
        {
            drag.Enabled = !drag.Enabled;
            startMenu.Enabled = drag.Enabled;
            alwaysOnTop.Enabled = drag.Enabled;
            UpdateTiling();
            ShowState();
        }

        private void ShowHotkeyConflict()
        {
            notifyIcon.ShowBalloonTip(5000, Program.AppName, "Win+Ctrl+T is already in use by another app, so always on top is unavailable.", ToolTipIcon.Warning);
        }

        private void UpdateTiling()
        {
            tiler.Enabled = Settings.AutoTiling && drag.Enabled;
        }

        private ToolStripMenuItem CreateAutoTilingItem()
        {
            var item = new ToolStripMenuItem("Auto tiling") { Checked = Settings.AutoTiling };
            item.Click += delegate
            {
                Settings.AutoTiling = !Settings.AutoTiling;
                item.Checked = Settings.AutoTiling;
                UpdateTiling();
            };
            return item;
        }

        private void ShowState()
        {
            bool enabled = drag.Enabled;
            enabledItem.Checked = enabled;
            notifyIcon.Icon = icons.For(enabled);
            notifyIcon.Text = enabled ? Program.AppName + ": Win+LMB move, Win+RMB resize" : Program.AppName + ": paused";
        }

        private ToolStripMenuItem CreateBlockLeftWinItem()
        {
            var item = new ToolStripMenuItem("Block Start menu on left Win") { Checked = startMenu.BlockLeftWin };
            item.Click += delegate
            {
                Settings.BlockLeftWin = !startMenu.BlockLeftWin;
                startMenu.BlockLeftWin = Settings.BlockLeftWin;
                item.Checked = startMenu.BlockLeftWin;
            };
            return item;
        }

        private static ToolStripMenuItem CreateAutostartItem()
        {
            var item = new ToolStripMenuItem("Start with Windows") { Checked = Autostart.IsEnabled() };
            item.Click += delegate
            {
                Autostart.SetEnabled(!item.Checked);
                item.Checked = Autostart.IsEnabled();
            };
            return item;
        }
    }
}

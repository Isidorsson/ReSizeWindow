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

        public Tray(DragController drag, StartMenuPolicy startMenu)
        {
            this.drag = drag;
            this.startMenu = startMenu;
            enabledItem = new ToolStripMenuItem("Enabled", null, delegate { ToggleEnabled(); });

            var menu = new ContextMenuStrip();
            menu.Items.Add(enabledItem);
            menu.Items.Add(CreateBlockLeftWinItem());
            menu.Items.Add(CreateAutostartItem());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, delegate { Application.Exit(); });

            notifyIcon.ContextMenuStrip = menu;
            notifyIcon.DoubleClick += delegate { ToggleEnabled(); };
            ShowState();
            notifyIcon.Visible = true;
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
            ShowState();
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

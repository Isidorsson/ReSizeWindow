# ReSizeWindow

Move and resize any window on Windows by holding the **Win** key, Linux desktop style. No need to aim for the title bar or a thin border.

| Action | Result |
| --- | --- |
| **Win + left-drag** | Move the window under the cursor. Maximized windows restore and follow the cursor. |
| **Win + Shift + left-drag** | Move the window under the cursor together with every window touching it, directly or through a chain of windows. |
| **Win + right-drag** | Resize from the nearest edge or corner. |
| **Win + Ctrl + T** | Toggle always on top for the focused window. Does nothing while paused. |

For resizing, the window is split into a 3×3 grid. Grab near an edge to move that edge, near a corner to move that corner. Grabbing the middle picks the nearest corner.

While dragging, window edges snap to other windows and to the edges of the screen's work area (15 px, scaled by DPI). Hold **Ctrl** during the drag to turn snapping off.

Releasing Win after a drag does not open the Start menu.

## Download

Grab `ReSizeWindow.exe` from the [latest release](https://github.com/Isidorsson/ReSizeWindow/releases/latest) and run it. No installer, just one file. Requires .NET Framework 4, which every Windows 10/11 machine already has.

The exe is unsigned, so Windows SmartScreen may warn on first launch. Choose **More info → Run anyway**, or build it yourself (below).

## Tray menu

The app lives in the system tray. The icon is blue when active and grey when paused.

- **Enabled**: pause or resume. Double-clicking the icon does the same.
- **Auto tiling**: tile windows automatically (see below). Off by default, remembered between restarts.
- **Block Start menu on left Win**: tapping left Win alone no longer opens Start. Win shortcuts (Win+E, Win+D, ...) keep working, and right Win or Ctrl+Esc still open Start. Remembered between restarts.
- **Start with Windows**: launch at login.
- **Exit**

## Auto tiling

With **Auto tiling** on, windows on each monitor are arranged in a dwindle layout: the first window fills the screen, and each new one takes half of the previous window's space, splitting it along its longer side, until you resize them. There is an 8 px gap (scaled by DPI) between windows and around the screen edge. Turning it on tiles every open window. Turning it off leaves them where they are. Pausing the app (**Enabled**) also pauses tiling.

Tiled: normal top-level windows that can be resized. Floating: always-on-top windows, tool windows, dialogs and other owned windows, minimized and maximized windows (they rejoin the layout when restored), and anything on another virtual desktop.

Win + drag a tiled window onto another to swap their places. Drop it on another monitor to move it there. A title-bar drag does the same. The layout waits until the drag ends. Resizing a tiled window (Win + right-drag or its border) moves the split it shares with its neighbours, so they shrink or grow to fill the space and the new size sticks. Edges on the screen border stay put. Each window keeps at least 10% of its split. Toggling always on top takes a window out of the layout or puts it back at the end. Win + Shift + drag rarely has anything to carry along while tiling is on, since tiled windows have gaps between them.

## Build

No install needed. It compiles with the C# compiler that ships with Windows (.NET Framework 4).

```powershell
./build.ps1   # -> bin/ReSizeWindow.exe
./test.ps1    # runs the unit tests
```

Exit the running app before rebuilding, since Windows locks the exe while it runs.

## Limitations

- Windows running as administrator (Task Manager, elevated terminals) can't be moved unless ReSizeWindow is also run as administrator.
- Maximized windows and fixed-size windows aren't resized. Win + right-click passes through to them as a normal click.

## How it works

Low-level mouse and keyboard hooks (`WH_MOUSE_LL`, `WH_KEYBOARD_LL`). The Win+click is swallowed and the target window is moved with `SetWindowPos`; a group of touching windows moves in a single `DeferWindowPos` batch. Snapping and touch detection work on the visible frame from `DwmGetWindowAttribute(DWMWA_EXTENDED_FRAME_BOUNDS)`, since on Windows 10/11 `GetWindowRect` includes about 7 px of invisible resize border, and the result is converted back to a window rect before it is applied. Auto tiling listens to window events (`SetWinEventHook`) and re-lays out all windows in one batch (`DeferWindowPos`) whenever one appears, closes, minimizes, restores or is dropped. To keep Start closed, the Win key release is held back and replayed after an unassigned key (`vkE8`), so Windows treats Win as part of a shortcut.

Always on top is a registered hotkey (`RegisterHotKey`) that flips `HWND_TOPMOST` / `HWND_NOTOPMOST` on the focused window. If another app such as PowerToys already owns Win+Ctrl+T, a tray notification says so and the shortcut stays off.

## License

[MIT](LICENSE)

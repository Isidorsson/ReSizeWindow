# ReSizeWindow

Move and resize any window on Windows by holding the **Win** key, Linux desktop style. No need to aim for the title bar or a thin border.

| Action | Result |
| --- | --- |
| **Win + left-drag** | Move the window under the cursor. Maximized windows restore and follow the cursor. |
| **Win + right-drag** | Resize from the nearest edge or corner. |

For resizing, the window is split into a 3×3 grid. Grab near an edge to move that edge, near a corner to move that corner. Grabbing the middle picks the nearest corner.

Releasing Win after a drag does not open the Start menu.

## Download

Grab `ReSizeWindow.exe` from the [latest release](https://github.com/Isidorsson/ReSizeWindow/releases/latest) and run it. No installer, just one file. Requires .NET Framework 4, which every Windows 10/11 machine already has.

The exe is unsigned, so Windows SmartScreen may warn on first launch. Choose **More info → Run anyway**, or build it yourself (below).

## Tray menu

The app lives in the system tray. The icon is blue when active and grey when paused.

- **Enabled**: pause or resume. Double-clicking the icon does the same.
- **Block Start menu on left Win**: tapping left Win alone no longer opens Start. Win shortcuts (Win+E, Win+D, ...) keep working, and right Win or Ctrl+Esc still open Start. Remembered between restarts.
- **Start with Windows**: launch at login.
- **Exit**

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

Low-level mouse and keyboard hooks (`WH_MOUSE_LL`, `WH_KEYBOARD_LL`). The Win+click is swallowed and the target window is moved with `SetWindowPos`. To keep Start closed, the Win key release is held back and replayed after an unassigned key (`vkE8`), so Windows treats Win as part of a shortcut.

## License

[MIT](LICENSE)

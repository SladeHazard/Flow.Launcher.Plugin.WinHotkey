# ArcWinHotKey - Flow Launcher Win Hotkey Plugin

ArcWinHotKey lets you activate Flow Launcher using Windows key shortcuts, including the original `LWin` (Left Windows) button or the new `LWin + Space` combination, instead of the default `Alt + Space` hotkey.

The plugin uses the native Windows low-level keyboard hook and `SendInput` APIs.
It does not include AutoHotkey or execute AutoHotkey scripts. Like the original
AutoHotkey implementation, it opens Flow by sending Flow Launcher's configured
hotkey, allowing the normal global-hotkey path to handle activation.

> **Gaming/anti-cheat notice:** this version has no AutoHotkey dependency, but
> it does generate synthetic keyboard input. No input-hook or automation tool
> can be guaranteed compatible with every anti-cheat product.

## Installation

Because ArcWinHotKey is not published to the Flow Launcher package repository, you need to install it manually:

1. Clone or download this repository.
2. Build the plugin with the .NET SDK:

   ```
   dotnet build -c Release
   ```

   This creates the plugin binaries under `bin/Release/`.
3. Copy the entire contents of that folder into Flow Launcher’s plugin directory, e.g. `%LOCALAPPDATA%\FlowLauncher\Plugins\ArcWinHotKey`.
4. Restart Flow Launcher so it can load the newly copied plugin.

   ![Flow Launcher Settings](Flowlaunchersettings.png)

## Usage

- To trigger Flow Launcher, press the hotkey configured in the plugin settings (e.g., `LWin`, `LWin + Space`, or `LCtrl + Space`). For a Space chord, press Space while holding the modifier and then release the modifier. The plugin consumes Space so Windows or the foreground application cannot handle the same chord first; the single-modifier press timeout does not apply.
- For Main Windows shortcuts like `Win + R` or `Win + D`:

  - Hold down the `LWin` button until the timeout exceeds `200 ms` by default (which can be changed in settings), then press the desired key combination.

- To show the start menu, hold down the `LWin` button until the timeout exceeds `200 ms`, then release the `LWin` button.

  ## Considerations:

- Any changes to the Timeout setting will not apply until the Flow Launcher is restarted.
- Any modifications to the Flow Launcher Hotkey plugin will not work properly until the Flow Launcher is restarted.

# ArcWinHotKey - Flow Launcher Win Hotkey Plugin

ArcWinHotKey lets you activate Flow Launcher using Windows key shortcuts, including the original `LWin` (Left Windows) button or the new `LWin + Space` combination, instead of the default `Alt + Space` hotkey.

## Installation

Because ArcWinHotKey is not published to the Flow Launcher package repository, you need to install it manually:

1. Clone or download this repository.
2. Build the plugin with the .NET SDK:

   ```
   dotnet build -c Release
   ```

   This creates the plugin binaries under `bin/Release/net7.0-windows/`.
3. Copy the entire contents of that folder into Flow Launcher’s plugin directory, e.g. `%LOCALAPPDATA%\FlowLauncher\Plugins\ArcWinHotKey`.
4. Restart Flow Launcher so it can load the newly copied plugin.

   ![Flow Launcher Settings](Flowlaunchersettings.png)

## Usage

- To trigger Flow Launcher, press the hotkey configured in the plugin settings (e.g., `LWin`, `LWin + Space`, or `LCtrl + Space`).
- For Main Windows shortcuts like `Win + R` or `Win + D`:

  - Hold down the `LWin` button until the timeout exceeds `200 ms` by default (which can be changed in settings), then press the desired key combination.

- To show the start menu, hold down the `LWin` button until the timeout exceeds `200 ms`, then release the `LWin` button.

  ## Considerations:

- Any changes to the Timeout setting will not apply until the Flow Launcher is restarted.
- Any modifications to the Flow Launcher Hotkey plugin will not work properly until the Flow Launcher is restarted.

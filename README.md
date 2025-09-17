# ArcWinHotKey - Flow Launcher Win Hotkey Plugin

ArcWinHotKey lets you activate Flow Launcher using Windows key shortcuts, including the original `LWin` (Left Windows) button or the new `LWin + Space` combination, instead of the default `Alt + Space` hotkey.

## Installation

1. Begin by installing the plugin.

   ```
   pm install ArcWinHotKey
   ```
2. ~~Ensure that your Flow Launcher hotkey is set to `Alt + Space`.~~

   ![Flow Launcher Settings](Flowlaunchersettings.png)

## Usage

- To trigger Flow Launcher, press the hotkey configured in the plugin settings (e.g., `LWin` or `LWin + Space`).
- For Main Windows shortcuts like `Win + R` or `Win + D`:

  - Hold down the `LWin` button until the timeout exceeds `200 ms` by default (which can be changed in settings), then press the desired key combination.

- To show the start menu, hold down the `LWin` button until the timeout exceeds `200 ms`, then release the `LWin` button.

  ## Considerations:

- Any changes to the Timeout setting will not apply until the Flow Launcher is restarted.
- Any modifications to the Flow Launcher Hotkey plugin will not work properly until the Flow Launcher is restarted.

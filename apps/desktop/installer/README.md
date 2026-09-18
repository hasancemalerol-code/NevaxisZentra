# NevaxisZentra Desktop Installer

## Shortcut behavior (Development and production installers)

The per-machine MSI always creates both shortcuts:

- **All-users Start Menu:** `%ProgramData%\Microsoft\Windows\Start Menu\Programs\NevaxisZentra\NevaxisZentra.lnk`
- **All-users Public Desktop:** `%Public%\Desktop\NevaxisZentra.lnk`

There is no optional desktop-shortcut checkbox. Shortcuts are advertised on `NevaxisZentra.Desktop.exe` and converted to standard `.lnk` files at install time via `DISABLEADVTSHORTCUTS=1`.

## WebView2 bootstrapper

The Microsoft Evergreen bootstrapper is not stored in source control. Packaging downloads it into `apps/desktop/installer/.cache/webview2/` and verifies the pinned SHA-256 in `redistributables/webview2-bootstrapper.json` before embedding it in the MSI.

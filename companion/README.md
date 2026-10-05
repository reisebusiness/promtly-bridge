# Promtly companion

A compact Windows project dock with Grove, an original forest adventurer.
No ChatGPT or Codex installation is required. **Automatic** mode uses an existing
supported Codex Windows pet when present, attaching the dock around its live
window and hiding Grove. When that pet closes or its version/geometry cannot be
verified, Grove returns. Choose **Pet: Use Grove** in the tray/right-click menu
to keep the independent companion. This app never replaces another pet's art.

The current adapter covers Codex 26.930's default 112×121-DIP logical placement
with its 80×87-DIP drawing. Other versions/custom sizes and ChatGPT Work pets
need their own verified adapter; they use Grove for now. Existing Codex pets
keep their own animation; cursor gaze belongs to Grove. Only pet-open/placement
UI fields are projected with an 8-MiB streaming limit. Other fields are skipped,
and no Codex state is written, deserialized into a settings map or packaged.

Hover over Grove to reveal Hugin, Parley × Promtly, an optional Claude shortcut,
AEVUM and two editable slots, initially Your project and Folders. The Hayden
preset replaces Your project with Cashflo. Hover labels stay available and are clickable.
Drag the character to move the dock. Right-click → **Dock settings** to change
any destination, or click an empty project slot. The tray menu provides settings
and Exit. Pin **Promtly companion** from Windows Start after setup.

## Run the local package

The ZIP contains the executable, its source, assets, public Promtly bridge,
licenses and an SHA-256 manifest. Verify its outer checksum against the sender's
prompt before extracting it. Inspect README.md, setup.ps1 and the source first.

Portable: double-click `Launch.cmd` in the extracted folder. Nothing is installed.
For Desktop/Start shortcuts, run from that folder in Windows PowerShell:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\setup.ps1 -Open
```

This copies only manifest files to `%LOCALAPPDATA%\PromtlyDock\versions\0.1.0`.
It preserves existing settings and refuses conflicting installations. Startup
is **off** unless you explicitly add `-Startup`. No administrator access, service,
scheduled task, account creation or subscription is required. The package is
unsigned; the source and checksums are available for inspection.

For the full Parley × Promtly Windows preview, use the matching `Parley-Setup.exe`
provided beside this companion ZIP. That separately licensed preview includes
its own Node runtime and local Promtly board/bridge. It starts a fresh personal
workspace and asks for per-user installation/start-at-sign-in consent. Its
current installer opens local setup in the browser; it is distinct from the
repository's private WebView desktop shell. The open-source companion ZIP alone
does not install that workstation. `SETUP_PROMPT.md` is the Cashflo preset;
`PUBLIC_SETUP_PROMPT.md` is the generic article prompt. The matching binaries,
checksums and both prompts are published in the
[Windows companion preview release](https://github.com/reisebusiness/promtly-bridge/releases/tag/v0.1.1-companion-preview).

Windows 10/11 x64 with .NET Framework 4.8 is required. Source builds use Windows'
existing compiler: `powershell.exe -NoProfile -File .\build.ps1`. Native tests:
`pwsh -NoProfile -File .\test.ps1`. Test output uses a fresh private profile.

## Destinations and your own projects

- Hugin opens `https://hugin.studio`.
- Parley opens an existing local Start-menu shortcut when installed; otherwise
  `https://hugin.studio/parley`. Parley's private workstation is a separate
  product and is **not** bundled into this open-source bridge extension.
- Promtly reuses its local overlay on port 37222. With Node on PATH, it can start
  the included bridge and summon the overlay. If Node is absent or the bridge
  cannot start, it opens `https://promtly.dev`. Install Node LTS from
  `https://nodejs.org/en/download` for the local prompt overlay.
- Claude prefers its installed Windows shortcut. Setup can resolve an exact
  Claude app from Windows Start's inventory; otherwise the fallback is
  `https://claude.ai`. Choose the installed app manually in settings if necessary.
- AEVUM opens `https://aevumresearch.com/play`.
- Your project starts empty. Click it to add your own destination. Setup with
  `-Preset Hayden` initializes a fresh profile with `https://cashflo.org/` and
  the approved blue/mint chrome CF monogram. Existing settings are preserved.
- Hover over Folders to expand a rounded quick-access panel with your saved
  locations first, then Documents, Downloads and Desktop. Move into the panel
  and click a row to open it; crossing the gap has 800 ms of grace. **+ Add folder**
  opens inline path/name fields: paste a local folder path and Save. **Edit** lets
  you click a saved row to rename/change its path or × to remove only its shortcut.
  There is no separate picker or management dialog. Up to 24 locations persist
  locally with a recoverable `.previous` copy. Missing folders keep their shortcut;
  failures appear inside the panel. An unreadable saved file is preserved and
  protected from overwriting.

Both project slots can be replaced, cleared or pointed at your own apps/URLs.

Targets accept HTTPS links, local `.exe` / `.lnk` shortcuts or local project
folders. No arbitrary shell text, command arguments, credentials or scripts
are accepted in settings. A selected shortcut is a user-trusted Windows file;
its own contents determine what Windows launches. The original amber symbol is
an independent launch icon, not an Anthropic logo or endorsement.

Folder shortcuts live in `%LOCALAPPDATA%\PromtlyDock\folders.json`.
Settings live in `%LOCALAPPDATA%\PromtlyDock\settings.json`, with a recoverable
`.previous` copy. `PROMTLY_DOCK_HOME` selects a different local profile. The dock
reads only the bounded Codex pet UI projection in Automatic mode; chats, clipboard
and Claude account storage are outside the adapter. Normal
bridge behavior is documented separately in `BRIDGE-README.md`: Promtly reads
the clipboard only for its explicit capture/paste features and mirrors its web
board from promtly.dev. Its pads remain in the bridge's existing local store.

## Cursor response and resource limits

Grove uses nine generated directional sprite poses. It picks the direction from
the cursor relative to its face, including hovered icons. Direction changes have
a short dwell; this is a sprite companion, not a rigged 3D avatar. Hover feedback
eases and presses cancel when released on another target. Reduced system motion
and the motion setting are respected. The pointer probe runs at 10 Hz, rendering
only on a pose/state change; eased hover and launch feedback use a 30 Hz timer
that stops once settled. Art is cached per scale. All animation runs locally,
with no model calls, GPU server or account request at runtime.

## Public source path

Published as the `companion/` extension in
[reisebusiness/promtly-bridge](https://github.com/reisebusiness/promtly-bridge/tree/main/companion).
The website is separately licensed.
See `LICENSE` for the MIT source license and `ASSETS.md` for artwork provenance.
No private workstation code, task database, credentials or personal avatar is
part of this package. To exit, use the tray menu. Remove its shortcuts and
version folder if desired; retain the settings/pads folder to keep your data.

# promtly-bridge

**The local half of [promtly.dev](https://promtly.dev) — open, because you are
about to run it on your own machine.**

Promtly is a prompt launcher: one chord over whatever you are already in, and
the sentence you keep retyping lands in that window's text box. The board is a
web page. This file is the part a web page is not permitted to be.

It is **one Node script, no dependencies**, about 2,000 lines. It is published
for one reason: it presses keys into your windows and reads your clipboard, and
you should be able to read exactly how before you trust it with that.

```bash
node promtly-bridge.mjs
```

Or download **`promtly-bridge.cmd`** from [promtly.dev/bridge](https://promtly.dev/bridge)
and double-click it — it fetches this script itself and starts it.

## Windows companion and Parley preview

The optional [open-source companion](companion/README.md) adds an original
forest adventurer, a project dock, clickable hover labels and saved folder
shortcuts. It can sit around a supported existing Codex pet or use Grove by itself.

Download the [Windows preview release](https://github.com/reisebusiness/promtly-bridge/releases/tag/v0.1.1-companion-preview).
The release includes the MIT companion ZIP and a separately licensed Parley
Windows preview with Node and the local Promtly board/bridge. The preview opens
local setup in your browser. It is unsigned; hashes and source are available.
No ChatGPT subscription is required. Installation needs your Windows consent;
ordinary chat without local execution tools needs the documented manual steps.

Copy one prompt into a coding assistant with local Windows tools:

- [Public setup prompt](companion/PUBLIC_SETUP_PROMPT.md): an empty project slot plus Folders.
- [Cashflo setup prompt](companion/SETUP_PROMPT.md): Cashflo plus Folders.

Existing settings win over presets. The prompts include public download links
and fixed SHA-256 checksums. No private workstation source or personal queue is included.

## What it does, and what it refuses to do

| | |
| --- | --- |
| **Binds to 127.0.0.1 only** | `server.listen(PORT, "127.0.0.1")` — nothing off your machine can reach it |
| **Talks only to promtly.dev** | two kinds of outbound call, both to that one host: fetching the board, and asking every six hours whether a newer bridge exists. Neither sends anything of yours |
| **Answers only named origins** | a state-changing request without an allowed `Origin` is refused, so another site cannot drive it. No local origin is trusted by default |
| **Verifies before it types** | focuses the target, checks focus actually landed, and declines to send Ctrl+V if Windows refused the switch |
| **Keeps your data local** | pads live in browser storage and a local `pads.json` snapshot for companion tools. Captured selections stay in memory until saved as pads |

That fourth row is the important one. A prompt typed into the wrong window is
worse than no paste, so it would rather tell you to press Ctrl+V yourself.

### What it does write to disk

Being precise, because "stores nothing" would be a lie:

| path | what |
| --- | --- |
| `promtly-local/theme.css` | yours to edit; created once and never overwritten |
| `promtly-local/cache/` | a copy of the board from promtly.dev, so the launcher opens offline |
| `PROMTLY-CUSTOMIZE.md` | the token reference, rewritten when it changes |
| a temp `.ps1` | the PowerShell driver source, in the OS temp dir |
| a Startup `.cmd` | **only** if you ask for it (`--install-startup`) |
| `promtly-bridge.mjs.bak` | the previous version, kept when you take an update |
| `%LOCALAPPDATA%/Promtly/pads.json` | deck names and complete prompt text shared by the local board; writes replace the snapshot atomically |
| `%LOCALAPPDATA%/Promtly/settings.json` | the selected destination app |

Pad text is readable by other programs on the same machine through the loopback
API. `PROMTLY_PADS_FILE` can choose another private path. Do not put it in a
public website folder. Settings and usage counts from the board are not shared.

### Bundled board mode

Parley's local preview can ship a board alongside this bridge. Setting
`PROMTLY_BOARD_DIR` selects that versioned, SHA-256-checked bundle. The bridge
then makes no mirror or update requests and refuses standalone self-updates;
replace the complete package to upgrade. Missing or changed files produce a
local recovery message. The board remains a separately licensed component.

Mirror mode coalesces duplicate fetches, limits requests to 15 seconds and 8 MiB,
and warms at most four static assets simultaneously. These limits bound local
work; they do not establish savings in model cost or operator time.

## How it works

Three things a browser tab cannot do, which is the entire reason this exists:

1. **Answer a global hotkey.** It runs two long-lived PowerShell hosts driving
   `user32.dll` — one blocking on stdin for commands, one blocking on a
   `GetMessage` loop for hotkeys. One process cannot do both.
2. **Focus the window you came from.** Targeting is *armed at summon*, not
   guessed at paste: the chord records the window you were in, then raises the
   launcher over it.
3. **Press the keys.** `AttachThreadInput` + `SetForegroundWindow`, then a
   verification that focus settled before any keystroke is sent.

It also **serves the board over `127.0.0.1:37222`**, mirrored from promtly.dev
with a 60-second freshness window. That is not a convenience: Chrome gates
public-origin requests to loopback and a gated request hangs silently. Served
from here, the page is same-origin with this process and there is nothing to
negotiate — and because the mirror is cached, the launcher still opens with no
connection.

## Make it yours

The bridge injects two files from `promtly-local/`, next to itself, into the
page it serves:

| file | what it does |
| --- | --- |
| `theme.css` | every colour, corner and metric is a token; anything you redefine wins |
| `custom.js` | runs in the page — seed pads, add behaviour, migrate your board |

Save either and the open window follows in about a second. It also writes
**`PROMTLY-CUSTOMIZE.md`** next to itself listing every token and what it
paints, so you can hand that file to a coding assistant and have the first
attempt work.

## Platforms

Windows today. The board works everywhere; the keystrokes do not.

**macOS and Linux support is the most valuable thing missing.** The shape to
copy is `WindowsDriver` in this file — everything above it is platform-neutral.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). Two rules are load-bearing rather than
stylistic: **no dependencies**, and **nothing of the user's leaves the machine**.

## Licence

MIT — see [LICENSE](LICENSE). The promtly.dev board is a separate, closed
codebase; this is the part that runs on your computer.

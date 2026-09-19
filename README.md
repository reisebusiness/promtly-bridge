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

## What it does, and what it refuses to do

| | |
| --- | --- |
| **Binds to 127.0.0.1 only** | nothing off your machine can reach it |
| **One outbound request** | fetches the board from promtly.dev; sends nothing of yours with it |
| **Answers only named origins** | a state-changing request without an allowed `Origin` is refused, so another site cannot drive it |
| **Verifies before it types** | focuses the target, checks focus actually landed, and declines to send Ctrl+V if Windows refused the switch |
| **Stores nothing** | your pads live in your browser's storage; this process holds a window handle and a clipboard string |

The last one is the important one. A prompt typed into the wrong window is
worse than no paste, so it would rather tell you to press Ctrl+V yourself.

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

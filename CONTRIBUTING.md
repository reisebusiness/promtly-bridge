# Contributing

## Running it

```bash
node promtly-bridge.mjs
```

No install step — that is the point. It needs Node and nothing else. It will
serve the board at `http://127.0.0.1:37222` and print a line for every paste,
including what it targeted and whether focus settled. That log is usually the
whole diagnosis for a bug report.

```bash
node --check promtly-bridge.mjs        # what CI runs
node promtly-bridge.mjs --install-startup    # start at sign-in
node promtly-bridge.mjs --uninstall-startup
```

## Two rules that are load-bearing

**No dependencies.** This is code people download from a website and run on
their own machine. Every dependency is something they would have to trust too,
and "audit this one file" stops being a real offer the moment there is a
`node_modules`. Node's standard library has been enough for 2,000 lines.

**Nothing of the user's leaves the machine.** It binds to `127.0.0.1`, and its
one outbound request fetches the board. A change that sends prompts, window
titles or clipboard contents anywhere is not a feature request — it is a
different program, and it would break the promise the README makes.

## What is most wanted

**macOS and Linux keystroke support.** Everything above `WindowsDriver` is
platform-neutral: the HTTP surface, the mirror cache, targeting, the paste
verification contract. A `MacDriver` implementing the same small interface
(`target`, `arm`, `focus`, `paste`, `place`, `dismiss`) is the whole job.

**Bug reports with the bridge's own output**, which names what it did and where.

## Style

Match what is there. Comments explain *why*, especially the traps — most of
them cost somebody a day, and the next person should not pay it twice. If you
fix something subtle, say what it was.

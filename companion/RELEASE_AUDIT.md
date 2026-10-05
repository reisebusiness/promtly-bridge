# Public release audit — October 4

The corrected Windows preview was checked against the extracted installer,
companion ZIP, embedded Android APK and all ten existing public commits.
The scan covered 2,101 files/blobs and approximately 302 MB, including ASCII
and relevant UTF-16 binary strings, PNG metadata and private queue identifiers.
No credentials, private task/chat data, user configuration, personal avatar
assets, signing keys or source maps were found. Approved platform marks, the
original Grove companion and public product links remain intentional defaults.

The initial installer contained an internal connection default and operator
documentation references. That release was withdrawn; the replacement removes
both and requires an explicit connection address. The build now rejects those
internal defaults before packaging. Private reviewed service sources are unchanged.

Reviewed flags were public support contacts, a dummy email in a security fixture,
Node/npm license authors and npm documentation explicitly containing a sample
password. The repository already had a Gmail Git author email before this release;
new public commits use the account noreply address. History was preserved.

Checks: 29 focused regressions passed, installer extraction/self-test passed,
fresh packaged task workflow passed with zero provider calls, and bundled offline
Promtly startup, Unicode save and invalid-save preservation passed. The unchanged
companion had 4,147 native assertions pass. This audit does not certify every
possible runtime behavior or a fresh-user attended installation.

# AGENTS.md

## Cursor Cloud specific instructions

### What this repo is
A single **Unity 2023.1.22f1** 2D action-platformer game (C#). There is **no backend, database, or network layer** — it is a standalone offline game. The Unity version is pinned in `ProjectSettings/ProjectVersion.txt`; do not change it. Dependencies are Unity packages declared in `Packages/manifest.json` and resolved automatically by the Editor into the gitignored `Library/` on first open (no `npm`/`pip`/`make` step exists).

### Unity Editor location
The matching Editor is pre-installed (baked into the VM snapshot) at:
`/opt/unity/editor-2023.1.22f1/Editor/Unity`
`Unity -version` confirms `2023.1.22f1`.

### Licensing (required before anything runs)
Unity refuses to compile, test, build, or run without an activated license, even in `-batchmode`. A free **Personal** license works. Activation is a one-time step and is NOT part of the update script.

Two supported ways to activate on a fresh VM:
- Preferred: store a Unity `.ulf` license file contents in the `UNITY_LICENSE` secret, write it to a file, and import it:
  `printf '%s' "$UNITY_LICENSE" > /tmp/Unity.ulf && /opt/unity/editor-2023.1.22f1/Editor/Unity -quit -batchmode -nographics -manualLicenseFile /tmp/Unity.ulf -logFile -`
  (The `.ulf` is obtained by generating an `.alf` with `-createManualActivationFile` and uploading it at https://license.unity3d.com/manual with a free Unity account.)
- Alternatively, activate with account credentials via secrets `UNITY_EMAIL` / `UNITY_PASSWORD` (and `UNITY_SERIAL` only for Plus/Pro).

Check activation succeeded in the log (`License ... successfully` / no "No valid Unity Editor license found").

### Compile / open / test / build / run
A virtual X display is available at `DISPLAY=:1` for interactive play or running a built player. Batch commands do not need it (add `-nographics`).
- Open/compile the project headless (also imports assets on first run — can be slow):
  `/opt/unity/editor-2023.1.22f1/Editor/Unity -quit -batchmode -nographics -projectPath . -logFile -`
- Run EditMode/PlayMode tests (Test Framework is installed, but the repo currently has no test assemblies):
  `/opt/unity/editor-2023.1.22f1/Editor/Unity -runTests -batchmode -projectPath . -testResults /tmp/results.xml -logFile -`
- Build a Linux player: requires an editor build script (a static method invoked via `-executeMethod`); none exists in the repo yet.
- Play/interactive testing uses the single scene `Assets/Scenes/SampleScene.unity`.

### Gotchas
- The first headless open is slow (asset import populates `Library/`); subsequent opens are fast. Do not delete `Library/`.
- Only one Unity process may hold the project at a time (a lock is held on `Temp/UnityLockfile`).

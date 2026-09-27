# Operation Sindoor

A fictional single-player aviation campaign for Windows. Five linked missions, story sequences before flight, original procedural aircraft and environments, cockpit and chase views. Built with Unity **6000.6.0f1**, C#, URP 17.6 and Input System 1.19.

## Play

Run `Builds/Windows/OperationSindoor.exe`. Keep it with `UnityPlayer.dll`, `MonoBleedingEdge` and `OperationSindoor_Data`. The player does not require Unity to be installed.

For a portable copy, extract `Builds/OperationSindoor-Windows.zip`, then run `OperationSindoor/OperationSindoor.exe` inside the extracted folder. Extract the entire archive before launching. Documentation and validation reports are included.

Choose **PLAY** to begin. ENTER advances cinematic shots. In the cockpit ENTER starts the engine, W raises throttle and DOWN ARROW raises the nose. **H toggles route assistance**, including landing. Manual pitch, yaw or roll disengages it. Assistance turns toward a selected target in combat; you still control weapons and countermeasures.

| Action | Input |
|---|---|
| Throttle | W / S |
| Nose down / up | Up / Down arrow |
| Roll | A / D |
| Yaw | Q / E |
| Mouse steering | Hold right mouse and move |
| Afterburner | Left Shift |
| Select / identify target | F |
| Guided missile | R after lock |
| Cannon | Space |
| Chaff / flare | X |
| Route assistance | H |
| Cockpit / chase | C |
| Radar | Tab |
| Engine start / shutdown / cinematic advance | Enter |
| Pause | Esc |

Primary action keys can be rebound in Controls. Controller flight is supported; menus use a mouse. Controller mapping is shown in Controls.

## Campaign

1. **Scramble**: start, take off, identify a training contact, engage, return and land.
2. **Air Defence**: protect the formation against an interception.
3. **Operation Sindoor**: valley transit, two fictional military relays, defensive combat and withdrawal.
4. **The Long Return**: airborne interception in storm conditions with limited missiles.
5. **Homecoming**: approach, landing, shutdown, reunion and tribute.

Restore the last major objective checkpoint from pause/failure menus. Campaign unlocks, scores, settings and bindings persist at `%USERPROFILE%/AppData/LocalLow/Sentinel Studio/Operation Sindoor/campaign.json`. A backup recovers damaged saves. In-flight checkpoints are session-local.

## Build and validate

Open this directory using Unity **6000.6.0f1** with Windows Build Support. This installed editor is a Unity 6 Update release, not the requested LTS line. Older editors require package migration.

**Operation Sindoor → Setup and build** creates content and all scene gateways, validates and builds Windows. No manual GameObject, material, prefab or scene assembly is needed. `Tools/Build.ps1` does the same in batch mode. The Mono build needs no C++ toolchain.

`Tools/Playtest.ps1` launches a hidden Windows player with an accelerated automated pilot using normal flight, combat and objective logic. Use `Tools/Playtest.ps1 -ShowWindow` for a visible run that also captures screenshots. Hidden Windows players do not render screenshots reliably, so the default test explicitly skips visual capture. Results go to `Artifacts`. Tests use an isolated in-memory save. This does not replace human playtesting or hardware profiling.

After a successful build and fresh campaign test, `Tools/Package.ps1` creates the portable Windows ZIP and a SHA-256 checksum. It includes runtime files and notices, and excludes Unity's development backup folder and debug symbols.

The asset upgrade adds fourteen modular prefab families, exterior/cockpit detail, PBR material maps, terrain LOD tiles, airbase structures, original particle effects and layered synthesized audio. The existing gameplay, mission definitions, controls, saves and camera paths are preserved. The earlier cinematic title screen and HUD are retained.

Run `Tools/ReviewAssets.ps1` for visible asset captures and normal-speed transit frame samples, then `Tools/CheckGameplayPreservation.ps1` for the local baseline comparison. Packaging requires fresh passing campaign, asset-review and preservation reports. The architecture and remaining art limitations are documented in [visual asset architecture](Documentation/VISUAL_ASSET_ARCHITECTURE.md). This is an integrated procedural upgrade; it does not yet meet a photorealistic AAA art standard.

Read [development report](Documentation/DEVELOPMENT_REPORT.md), [development notes](DEVELOPMENT_NOTES.md), [asset licenses](ASSET_LICENSES.md) and [attributions](ATTRIBUTIONS.md).

## Fictionalization

THIS GAME IS A FICTIONALIZED INTERPRETATION INSPIRED BY PUBLICLY REPORTED EVENTS. CHARACTERS, LOCATIONS, MISSIONS, DIALOGUE AND GAMEPLAY SCENARIOS HAVE BEEN FICTIONALIZED FOR ENTERTAINMENT.

No real operational routes, coordinates, targets, weapon parameters, personnel identities or casualty names are represented.

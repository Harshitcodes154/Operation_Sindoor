# Development notes

## Design

- One procedural world avoids asset download failures and streaming stalls.
- Five mission definitions drive a reusable objective state machine.
- World and UI objects are created automatically. The 12 scene gateways are valid standalone editor entry points; the campaign uses a persistent runtime director rather than reloading the same world.
- Custom camera direction is the equivalent fallback for Timeline/Cinemachine. Flight uses the New Input System. IMGUI supplies a resolution-scaled interface. TMP, Addressables, AI Navigation and VFX Graph are not required.
- Flight uses fictional, forgiving values. Banking induces turns, low speed causes descent, and damaged controls reduce handling.
- Optional route assistance performs flight and landing; objectives are not completed by timers.
- Unknown/friendly contacts cannot be missile targets. Cannon damage only applies to designated hostile entities. Civilian buildings cannot be scored or destroyed.
- Checkpoints snapshot the aircraft, objective, remaining contacts and loadout. Recovery grants a minimum viable hull/fuel state.

## Practical limits

This is a small independent game, not an AAA asset production. People and aircraft use original geometric art. Walking, salutes and cockpit entry are procedural, without motion capture or hand interaction. Audio is synthesized; dialogue is subtitled. Briefings are directed scenes rather than explorable interiors. The single aircraft is presented during preparation.

The world is stylized rather than photorealistic. Clouds use soft procedural textures on camera-facing planes; weather emphasizes lighting, fog and readability. There are no licensed real-world aircraft, realistic avionics, classified weapon models, multiplayer, ejection sequences, clickable cockpit switches or historical mission reconstructions.

Session checkpoints are not persistent quicksaves. Campaign progress/settings persist. The 30–60 minute pacing target and RTX 2050 60 FPS target require human playthroughs and representative profiling; neither follows from compilation or accelerated tests.

## Engineering

OperationGame partials own state, flight/combat, cinematics, UI and validation. WorldFactory places the world and resolves model factories through VisualAssetLibrary. Assets/Scripts/Visuals contains separate original asset authoring, LOD, atmospheric, cockpit, aircraft and effect presentation. GameData provides unchanged missions/preferences/save/input plus audio cue integration. OperationSindoorSetupWindow configures URP, authors content, opens scene gateways and builds the player. See Documentation/VISUAL_ASSET_ARCHITECTURE.md for model contracts.

Evidence goes to Artifacts; Windows distribution to Builds/Windows. Both are excluded from Git. Source assets, meta files, packages and settings should be committed together. No commit or remote publication is automatic.

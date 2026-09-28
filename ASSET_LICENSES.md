# Asset provenance and licenses

No ripped game, film, military-simulation, proprietary aircraft-package, or unverified repository assets are included. The aircraft designs, personnel, locations and missions are fictional.

| Asset inventory | Project source | Origin / license treatment |
|---|---|---|
| Kestrel, Raven, cockpit, gear, stores and missiles | OriginalAircraft.cs, OriginalProps.cs; Assets/GeneratedVisuals | Original procedural geometry; no manufacturer or stock model; no added third-party art license. |
| Pilots, officers, helmet, mask, uniform, modular joints, walking/salute/climb animation | OriginalCharacters.cs, CharacterPresentation.cs | Original geometry and procedural animation; existing cinematic direction retained. No scanned person or imported mocap. |
| Hangars, trucks, tower, barracks, buildings, relay, trees and rocks | OriginalProps.cs, OriginalStructures.cs | Original procedural meshes and LODs. |
| Terrain, roads, bridge, airbase infrastructure, radar array | VisualEnvironment.cs, WorldFactory.cs | Original fictional environment; not a surveyed real base. |
| PBR base-color, normal, metallic/AO/smoothness maps | VisualMaterialAuthoring.cs; Assets/GeneratedVisuals/Materials | Original deterministic synthetic maps, not measured scans. |
| Dry foothill ground base color | Assets/Art/DryFoothillAlbedo.png | Generated for this project using OpenAI's built-in image tool on 26 September 2026. No reference image supplied. Service terms apply; not a stock photograph or CC0 scan. Exact prompt in Documentation/GENERATED_ART_PROMPTS.md. |
| Title-screen key art | Assets/Resources/UI/TitleKeyArt.png | Generated for this project with OpenAI's built-in image tool during the earlier UI pass. No reference image supplied. Service terms apply. This illustration is not an in-engine screenshot. |
| Clouds, smoke, particles, terrain/water/glass shaders | WorldFactory.cs, VisualAssetBuilder.cs, VisualEffects.cs, Assets/Art/*.shader | Original procedural content and shader code; no stock VFX atlases. |
| Flag and aircraft/sleeve markings | WorldFactory.cs, OriginalAircraft.cs, OriginalCharacters.cs | Original depictions. No downloaded emblem or claim of official endorsement. |
| Engine, afterburner, wind, launch, cannon, explosion, radio cues | AudioAssetSynthesis.cs, AudioDirector in GameData.cs | Original sound synthesis. No sampled recordings or cloned voices. |
| Ambient score, warning and UI cues | AudioDirector in GameData.cs | Original synthesis. Radio dialogue is subtitled. |
| UI, story, dialogue | Existing project scripts | Original project content; no real pilot identities or military documents. |
| Unity engine, primitive meshes and runtime font | Installed Unity 6000.6.0f1 | Unity's applicable engine/editor and redistributable terms. No system font files copied. |
| URP, Input System and dependencies | Packages/manifest.json and packages-lock.json | Their package licenses and third-party notices apply. Generated player notices accompany the build. |

All fourteen generated prefab families are covered above. The owner may choose a distribution license for original project code/content. This inventory does not assert exclusive copyright in AI output or waive Unity, package or service terms. No external stock-asset attribution obligations were introduced.


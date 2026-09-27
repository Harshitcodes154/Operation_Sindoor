# Visual asset integration

## Replacement boundary

Existing game state/controller → WorldFactory factory → VisualAssetLibrary prefab → Visual Model/LOD meshes.

Assets/Resources/Visuals/VisualAssetLibrary.asset holds editable prefab references. Aircraft physics, health, AI, radar and weapons remain on the existing director/contact data. Meshes implement no flight or damage. The player's visibility adapter gates exhaust when switching to cockpit view. VisualContactPresentation adapts ground relays without changing contact spawning, health or targeting.

OriginalAircraft, OriginalCharacters, OriginalProps and OriginalStructures author fourteen original prefab families. VisualAssetBuilder persists assets and validates materials, LODs, references and joints. Regeneration overwrites generated content; keep custom replacement prefabs outside Assets/GeneratedVisuals and update the builder mapping before regenerating.

## Aircraft contract

Unity metres, +Z forward, +Y up, unit root scale. Keep the gameplay root separate. Approximate Kestrel dimensions: 17.5 m fuselage, 14.2 m span. Kestrel and Raven share these local attachment conventions:

| Attachment | X, Y, Z |
|---|---|
| CockpitCamera | 0, 1.5, 4.5 |
| CannonMuzzle | 0, -0.5, 9 |
| MissileLaunch | 0, -1, 8 |
| ExhaustL | -0.69, -0.1, -8.2 |
| ExhaustR | 0.69, -0.1, -8.2 |

These match existing gameplay offsets. Flight and weapon hits still use the original analytical terrain/flight and swept-hit calculations. Render meshes add no collision bodies. Gear is a separate visual transform. Stores are presentation geometry; ammunition logic remains authoritative. There was no bomb gameplay system to replace; this pass does not add one.

## Character contract

Preserve direct-child joints named Leg L, Leg R, Arm L and Arm R, +Z orientation and approximately 1.9 m stature. Existing walking, salute, preparation, entry and briefing direction animates these joints. Added meshes include gloves, mask hose, visor, uniform details and elbow children. This is a modular rigid-joint procedural rig, not a skinned production character or motion-capture set.

## Materials and lighting

Hero exterior/ground maps are capped at 1024 pixels, secondary maps at 512, rubber at 256 and smoke at 128. Base color is sRGB; normals and packed metallic/AO/smoothness are linear. Textures have compression and mipmaps. The generated rocky albedo is not a measured scan; its relief maps remain procedural approximations.

URP Lit handles opaque materials. Custom canopy shading uses Fresnel and probe reflection; terrain blends slopes and triplanar rock; water uses animated normals, Fresnel and sun glint. Runtime sky fill is explicit. Close-shot depth of field preserves existing camera paths and timing. Dawn/day/sunset/storm mission weather remains intact. AtmospherePresentation.nightLighting provides a reversible optional night override; no campaign mission was reassigned.

## Rendering budget

- Aircraft have three LODs; characters two; structures, trucks, trees and rocks three. Exact counts are recorded in Artifacts/visual-asset-validation.txt.
- Terrain uses 256 independently culled tiles with 33×33, 17×17 and 9×9 samples and skirts. Sampling preserves the gameplay height function; distant mesh approximations are not collision meshes.
- Unity performs frustum culling. SceneryVisibility adds conservative amortized terrain occlusion/distance culling for static props, never combat contacts. It is not baked indoor occlusion.
- Parts are batched by material and shared prefab meshes permit instancing. This does not guarantee one draw call for every instance.
- A sky-only 128-pixel reflection probe refreshes on startup. No full-scene live reflections or baked GI.
- Limited-distance soft cascaded shadows; Performance settings can disable them. Apron/impact lights cast no additional shadows.
- The existing bounded impact pool is retained. Each effect has bounded particle counts and lifetimes. No persistent debris physics, volumetric cloud ray marching or full-screen distortion.

## Verification and art limits

Tools/CheckGameplayPreservation.ps1 compares protected methods with the local pre-pass baseline in Artifacts/AssetUpgradeBaseline. Keep that directory to rerun this comparison. Tools/Build.ps1 opens editor scenes and checks assets and save recovery. Tools/Playtest.ps1 -ShowWindow runs all five missions with rendered evidence. Tools/ReviewAssets.ps1 loads all twelve scene gateways, captures assets and lighting, and samples normal-speed transit.

This is an integrated original procedural asset upgrade, **not a completed photorealistic AAA art production**. Humans, foliage, terrain silhouette and some props remain visibly procedural; animations are simple; radio is subtitled and audio synthesized. The model boundary supports professionally authored licensed replacements without rewriting gameplay. Automated success establishes tested paths, not subjective final art quality or every possible player action.


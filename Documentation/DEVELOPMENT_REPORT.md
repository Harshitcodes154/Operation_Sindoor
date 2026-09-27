# Development report

## Delivered systems

The existing five-mission campaign, mission/story progression, flight physics, controls, AI, targeting/radar, weapons, damage, checkpoints, saves and camera direction remain intact. The earlier title-screen and HUD redesign is retained.

The in-place asset pass adds a Resources-based visual catalog and fourteen generated prefab families: player fighter, opposing fighter, pilot, officer, cockpit, missile, hangar, truck, tree, rock, control tower, barracks, settlement building and military relay. It includes independent visual gear, stores/nozzles, instrument readouts, modular character joints, PBR maps, tiled terrain with LOD skirts, service infrastructure, original layered particle effects, optional night presentation and additional synthesized sound layers.

ASSET_LICENSES.md inventories all content. Geometry, material maps and audio are original procedural work. Two project-specific generated bitmaps supply title key art and rocky terrain albedo. No imported third-party stock models or recordings were used.

## Preservation

Tools/CheckGameplayPreservation.ps1 compares protected gameplay methods and full HUD/UI/cinematic files against the local pre-pass baseline. It also checks the mission/settings/save/input portion of GameData. Visual integration changes are limited to asset factories, missile/effect construction, exterior effect visibility, audio presentation, and opt-in test startup.

## Validation

- Editor: compilation; all twelve scene gateways opened; descendant missing-script checks; prefab/material references; decreasing LOD triangle counts; aircraft attachment and character joint presence; terrain range; save round-trip and corrupt-save backup recovery.
- Visible campaign player: all five missions through normal flight/combat/objective code with accelerated time; checkpoint recovery; pause/resume; new-mission checkpoint isolation; actual Input System mouse/controller takeover; endings and fresh screenshots.
- Visible asset player: all twelve scene gateways loaded; aircraft, pilot, cockpit, base, terrain, effects and night screenshots; normal-speed uncapped transit frame samples on the available RTX 2050/i5-12450HX/12 GB machine.
- Source preservation: protected gameplay comparison.
- Package: fresh build/test requirements, required runtime archive entries and SHA-256 checksum.

Exact results are in Artifacts/build-result.txt, editor-validation.txt, visual-asset-validation.txt, campaign-validation.txt, asset-review-validation.txt and gameplay-preservation.txt. Copies ship under Validation in the portable ZIP. Screenshots remain in Artifacts and Artifacts/AssetReview. Reports must postdate the corresponding build to establish final validation.

The frame samples measure player frame intervals in a short lightweight transit scenario. They are not a representative worst-case GPU benchmark or proof of sustained combat performance. The driver reported unreliable swap-chain frame statistics, so do not use these unusually high uncapped values as a promised gameplay frame rate. Normal play is capped at 60 FPS by the existing settings.

## Rendering

Three aircraft/prop LODs, two character LODs, frustum culling, conservative terrain occlusion for scenery, bounded effects, shared meshes/materials, compressed mipmapped 256–1024 pixel maps, limited-distance soft shadow cascades and one small sky-only reflection probe. See VISUAL_ASSET_ARCHITECTURE.md for exact contracts and limitations.

## Remaining production-art limits

This pass is playable and integrated, but it is not a finished photorealistic AAA art production. Human anatomy/rigs and animation, vegetation and terrain silhouette remain visibly procedural. Clouds remain textured billboards. Dialogue is subtitled; sound is synthesized. A professional character/aircraft/environment art pass and human playtesting remain necessary to reach the requested final realism standard. No unavailable assets, manual QA, GPU benchmark or licensed voice work is claimed.

The installed editor is Unity 6000.6.0f1, not an LTS line. No gameplay architecture rebuild, repository commit, remote publication or installer deployment is performed.


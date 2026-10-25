# Canyon Climber — first movement prototype

Created for Javed Ur Rehman. Target editor: **Unity 6000.3.8f1** (Unity 6.3). Android, portrait, URP 17.3.0.

## Open and play

1. Extract this ZIP completely into a writable folder. Keep `Assets`, `Packages` and `ProjectSettings` together inside `CanyonClimber`.
2. In Unity Hub, choose **Add project from disk** and select that `CanyonClimber` folder. Open with **6000.3.8f1**. Allow Unity to download its URP dependencies and finish importing. Internet is required for this first package resolution.
3. The first-import editor helper checks the baked meshes, assigns URP and applies Android/portrait settings. No scene geometry is generated in Play Mode.
4. Open **Assets/Scenes/CanyonPrototype.unity** if it is not already open. Set the Game view to **9:16** (for example 540 × 960).
5. Press **Play**, then **LET'S CLIMB**. Use the mouse in the Editor or a finger on Android.

If Console reports an error, resolve it before judging the controls. Use **Canyon Climber > Validate scene and assets** to check the loaded scene, references, mesh assets, materials and shader support in your actual Unity installation.

## Controls

- Touch/press anywhere below the top HUD area and drag. The initial touch position is the neutral point; a small square indicator follows your thumb.
- Aim toward a nearby hold and release to reach it. Aim farther and release to jump.
- While hanging from a hold, move the thumb left and right in rhythm with the body's swing. Pumping with the movement builds energy; pumping against it slows the swing. The drag origin stays fixed for that gesture.
- Release at a useful swing phase to carry momentum into a jump. The release direction and drag distance also contribute to the impulse.
- Return the thumb close to the original press position before releasing to cancel. There is a small dead zone.
- Green outline + short line: supported reach. Cyan pulsing outline + flight path: a catch is predicted. Orange path + warning text: no catch is predicted; releasing still commits the risky jump.
- Hands catch valid nearby holds automatically in flight. There is no separate catch button or air steering.
- Turquoise ledges are resting checkpoints. Stamina regenerates automatically there. Ordinary holds drain stamina slowly; pumping and jumping cost more effort.
- Low stamina reduces jump power; nearby supported reaches remain reliable. At zero stamina, a visible three-second slipping countdown gives you a final chance to move.
- A missed jump allows a short automatic emergency-catch window. Otherwise the climber returns to the last resting checkpoint.
- **RESET** returns to that checkpoint. **PAUSE/PLAY** freezes and resumes the simulation. **R** is a checkpoint shortcut in the Editor.
- Reaching the final turquoise hold completes the route and shows time/falls.

## Included scene and assets

`CanyonPrototype.unity` is already populated and saved, rather than an empty scene with a runtime scene generator.

- Ground and base camp, tent, supplies, trail sign and campfire stones.
- Faceted sandstone climbing tower, layered canyon mesas, loose stones, cacti and juniper trees.
- 40 distinct holds, four resting ledges/checkpoints including start and finish, and two larger jump gaps.
- A route that travels about 315 degrees around the tower and finishes near a 35-metre-high hold.
- An original modular low-poly climber with helmet, goggles, backpack, chalk pouch, harness and articulated arms/legs.
- Six original flat-normal mesh assets, a bright material palette and two URP shaders.
- Main camera, directional light, trajectory/ring renderers, portrait touch input and in-game HUD.
- URP pipeline/renderer assets, Android build settings and editor setup/validation/build helpers.

Meshes are native `.asset` files under `Assets/Art/Meshes`. Editable OBJ source copies are included in `Documentation/MeshSources`. The mesh catalog is an editor-only recovery source. The scene uses saved mesh assets directly. The visual controller only repositions the existing body parts during play.

`Documentation/ScenePreview.png` is an offline render of the supplied geometry, **not a Unity screenshot**. It does not verify the final Unity shading or runtime HUD.

## Android

Install **Android Build Support**, **SDK & NDK Tools** and **OpenJDK** for Unity 6000.3.8f1 through Unity Hub.

1. Use **File > Build Profiles** and switch to Android; wait for import/compilation.
2. Open the prototype scene and run **Canyon Climber > Validate scene and assets**.
3. Choose **Canyon Climber > Build Android APK**, or use Build Profiles with `CanyonPrototype` enabled.
4. Install the development APK on your device and test touch controls.

The helper targets ARM64, IL2CPP, OpenGL ES 3, portrait and Android API 26 or newer. It uses automatic target SDK selection from your Unity Android installation. No keystore, account credentials or commercial services are included. Debug signing is sufficient for local testing.

The game requests 60 FPS; this is a target, not a measured performance claim. Use a Development Build and the Unity Profiler to measure the actual device. A release build and store submission are outside this package.

## Tune the feel

Select **Assets/Settings/ClimbSettings.asset** in the Inspector. Its values control supported reach distance/duration, pendulum gravity, pumping, damping, swing limits, launch impulses, momentum carry, catch radius, recovery time, stamina and drag sensitivity.

Start by evaluating pump timing and jump aim. If those conflict with one thumb, keep the motor and replace the input mapping. `ClimbInput` is separate from the motor for that reason. Landscape/two-thumb controls are not implemented in this first version.

The orbit camera follows the supporting grip during hanging to reduce camera movement while pumping; it follows the player's surface position in flight. It automatically reveals the next side of the tower.

## Implementation boundary

This is a **surface-constrained arcade physics prototype**. It integrates a driven pendulum while hanging and ballistic motion while jumping, using unwrapped distance-around-the-cliff and height as simulation coordinates. World positions and orientation are mapped onto a cylindrical surface. It does not use a fully articulated Rigidbody ragdoll or unconstrained 3D climbing physics.

Hand catches use a swept proximity test in that surface model. Terrain, ledge and tower MeshColliders are present for scene geometry, but the current motor uses its own surface/ground rules rather than Unity Rigidbody collision response. It is intended for this authored tower; arbitrary overhangs, concave cliffs and out-of-surface jumps need an expanded surface/collision model.

Procedural body-part posing supplies visual animation. It is not a skinned character with production animation clips or anatomical IK. Humor comes from swinging limbs and exaggerated poses. Reach transfers are assisted interpolations, while jump/swing momentum is simulated.

Prediction uses the same fixed-step integration as the motor and shows the current launch prediction; moving your thumb or changing swing momentum updates it. Automatic catching is limited and does not teleport to a selected distant hold. There are no active weather hazards in this test.

Multiplayer, ghost recording, wind, falling debris, ice, fatigue, tools, upgrades, XP, shop and purchases are **not implemented**. Input, movement settings, route holds, presentation and camera are separated to make later changes easier. This does not constitute finished multiplayer architecture or deterministic network simulation.

## Validation status

The creation environment did not contain the Unity Editor or Android toolchain. **Unity import, C# compilation against Unity, shader compilation, Play Mode, APK building and device performance have not been run.**

Completed offline checks: C# syntax parsing, Unity YAML parsing, scene/component/GUID reference integrity, mesh buffers/bounds/winding/normals, supported-reach spacing, route circumference and numerical catch feasibility at both large gaps. See `Documentation/StaticValidation.json` for results. Numerical feasibility does not establish that the input timing is enjoyable or that the project is error-free in Unity.

Run the supplied Unity validation menu and the playtest checklist before expanding the prototype. Report any Console errors with their full text for a targeted fix.

## Technical references

The project uses Unity's own URP package and APIs. No third-party assets, paid plugins, external fonts or external game code are included.

- Unity 6.3 URP package documentation: https://docs.unity.com/en-us/engine/6000.3/manual/packages-list/packages-all/pack-core/com-unity-render-pipelines-universal
- Official URP pipeline source/API: https://github.com/Unity-Technologies/Graphics/blob/6000.3/staging/Packages/com.unity.render-pipelines.universal/Runtime/Data/UniversalRenderPipelineAsset.cs

The inspiration discussed during planning informed the general genre. All supplied meshes, scene arrangement, shader and gameplay source were created for this prototype.

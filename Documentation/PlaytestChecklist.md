# First Unity and device playtest

## Import gate

- Open in Unity 6000.3.8f1, let packages resolve and confirm no red Console messages.
- Confirm CanyonPrototype contains the saved environment and player before pressing Play.
- Run Canyon Climber > Validate scene and assets.
- Confirm all materials render in color, with no pink or missing meshes.

## Movement gate

- From the starting ledge, reach the first gold hold with a short upward-right drag.
- Aim at a nearby hold: green ring. Return thumb to neutral and release: no jump.
- Hang from a non-resting hold and pump left/right. Repeating in rhythm should increase the swing; opposing it should reduce it.
- At a long gap, build momentum, aim and release. Verify cyan prediction is understandable and that repeated similar inputs yield similar outcomes.
- Intentionally release toward empty space: orange preview should warn, without blocking release.
- Miss a jump near another hold: confirm a plausible emergency catch. Miss all holds: return to the last checkpoint.
- Reach each resting ledge: confirm feet appear supported, stamina refills and RESET returns there.
- Continue around the far side: screen-right should remain consistent, and camera movement should not make aiming confusing.
- At zero stamina, confirm the slipping warning appears before release. Check that a nearby reach can still save the climber.
- Finish the route, then CLIMB AGAIN: timer/falls/checkpoint reset.

## Mobile gate

- Build a development ARM64 Android APK with the installed Unity Android modules.
- Confirm a single touch works and that additional fingers do not steal the gesture.
- Move a finger over UI while dragging; release/cancel should not leave input stuck.
- Background the app mid-gesture, then return: game should be paused with no unintended jump.
- Check safe-area padding, HUD legibility, thumb visibility and target readability at the actual screen size.
- Profile frame time, allocations and thermals during repeated full runs. The requested 60 FPS must be measured.

## Decision after testing

Record: device; average frame time; accidental jump count; whether players understand green/cyan/orange; whether they can deliberately repeat a long jump; camera discomfort; and whether one-thumb pumping interferes with aim.

If input conflicts dominate, test a landscape two-thumb input layer before adding hazards, progression or multiplayer.

# CSM Development Reference (Agent-Only)

## Platform Differences
- PCVR: Mono runtime; Harmony patches OK; file I/O OK.
- Nomad: IL2CPP; Harmony unreliable; avoid file I/O and Newtonsoft.Json.
- Use `#if NOMAD` to route Nomad to `Hooks/EventHooks.cs` and PCVR to Harmony patches.

## ModOptions System
- Options live in `Configuration/CSMModOptions.cs` as a **public static** class.
- Sliders require `valueSourceName` + `interactionType = (ModOption.InteractionType)2`.
- `defaultValueIndex` is the index into the provider array.
- Arrow lists are InteractionType `0` (default); buttons are `1`.

## EventManager Events (Nomad-safe)
- `EventManager.onCreatureKill` (Creature, Player, CollisionInstance, EventTime)
- `EventManager.onCreatureHit` (Creature, CollisionInstance, EventTime)
- Use `EventTime.OnEnd` for finalized data.
- No `onCreatureParry` or `onRagdollSlice` events; detect via deflect hooks or ragdoll flags.

## Project Structure (Key Files)
- `Configuration/CSMModOptions.cs`: UI options + preset parsing + shared trigger accessors.
- `Core/CSMModule.cs`: ThunderScript entry.
- `Core/CSMManager.cs`: main runtime logic.
- `Core/CSMModOptionVisibility.cs`: menu sync + preset application.
- `Hooks/EventHooks.cs`: Nomad event subscriptions.
- `Patches/`: PCVR Harmony patches (excluded from Nomad build).

## Build Configuration
- Nomad: `dotnet build -c Nomad`
- PCVR: `dotnet build -c Release`
- Nomad build excludes `Patches/**` and defines `NOMAD`.
- PCVR build includes Harmony.

## manifest.json (Critical)
- `GameVersion` must match the game's `minModVersion` **exactly** (`X.X.X.X`).
- Mismatch causes assemblies not to load (`Loaded 0 of Metadata Assemblies` in Player.log).

## Common Pitfalls (Actionable)
- Use `Time.unscaledTime` / `Time.unscaledDeltaTime` for timers during slow motion.
- Avoid Newtonsoft.Json and file I/O on Nomad.
- Always unsubscribe before re-subscribing to events to prevent duplicates.

## Known Limitations (Game Engine)
- **Slider drag doesn't commit values**: ThunderRoad's ModOption slider UI only commits values when arrow buttons are clicked. Dragging the slider moves it visually but does not fire the value change callback until an arrow is pressed. This is a game engine limitation affecting all mods using `InteractionType.Slider` - not fixable from mod code.

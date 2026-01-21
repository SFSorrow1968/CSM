# CSM Development Reference

This document captures important technical knowledge for developing Blade & Sorcery mods, specifically for the CSM (Conditional Slow Motion) mod.

## Table of Contents
- [Platform Differences: PCVR vs Nomad](#platform-differences-pcvr-vs-nomad)
- [ThunderRoad SDK](#thunderroad-sdk)
- [ModOptions System](#modoptions-system)
- [EventManager Events](#eventmanager-events)
- [ThunderScript Lifecycle](#thunderscript-lifecycle)
- [Project Structure](#project-structure)
- [Build Configuration](#build-configuration)
- [Common Pitfalls](#common-pitfalls)
- [Debugging](#debugging)

---

## Platform Differences: PCVR vs Nomad

### PCVR (PC VR)
- **Runtime**: Mono (.NET)
- **Harmony Patching**: Fully supported
- **File I/O**: Works normally
- **JSON Libraries**: Newtonsoft.Json works fine
- **Performance**: More headroom for complex operations

### Nomad (Quest/Android)
- **Runtime**: IL2CPP (compiled to native code)
- **Harmony Patching**: Unreliable - IL2CPP strips/modifies method signatures
- **File I/O**: Restricted on Android, avoid file-based configuration
- **JSON Libraries**: Newtonsoft.Json can cause IL2CPP issues - avoid bundling
- **Performance**: More constrained, optimize where possible

### Recommended Approach
- Use **conditional compilation** (`#if NOMAD`) to handle platform differences
- Use **EventManager hooks** for Nomad (IL2CPP safe)
- Use **Harmony patches** for PCVR (more comprehensive)
- Use **ModOptions** for configuration (works on both platforms)

---

## ThunderRoad SDK

### Key DLLs
| DLL | Purpose | Size |
|-----|---------|------|
| `ThunderRoad.dll` | Main game types (Creature, Item, etc.) | ~3.7MB |
| `Assembly-CSharp.dll` | Game-specific code | ~500KB |
| `Assembly-CSharp-firstpass.dll` | Early initialization code | Small |

### Important Namespaces
```csharp
using ThunderRoad;      // Core game types
using UnityEngine;      // Unity types
```

### Key Types
- `ThunderScript` - Base class for mod entry points
- `Creature` - NPCs and player
- `RagdollPart` - Body parts for dismemberment detection
- `CollisionInstance` - Collision/damage data
- `EventManager` - Game event subscriptions
- `ModManager.ModData` - Mod metadata

---

## ModOptions System

ModOptions is the SDK's built-in configuration system. Settings appear in the in-game book under "Mod Options".

### Correct Pattern (from CarnageReborn)
```csharp
// ModOptions must be a PUBLIC STATIC CLASS (NOT extending ThunderScript)
public static class ModOptions
{
    // Value provider methods for sliders
    public static ModOptionFloat[] FloatProvider()
    {
        return new ModOptionFloat[]
        {
            new ModOptionFloat("0.1", 0.1f),
            new ModOptionFloat("0.5", 0.5f),
            new ModOptionFloat("1.0", 1.0f)
        };
    }

    // Boolean option (simple toggle)
    [ModOption(name = "Enable Feature", category = "MyMod", defaultValueIndex = 0, tooltip = "Description")]
    public static bool myBool = true;

    // Float option with slider (interactionType = 2)
    [ModOption(name = "My Value", category = "MyMod", defaultValueIndex = 1,
               valueSourceName = "FloatProvider", interactionType = (ModOption.InteractionType)2,
               tooltip = "Description")]
    public static float myFloat = 0.5f;
}
```

### Key Points
- Class MUST be `public static` (not extending ThunderScript)
- Use named parameters: `name =`, `category =`, `tooltip =`
- For sliders: `valueSourceName = "MethodName"` + `interactionType = (ModOption.InteractionType)2`
- Provider methods return `ModOptionFloat[]` or `ModOptionInt[]`
- `defaultValueIndex` is the index into the provider array

### InteractionType Values
- `0` = ArrowList (default) - left/right arrows
- `1` = ButtonList - cycles on click
- `2` = Slider - horizontal slider

### Old Pattern (AVOID)
```csharp
// This pattern may not work reliably:
[ModOptionCategory("Category Name", orderIndex)]
[ModOptionOrder(orderWithinCategory)]
[ModOptionTooltip("Description text")]
[ModOption("Display Name", defaultValueIndex = 0)]
```

### Boolean Options
```csharp
[ModOption("Enable Feature", defaultValueIndex = 0)]  // 0 = true, 1 = false
public static bool MyFeature = true;
```

### Slider Options (Float)
```csharp
// Define value list method
public static ModOptionFloat[] MyValues()
{
    return new ModOptionFloat[]
    {
        new ModOptionFloat("Low", 0.5f),
        new ModOptionFloat("Medium", 1.0f),
        new ModOptionFloat("High", 2.0f)
    };
}

// Use in option
[ModOption("My Setting", nameof(MyValues), defaultValueIndex = 1)]
public static float MySetting = 1.0f;
```

### Category Organization
- Categories are sorted by their order index (second parameter)
- Options within categories are sorted by `[ModOptionOrder]`
- Use consistent naming: "ModName - Category" format

### Important Notes
- Options must be `public static` fields
- The class containing options should be `public static`
- Changes are applied immediately when user adjusts in-game
- No file I/O needed - game handles persistence

---

## EventManager Events

### Confirmed Working Events
```csharp
EventManager.onCreatureKill   // (Creature, Player, CollisionInstance, EventTime)
EventManager.onCreatureHit    // (Creature, CollisionInstance, EventTime)
```

### Event Signature Pattern
```csharp
private static void OnCreatureKill(
    Creature creature,           // The creature affected
    Player player,               // May be null
    CollisionInstance collision, // Damage/collision data
    EventTime eventTime          // OnStart or OnEnd
)
```

### EventTime
- `EventTime.OnStart` - Before the event completes
- `EventTime.OnEnd` - After the event completes (usually what you want)

### Events NOT in SDK (as of current version)
- `onCreatureParry` - Does not exist, use Harmony patch on `Creature.TryPush` for PCVR
- `onRagdollSlice` - Does not exist, check `RagdollPart.isSliced` in hit events

### Subscription Pattern
```csharp
public static void Subscribe()
{
    // Always unsubscribe first to prevent duplicates
    EventManager.onCreatureKill -= OnCreatureKill;
    EventManager.onCreatureKill += OnCreatureKill;
}

public static void Unsubscribe()
{
    try { EventManager.onCreatureKill -= OnCreatureKill; } catch { }
}
```

---

## ThunderScript Lifecycle

```
ScriptLoaded(ModData)  →  Called when mod DLL loads
        ↓
ScriptEnable()         →  Called when mod is enabled
        ↓
ScriptUpdate()         →  Called every frame (like Unity Update)
        ↓
ScriptDisable()        →  Called when mod is disabled
        ↓
ScriptUnload()         →  Called when mod DLL unloads
```

### Best Practices
```csharp
public class MyMod : ThunderScript
{
    public static MyMod Instance { get; private set; }
    private bool _initialized = false;

    public override void ScriptLoaded(ModManager.ModData modData)
    {
        base.ScriptLoaded(modData);
        Instance = this;

        // Initialize core systems
        // ModOptions are auto-discovered, no loading needed
        _initialized = true;
    }

    public override void ScriptEnable()
    {
        base.ScriptEnable();

        // Subscribe to events
        // Apply patches (PCVR only)
    }

    public override void ScriptUpdate()
    {
        base.ScriptUpdate();
        // Per-frame logic
    }

    public override void ScriptDisable()
    {
        // Unsubscribe from events
        // Remove patches
        base.ScriptDisable();
    }

    public override void ScriptUnload()
    {
        // Final cleanup
        Instance = null;
        base.ScriptUnload();
    }
}
```

---

## Project Structure

```
CSM/
├── Configuration/
│   ├── CSMModOptions.cs    # ModOptions definitions
│   ├── CSMConfig.cs        # Settings facade/accessor
│   └── TriggerType.cs      # Enum definitions
├── Core/
│   ├── CSMModule.cs        # ThunderScript entry point
│   ├── CSMManager.cs       # Core logic manager
│   └── CSMLogger.cs        # Centralized logging
├── Hooks/
│   └── EventHooks.cs       # EventManager subscriptions (Nomad)
├── Patches/                # PCVR only (excluded from Nomad build)
│   ├── CSMPatches.cs       # Harmony patch manager
│   ├── KillPatches.cs
│   ├── DismemberPatches.cs
│   └── PlayerPatches.cs
├── libs/                   # Game DLLs (not committed)
├── builds/                 # Build output packages
├── manifest.json           # Mod metadata for game
└── CSM.csproj             # Build configuration
```

---

## Build Configuration

### csproj Setup
```xml
<PropertyGroup>
  <TargetFramework>net472</TargetFramework>
  <Configurations>Debug;Release;Nomad</Configurations>
</PropertyGroup>

<!-- Define NOMAD symbol for conditional compilation -->
<PropertyGroup Condition="'$(Configuration)'=='Nomad'">
  <DefineConstants>NOMAD</DefineConstants>
</PropertyGroup>

<!-- Exclude Patches folder from Nomad build -->
<ItemGroup Condition="'$(Configuration)'=='Nomad'">
  <Compile Remove="Patches\**\*.cs" />
</ItemGroup>

<!-- Harmony only for PCVR -->
<ItemGroup Condition="'$(Configuration)'!='Nomad'">
  <PackageReference Include="Lib.Harmony" Version="2.2.2" />
</ItemGroup>
```

### Conditional Compilation
```csharp
#if NOMAD
    // Nomad-specific code (EventManager hooks)
    EventHooks.Subscribe();
#else
    // PCVR-specific code (Harmony patches)
    CSMPatches.ApplyPatches();
#endif
```

### Build Commands
```bash
# Nomad build
dotnet build -c Nomad

# PCVR build
dotnet build -c Release
```

### manifest.json
```json
{
  "Name": "Mod Display Name",
  "Description": "Mod description",
  "Author": "YourName",
  "ModVersion": "1.0.0",
  "GameVersion": "1.1.0.0"
}
```
**CRITICAL**: GameVersion must match the game's `minModVersion`:
- Format: `X.X.X.X` (4 parts required)
- Check Player.log for `Game version: X.X.X.X` to see current version
- For B&S 1.1.x, use `"GameVersion": "1.1.0.0"`
- **If GameVersion doesn't match, the mod will NOT load assemblies!**
- The log will show `Loaded 0 of Metadata Assemblies` if there's a version mismatch

---

## Common Pitfalls

### 1. Newtonsoft.Json on Nomad
**Problem**: Bundling Newtonsoft.Json causes IL2CPP issues.
**Solution**: Use ModOptions for configuration, avoid JSON parsing.

### 2. File I/O on Nomad
**Problem**: Android file system restrictions.
**Solution**: Use ModOptions, game handles persistence.

### 3. Harmony on Nomad
**Problem**: IL2CPP modifies method signatures, patches fail.
**Solution**: Use EventManager hooks, conditional compilation.

### 4. Missing EventManager Events
**Problem**: Some events (like `onCreatureParry`) don't exist.
**Solution**: Check SDK, use alternative detection methods.

### 5. Time.deltaTime During Slow Motion
**Problem**: `Time.deltaTime` is scaled, timers run slow.
**Solution**: Use `Time.unscaledTime` and `Time.unscaledDeltaTime`.

### 6. Null References in Event Handlers
**Problem**: Creature/collision data can be null.
**Solution**: Always null-check before accessing properties.

### 7. Duplicate Event Subscriptions
**Problem**: Events fire multiple times.
**Solution**: Always unsubscribe before subscribing.

---

## Debugging

### Log Locations
- **PCVR**: `%APPDATA%\..\LocalLow\WarpFrog\Blade & Sorcery\Player.log`
- **Nomad**: Use SideQuest or ADB to pull logs from device

### Logging Pattern
```csharp
public static class MyLogger
{
    private const string PREFIX = "[MyMod]";

    public static void Info(string msg) => Debug.Log($"{PREFIX} {msg}");
    public static void Warn(string msg) => Debug.LogWarning($"{PREFIX} {msg}");
    public static void Error(string msg) => Debug.LogError($"{PREFIX} {msg}");

    public static void Verbose(string msg)
    {
        if (MyModOptions.DebugLogging)
            Debug.Log($"{PREFIX}[V] {msg}");
    }
}
```

### Key Log Points
1. **Boot** - Platform, version, mod path
2. **Settings Ready** - Confirm ModOptions loaded
3. **Hooks Init** - Which events subscribed successfully
4. **Trigger Attempts** - Success/failure with reason
5. **Errors** - Full exception with stack trace

### Searching Logs
```bash
# Windows PowerShell
Get-Content Player.log | Select-String "\[CSM\]"

# Linux/WSL
grep "\[CSM\]" Player.log
```

---

## Useful Code Snippets

### Check if Player
```csharp
if (creature.isPlayer) return;
```

### Check Body Part Type
```csharp
var partType = ragdollPart.type;
bool isHead = (partType & RagdollPart.Type.Head) != 0;
bool isNeck = (partType & RagdollPart.Type.Neck) != 0;
bool isHeadOrNeck = (partType & (RagdollPart.Type.Head | RagdollPart.Type.Neck)) != 0;
```

### Check for Dismemberment
```csharp
if (ragdollPart.isSliced) { /* limb was cut off */ }
```

### Get Health Ratio
```csharp
float GetHealthRatio(Creature c)
{
    if (c == null || c.maxHealth <= 0) return 1f;
    return Mathf.Clamp01(c.currentHealth / c.maxHealth);
}
```

### Count Living Enemies
```csharp
bool IsLastEnemy()
{
    foreach (var c in Creature.allActive)
    {
        if (c != null && !c.isPlayer && !c.isKilled)
            return false;
    }
    return true;
}
```

### Safe Time Scale Modification
```csharp
float originalTimeScale = Time.timeScale;
float originalFixedDelta = Time.fixedDeltaTime;

// Apply slow motion
Time.timeScale = 0.2f;
Time.fixedDeltaTime = originalFixedDelta * 0.2f;

// Restore (always restore both!)
Time.timeScale = originalTimeScale;
Time.fixedDeltaTime = originalFixedDelta;
```

---

## Resources

- **BasSDK**: Included in project at `BasSDK/` folder
- **ThunderRoad Discord**: Community support for modding
- **mod.io**: Distribution platform for B&S mods

---

*Last updated: v1.1.0 - January 2026*

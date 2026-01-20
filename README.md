# CSM - Conditional Slow Motion

A Blade & Sorcery mod that adds Matrix-style slow motion effects triggered by combat events.

## Features

- **Kill Slow Motion** - Triggers on enemy kills with configurable chance
- **Critical Kills** - Head/neck hits trigger extended slow motion
- **Decapitation** - Dramatic slow motion on beheadings
- **Dismemberment** - Slow motion when limbs are severed
- **Parry** - Slow motion on successful parries
- **Last Enemy** - Epic slow motion when killing the final enemy
- **Last Stand** - Slow motion when player health drops critically low

All triggers are fully configurable with individual settings for:
- Enable/disable
- Trigger chance (0-100%)
- Time scale (how slow)
- Duration
- Cooldown

## Installation

### PCVR
1. Download the latest PCVR release
2. Extract to `[Blade & Sorcery]\BladeAndSorcery_Data\StreamingAssets\Mods\CSM\`
3. Launch the game

### Nomad (Quest)
1. Download the latest Nomad release
2. Extract to `[Device]\Android\data\com.Warpfrog.BladeAndSorcery\files\Mods\CSM\`
3. Launch the game

## Configuration

Edit `settings.json` in the mod folder to customize triggers:

```json
{
  "Enabled": true,
  "GlobalCooldown": 0,
  "Triggers": {
    "BasicKill": {
      "Enabled": true,
      "Chance": 0.15,
      "TimeScale": 0.3,
      "Duration": 1.0,
      "Cooldown": 0
    },
    "Decapitation": {
      "Enabled": true,
      "Chance": 1.0,
      "TimeScale": 0.15,
      "Duration": 2.0,
      "Cooldown": 0
    }
  }
}
```

## Building from Source

### Requirements
- .NET SDK 6.0+
- Blade & Sorcery game installation (for reference DLLs)

### Setup
1. Clone the repository
2. Copy required DLLs from your game installation to `libs/`:
   - `ThunderRoad.dll`
   - `Assembly-CSharp.dll`
   - `Assembly-CSharp-firstpass.dll`
   - `UnityEngine.dll`
   - `UnityEngine.CoreModule.dll`

   From: `[Game Install]\BladeAndSorcery_Data\Managed\`

### Build Commands
```bash
# PCVR version (uses Harmony patches)
dotnet build -c Release

# Nomad version (uses EventManager, IL2CPP compatible)
dotnet build -c Nomad
```

Output will be in `bin/Release/` or `bin/Nomad/`.

## Platform Differences

| Feature | PCVR | Nomad |
|---------|------|-------|
| Hook Method | Harmony Patches | EventManager |
| Parry Detection | Precise | Approximate |
| Dependencies | 0Harmony.dll | None |
| IL2CPP Compatible | N/A | Yes |

## Compatibility

- **Game Version**: 1.0.0.0+
- **PCVR**: Full support
- **Nomad**: Full support (IL2CPP compatible)

## License

MIT License - See [LICENSE](LICENSE) file.

## Credits

- **Author**: dkatz
- **Framework**: [ThunderRoad](https://github.com/KospY/BasSDK) by KospY/Warpfrog

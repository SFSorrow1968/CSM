# CinematicKill

A client-side mod for **7 Days to Die** that transforms every kill into a cinematic moment with slow-motion, freeze frames, camera effects, and stylish visual effects—all fully customizable in-game.

![7 Days to Die](https://img.shields.io/badge/7_Days_to_Die-v2.5-red)
![Mod Type](https://img.shields.io/badge/Type-Client--Side-blue)
![License](https://img.shields.io/badge/License-MIT-green)

## ✨ Features

### Core Effects
- **Slow-Motion Killcams** — Dramatic time slowdown on kills with configurable intensity
- **Freeze Frame** — Per-camera freeze effects with super slow-mo and contrast boost
- **Dynamic FOV** — Camera zoom during kills with customizable timing
- **Visual Effects** — Kill flash, vignette, desaturation, and blood splatter

### Smart Triggers
- **Basic Kill** — Chance-based cinematics on any kill
- **Context-Aware** — Headshots, dismemberments, long-range, low-health, criticals
- **Last Enemy** — Special extended cinematic for final kills
- **Killstreaks** — Enhanced effects for rapid consecutive kills

### Camera System
- **First Person Camera** — Stay in FP view with slow-mo, FOV zoom, and freeze effects
- **Projectile Camera** — Third-person view following the enemy with 7 preset angles
- **Independent Freeze Settings** — Each camera type has its own freeze configuration
- **Post-Freeze Actions** — End, Continue Cinematic, Switch Camera, or Skip

### Customization
- **Instant In-Game Config** — Press `\` (backslash) to open the menu
- **7 Tabs** — Main, Triggers, Camera, Effects, HUD, Advanced, Experimental
- **Import/Export** — Backup and restore your settings
- **Multi-language** — 13 languages supported

## 📦 Installation

1. Download the latest release
2. Extract the `CinematicKill` folder into your `7 Days To Die/Mods` directory
3. Start the game
4. Press `\` to open the configuration menu

## ⚙️ Quick Start

| Setting | Default | Description |
|---------|---------|-------------|
| Basic Kill Chance | 15% | Chance to trigger on any kill |
| Basic Kill Duration | 2.0s | How long the effect lasts |
| Basic Kill Time Scale | 0.20x | Slow-motion intensity |
| Trigger Chance | 33% | Chance for special trigger cinematics |
| Menu Key | `\` | Open configuration menu |

## 🔧 Building from Source

### Requirements
- .NET SDK 6.0+
- 7 Days to Die assembly references (not included)

### Build
```bash
dotnet build
```

The compiled DLL will be in `bin/Debug/` or `bin/Release/`.

## 📁 Project Structure

```
CinematicKill/
├── Config/           # Default configuration
├── Harmony/          # Harmony patches for game hooks
├── Properties/       # Assembly info
├── Scripts/
│   ├── Cinematics/   # Core cinematic effects (FOV, screen effects, settings)
│   └── Systems/      # Main systems (manager, menu, HUD, localization)
├── Init.cs           # Mod entry point
├── ModInfo.xml       # 7D2D mod metadata
└── README.md
```

## 🎮 Compatibility

- ✅ **Client-side mod** — Only affects your game
- ✅ **Singleplayer** — Designed for solo play
- ✅ **7 Days to Die v2.5** — Built for version 2.5

## 📝 License

MIT License - See [LICENSE](LICENSE) for details.

## 👤 Credits

Created by **SFSorrow1968**

---

*Transform your 7 Days to Die experience with cinematic slow-motion kills!*

# Description

## Overview (<=250 chars)
Conditional Slow Motion adds cinematic slow motion for key combat moments with presets, trigger profiles, third-person distribution, and per-trigger customization, plus killcam positioning and debug tools.

## Detailed Description (<=50,000 chars)
⚠️ **Trigger Warning:** This mod is 100% vibe coded. If the mere thought of AI-assisted development sends you into an existential spiral about the sanctity of "real" programming, I'd recommend you close this page before your monocle pops out. The code compiles, the tests pass, and the mod works exactly as intended—but I understand if that's less important to you than the private pleasure of stroking every line of code yourself. ⚠️

Conditional Slow Motion (CSM) brings controlled, cinematic slow motion to Blade & Sorcery. It triggers on meaningful combat moments and provides a clear, layered configuration model: presets set the baseline, trigger toggles control eligibility, and per-trigger custom values override everything else.

Key features:
- Trigger profiles: All, Kills Only, Highlights, Last Enemy Only, and Parry Only to quickly scope which events are active.
- Preset system: intensity, chance, cooldown, duration, transition, and third-person distribution presets set consistent defaults across all triggers.
- Per-trigger customization: adjust chance, time scale, duration, cooldown, transition curve, and third-person distribution for each trigger.
- Transition curves: Off (instant), Smoothstep (smooth ramp), or Linear. Ramp duration is 20% of slow-mo duration.
- Third-person distribution control: configurable frequency for killcam use, with first-person defaults for VR and per-trigger eligibility.
- Killcam positioning: distance, height, and orbit speed with optional randomization.
- Debug/QA tools: quick-test trigger and detailed debug logging, including effective values tooltips.

The system is designed so presets and profiles shape behavior quickly, while per-trigger controls allow fine-tuning without hidden state. It supports both PCVR and Nomad builds.

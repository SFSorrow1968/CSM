from pathlib import Path
import re

text = Path(r"C:\Users\dkatz\Documents\Projects\CSM\Core\CSMManager.cs").read_text(encoding="utf-8")

def extract_block(text: str, anchor: str) -> str:
    idx = text.find(anchor)
    if idx == -1:
        raise SystemExit("anchor not found")
    brace_start = text.find('{', idx)
    if brace_start == -1:
        raise SystemExit("no brace")
    depth = 0
    for i in range(brace_start, len(text)):
        ch = text[i]
        if ch == '{':
            depth += 1
        elif ch == '}':
            depth -= 1
            if depth == 0:
                return text[brace_start + 1:i]
    raise SystemExit("no close")

block = extract_block(text, "GetPresetValues(")
print("len", len(block))
print("has case Subtle", "case CSMModOptions.Preset.Subtle" in block)
print("preset cases", len(re.findall(r"case\s+CSMModOptions\.Preset\.(\w+)", block)))
print("trigger cases", len(re.findall(r"case\s+TriggerType\.(\w+)", block)))

m = re.search(r"case\s+TriggerType\.BasicKill\s*:(.*?)case\s+TriggerType\.", block, re.S)
print("basic match", bool(m))
if m:
    bb = m.group(1)
    print("chance assigns", re.findall(r"chance\s*=\s*([0-9.]+)f;", bb))
    print("timeScale assigns", re.findall(r"timeScale\s*=\s*([0-9.]+)f;", bb))

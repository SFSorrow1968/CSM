# -*- coding: utf-8 -*-
"""Build and release CSM mod."""
import subprocess
import json
from pathlib import Path

BASE = Path(__file__).parent.parent
MANIFEST = BASE / "manifest.json"

def get_version():
    with open(MANIFEST, 'r') as f:
        return json.load(f)['ModVersion']

def run(cmd, cwd=None):
    print(f"$ {cmd}")
    subprocess.run(cmd, shell=True, cwd=cwd or BASE, check=True)

def require_clean_non_main_git_state():
    branch = subprocess.check_output("git branch --show-current", shell=True, cwd=BASE, text=True).strip()
    if branch in {"main", "master"}:
        raise RuntimeError(f"Refusing release on protected branch '{branch}'. Use a feature/release branch.")

    status = subprocess.check_output("git status --porcelain", shell=True, cwd=BASE, text=True).strip()
    if status:
        raise RuntimeError("Working tree is dirty. Commit or stash changes before releasing.")

def main():
    version = get_version()
    tag = f"v{version}"

    print(f"\n=== Building CSM {version} ===\n")

    require_clean_non_main_git_state()
    run("powershell -ExecutionPolicy Bypass -File _agent/ci-smoke.ps1 -Strict")

    # Create zips
    print("\n=== Creating release zips ===\n")
    run(f'powershell -Command "Compress-Archive -Path bin/Release/PCVR/CSM -DestinationPath CSM-PCVR.zip -Force"')
    run(f'powershell -Command "Compress-Archive -Path bin/Release/Nomad/CSM -DestinationPath CSM-Nomad.zip -Force"')

    # Git tag (if not exists)
    result = subprocess.run(f"git tag -l {tag}", shell=True, capture_output=True, text=True, cwd=BASE)
    if tag not in result.stdout:
        run(f"git tag {tag}")

    # Push tag
    run(f"git push origin {tag}")

    # Create GitHub release
    print("\n=== Creating GitHub release ===\n")
    run(f'gh release create {tag} CSM-PCVR.zip CSM-Nomad.zip --title "CSM {version}" --notes "Release {version}"')

    print(f"\n=== CSM {version} released! ===\n")

if __name__ == '__main__':
    main()

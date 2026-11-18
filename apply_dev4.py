#!/usr/bin/env python3
"""BlockPuzzle dev.4 patch installer. Run from Unity project root after extracting zip.

    python apply_dev4.py --check
    python apply_dev4.py --apply

All originals are backed up under Library/BlockPuzzleDev4Backups.
The installer never writes AdventureSaveService, Classic or game data assets.
"""
from __future__ import annotations

import argparse
import json
import os
from pathlib import Path


def main() -> None:
    parser = argparse.ArgumentParser(description="Install BlockPuzzle v2.0.0-dev.4 source changes")
    mode = parser.add_mutually_exclusive_group(required=True)
    mode.add_argument("--check", action="store_true", help="Verify that the source matches")
    mode.add_argument("--apply", action="store_true", help="Apply the verified patches")
    args = parser.parse_args()

    root = Path(__file__).resolve().parent
    manifest = json.loads((root / "installer" / "patches.json").read_text(encoding="utf-8"))
    changes: dict[Path, tuple[str, str, bool]] = {}
    counts = {"pending": 0, "already_applied": 0}

    # Preflight every patch before writing anything.
    for patch in manifest["patches"]:
        destination = root / patch["path"]
        if not destination.is_file():
            raise RuntimeError(f"Missing Unity project file: {destination}")
        if destination not in changes:
            original_bytes = destination.read_bytes()
            newline = "\r\n" if b"\r\n" in original_bytes else "\n"
            text = original_bytes.decode("utf-8-sig").replace("\r\n", "\n")
            changes[destination] = (text, newline, False)

        source, newline, changed = changes[destination]
        before = patch["old"].replace("\r\n", "\n")
        after = patch["new"].replace("\r\n", "\n")
        occurrences = source.count(before)
        if after in source:
            # Some replacements contain the original anchor as a prefix;
            # checking the finished replacement first makes reruns safe.
            counts["already_applied"] += 1
        elif occurrences == 1:
            source = source.replace(before, after, 1)
            changed = True
            counts["pending"] += 1
        else:
            raise RuntimeError(
                f"Preflight failed at {patch['path']}: {patch['name']} "
                f"(found {occurrences} occurrences). No files changed."
            )
        changes[destination] = (source, newline, changed)

    to_write = {k: (v[0], v[1]) for k, v in changes.items() if v[2]}
    print(f"Preflight OK. Patches to apply: {counts['pending']}; "
          f"previously applied: {counts['already_applied']}; "
          f"files to modify: {len(to_write)}.")
    if args.check or not to_write:
        return

    backup_root = root / "Library" / "BlockPuzzleDev4Backups"
    for path in to_write:
        backup = backup_root / path.relative_to(root)
        backup.parent.mkdir(parents=True, exist_ok=True)
        if not backup.exists():
            backup.write_bytes(path.read_bytes())

    for path, (new_text, newline) in to_write.items():
        temp = path.with_name(path.name + ".dev4.tmp")
        temp.write_bytes(new_text.replace("\n", newline).encode("utf-8"))
        os.replace(temp, path)
        print("Patched:", path.relative_to(root))

    print("Dev.4 integration applied. Existing .meta, saves, level assets and catalogs untouched.")


if __name__ == "__main__":
    main()

"""Remap the dangling Blocks/game GUIDs in the staged sample assets to their package equivalents.

A GUID-for-GUID substitution is safe here only because every pair was resolved from the package's
own .meta files, and because Unity re-opens every asset afterwards and is required to report zero
unresolved references. The verification is what makes it a remap rather than a guess.
"""

import io, os, re, sys

STAGE = r"C:\Users\roger\AppData\Local\Temp\claude\C--Repos-Github-RunnerUGSTemplate\be5ea66d-27b9-4b2b-b4df-6c5e359cd31d\scratchpad\SamplesStaging\Assets\UGS-Scenes"

# old guid -> (new guid, what it is).  All new guids read from the package .meta files.
REMAP = {
    # every panel-settings reference collapses onto the single package asset
    "7a38f2dd4a52f3c43802f4f88af54bfe": ("557bff3f5a41b6b3aaffb4f32758faac", "Blocks BlocksPanelSettings -> UgsPanelSettings"),
    "097332d7a39dc7848947505b553aa666": ("557bff3f5a41b6b3aaffb4f32758faac", "PS_Login -> UgsPanelSettings"),
    "4c8011be1283eb042ad4a0e7a6f30d4a": ("557bff3f5a41b6b3aaffb4f32758faac", "UGS Panel Settings -> UgsPanelSettings"),
    # the sign-in modal
    "bb35356fde6fea2479e1bfe9dfd8371f": ("a602e73cdd00535a0b297b19c7698d87", "PlayerAccountLogin.uxml"),
    # achievement icons
    "9d64ab3d9efdd44d3b35a50db335af8c": ("270632bccfacba130a6de980c6677a57", "thumbnail"),
    "b44d5d04e5bc047dd9c197975b3c2b18": ("3123ed13657d7e53cf3c776941acb64c", "thumbnail_black"),
    "6842d3cc8e24d4f03b10428facaeac46": ("325c5cc90d62af5e4e7841cbe5d3f612", "thumbnail_blue"),
    "3429dc3b80e8141d0b696b17d76131c7": ("6d0ae46d398e6a05b47797b23ec10b53", "thumbnail_green"),
    "ec0cde348bd904ba6bf1d22ca12d9348": ("0c6a4d05039ce6c32525f04b73cb9042", "thumbnail_red"),
    # the runtime theme, in case a panel-settings copy travels
    "2e74663645b63fe43ad48957a2398ce5": ("85a690dbfb5f6df3cd8b3970d3155447", "UgsRuntimeTheme.tss"),
}

# The lighting settings live in the game's TempleRun domain and must not travel at all. These are
# camera-less additive service scenes that own no lighting, so the default is correct.
LIGHTING = re.compile(
    r"  m_LightingSettings: \{fileID: \d+, guid: 514fa5d4c9ef8e144abb009fe3ef8b55,\r?\n"
    r"    type: 2\}"
)

counts = {}
lighting_cleared = []

for root, _dirs, files in os.walk(STAGE):
    for name in files:
        if not (name.endswith(".unity") or name.endswith(".prefab")):
            continue
        path = os.path.join(root, name)
        text = io.open(path, encoding="utf-8").read()
        original = text

        for old, (new, what) in REMAP.items():
            n = text.count(old)
            if n:
                text = text.replace(old, new)
                counts[what] = counts.get(what, 0) + n

        text, n = LIGHTING.subn("  m_LightingSettings: {fileID: 0}", text)
        if n:
            lighting_cleared.append(name)

        if text != original:
            io.open(path, "w", encoding="utf-8", newline="").write(text)

print("GUID references remapped:")
for what in sorted(counts):
    print("  %-45s %d" % (what, counts[what]))
print()
print("lighting reference cleared in %d scene(s): %s" % (len(lighting_cleared), ", ".join(sorted(lighting_cleared))))

leftover = []
for root, _dirs, files in os.walk(STAGE):
    for name in files:
        if not (name.endswith(".unity") or name.endswith(".prefab")):
            continue
        text = io.open(os.path.join(root, name), encoding="utf-8").read()
        for old in list(REMAP) + ["514fa5d4c9ef8e144abb009fe3ef8b55"]:
            if old in text:
                leftover.append("%s still contains %s" % (name, old))
print()
print("leftover old guids:", leftover if leftover else "none")

# EE2 Asset Pipeline Guide

## 1) Extract EE2 files

Run:

```powershell
powershell -ExecutionPolicy Bypass -File Tools\Extract-EE2Assets.ps1 -ClearOutput
```

Output folder:

- `ExternalAssets/EE2_Extracted`

## 2) Convert candidate models for Unity

Run:

```powershell
powershell -ExecutionPolicy Bypass -File Tools\Convert-EE2ToUnity.ps1 -ClearOutput
```

Inputs:

- `ExternalAssets/EE2_Extracted/graphics/**.nif*`
- `Tools/ee2_convert_targets.csv`

Outputs:

- Models: `Assets/External/EE2Converted/Models`
- Logs: `ExternalAssets/EE2_Converted/logs`
- Manifest: `ExternalAssets/EE2_Converted/convert_manifest.csv`

## 3) Generate prefabs in Unity

In Unity menu:

- `Tools/EE2/Generate Prefab Library`

Output:

- `Assets/Resources/EE2/*.prefab`

## 4) Runtime mapping file

Mapping file:

- `Assets/Resources/EE2/visual_map.json`

You can edit prefab candidate names here without touching C# code.

Optional menu actions:

- `Tools/EE2/Write Default Visual Map`
- `Tools/EE2/Print Active Visual Map`

## 5) Known blocker

Current Noesis command-line conversion can read built-in formats (for example `.dae`) but returns
`Detected file type: Unknown` for EE2 `.nif/.kf`. This indicates a format handler issue in current environment.
Use `convert_manifest.csv` + per-file logs to track failures and quickly retry after tool updates.

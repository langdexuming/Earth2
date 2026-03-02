EE2 extracted sample pack for Unity prototype

Source game root:
F:\BaiduNetdiskDownload\earth2

This extraction includes a modern-style subset:
- HQ: bld_citycenter13_*.nifcache
- Barracks: bld_barracks13.nifcache
- Factory: bld_robotics_factory.nifcache
- Airfield: bld_airport13.nifcache
- Infantry: lhi13_assaultrifleman_*.nifcache
- Tank: lhm13_mainbattletank.nifcache
- Fighter: AF13_JetFighter.nifcache

Output structure:
- graphics\cache\nifcache\*.nifcache (raw)
- graphics\cache\nifcache\*.nif (same bytes, easier to open with NIF tools)
- graphics\Graphics\...\*.kf (animation clips)
- textures\cache\texcache\*.texcache (raw texture data)
- textures\cache\texcache\*.tex.nif (same bytes, easier to inspect with NIF tools)
- textures\cache\pcpcache\*.pcpatch (raw patches)
- textures\cache\pcpcache\*.pcpatch.nif (same bytes, easier to inspect with NIF tools)

Manifest:
- extract_manifest.csv (full source entry -> output path mapping)

Important:
- Files are in Gamebryo/NIF-family formats and are not directly importable by Unity.
- Typical conversion pipeline is:
  1) Open .nif in NifSkope/Noesis/Blender NIF plugin.
  2) Export mesh to FBX/OBJ.
  3) Convert texture data to PNG/TGA (if needed).
  4) Reassign materials in Unity.

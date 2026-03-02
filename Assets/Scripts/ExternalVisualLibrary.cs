using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class ExternalVisualLibrary
{
    private const string ResourceRoot = "EE2/";
    private const string VisualMapResource = "EE2/visual_map";

    private static readonly Dictionary<BuildingType, string[]> DefaultBuildingCandidates = new Dictionary<BuildingType, string[]>
    {
        { BuildingType.Headquarters, new[] { "bld_citycenter13", "bld_citycenter13_a", "Headquarters" } },
        { BuildingType.Barracks, new[] { "bld_barracks13", "Barracks" } },
        { BuildingType.Factory, new[] { "bld_robotics_factory", "Factory" } },
        { BuildingType.Airfield, new[] { "bld_airport13", "Airfield" } }
    };

    private static readonly Dictionary<UnitType, string[]> DefaultUnitCandidates = new Dictionary<UnitType, string[]>
    {
        { UnitType.Infantry, new[] { "lhi13_assaultrifleman_we", "lhi13_assaultrifleman", "Infantry" } },
        { UnitType.Tank, new[] { "lhm13_mainbattletank", "Tank" } },
        { UnitType.Aircraft, new[] { "AF13_JetFighter", "AF13_jetfighter", "Aircraft" } }
    };

    private static readonly Dictionary<BuildingType, string[]> BuildingCandidates = new Dictionary<BuildingType, string[]>();
    private static readonly Dictionary<UnitType, string[]> UnitCandidates = new Dictionary<UnitType, string[]>();
    private static readonly Dictionary<string, GameObject> CachedPrefabs = new Dictionary<string, GameObject>();
    private static bool isInitialized;

    public static bool TryInstantiateBuilding(BuildingType type, out GameObject instance)
    {
        EnsureInitialized();

        if (!BuildingCandidates.TryGetValue(type, out string[] candidates))
        {
            instance = null;
            return false;
        }

        return TryInstantiate(candidates, out instance);
    }

    public static bool TryInstantiateUnit(UnitType type, out GameObject instance)
    {
        EnsureInitialized();

        if (!UnitCandidates.TryGetValue(type, out string[] candidates))
        {
            instance = null;
            return false;
        }

        return TryInstantiate(candidates, out instance);
    }

    private static bool TryInstantiate(string[] candidates, out GameObject instance)
    {
        instance = null;
        for (int i = 0; i < candidates.Length; i++)
        {
            string resourceName = candidates[i];
            GameObject prefab = LoadPrefab(resourceName);
            if (prefab == null)
            {
                continue;
            }

            instance = Object.Instantiate(prefab);
            instance.name = resourceName + "_Visual";
            return true;
        }

        return false;
    }

    private static void EnsureInitialized()
    {
        if (isInitialized)
        {
            return;
        }

        isInitialized = true;
        BuildingCandidates.Clear();
        UnitCandidates.Clear();

        CopyDefaults();
        ApplyJsonOverrides();
    }

    private static void CopyDefaults()
    {
        foreach (KeyValuePair<BuildingType, string[]> pair in DefaultBuildingCandidates)
        {
            BuildingCandidates[pair.Key] = (string[])pair.Value.Clone();
        }

        foreach (KeyValuePair<UnitType, string[]> pair in DefaultUnitCandidates)
        {
            UnitCandidates[pair.Key] = (string[])pair.Value.Clone();
        }
    }

    private static void ApplyJsonOverrides()
    {
        TextAsset mapText = Resources.Load<TextAsset>(VisualMapResource);
        if (mapText == null || string.IsNullOrWhiteSpace(mapText.text))
        {
            return;
        }

        ExternalVisualMapConfig config;
        try
        {
            config = JsonUtility.FromJson<ExternalVisualMapConfig>(mapText.text);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("ExternalVisualLibrary failed to parse visual_map.json: " + ex.Message);
            return;
        }

        if (config == null)
        {
            return;
        }

        if (config.buildings != null)
        {
            for (int i = 0; i < config.buildings.Length; i++)
            {
                ExternalVisualMapEntry entry = config.buildings[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.type) || entry.prefabs == null || entry.prefabs.Length == 0)
                {
                    continue;
                }

                if (!System.Enum.TryParse(entry.type, true, out BuildingType buildingType))
                {
                    continue;
                }

                string[] cleaned = CleanupCandidateList(entry.prefabs);
                if (cleaned.Length > 0)
                {
                    BuildingCandidates[buildingType] = cleaned;
                }
            }
        }

        if (config.units != null)
        {
            for (int i = 0; i < config.units.Length; i++)
            {
                ExternalVisualMapEntry entry = config.units[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.type) || entry.prefabs == null || entry.prefabs.Length == 0)
                {
                    continue;
                }

                if (!System.Enum.TryParse(entry.type, true, out UnitType unitType))
                {
                    continue;
                }

                string[] cleaned = CleanupCandidateList(entry.prefabs);
                if (cleaned.Length > 0)
                {
                    UnitCandidates[unitType] = cleaned;
                }
            }
        }
    }

    private static string[] CleanupCandidateList(string[] input)
    {
        List<string> cleaned = new List<string>(input.Length);
        for (int i = 0; i < input.Length; i++)
        {
            string value = input[i];
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            string trimmed = value.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            bool exists = false;
            for (int j = 0; j < cleaned.Count; j++)
            {
                if (string.Equals(cleaned[j], trimmed, System.StringComparison.OrdinalIgnoreCase))
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                cleaned.Add(trimmed);
            }
        }

        return cleaned.ToArray();
    }

    public static string GetCurrentMappingSnapshot()
    {
        EnsureInitialized();

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Buildings:");
        foreach (KeyValuePair<BuildingType, string[]> pair in BuildingCandidates)
        {
            builder.Append("  ");
            builder.Append(pair.Key);
            builder.Append(": ");
            builder.AppendLine(string.Join(", ", pair.Value));
        }

        builder.AppendLine("Units:");
        foreach (KeyValuePair<UnitType, string[]> pair in UnitCandidates)
        {
            builder.Append("  ");
            builder.Append(pair.Key);
            builder.Append(": ");
            builder.AppendLine(string.Join(", ", pair.Value));
        }

        return builder.ToString();
    }

    private static GameObject LoadPrefab(string resourceName)
    {
        if (string.IsNullOrWhiteSpace(resourceName))
        {
            return null;
        }

        if (CachedPrefabs.TryGetValue(resourceName, out GameObject prefab))
        {
            return prefab;
        }

        prefab = Resources.Load<GameObject>(ResourceRoot + resourceName);
        CachedPrefabs[resourceName] = prefab;
        return prefab;
    }
}

[System.Serializable]
public sealed class ExternalVisualMapConfig
{
    public ExternalVisualMapEntry[] buildings;
    public ExternalVisualMapEntry[] units;
}

[System.Serializable]
public sealed class ExternalVisualMapEntry
{
    public string type;
    public string[] prefabs;
}

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class EE2PrefabGenerator
{
    private const string SourceFolder = "Assets/External/EE2Converted/Models";
    private const string OutputFolder = "Assets/Resources/EE2";
    private const string VisualMapPath = OutputFolder + "/visual_map.json";

    private sealed class Rule
    {
        public string outputName;
        public string[] sourceNameCandidates;
        public float targetFootprint;
        public float targetHeight;
    }

    private static readonly Rule[] Rules =
    {
        new Rule { outputName = "bld_citycenter13", sourceNameCandidates = new[] { "bld_citycenter13" }, targetFootprint = 7.8f, targetHeight = 5.5f },
        new Rule { outputName = "bld_barracks13", sourceNameCandidates = new[] { "bld_barracks13" }, targetFootprint = 6.2f, targetHeight = 4.0f },
        new Rule { outputName = "bld_robotics_factory", sourceNameCandidates = new[] { "bld_robotics_factory" }, targetFootprint = 7.0f, targetHeight = 4.6f },
        new Rule { outputName = "bld_airport13", sourceNameCandidates = new[] { "bld_airport13" }, targetFootprint = 9.2f, targetHeight = 3.4f },
        new Rule { outputName = "lhi13_assaultrifleman_we", sourceNameCandidates = new[] { "lhi13_assaultrifleman_we", "lhi13_assaultrifleman" }, targetFootprint = 0.7f, targetHeight = 1.7f },
        new Rule { outputName = "lhm13_mainbattletank", sourceNameCandidates = new[] { "lhm13_mainbattletank" }, targetFootprint = 3.0f, targetHeight = 1.2f },
        new Rule { outputName = "AF13_JetFighter", sourceNameCandidates = new[] { "AF13_JetFighter", "AF13_jetfighter" }, targetFootprint = 3.4f, targetHeight = 1.3f }
    };

    [MenuItem("Tools/EE2/Generate Prefab Library")]
    public static void GeneratePrefabLibrary()
    {
        if (!AssetDatabase.IsValidFolder(SourceFolder))
        {
            Debug.LogWarning("EE2 source folder not found: " + SourceFolder);
            return;
        }

        EnsureAssetFolder(OutputFolder);

        List<string> modelPaths = CollectModelPaths(SourceFolder);
        if (modelPaths.Count == 0)
        {
            Debug.LogWarning("No model assets found in: " + SourceFolder);
            return;
        }

        int generated = 0;
        int missing = 0;

        foreach (Rule rule in Rules)
        {
            string sourcePath = FindBestSourcePath(modelPaths, rule.sourceNameCandidates);
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                missing++;
                Debug.LogWarning("EE2 prefab skipped (source not found): " + rule.outputName);
                continue;
            }

            GameObject sourceAsset = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (sourceAsset == null)
            {
                missing++;
                Debug.LogWarning("EE2 prefab skipped (failed to load source asset): " + sourcePath);
                continue;
            }

            GameObject root = new GameObject(rule.outputName);
            try
            {
                GameObject modelInstance = PrefabUtility.InstantiatePrefab(sourceAsset) as GameObject;
                if (modelInstance == null)
                {
                    modelInstance = UnityEngine.Object.Instantiate(sourceAsset);
                }

                modelInstance.name = "Model";
                modelInstance.transform.SetParent(root.transform, false);

                NormalizeModel(root, rule.targetFootprint, rule.targetHeight);
                EnsureBoxCollider(root);

                string prefabPath = OutputFolder + "/" + rule.outputName + ".prefab";
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                generated++;
                Debug.Log("EE2 prefab generated: " + prefabPath + " <- " + sourcePath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("EE2 prefab generation complete. Generated: " + generated + ", Missing: " + missing);
    }

    [MenuItem("Tools/EE2/Open Converted Models Folder")]
    private static void OpenConvertedModelFolder()
    {
        string absolutePath = Path.GetFullPath(SourceFolder);
        if (!Directory.Exists(absolutePath))
        {
            Directory.CreateDirectory(absolutePath);
        }

        EditorUtility.RevealInFinder(absolutePath);
    }

    [MenuItem("Tools/EE2/Write Default Visual Map")]
    private static void WriteDefaultVisualMap()
    {
        EnsureAssetFolder(OutputFolder);

        ExternalVisualMapConfig config = new ExternalVisualMapConfig
        {
            buildings = new[]
            {
                new ExternalVisualMapEntry { type = "Headquarters", prefabs = new[] { "bld_citycenter13", "bld_citycenter13_a", "Headquarters" } },
                new ExternalVisualMapEntry { type = "Barracks", prefabs = new[] { "bld_barracks13", "Barracks" } },
                new ExternalVisualMapEntry { type = "Factory", prefabs = new[] { "bld_robotics_factory", "Factory" } },
                new ExternalVisualMapEntry { type = "Airfield", prefabs = new[] { "bld_airport13", "Airfield" } }
            },
            units = new[]
            {
                new ExternalVisualMapEntry { type = "Infantry", prefabs = new[] { "lhi13_assaultrifleman_we", "lhi13_assaultrifleman", "Infantry" } },
                new ExternalVisualMapEntry { type = "Tank", prefabs = new[] { "lhm13_mainbattletank", "Tank" } },
                new ExternalVisualMapEntry { type = "Aircraft", prefabs = new[] { "AF13_JetFighter", "AF13_jetfighter", "Aircraft" } }
            }
        };

        string json = JsonUtility.ToJson(config, true);
        File.WriteAllText(VisualMapPath, json);
        AssetDatabase.Refresh();
        Debug.Log("Wrote visual map file: " + VisualMapPath);
    }

    [MenuItem("Tools/EE2/Print Active Visual Map")]
    private static void PrintActiveVisualMap()
    {
        Debug.Log(ExternalVisualLibrary.GetCurrentMappingSnapshot());
    }

    private static List<string> CollectModelPaths(string sourceFolder)
    {
        string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { sourceFolder });
        List<string> results = new List<string>(guids.Length);
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            string extension = Path.GetExtension(assetPath).ToLowerInvariant();
            if (extension == ".fbx" || extension == ".obj" || extension == ".dae" || extension == ".prefab")
            {
                results.Add(assetPath);
            }
        }

        return results;
    }

    private static string FindBestSourcePath(List<string> modelPaths, string[] nameCandidates)
    {
        for (int i = 0; i < nameCandidates.Length; i++)
        {
            string candidate = nameCandidates[i];
            for (int j = 0; j < modelPaths.Count; j++)
            {
                string modelName = Path.GetFileNameWithoutExtension(modelPaths[j]);
                if (string.Equals(modelName, candidate, StringComparison.OrdinalIgnoreCase))
                {
                    return modelPaths[j];
                }
            }
        }

        for (int i = 0; i < nameCandidates.Length; i++)
        {
            string candidate = nameCandidates[i];
            for (int j = 0; j < modelPaths.Count; j++)
            {
                string modelName = Path.GetFileNameWithoutExtension(modelPaths[j]);
                if (modelName.IndexOf(candidate, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return modelPaths[j];
                }
            }
        }

        return null;
    }

    private static void NormalizeModel(GameObject root, float targetFootprint, float targetHeight)
    {
        if (!TryGetRendererBounds(root, out Bounds initialBounds))
        {
            return;
        }

        float currentFootprint = Mathf.Max(initialBounds.size.x, initialBounds.size.z, 0.001f);
        float currentHeight = Mathf.Max(initialBounds.size.y, 0.001f);
        float footprintScale = targetFootprint / currentFootprint;
        float heightScale = targetHeight / currentHeight;
        float scaleFactor = Mathf.Max(0.0001f, Mathf.Min(footprintScale, heightScale));

        root.transform.localScale *= scaleFactor;

        if (!TryGetRendererBounds(root, out Bounds scaledBounds))
        {
            return;
        }

        Vector3 placementOffset = new Vector3(
            -scaledBounds.center.x,
            -scaledBounds.min.y,
            -scaledBounds.center.z
        );
        root.transform.position += placementOffset;
    }

    private static void EnsureBoxCollider(GameObject root)
    {
        if (root.GetComponentInChildren<Collider>() != null)
        {
            return;
        }

        if (!TryGetRendererBounds(root, out Bounds bounds))
        {
            return;
        }

        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = root.transform.InverseTransformPoint(bounds.center);

        Vector3 localSize = root.transform.InverseTransformVector(bounds.size);
        localSize.x = Mathf.Max(Mathf.Abs(localSize.x), 0.1f);
        localSize.y = Mathf.Max(Mathf.Abs(localSize.y), 0.1f);
        localSize.z = Mathf.Max(Mathf.Abs(localSize.z), 0.1f);
        collider.size = localSize;
    }

    private static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return true;
    }

    private static void EnsureAssetFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

public class SimpleRtsGameManager : MonoBehaviour
{
    public static SimpleRtsGameManager Instance { get; private set; }

    [Header("Map")]
    public float mapSize = 180f;
    public Color mapColor = new Color(0.22f, 0.45f, 0.24f, 1f);
    public Color mapLineColor = new Color(0.13f, 0.26f, 0.13f, 1f);
    public float mapGridWorldSize = 8f;

    [Header("Resources")]
    public int startResources = 1800;
    public int playerResources;

    public bool IsPlacingBuilding => pendingBuildingType.HasValue;

    public BuildingType? pendingBuildingType;
    private Camera mainCamera;
    private string statusMessage = string.Empty;
    private float statusMessageTimer = 0f;

    private readonly List<Building> playerBuildings = new List<Building>();
    private readonly List<Building> enemyBuildings = new List<Building>();
    private readonly List<Material> runtimeMaterials = new List<Material>();
    private readonly List<Texture2D> runtimeTextures = new List<Texture2D>();
    private int externalVisualCount = 0;
    private int fallbackVisualCount = 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapOnSceneLoad()
    {
        if (FindObjectOfType<SimpleRtsGameManager>() != null)
        {
            return;
        }

        GameObject managerObject = new GameObject("SimpleRTS_GameManager");
        managerObject.AddComponent<SimpleRtsGameManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        playerResources = startResources;
    }

    private void OnDestroy()
    {
        foreach (Material material in runtimeMaterials)
        {
            if (material != null)
            {
                Destroy(material);
            }
        }

        foreach (Texture2D texture in runtimeTextures)
        {
            if (texture != null)
            {
                Destroy(texture);
            }
        }
    }

    private void Start()
    {
        EnsureSelectionManagerExists();
        EnsureHudExists();
        PrepareScene();
        CreateMap();
        SpawnInitialBases();
        SpawnInitialUnits();
        SetStatus("Empire RTS initialized - Build your army!");
    }

    private void Update()
    {
        HandleBuildingPlacementInput();
        HandleProductionInput();

        if (statusMessageTimer > 0f)
        {
            statusMessageTimer -= Time.deltaTime;
            if (statusMessageTimer <= 0f)
            {
                statusMessage = string.Empty;
            }
        }
    }

    private void EnsureSelectionManagerExists()
    {
        if (SelectionManager.Instance != null)
        {
            return;
        }

        GameObject selectionObject = new GameObject("SelectionManager");
        selectionObject.AddComponent<SelectionManager>();
    }

    private void EnsureHudExists()
    {
        if (RtsHud.Instance != null)
        {
            return;
        }

        GameObject hudObject = new GameObject("RTS_HUD");
        hudObject.AddComponent<RtsHud>();
    }

    private void PrepareScene()
    {
        Terrain[] terrains = FindObjectsOfType<Terrain>();
        foreach (Terrain terrain in terrains)
        {
            Destroy(terrain.gameObject);
        }

        Unit[] existingUnits = FindObjectsOfType<Unit>();
        foreach (Unit unit in existingUnits)
        {
            Destroy(unit.gameObject);
        }

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObj = new GameObject("Main Camera");
            cameraObj.tag = "MainCamera";
            mainCamera = cameraObj.AddComponent<Camera>();
            cameraObj.AddComponent<AudioListener>();
        }

        CameraController[] controllers = FindObjectsOfType<CameraController>();
        foreach (CameraController controller in controllers)
        {
            if (controller.gameObject != mainCamera.gameObject)
            {
                Destroy(controller);
            }
        }

        CameraController cameraController = mainCamera.GetComponent<CameraController>();
        if (cameraController == null)
        {
            cameraController = mainCamera.gameObject.AddComponent<CameraController>();
        }

        mainCamera.transform.position = new Vector3(0f, 58f, -44f);
        mainCamera.transform.rotation = Quaternion.Euler(55f, 0f, 0f);

        cameraController.panLimit = new Vector2(mapSize * 0.47f, mapSize * 0.47f);
        cameraController.panSpeed = 42f;
        cameraController.scrollSpeed = 34f;
        cameraController.minY = 18f;
        cameraController.maxY = 95f;
        cameraController.tiltAngle = 55f;
    }

    private void CreateMap()
    {
        GameObject existingGround = GameObject.Find("RTS_Ground");
        if (existingGround != null)
        {
            Destroy(existingGround);
        }

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "RTS_Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(mapSize / 10f, 1f, mapSize / 10f);

        Renderer renderer = ground.GetComponent<Renderer>();
        Material groundMaterial = CreateColorMaterial(mapColor);
        Texture2D gridTexture = CreateGroundGridTexture(256, 256, 16, mapColor, mapLineColor);

        if (groundMaterial.HasProperty("_BaseMap"))
        {
            groundMaterial.SetTexture("_BaseMap", gridTexture);
            groundMaterial.SetTextureScale("_BaseMap", new Vector2(mapSize / mapGridWorldSize, mapSize / mapGridWorldSize));
        }
        if (groundMaterial.HasProperty("_MainTex"))
        {
            groundMaterial.SetTexture("_MainTex", gridTexture);
            groundMaterial.SetTextureScale("_MainTex", new Vector2(mapSize / mapGridWorldSize, mapSize / mapGridWorldSize));
        }

        renderer.material = groundMaterial;
    }

    private void SpawnInitialBases()
    {
        CreateBuilding(BuildingType.Headquarters, new Vector3(-36f, 0f, -30f), 0, 0f);
        CreateBuilding(BuildingType.Barracks, new Vector3(-24f, 0f, -38f), 0, 0f);
        CreateBuilding(BuildingType.Factory, new Vector3(-12f, 0f, -30f), 0, 0f);
        CreateBuilding(BuildingType.Airfield, new Vector3(-24f, 0f, -18f), 0, 0f);

        CreateBuilding(BuildingType.Headquarters, new Vector3(36f, 0f, 30f), 1, 180f);
        CreateBuilding(BuildingType.Barracks, new Vector3(24f, 0f, 38f), 1, 180f);
        CreateBuilding(BuildingType.Factory, new Vector3(12f, 0f, 30f), 1, 180f);
        CreateBuilding(BuildingType.Airfield, new Vector3(24f, 0f, 18f), 1, 180f);
    }

    private void SpawnInitialUnits()
    {
        SpawnUnit(UnitType.Infantry, new Vector3(-18f, 0f, -12f), 0);
        SpawnUnit(UnitType.Infantry, new Vector3(-14f, 0f, -12f), 0);
        SpawnUnit(UnitType.Tank, new Vector3(-10f, 0f, -12f), 0);
        SpawnUnit(UnitType.Aircraft, new Vector3(-8f, 0f, -16f), 0);

        SpawnUnit(UnitType.Infantry, new Vector3(18f, 0f, 12f), 1);
        SpawnUnit(UnitType.Infantry, new Vector3(14f, 0f, 12f), 1);
        SpawnUnit(UnitType.Tank, new Vector3(10f, 0f, 12f), 1);
        SpawnUnit(UnitType.Aircraft, new Vector3(8f, 0f, 16f), 1);
    }

    public Building CreateBuilding(BuildingType type, Vector3 worldPosition, int team, float yRotation)
    {
        GameObject buildingObject = CreateBuildingBody(type);
        buildingObject.name = (team == 0 ? "Player_" : "Enemy_") + type;
        buildingObject.transform.position = worldPosition;
        buildingObject.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);

        AlignObjectToGround(buildingObject, worldPosition.y);

        Building building = buildingObject.AddComponent<Building>();
        building.Configure(type, team);

        Bounds bounds = CalculateRendererBounds(buildingObject);
        float forwardOffset = Mathf.Max(bounds.extents.x, bounds.extents.z) + 2.2f;

        Transform spawnPoint = new GameObject("SpawnPoint").transform;
        spawnPoint.SetParent(buildingObject.transform, false);
        spawnPoint.localPosition = new Vector3(0f, 0f, forwardOffset);
        building.spawnPoint = spawnPoint;
        building.spawnRadius = forwardOffset;

        if (team == 0)
        {
            playerBuildings.Add(building);
        }
        else
        {
            enemyBuildings.Add(building);
        }

        return building;
    }

    public Unit SpawnUnit(UnitType unitType, Vector3 position, int team)
    {
        GameObject unitObject = CreateUnitBody(unitType);
        unitObject.name = (team == 0 ? "Player_" : "Enemy_") + unitType;
        unitObject.transform.position = position;

        Unit unit = unitObject.AddComponent<Unit>();
        unit.Configure(unitType, team);
        return unit;
    }

    private GameObject CreateBuildingBody(BuildingType type)
    {
        if (ExternalVisualLibrary.TryInstantiateBuilding(type, out GameObject externalBuilding))
        {
            NormalizeExternalVisual(externalBuilding, GetBuildingVisualFootprint(type), GetBuildingHeight(type));
            EnsureEntityCollider(externalBuilding);
            externalVisualCount++;
            return externalBuilding;
        }

        fallbackVisualCount++;
        GameObject root = new GameObject(type + "_Body");

        Color concrete = new Color(0.62f, 0.64f, 0.66f, 1f);
        Color darkMetal = new Color(0.26f, 0.28f, 0.3f, 1f);
        Color roof = new Color(0.44f, 0.46f, 0.5f, 1f);
        Color accent = new Color(0.85f, 0.78f, 0.42f, 1f);

        switch (type)
        {
            case BuildingType.Headquarters:
                AddPart(root.transform, PrimitiveType.Cube, new Vector3(0f, 1.8f, 0f), new Vector3(7.2f, 3.6f, 7.2f), concrete, Vector3.zero, true);
                AddPart(root.transform, PrimitiveType.Cube, new Vector3(0f, 4.2f, 0f), new Vector3(4.2f, 1.1f, 4.2f), roof, Vector3.zero, false);
                AddPart(root.transform, PrimitiveType.Cylinder, new Vector3(-2.4f, 2.8f, -2.4f), new Vector3(0.9f, 1.8f, 0.9f), darkMetal);
                AddPart(root.transform, PrimitiveType.Cylinder, new Vector3(2.4f, 2.8f, -2.4f), new Vector3(0.9f, 1.8f, 0.9f), darkMetal);
                AddPart(root.transform, PrimitiveType.Cylinder, new Vector3(-2.4f, 2.8f, 2.4f), new Vector3(0.9f, 1.8f, 0.9f), darkMetal);
                AddPart(root.transform, PrimitiveType.Cylinder, new Vector3(2.4f, 2.8f, 2.4f), new Vector3(0.9f, 1.8f, 0.9f), darkMetal);
                AddPart(root.transform, PrimitiveType.Cube, new Vector3(0f, 1.2f, 3.4f), new Vector3(1.4f, 2.0f, 0.25f), accent);
                break;

            case BuildingType.Barracks:
                AddPart(root.transform, PrimitiveType.Cube, new Vector3(0f, 1.45f, 0f), new Vector3(6.3f, 2.9f, 4.8f), concrete, Vector3.zero, true);
                AddPart(root.transform, PrimitiveType.Cube, new Vector3(0f, 3.2f, 0f), new Vector3(5.2f, 0.5f, 3.1f), roof);
                AddPart(root.transform, PrimitiveType.Cube, new Vector3(0f, 1.15f, 2.48f), new Vector3(1.8f, 1.9f, 0.2f), darkMetal);
                AddPart(root.transform, PrimitiveType.Cube, new Vector3(-1.9f, 1.7f, 2.48f), new Vector3(0.8f, 0.45f, 0.2f), accent);
                AddPart(root.transform, PrimitiveType.Cube, new Vector3(1.9f, 1.7f, 2.48f), new Vector3(0.8f, 0.45f, 0.2f), accent);
                break;

            case BuildingType.Factory:
                AddPart(root.transform, PrimitiveType.Cube, new Vector3(0f, 1.6f, 0f), new Vector3(7f, 3.2f, 5.6f), concrete, Vector3.zero, true);
                AddPart(root.transform, PrimitiveType.Cube, new Vector3(0f, 1.15f, 2.85f), new Vector3(2.1f, 1.8f, 0.2f), darkMetal);
                AddPart(root.transform, PrimitiveType.Cylinder, new Vector3(-2.2f, 3.55f, -1.8f), new Vector3(0.45f, 2f, 0.45f), darkMetal);
                AddPart(root.transform, PrimitiveType.Cylinder, new Vector3(-1.1f, 3.95f, -1.8f), new Vector3(0.35f, 2.4f, 0.35f), darkMetal);
                AddPart(root.transform, PrimitiveType.Cylinder, new Vector3(0f, 4.35f, -1.8f), new Vector3(0.28f, 2.8f, 0.28f), darkMetal);
                AddPart(root.transform, PrimitiveType.Cube, new Vector3(2.2f, 2.4f, -1.5f), new Vector3(1.5f, 1.6f, 1.6f), roof);
                break;

            case BuildingType.Airfield:
                AddPart(root.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.22f, 0f), new Vector3(5.7f, 0.22f, 5.7f), new Color(0.48f, 0.5f, 0.53f, 1f), Vector3.zero, true);
                AddPart(root.transform, PrimitiveType.Cube, new Vector3(0f, 0.42f, 0f), new Vector3(2.2f, 0.15f, 7.8f), new Color(0.3f, 0.32f, 0.34f, 1f));
                AddPart(root.transform, PrimitiveType.Cube, new Vector3(0f, 1.25f, -2.5f), new Vector3(3.3f, 1.55f, 2.1f), concrete);
                AddPart(root.transform, PrimitiveType.Cube, new Vector3(0f, 2.25f, -2.5f), new Vector3(3.3f, 0.3f, 2.1f), roof);
                AddPart(root.transform, PrimitiveType.Cylinder, new Vector3(2.2f, 1.2f, 1.9f), new Vector3(0.2f, 1.1f, 0.2f), darkMetal);
                AddPart(root.transform, PrimitiveType.Sphere, new Vector3(2.2f, 2.28f, 1.9f), new Vector3(0.55f, 0.22f, 0.55f), accent);
                break;

            default:
                AddPart(root.transform, PrimitiveType.Cube, new Vector3(0f, 1.5f, 0f), new Vector3(3f, 3f, 3f), concrete, Vector3.zero, true);
                break;
        }

        return root;
    }

    private GameObject CreateUnitBody(UnitType type)
    {
        if (ExternalVisualLibrary.TryInstantiateUnit(type, out GameObject externalUnit))
        {
            NormalizeExternalVisual(externalUnit, GetUnitFootprint(type), GetUnitHeight(type));
            EnsureEntityCollider(externalUnit);
            externalVisualCount++;
            return externalUnit;
        }

        fallbackVisualCount++;
        GameObject root;

        Color cloth = new Color(0.58f, 0.62f, 0.65f, 1f);
        Color armor = new Color(0.38f, 0.42f, 0.46f, 1f);
        Color dark = new Color(0.2f, 0.22f, 0.24f, 1f);
        Color cockpit = new Color(0.83f, 0.9f, 0.95f, 1f);

        switch (type)
        {
            case UnitType.Infantry:
                root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                root.transform.localScale = new Vector3(0.56f, 0.95f, 0.56f);
                SetPartColor(root, cloth);

                AddPart(root.transform, PrimitiveType.Sphere, new Vector3(0f, 1.05f, 0f), new Vector3(0.42f, 0.42f, 0.42f), new Color(0.86f, 0.78f, 0.66f, 1f));
                AddPart(root.transform, PrimitiveType.Sphere, new Vector3(0f, 1.24f, 0f), new Vector3(0.44f, 0.18f, 0.44f), dark);
                AddPart(root.transform, PrimitiveType.Cube, new Vector3(0.25f, 0.6f, 0.24f), new Vector3(0.12f, 0.12f, 0.8f), dark);
                AddPart(root.transform, PrimitiveType.Cube, new Vector3(0f, 0.6f, -0.3f), new Vector3(0.28f, 0.38f, 0.2f), armor);
                break;

            case UnitType.Tank:
                root = GameObject.CreatePrimitive(PrimitiveType.Cube);
                root.transform.localScale = new Vector3(2.2f, 0.52f, 3.05f);
                SetPartColor(root, armor);

                AddPart(root.transform, PrimitiveType.Cube, new Vector3(-1.04f, -0.16f, 0f), new Vector3(0.48f, 0.4f, 2.95f), dark);
                AddPart(root.transform, PrimitiveType.Cube, new Vector3(1.04f, -0.16f, 0f), new Vector3(0.48f, 0.4f, 2.95f), dark);
                AddPart(root.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.52f, -0.15f), new Vector3(0.58f, 0.35f, 0.58f), armor);
                AddPart(root.transform, PrimitiveType.Cube, new Vector3(0f, 0.62f, 1.48f), new Vector3(0.2f, 0.2f, 1.9f), dark);
                AddPart(root.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.9f, -0.15f), new Vector3(0.15f, 0.12f, 0.15f), dark);
                break;

            case UnitType.Aircraft:
                root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                root.transform.localScale = new Vector3(1.22f, 0.4f, 2.35f);
                SetPartColor(root, armor);

                AddPart(root.transform, PrimitiveType.Cube, new Vector3(0f, 0f, -0.05f), new Vector3(3.2f, 0.08f, 0.55f), armor);
                AddPart(root.transform, PrimitiveType.Cube, new Vector3(0f, 0.2f, -1.2f), new Vector3(0.65f, 0.4f, 0.55f), dark);
                AddPart(root.transform, PrimitiveType.Cube, new Vector3(0f, 0.44f, -1.42f), new Vector3(0.1f, 0.5f, 0.44f), dark);
                AddPart(root.transform, PrimitiveType.Sphere, new Vector3(0f, 0.1f, 1.24f), new Vector3(0.42f, 0.26f, 0.42f), cockpit);
                AddPart(root.transform, PrimitiveType.Cylinder, new Vector3(-0.48f, -0.05f, -0.55f), new Vector3(0.16f, 0.18f, 0.16f), dark, new Vector3(90f, 0f, 0f));
                AddPart(root.transform, PrimitiveType.Cylinder, new Vector3(0.48f, -0.05f, -0.55f), new Vector3(0.16f, 0.18f, 0.16f), dark, new Vector3(90f, 0f, 0f));
                break;

            default:
                root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                root.transform.localScale = new Vector3(0.56f, 0.95f, 0.56f);
                SetPartColor(root, cloth);
                break;
        }

        return root;
    }

    private float GetBuildingVisualFootprint(BuildingType type)
    {
        switch (type)
        {
            case BuildingType.Headquarters:
                return 7.8f;
            case BuildingType.Barracks:
                return 6.2f;
            case BuildingType.Factory:
                return 7.0f;
            case BuildingType.Airfield:
                return 9.2f;
            default:
                return 4.5f;
        }
    }

    private float GetBuildingHeight(BuildingType type)
    {
        switch (type)
        {
            case BuildingType.Headquarters:
                return 5.5f;
            case BuildingType.Barracks:
                return 4.0f;
            case BuildingType.Factory:
                return 4.6f;
            case BuildingType.Airfield:
                return 3.4f;
            default:
                return 3.5f;
        }
    }

    private float GetUnitFootprint(UnitType type)
    {
        switch (type)
        {
            case UnitType.Infantry:
                return 0.7f;
            case UnitType.Tank:
                return 3.0f;
            case UnitType.Aircraft:
                return 3.4f;
            default:
                return 1f;
        }
    }

    private float GetUnitHeight(UnitType type)
    {
        switch (type)
        {
            case UnitType.Infantry:
                return 1.7f;
            case UnitType.Tank:
                return 1.2f;
            case UnitType.Aircraft:
                return 1.3f;
            default:
                return 1.4f;
        }
    }

    private void NormalizeExternalVisual(GameObject visualRoot, float targetFootprint, float targetHeight)
    {
        if (visualRoot == null)
        {
            return;
        }

        Bounds initialBounds = CalculateRendererBounds(visualRoot);
        float currentFootprint = Mathf.Max(initialBounds.size.x, initialBounds.size.z, 0.001f);
        float currentHeight = Mathf.Max(initialBounds.size.y, 0.001f);

        float footprintScale = targetFootprint / currentFootprint;
        float heightScale = targetHeight / currentHeight;
        float scaleFactor = Mathf.Max(0.0001f, Mathf.Min(footprintScale, heightScale));

        visualRoot.transform.localScale *= scaleFactor;

        Bounds scaledBounds = CalculateRendererBounds(visualRoot);
        Vector3 placementOffset = new Vector3(
            -scaledBounds.center.x,
            -scaledBounds.min.y,
            -scaledBounds.center.z
        );
        visualRoot.transform.position += placementOffset;
    }

    private void EnsureEntityCollider(GameObject entityRoot)
    {
        if (entityRoot == null)
        {
            return;
        }

        if (entityRoot.GetComponentInChildren<Collider>() != null)
        {
            return;
        }

        Bounds bounds = CalculateRendererBounds(entityRoot);
        BoxCollider collider = entityRoot.AddComponent<BoxCollider>();
        collider.center = entityRoot.transform.InverseTransformPoint(bounds.center);

        Vector3 localSize = entityRoot.transform.InverseTransformVector(bounds.size);
        localSize.x = Mathf.Max(Mathf.Abs(localSize.x), 0.1f);
        localSize.y = Mathf.Max(Mathf.Abs(localSize.y), 0.1f);
        localSize.z = Mathf.Max(Mathf.Abs(localSize.z), 0.1f);
        collider.size = localSize;
    }

    private GameObject AddPart(Transform parent, PrimitiveType primitive, Vector3 localPosition, Vector3 localScale, Color color)
    {
        return AddPart(parent, primitive, localPosition, localScale, color, Vector3.zero, false);
    }

    private GameObject AddPart(Transform parent, PrimitiveType primitive, Vector3 localPosition, Vector3 localScale, Color color, Vector3 localEuler)
    {
        return AddPart(parent, primitive, localPosition, localScale, color, localEuler, false);
    }

    private GameObject AddPart(
        Transform parent,
        PrimitiveType primitive,
        Vector3 localPosition,
        Vector3 localScale,
        Color color,
        Vector3 localEuler,
        bool keepCollider)
    {
        GameObject part = GameObject.CreatePrimitive(primitive);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.Euler(localEuler);
        part.transform.localScale = localScale;

        SetPartColor(part, color);

        if (!keepCollider)
        {
            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }

        return part;
    }

    private void SetPartColor(GameObject part, Color color)
    {
        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        renderer.material = CreateColorMaterial(color);
    }

    private void AlignObjectToGround(GameObject target, float groundY)
    {
        Bounds bounds = CalculateRendererBounds(target);
        float offsetY = groundY - bounds.min.y;
        target.transform.position += new Vector3(0f, offsetY, 0f);
    }

    private Bounds CalculateRendererBounds(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Bounds(target.transform.position, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private Texture2D CreateGroundGridTexture(int width, int height, int cellSize, Color grassColor, Color lineColor)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;

        Color darkGrass = Color.Lerp(grassColor, Color.black, 0.18f);
        Color lightGrass = Color.Lerp(grassColor, Color.white, 0.08f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool line = (x % cellSize == 0) || (y % cellSize == 0);
                bool checker = ((x / (cellSize / 2)) + (y / (cellSize / 2))) % 2 == 0;
                Color baseColor = checker ? lightGrass : darkGrass;
                Color finalColor = line ? Color.Lerp(baseColor, lineColor, 0.82f) : baseColor;
                texture.SetPixel(x, y, finalColor);
            }
        }

        texture.Apply();
        runtimeTextures.Add(texture);
        return texture;
    }

    private Material CreateColorMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        runtimeMaterials.Add(material);
        return material;
    }

    private void HandleBuildingPlacementInput()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            pendingBuildingType = BuildingType.Barracks;
            SetStatus("Build mode: Barracks (LMB place, RMB or Esc cancel)");
        }
        if (Input.GetKeyDown(KeyCode.N))
        {
            pendingBuildingType = BuildingType.Factory;
            SetStatus("Build mode: Factory (LMB place, RMB or Esc cancel)");
        }
        if (Input.GetKeyDown(KeyCode.M))
        {
            pendingBuildingType = BuildingType.Airfield;
            SetStatus("Build mode: Airfield (LMB place, RMB or Esc cancel)");
        }

        if (!pendingBuildingType.HasValue)
        {
            return;
        }

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            SetStatus("Build canceled");
            pendingBuildingType = null;
            return;
        }

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        if (mainCamera == null)
        {
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            return;
        }

        TryPlaceBuildingAt(hit.point);
    }

    private void TryPlaceBuildingAt(Vector3 worldPoint)
    {
        if (!pendingBuildingType.HasValue)
        {
            return;
        }

        BuildingType type = pendingBuildingType.Value;
        int cost = GetBuildingCost(type);

        if (!TrySpendResource(cost))
        {
            SetStatus("Not enough resources to build");
            return;
        }

        Vector3 pos = new Vector3(worldPoint.x, 0f, worldPoint.z);
        float mapHalf = mapSize * 0.48f;
        pos.x = Mathf.Clamp(pos.x, -mapHalf, mapHalf);
        pos.z = Mathf.Clamp(pos.z, -mapHalf, mapHalf);

        float footprint = GetBuildingFootprint(type);
        Collider[] overlaps = Physics.OverlapSphere(new Vector3(pos.x, 1f, pos.z), footprint);
        foreach (Collider col in overlaps)
        {
            if (col.GetComponentInParent<Building>() != null)
            {
                playerResources += cost;
                SetStatus("There is already a building here");
                return;
            }
        }

        CreateBuilding(type, pos, 0, 0f);
        pendingBuildingType = null;
        SetStatus("Built: " + GetBuildingDisplayName(type), 1.6f);
    }

    private void HandleProductionInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TryQueueFromSelectedBuilding(UnitType.Infantry);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TryQueueFromSelectedBuilding(UnitType.Tank);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            TryQueueFromSelectedBuilding(UnitType.Aircraft);
        }
    }

    private void TryQueueFromSelectedBuilding(UnitType unitType)
    {
        if (SelectionManager.Instance == null)
        {
            return;
        }

        Building selectedBuilding = SelectionManager.Instance.GetFirstSelectedBuilding(0);
        if (selectedBuilding == null)
        {
            SetStatus("Select one of your buildings first");
            return;
        }

        if (!selectedBuilding.CanProduce(unitType))
        {
            SetStatus(selectedBuilding.buildingName + " cannot produce " + GetUnitDisplayName(unitType));
            return;
        }

        selectedBuilding.TryQueueUnit(unitType);
    }

    private int GetBuildingCost(BuildingType type)
    {
        switch (type)
        {
            case BuildingType.Barracks:
                return 260;
            case BuildingType.Factory:
                return 420;
            case BuildingType.Airfield:
                return 520;
            default:
                return 300;
        }
    }

    private float GetBuildingFootprint(BuildingType type)
    {
        switch (type)
        {
            case BuildingType.Barracks:
                return 3.8f;
            case BuildingType.Factory:
                return 4.5f;
            case BuildingType.Airfield:
                return 5.4f;
            default:
                return 4.2f;
        }
    }

    public bool TrySpendResource(int cost)
    {
        if (playerResources < cost)
        {
            return false;
        }

        playerResources -= cost;
        return true;
    }

    public void SetStatus(string message, float duration = 2.4f)
    {
        statusMessage = message;
        statusMessageTimer = duration;
    }

    public string GetCurrentStatusMessage()
    {
        return statusMessage;
    }

    public BuildingType? GetPendingBuildingType()
    {
        return pendingBuildingType;
    }

    public void StartBuildMode(KeyCode buildKey)
    {
        switch (buildKey)
        {
            case KeyCode.B:
                pendingBuildingType = BuildingType.Barracks;
                SetStatus("Build mode: Barracks (LMB place, RMB or Esc cancel)");
                break;
            case KeyCode.N:
                pendingBuildingType = BuildingType.Factory;
                SetStatus("Build mode: Factory (LMB place, RMB or Esc cancel)");
                break;
            case KeyCode.M:
                pendingBuildingType = BuildingType.Airfield;
                SetStatus("Build mode: Airfield (LMB place, RMB or Esc cancel)");
                break;
        }
    }

    private string GetBuildingDisplayName(BuildingType type)
    {
        switch (type)
        {
            case BuildingType.Headquarters:
                return "HQ";
            case BuildingType.Barracks:
                return "Barracks";
            case BuildingType.Factory:
                return "Factory";
            case BuildingType.Airfield:
                return "Airfield";
            default:
                return type.ToString();
        }
    }

    private string GetUnitDisplayName(UnitType type)
    {
        switch (type)
        {
            case UnitType.Infantry:
                return "Infantry";
            case UnitType.Tank:
                return "Tank";
            case UnitType.Aircraft:
                return "Aircraft";
            default:
                return type.ToString();
        }
    }

}

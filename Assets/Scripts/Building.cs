using System.Collections.Generic;
using UnityEngine;

public class Building : SelectableEntity
{
    [Header("Building")]
    public string buildingName = "Building";
    public BuildingType buildingType = BuildingType.Barracks;
    public Transform spawnPoint;
    public float spawnRadius = 4f;

    private readonly Queue<UnitType> productionQueue = new Queue<UnitType>();
    private float currentProductionTime = 0f;

    public int QueueCount => productionQueue.Count;

    public float CurrentProgress01
    {
        get
        {
            if (productionQueue.Count == 0)
            {
                return 0f;
            }

            float totalTime = GetUnitBuildTime(productionQueue.Peek());
            if (totalTime <= 0.01f)
            {
                return 1f;
            }

            return Mathf.Clamp01(1f - (currentProductionTime / totalTime));
        }
    }

    private void Update()
    {
        if (productionQueue.Count == 0)
        {
            return;
        }

        currentProductionTime -= Time.deltaTime;
        if (currentProductionTime > 0f)
        {
            return;
        }

        UnitType finishedType = productionQueue.Dequeue();
        Vector3 spawnPosition = GetSpawnPosition();
        if (SimpleRtsGameManager.Instance != null)
        {
            SimpleRtsGameManager.Instance.SpawnUnit(finishedType, spawnPosition, team);
            SimpleRtsGameManager.Instance.SetStatus(buildingName + " produced: " + GetUnitDisplayName(finishedType), 1.5f);
        }

        if (productionQueue.Count > 0)
        {
            currentProductionTime = GetUnitBuildTime(productionQueue.Peek());
        }
    }

    public void Configure(BuildingType type, int teamId)
    {
        buildingType = type;
        team = teamId;

        switch (buildingType)
        {
            case BuildingType.Headquarters:
                buildingName = "HQ";
                maxHealth = 800;
                break;
            case BuildingType.Barracks:
                buildingName = "Barracks";
                maxHealth = 520;
                break;
            case BuildingType.Factory:
                buildingName = "Factory";
                maxHealth = 600;
                break;
            case BuildingType.Airfield:
                buildingName = "Airfield";
                maxHealth = 560;
                break;
        }

        health = maxHealth;

        Color tint = team == 0 ? new Color(0.2f, 0.42f, 0.95f) : new Color(0.9f, 0.28f, 0.26f);
        if (buildingType == BuildingType.Factory)
        {
            tint = Color.Lerp(tint, Color.gray, 0.25f);
        }
        if (buildingType == BuildingType.Airfield)
        {
            tint = Color.Lerp(tint, Color.white, 0.2f);
        }
        SetTeamTint(tint);
    }

    public bool CanProduce(UnitType unitType)
    {
        switch (buildingType)
        {
            case BuildingType.Barracks:
                return unitType == UnitType.Infantry;
            case BuildingType.Factory:
                return unitType == UnitType.Tank;
            case BuildingType.Airfield:
                return unitType == UnitType.Aircraft;
            default:
                return false;
        }
    }

    public int GetUnitCost(UnitType unitType)
    {
        switch (unitType)
        {
            case UnitType.Infantry:
                return 90;
            case UnitType.Tank:
                return 260;
            case UnitType.Aircraft:
                return 380;
            default:
                return 100;
        }
    }

    public float GetUnitBuildTime(UnitType unitType)
    {
        switch (unitType)
        {
            case UnitType.Infantry:
                return 2.8f;
            case UnitType.Tank:
                return 5.8f;
            case UnitType.Aircraft:
                return 7.2f;
            default:
                return 3f;
        }
    }

    public bool TryQueueUnit(UnitType unitType)
    {
        if (!CanProduce(unitType))
        {
            return false;
        }

        if (SimpleRtsGameManager.Instance == null)
        {
            return false;
        }

        int cost = GetUnitCost(unitType);
        if (!SimpleRtsGameManager.Instance.TrySpendResource(cost))
        {
            SimpleRtsGameManager.Instance.SetStatus("Not enough resources for " + GetUnitDisplayName(unitType));
            return false;
        }

        productionQueue.Enqueue(unitType);
        if (productionQueue.Count == 1)
        {
            currentProductionTime = GetUnitBuildTime(unitType);
        }

        SimpleRtsGameManager.Instance.SetStatus(buildingName + " queued: " + GetUnitDisplayName(unitType));
        return true;
    }

    public string GetQueueText()
    {
        if (productionQueue.Count == 0)
        {
            return "Empty";
        }

        UnitType[] queueArray = productionQueue.ToArray();
        string[] names = new string[queueArray.Length];
        for (int i = 0; i < queueArray.Length; i++)
        {
            names[i] = GetUnitDisplayName(queueArray[i]);
        }

        return string.Join(" -> ", names);
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoint != null)
        {
            Vector3 basePosition = spawnPoint.position;
            Vector2 random = Random.insideUnitCircle * 1.5f;
            return new Vector3(basePosition.x + random.x, basePosition.y, basePosition.z + random.y);
        }

        Vector2 jitter = Random.insideUnitCircle * 1.5f;
        return new Vector3(
            transform.position.x + transform.forward.x * spawnRadius + jitter.x,
            transform.position.y,
            transform.position.z + transform.forward.z * spawnRadius + jitter.y
        );
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

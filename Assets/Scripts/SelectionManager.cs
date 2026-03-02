using System.Collections.Generic;
using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance;

    [Header("Selection Box")]
    public Color selectionBoxColor = new Color(0.8f, 0.8f, 0.95f, 0.25f);
    public Color selectionBorderColor = new Color(0.8f, 0.8f, 0.95f, 1f);
    public float selectionBorderThickness = 2f;
    public float clickThreshold = 10f;

    [Header("Selected Entities")]
    public List<SelectableEntity> selectedEntities = new List<SelectableEntity>();

    private Vector3 startPos;
    private bool isSelecting = false;
    private Texture2D whiteTexture;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CreateWhiteTexture();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (whiteTexture != null)
        {
            Destroy(whiteTexture);
        }
    }

    private void Update()
    {
        if (SimpleRtsGameManager.Instance != null && SimpleRtsGameManager.Instance.IsPlacingBuilding)
        {
            return;
        }

        HandleSelectionInput();
        HandleUnitCommands();
    }

    private void CreateWhiteTexture()
    {
        whiteTexture = new Texture2D(1, 1);
        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply();
    }

    private void HandleSelectionInput()
    {
        bool ctrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        if (Input.GetMouseButtonDown(0))
        {
            startPos = Input.mousePosition;
            isSelecting = true;

            if (!ctrlPressed)
            {
                DeselectAll();
            }
        }

        if (Input.GetMouseButtonUp(0) && isSelecting)
        {
            float dragDistance = Vector3.Distance(startPos, Input.mousePosition);
            if (dragDistance <= clickThreshold)
            {
                HandleSingleSelection(ctrlPressed);
            }
            else
            {
                SelectEntitiesInRectangle();
            }

            isSelecting = false;
        }
    }

    private void HandleSingleSelection(bool ctrlPressed)
    {
        if (Camera.main == null)
        {
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            return;
        }

        SelectableEntity entity = hit.collider.GetComponentInParent<SelectableEntity>();
        if (entity == null || entity.team != 0)
        {
            return;
        }

        if (ctrlPressed)
        {
            if (selectedEntities.Contains(entity))
            {
                DeselectEntity(entity);
            }
            else
            {
                SelectEntity(entity);
            }
        }
        else
        {
            SelectEntity(entity);
        }
    }

    private void SelectEntitiesInRectangle()
    {
        if (Camera.main == null)
        {
            return;
        }

        Rect selectionRect = GetScreenRect(startPos, Input.mousePosition);
        SelectableEntity[] entities = FindObjectsOfType<SelectableEntity>();

        foreach (SelectableEntity entity in entities)
        {
            if (entity == null || entity.team != 0)
            {
                continue;
            }

            Vector3 screenPos = Camera.main.WorldToScreenPoint(entity.transform.position);
            if (screenPos.z < 0f)
            {
                continue;
            }

            screenPos.y = Screen.height - screenPos.y;
            if (selectionRect.Contains(screenPos))
            {
                SelectEntity(entity);
            }
        }
    }

    private void HandleUnitCommands()
    {
        if (!Input.GetMouseButtonDown(1) || Camera.main == null)
        {
            return;
        }

        List<Unit> selectedUnits = GetSelectedUnits();
        if (selectedUnits.Count == 0)
        {
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            return;
        }

        SelectableEntity targetEntity = hit.collider.GetComponentInParent<SelectableEntity>();
        if (targetEntity != null && targetEntity.team != selectedUnits[0].team)
        {
            foreach (Unit unit in selectedUnits)
            {
                if (unit != null && unit.CanAttack())
                {
                    unit.Attack(targetEntity);
                }
            }

            return;
        }

        IssueMoveCommand(selectedUnits, hit.point);
    }

    private void IssueMoveCommand(List<Unit> selectedUnits, Vector3 destination)
    {
        if (selectedUnits.Count == 1)
        {
            selectedUnits[0].MoveTo(destination);
            return;
        }

        int columnCount = Mathf.CeilToInt(Mathf.Sqrt(selectedUnits.Count));
        float spacing = 2.2f;

        Vector3 forward = Camera.main != null
            ? Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized
            : Vector3.forward;

        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.forward;
        }

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        if (right.sqrMagnitude < 0.001f)
        {
            right = Vector3.right;
        }

        for (int i = 0; i < selectedUnits.Count; i++)
        {
            int row = i / columnCount;
            int col = i % columnCount;

            float xOffset = (col - (columnCount - 1) * 0.5f) * spacing;
            float zOffset = -row * spacing;
            Vector3 offset = right * xOffset + forward * zOffset;
            selectedUnits[i].MoveTo(destination + offset);
        }
    }

    public void SelectEntity(SelectableEntity entity)
    {
        if (entity == null)
        {
            return;
        }

        if (!selectedEntities.Contains(entity))
        {
            selectedEntities.Add(entity);
            entity.SetSelected(true);
        }
    }

    public void DeselectEntity(SelectableEntity entity)
    {
        if (entity == null)
        {
            return;
        }

        if (selectedEntities.Contains(entity))
        {
            selectedEntities.Remove(entity);
            entity.SetSelected(false);
        }
    }

    public void DeselectAll()
    {
        for (int i = selectedEntities.Count - 1; i >= 0; i--)
        {
            SelectableEntity entity = selectedEntities[i];
            if (entity != null)
            {
                entity.SetSelected(false);
            }
        }

        selectedEntities.Clear();
    }

    public List<Unit> GetSelectedUnits()
    {
        List<Unit> units = new List<Unit>();
        foreach (SelectableEntity entity in selectedEntities)
        {
            Unit unit = entity as Unit;
            if (unit != null)
            {
                units.Add(unit);
            }
        }

        return units;
    }

    public Building GetFirstSelectedBuilding(int teamFilter = 0)
    {
        foreach (SelectableEntity entity in selectedEntities)
        {
            Building building = entity as Building;
            if (building != null && building.team == teamFilter)
            {
                return building;
            }
        }

        return null;
    }

    public string GetSelectionSummary()
    {
        if (selectedEntities.Count == 0)
        {
            return "None";
        }

        Dictionary<string, int> counts = new Dictionary<string, int>();
        foreach (SelectableEntity entity in selectedEntities)
        {
            if (entity == null)
            {
                continue;
            }

            string key = entity is Building ? ((Building)entity).buildingName : ((Unit)entity).unitName;
            if (!counts.ContainsKey(key))
            {
                counts[key] = 0;
            }

            counts[key]++;
        }

        List<string> parts = new List<string>();
        foreach (KeyValuePair<string, int> pair in counts)
        {
            parts.Add(pair.Key + " x" + pair.Value);
        }

        return string.Join(" | ", parts);
    }

    private Rect GetScreenRect(Vector3 screenPosition1, Vector3 screenPosition2)
    {
        screenPosition1.y = Screen.height - screenPosition1.y;
        screenPosition2.y = Screen.height - screenPosition2.y;

        Vector3 topLeft = Vector3.Min(screenPosition1, screenPosition2);
        Vector3 bottomRight = Vector3.Max(screenPosition1, screenPosition2);
        return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
    }

    private void DrawScreenRect(Rect rect, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(rect, whiteTexture);
        GUI.color = Color.white;
    }

    private void DrawScreenRectBorder(Rect rect, float thickness, Color color)
    {
        DrawScreenRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
        DrawScreenRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
        DrawScreenRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);
        DrawScreenRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
    }

    private void OnGUI()
    {
        if (!isSelecting)
        {
            return;
        }

        float dragDistance = Vector3.Distance(startPos, Input.mousePosition);
        if (dragDistance <= clickThreshold)
        {
            return;
        }

        Rect selectionRect = GetScreenRect(startPos, Input.mousePosition);
        DrawScreenRect(selectionRect, selectionBoxColor);
        DrawScreenRectBorder(selectionRect, selectionBorderThickness, selectionBorderColor);
    }
}

using UnityEngine;

/// <summary>
/// 地球帝国2风格的 HUD 界面
/// 包含顶部资源栏、底部指挥面板、建筑/单位图标按钮、选中信息、生产队列等
/// @author make java
/// @since 2026-03-09
/// </summary>
public class RtsHud : MonoBehaviour
{
    public static RtsHud Instance { get; private set; }

    private GUIStyle labelStyle;
    private GUIStyle labelSmallStyle;
    private GUIStyle labelBoldStyle;
    private GUIStyle labelShadowStyle;
    private GUIStyle iconButtonStyle;
    private GUIStyle tooltipStyle;
    private GUIStyle headerStyle;
    private GUIStyle resourceStyle;
    private GUIStyle statusStyle;
    private bool stylesInitialized = false;

    private string tooltipText = "";

    private const float TOP_BAR_HEIGHT = 36f;
    private const float BOTTOM_PANEL_HEIGHT = 180f;
    private const float ICON_SIZE = 56f;
    private const float ICON_PADDING = 4f;
    private const float SECTION_PADDING = 12f;

    private static readonly Color GoldText = new Color(0.95f, 0.85f, 0.45f, 1f);
    private static readonly Color LightText = new Color(0.88f, 0.88f, 0.90f, 1f);
    private static readonly Color DimText = new Color(0.60f, 0.60f, 0.62f, 1f);
    private static readonly Color PanelBorderColor = new Color(0.55f, 0.46f, 0.24f, 0.80f);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        RtsIconAtlas.ReleaseAll();
    }

    private void InitStyles()
    {
        if (stylesInitialized)
        {
            return;
        }
        stylesInitialized = true;

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.normal.textColor = LightText;
        labelStyle.fontSize = 13;
        labelStyle.alignment = TextAnchor.MiddleLeft;

        labelSmallStyle = new GUIStyle(GUI.skin.label);
        labelSmallStyle.normal.textColor = DimText;
        labelSmallStyle.fontSize = 11;
        labelSmallStyle.alignment = TextAnchor.MiddleLeft;

        labelBoldStyle = new GUIStyle(GUI.skin.label);
        labelBoldStyle.normal.textColor = GoldText;
        labelBoldStyle.fontSize = 14;
        labelBoldStyle.fontStyle = FontStyle.Bold;
        labelBoldStyle.alignment = TextAnchor.MiddleLeft;

        labelShadowStyle = new GUIStyle(GUI.skin.label);
        labelShadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.6f);
        labelShadowStyle.fontSize = 14;
        labelShadowStyle.fontStyle = FontStyle.Bold;
        labelShadowStyle.alignment = TextAnchor.MiddleLeft;

        headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.normal.textColor = GoldText;
        headerStyle.fontSize = 12;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.alignment = TextAnchor.MiddleCenter;

        resourceStyle = new GUIStyle(GUI.skin.label);
        resourceStyle.normal.textColor = GoldText;
        resourceStyle.fontSize = 15;
        resourceStyle.fontStyle = FontStyle.Bold;
        resourceStyle.alignment = TextAnchor.MiddleLeft;

        iconButtonStyle = new GUIStyle();
        iconButtonStyle.normal.background = RtsIconAtlas.GetButtonNormal();
        iconButtonStyle.hover.background = RtsIconAtlas.GetButtonHover();
        iconButtonStyle.active.background = RtsIconAtlas.GetButtonActive();
        iconButtonStyle.border = new RectOffset(4, 4, 4, 4);
        iconButtonStyle.padding = new RectOffset(2, 2, 2, 2);

        tooltipStyle = new GUIStyle(GUI.skin.box);
        tooltipStyle.normal.background = RtsIconAtlas.GetTooltipBg();
        tooltipStyle.normal.textColor = LightText;
        tooltipStyle.fontSize = 12;
        tooltipStyle.padding = new RectOffset(8, 8, 4, 4);
        tooltipStyle.alignment = TextAnchor.MiddleCenter;
        tooltipStyle.border = new RectOffset(3, 3, 3, 3);

        statusStyle = new GUIStyle(GUI.skin.label);
        statusStyle.normal.textColor = new Color(1f, 0.92f, 0.60f, 1f);
        statusStyle.fontSize = 13;
        statusStyle.alignment = TextAnchor.MiddleCenter;
        statusStyle.fontStyle = FontStyle.Bold;
    }

    private void OnGUI()
    {
        InitStyles();

        DrawWorldHealthBars();
        DrawTopBar();
        DrawBottomPanel();
        DrawStatusMessage();
        DrawTooltip();
        DrawControlsHelp();
    }

    private void DrawWorldHealthBars()
    {
        if (SelectionManager.Instance == null || Camera.main == null)
        {
            return;
        }

        foreach (SelectableEntity entity in SelectionManager.Instance.selectedEntities)
        {
            if (entity == null)
            {
                continue;
            }

            Vector3 worldPos = entity.transform.position + Vector3.up * GetEntityHeadOffset(entity);
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            if (screenPos.z < 0f)
            {
                continue;
            }

            float guiY = Screen.height - screenPos.y;

            float barW = 48f;
            float barH = 6f;
            float iconSize = 20f;

            Texture2D icon = null;
            if (entity is Building bld)
            {
                icon = RtsIconAtlas.GetBuildingIcon(bld.buildingType);
                barW = 56f;
            }
            else if (entity is Unit unt)
            {
                icon = RtsIconAtlas.GetUnitIcon(unt.unitType);
            }

            float totalW = barW;
            if (icon != null)
            {
                totalW += iconSize + 2f;
            }

            float startX = screenPos.x - totalW / 2f;
            float curX = startX;

            if (icon != null)
            {
                GUI.DrawTexture(new Rect(curX, guiY - iconSize / 2f, iconSize, iconSize),
                    icon, ScaleMode.ScaleToFit);
                curX += iconSize + 2f;
            }

            float barY = guiY - barH / 2f;
            Rect bgRect = new Rect(curX, barY, barW, barH);
            GUI.DrawTexture(bgRect, RtsIconAtlas.GetHealthBarBg(), ScaleMode.StretchToFill);

            float hpPct = (float)entity.health / Mathf.Max(1, entity.maxHealth);
            Color hpColor = hpPct > 0.5f ? new Color(0.18f, 0.82f, 0.22f, 0.95f) :
                hpPct > 0.25f ? new Color(0.90f, 0.78f, 0.20f, 0.95f) :
                new Color(0.90f, 0.25f, 0.20f, 0.95f);

            GUI.DrawTexture(new Rect(curX, barY, barW * hpPct, barH),
                RtsIconAtlas.GetSolidTexture(hpColor), ScaleMode.StretchToFill);

            DrawRectBorderGUI(bgRect, new Color(0.2f, 0.2f, 0.2f, 0.7f));
        }
    }

    private float GetEntityHeadOffset(SelectableEntity entity)
    {
        if (entity is Building)
        {
            return 6f;
        }
        if (entity is Unit unit)
        {
            if (unit.unitType == UnitType.Aircraft)
            {
                return 2f;
            }
            if (unit.unitType == UnitType.Tank)
            {
                return 2.5f;
            }
            return 3f;
        }
        return 3f;
    }

    private void DrawTopBar()
    {
        float sw = Screen.width;
        Rect topRect = new Rect(0, 0, sw, TOP_BAR_HEIGHT);

        GUI.DrawTexture(topRect, RtsIconAtlas.GetPanelBackground(), ScaleMode.StretchToFill);

        DrawPanelBorder(topRect);

        Texture2D resIcon = RtsIconAtlas.GetResourceIcon();
        float iconY = (TOP_BAR_HEIGHT - 24f) / 2f;
        GUI.DrawTexture(new Rect(14f, iconY, 24f, 24f), resIcon, ScaleMode.ScaleToFit);

        int resources = 0;
        if (SimpleRtsGameManager.Instance != null)
        {
            resources = SimpleRtsGameManager.Instance.playerResources;
        }

        GUI.Label(new Rect(44f, 0f, 200f, TOP_BAR_HEIGHT), resources.ToString(), resourceStyle);

        string gameTitle = "EMPIRE RTS";
        GUI.Label(new Rect(sw - 200f, 0f, 190f, TOP_BAR_HEIGHT), gameTitle, headerStyle);
    }

    private void DrawBottomPanel()
    {
        float sw = Screen.width;
        float sh = Screen.height;
        Rect panelRect = new Rect(0, sh - BOTTOM_PANEL_HEIGHT, sw, BOTTOM_PANEL_HEIGHT);

        GUI.DrawTexture(panelRect, RtsIconAtlas.GetPanelBackground(), ScaleMode.StretchToFill);
        DrawPanelBorder(panelRect);

        float decorY = sh - BOTTOM_PANEL_HEIGHT - 2f;
        GUI.DrawTexture(new Rect(0, decorY, sw, 3f),
            RtsIconAtlas.GetSolidTexture(PanelBorderColor), ScaleMode.StretchToFill);

        float sectionX = SECTION_PADDING;
        float sectionY = sh - BOTTOM_PANEL_HEIGHT + 8f;

        float portraitW = DrawSelectionPortrait(sectionX, sectionY);
        sectionX += portraitW + SECTION_PADDING;

        float infoW = DrawSelectionInfo(sectionX, sectionY);
        sectionX += infoW + SECTION_PADDING;

        DrawSeparator(sectionX, sectionY, BOTTOM_PANEL_HEIGHT - 16f);
        sectionX += 8f;

        float actionW = DrawActionPanel(sectionX, sectionY);
        sectionX += actionW + SECTION_PADDING;

        DrawSeparator(sectionX, sectionY, BOTTOM_PANEL_HEIGHT - 16f);
        sectionX += 8f;

        DrawBuildPanel(sectionX, sectionY);
    }

    private float DrawSelectionPortrait(float x, float y)
    {
        float size = BOTTOM_PANEL_HEIGHT - 24f;
        Rect bgRect = new Rect(x, y, size, size);
        GUI.DrawTexture(bgRect, RtsIconAtlas.GetSolidTexture(new Color(0.05f, 0.06f, 0.08f, 0.8f)),
            ScaleMode.StretchToFill);
        DrawRectBorderGUI(bgRect, PanelBorderColor);

        if (SelectionManager.Instance == null || SelectionManager.Instance.selectedEntities.Count == 0)
        {
            GUIStyle emptyLabel = new GUIStyle(labelSmallStyle);
            emptyLabel.alignment = TextAnchor.MiddleCenter;
            GUI.Label(bgRect, "No\nSelection", emptyLabel);
            return size;
        }

        SelectableEntity first = SelectionManager.Instance.selectedEntities[0];
        if (first == null)
        {
            return size;
        }

        Texture2D portrait = null;
        if (first is Building bld)
        {
            portrait = RtsIconAtlas.GetBuildingIcon(bld.buildingType);
        }
        else if (first is Unit unt)
        {
            portrait = RtsIconAtlas.GetUnitIcon(unt.unitType);
        }

        if (portrait != null)
        {
            float pad = 6f;
            GUI.DrawTexture(new Rect(x + pad, y + pad, size - 2 * pad, size - 2 * pad),
                portrait, ScaleMode.ScaleToFit);
        }

        if (SelectionManager.Instance.selectedEntities.Count > 1)
        {
            GUIStyle countStyle = new GUIStyle(labelBoldStyle);
            countStyle.alignment = TextAnchor.LowerRight;
            GUI.Label(new Rect(x, y, size - 4f, size - 2f),
                "x" + SelectionManager.Instance.selectedEntities.Count, countStyle);
        }

        return size;
    }

    private float DrawSelectionInfo(float x, float y)
    {
        float width = 160f;
        float lineH = 20f;
        float curY = y;

        if (SelectionManager.Instance == null || SelectionManager.Instance.selectedEntities.Count == 0)
        {
            GUI.Label(new Rect(x, curY, width, lineH), "No selection", labelStyle);
            return width;
        }

        SelectableEntity first = SelectionManager.Instance.selectedEntities[0];
        if (first == null)
        {
            return width;
        }

        string entityName = "";
        if (first is Building bld)
        {
            entityName = bld.buildingName;
        }
        else if (first is Unit unt)
        {
            entityName = unt.unitName;
        }

        GUI.Label(new Rect(x + 1f, curY + 1f, width, lineH), entityName, labelShadowStyle);
        GUI.Label(new Rect(x, curY, width, lineH), entityName, labelBoldStyle);
        curY += lineH + 2f;

        float hpPercent = (float)first.health / Mathf.Max(1, first.maxHealth);
        string hpText = first.health + " / " + first.maxHealth;

        GUI.Label(new Rect(x, curY, width, lineH), "HP:", labelSmallStyle);
        curY += lineH - 2f;

        float barW = width - 4f;
        float barH = 10f;
        Rect hpBgRect = new Rect(x, curY, barW, barH);
        GUI.DrawTexture(hpBgRect, RtsIconAtlas.GetHealthBarBg(), ScaleMode.StretchToFill);

        Color hpColor = hpPercent > 0.5f ? new Color(0.18f, 0.82f, 0.22f, 0.95f) :
            hpPercent > 0.25f ? new Color(0.90f, 0.78f, 0.20f, 0.95f) :
            new Color(0.90f, 0.25f, 0.20f, 0.95f);
        GUI.DrawTexture(new Rect(x, curY, barW * hpPercent, barH),
            RtsIconAtlas.GetSolidTexture(hpColor), ScaleMode.StretchToFill);
        DrawRectBorderGUI(hpBgRect, new Color(0.3f, 0.3f, 0.3f, 0.6f));

        GUIStyle hpLabelStyle = new GUIStyle(labelSmallStyle);
        hpLabelStyle.alignment = TextAnchor.MiddleCenter;
        hpLabelStyle.fontSize = 9;
        hpLabelStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(x, curY - 1f, barW, barH + 2f), hpText, hpLabelStyle);
        curY += barH + 6f;

        string teamText = first.team == 0 ? "Player" : "Enemy";
        Color teamColor = first.team == 0 ? new Color(0.4f, 0.65f, 1f) : new Color(1f, 0.45f, 0.4f);
        GUIStyle teamStyle = new GUIStyle(labelSmallStyle);
        teamStyle.normal.textColor = teamColor;
        GUI.Label(new Rect(x, curY, width, lineH), "Team: " + teamText, teamStyle);
        curY += lineH;

        if (first is Unit unit)
        {
            GUI.Label(new Rect(x, curY, width, lineH),
                "ATK: " + unit.attackDamage + "  RNG: " + unit.attackRange.ToString("F0"),
                labelSmallStyle);
            curY += lineH;
            GUI.Label(new Rect(x, curY, width, lineH),
                "SPD: " + unit.moveSpeed.ToString("F1"),
                labelSmallStyle);
        }

        if (first is Building building && building.QueueCount > 0)
        {
            curY += 2f;
            GUI.Label(new Rect(x, curY, width, lineH), "Production:", labelSmallStyle);
            curY += lineH - 2f;

            float progBarW = width - 4f;
            float progBarH = 12f;
            Rect progBgRect = new Rect(x, curY, progBarW, progBarH);
            GUI.DrawTexture(progBgRect, RtsIconAtlas.GetProgressBarBg(), ScaleMode.StretchToFill);
            GUI.DrawTexture(new Rect(x, curY, progBarW * building.CurrentProgress01, progBarH),
                RtsIconAtlas.GetProgressBarFill(), ScaleMode.StretchToFill);
            DrawRectBorderGUI(progBgRect, new Color(0.3f, 0.3f, 0.3f, 0.5f));

            GUIStyle progLabel = new GUIStyle(labelSmallStyle);
            progLabel.alignment = TextAnchor.MiddleCenter;
            progLabel.fontSize = 9;
            progLabel.normal.textColor = Color.white;
            GUI.Label(new Rect(x, curY, progBarW, progBarH),
                Mathf.RoundToInt(building.CurrentProgress01 * 100f) + "%", progLabel);
            curY += progBarH + 4f;

            GUI.Label(new Rect(x, curY, width, lineH),
                "Queue: " + building.GetQueueText(), labelSmallStyle);
        }

        return width;
    }

    private float DrawActionPanel(float x, float y)
    {
        float width = 140f;
        float curY = y;

        GUI.Label(new Rect(x, curY, width, 18f), "COMMANDS", headerStyle);
        curY += 22f;

        Building selectedBuilding = null;
        if (SelectionManager.Instance != null)
        {
            selectedBuilding = SelectionManager.Instance.GetFirstSelectedBuilding(0);
        }

        if (selectedBuilding != null)
        {
            GUI.Label(new Rect(x, curY, width, 16f), "Produce Units:", labelSmallStyle);
            curY += 20f;

            float btnX = x;
            if (selectedBuilding.CanProduce(UnitType.Infantry))
            {
                if (DrawIconButton(new Rect(btnX, curY, ICON_SIZE, ICON_SIZE),
                    RtsIconAtlas.GetUnitIcon(UnitType.Infantry),
                    "Infantry [1]\nCost: 90"))
                {
                    selectedBuilding.TryQueueUnit(UnitType.Infantry);
                }
                DrawIconLabel(new Rect(btnX, curY + ICON_SIZE, ICON_SIZE, 14f), "Infantry", "1");
                btnX += ICON_SIZE + ICON_PADDING;
            }

            if (selectedBuilding.CanProduce(UnitType.Tank))
            {
                if (DrawIconButton(new Rect(btnX, curY, ICON_SIZE, ICON_SIZE),
                    RtsIconAtlas.GetUnitIcon(UnitType.Tank),
                    "Tank [2]\nCost: 260"))
                {
                    selectedBuilding.TryQueueUnit(UnitType.Tank);
                }
                DrawIconLabel(new Rect(btnX, curY + ICON_SIZE, ICON_SIZE, 14f), "Tank", "2");
                btnX += ICON_SIZE + ICON_PADDING;
            }

            if (selectedBuilding.CanProduce(UnitType.Aircraft))
            {
                if (DrawIconButton(new Rect(btnX, curY, ICON_SIZE, ICON_SIZE),
                    RtsIconAtlas.GetUnitIcon(UnitType.Aircraft),
                    "Aircraft [3]\nCost: 380"))
                {
                    selectedBuilding.TryQueueUnit(UnitType.Aircraft);
                }
                DrawIconLabel(new Rect(btnX, curY + ICON_SIZE, ICON_SIZE, 14f), "Aircraft", "3");
            }
        }
        else
        {
            GUI.Label(new Rect(x, curY, width, 16f), "Select a building", labelSmallStyle);
            curY += 18f;
            GUI.Label(new Rect(x, curY, width, 16f), "to produce units", labelSmallStyle);
        }

        return width;
    }

    private void DrawBuildPanel(float x, float y)
    {
        float curY = y;
        float width = 250f;

        GUI.Label(new Rect(x, curY, width, 18f), "BUILD", headerStyle);
        curY += 22f;

        GUI.Label(new Rect(x, curY, width, 16f), "Place Buildings:", labelSmallStyle);
        curY += 20f;

        float btnX = x;

        BuildingType? pending = null;
        if (SimpleRtsGameManager.Instance != null && SimpleRtsGameManager.Instance.IsPlacingBuilding)
        {
            pending = GetPendingBuildingType();
        }

        if (DrawBuildButton(new Rect(btnX, curY, ICON_SIZE, ICON_SIZE),
            RtsIconAtlas.GetBuildingIcon(BuildingType.Barracks),
            "Barracks [B]\nCost: 260",
            pending.HasValue && pending.Value == BuildingType.Barracks))
        {
            SimulateBuildKey(KeyCode.B);
        }
        DrawIconLabel(new Rect(btnX, curY + ICON_SIZE, ICON_SIZE, 14f), "Barracks", "B");
        btnX += ICON_SIZE + ICON_PADDING;

        if (DrawBuildButton(new Rect(btnX, curY, ICON_SIZE, ICON_SIZE),
            RtsIconAtlas.GetBuildingIcon(BuildingType.Factory),
            "Factory [N]\nCost: 420",
            pending.HasValue && pending.Value == BuildingType.Factory))
        {
            SimulateBuildKey(KeyCode.N);
        }
        DrawIconLabel(new Rect(btnX, curY + ICON_SIZE, ICON_SIZE, 14f), "Factory", "N");
        btnX += ICON_SIZE + ICON_PADDING;

        if (DrawBuildButton(new Rect(btnX, curY, ICON_SIZE, ICON_SIZE),
            RtsIconAtlas.GetBuildingIcon(BuildingType.Airfield),
            "Airfield [M]\nCost: 520",
            pending.HasValue && pending.Value == BuildingType.Airfield))
        {
            SimulateBuildKey(KeyCode.M);
        }
        DrawIconLabel(new Rect(btnX, curY + ICON_SIZE, ICON_SIZE, 14f), "Airfield", "M");
        btnX += ICON_SIZE + ICON_PADDING + 8f;

        DrawSeparator(btnX, y + 22f, BOTTOM_PANEL_HEIGHT - 38f);
        btnX += 8f;

        float infoX = btnX;
        float infoY = y;
        GUI.Label(new Rect(infoX, infoY, 100f, 18f), "OVERVIEW", headerStyle);
        infoY += 22f;

        Texture2D hqIcon = RtsIconAtlas.GetBuildingIcon(BuildingType.Headquarters);
        GUI.DrawTexture(new Rect(infoX, infoY, 32f, 32f), hqIcon, ScaleMode.ScaleToFit);
        GUI.Label(new Rect(infoX + 36f, infoY, 100f, 16f), "HQ", labelSmallStyle);
        GUI.Label(new Rect(infoX + 36f, infoY + 14f, 100f, 16f), "Command Center", labelSmallStyle);
        infoY += 38f;

        string selSummary = "None";
        if (SelectionManager.Instance != null)
        {
            selSummary = SelectionManager.Instance.GetSelectionSummary();
        }
        GUI.Label(new Rect(infoX, infoY, 140f, 16f), "Sel: " + selSummary, labelSmallStyle);
    }

    private void DrawStatusMessage()
    {
        if (SimpleRtsGameManager.Instance == null)
        {
            return;
        }

        string status = GetStatusMessage();
        if (string.IsNullOrEmpty(status))
        {
            return;
        }

        float sw = Screen.width;
        float sh = Screen.height;
        float msgW = Mathf.Min(500f, sw - 40f);
        float msgH = 32f;
        float msgY = sh - BOTTOM_PANEL_HEIGHT - 42f;

        Rect bgRect = new Rect((sw - msgW) / 2f, msgY, msgW, msgH);
        GUI.DrawTexture(bgRect, RtsIconAtlas.GetSolidTexture(new Color(0.04f, 0.05f, 0.08f, 0.88f)),
            ScaleMode.StretchToFill);
        DrawRectBorderGUI(bgRect, PanelBorderColor);

        GUI.Label(bgRect, status, statusStyle);
    }

    private void DrawTooltip()
    {
        if (string.IsNullOrEmpty(tooltipText))
        {
            return;
        }

        Vector2 mousePos = Event.current.mousePosition;
        float tw = tooltipText.Length * 8f + 24f;
        float th = 28f;

        if (tooltipText.Contains("\n"))
        {
            th = 44f;
            string[] lines = tooltipText.Split('\n');
            float maxLen = 0;
            foreach (string line in lines)
            {
                if (line.Length > maxLen)
                {
                    maxLen = line.Length;
                }
            }
            tw = maxLen * 8f + 24f;
        }

        float tx = mousePos.x + 16f;
        float ty = mousePos.y - th - 8f;
        if (tx + tw > Screen.width)
        {
            tx = Screen.width - tw - 4f;
        }
        if (ty < 0f)
        {
            ty = mousePos.y + 20f;
        }

        GUI.Box(new Rect(tx, ty, tw, th), tooltipText, tooltipStyle);
        tooltipText = "";
    }

    private void DrawControlsHelp()
    {
        float sh = Screen.height;
        float helpY = TOP_BAR_HEIGHT + 6f;
        float helpX = 10f;
        float lineH = 16f;

        GUIStyle helpStyle = new GUIStyle(labelSmallStyle);
        helpStyle.normal.textColor = new Color(0.6f, 0.6f, 0.62f, 0.6f);
        helpStyle.fontSize = 10;

        GUI.Label(new Rect(helpX, helpY, 320f, lineH), "Camera: WASD / Arrows / Edge Pan | Zoom: Scroll", helpStyle);
        GUI.Label(new Rect(helpX, helpY + lineH, 320f, lineH), "Select: LMB | Command: RMB | Ctrl+Click: Multi-select", helpStyle);
    }

    private bool DrawIconButton(Rect rect, Texture2D icon, string tooltip)
    {
        bool clicked = GUI.Button(rect, GUIContent.none, iconButtonStyle);

        if (icon != null)
        {
            float pad = 4f;
            GUI.DrawTexture(new Rect(rect.x + pad, rect.y + pad,
                rect.width - 2 * pad, rect.height - 2 * pad), icon, ScaleMode.ScaleToFit);
        }

        if (rect.Contains(Event.current.mousePosition))
        {
            tooltipText = tooltip;
            GUI.DrawTexture(rect, RtsIconAtlas.GetSolidTexture(new Color(1f, 1f, 1f, 0.08f)),
                ScaleMode.StretchToFill);
        }

        return clicked;
    }

    private bool DrawBuildButton(Rect rect, Texture2D icon, string tooltip, bool isActive)
    {
        if (isActive)
        {
            GUI.DrawTexture(rect, RtsIconAtlas.GetButtonActive(), ScaleMode.StretchToFill);
        }

        bool clicked = GUI.Button(rect, GUIContent.none, iconButtonStyle);

        if (icon != null)
        {
            float pad = 4f;
            GUI.DrawTexture(new Rect(rect.x + pad, rect.y + pad,
                rect.width - 2 * pad, rect.height - 2 * pad), icon, ScaleMode.ScaleToFit);
        }

        if (isActive)
        {
            DrawRectBorderGUI(rect, new Color(0.95f, 0.82f, 0.35f, 0.9f));
        }

        if (rect.Contains(Event.current.mousePosition))
        {
            tooltipText = tooltip;
            GUI.DrawTexture(rect, RtsIconAtlas.GetSolidTexture(new Color(1f, 1f, 1f, 0.08f)),
                ScaleMode.StretchToFill);
        }

        return clicked;
    }

    private void DrawIconLabel(Rect rect, string name, string hotkey)
    {
        GUIStyle nameStyle = new GUIStyle(labelSmallStyle);
        nameStyle.alignment = TextAnchor.UpperCenter;
        nameStyle.fontSize = 9;
        nameStyle.normal.textColor = DimText;
        GUI.Label(rect, name, nameStyle);

        GUIStyle keyStyle = new GUIStyle(nameStyle);
        keyStyle.normal.textColor = GoldText;
        keyStyle.fontSize = 9;
        float keyW = 16f;
        GUI.Label(new Rect(rect.x + rect.width - keyW - 2f, rect.y - ICON_SIZE + 2f, keyW, 14f),
            hotkey, keyStyle);
    }

    private void DrawSeparator(float x, float y, float height)
    {
        GUI.DrawTexture(new Rect(x, y, 1f, height),
            RtsIconAtlas.GetSolidTexture(new Color(0.40f, 0.34f, 0.18f, 0.5f)),
            ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(x + 1f, y, 1f, height),
            RtsIconAtlas.GetSolidTexture(new Color(0.15f, 0.12f, 0.08f, 0.5f)),
            ScaleMode.StretchToFill);
    }

    private void DrawPanelBorder(Rect rect)
    {
        float t = 1f;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, t),
            RtsIconAtlas.GetSolidTexture(PanelBorderColor), ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - t, rect.width, t),
            RtsIconAtlas.GetSolidTexture(PanelBorderColor), ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(rect.x, rect.y, t, rect.height),
            RtsIconAtlas.GetSolidTexture(PanelBorderColor), ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(rect.xMax - t, rect.y, t, rect.height),
            RtsIconAtlas.GetSolidTexture(PanelBorderColor), ScaleMode.StretchToFill);
    }

    private void DrawRectBorderGUI(Rect rect, Color color)
    {
        Texture2D tex = RtsIconAtlas.GetSolidTexture(color);
        float t = 1f;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, t), tex, ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - t, rect.width, t), tex, ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(rect.x, rect.y, t, rect.height), tex, ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(rect.xMax - t, rect.y, t, rect.height), tex, ScaleMode.StretchToFill);
    }

    private string GetStatusMessage()
    {
        if (SimpleRtsGameManager.Instance == null)
        {
            return "";
        }

        return SimpleRtsGameManager.Instance.GetCurrentStatusMessage();
    }

    private BuildingType? GetPendingBuildingType()
    {
        if (SimpleRtsGameManager.Instance == null)
        {
            return null;
        }

        return SimpleRtsGameManager.Instance.GetPendingBuildingType();
    }

    private void SimulateBuildKey(KeyCode key)
    {
        if (SimpleRtsGameManager.Instance == null)
        {
            return;
        }

        SimpleRtsGameManager.Instance.StartBuildMode(key);
    }
}

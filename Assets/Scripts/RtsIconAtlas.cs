using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 程序化生成 RTS 游戏图标（建筑、单位、资源、操作）
/// 参考地球帝国2的图标风格：深色背景 + 主体剪影 + 金色边框
/// @author make java
/// @since 2026-03-09
/// </summary>
public static class RtsIconAtlas
{
    private static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

    private static readonly Color FrameGold = new Color(0.78f, 0.65f, 0.32f, 1f);
    private static readonly Color FrameGoldDark = new Color(0.52f, 0.42f, 0.18f, 1f);
    private static readonly Color FrameGoldBright = new Color(0.95f, 0.82f, 0.45f, 1f);
    private static readonly Color IconBgDark = new Color(0.08f, 0.10f, 0.14f, 1f);
    private static readonly Color IconBgGrad = new Color(0.12f, 0.15f, 0.22f, 1f);


    public static int IconSize => 64;

    public static Texture2D GetBuildingIcon(BuildingType type)
    {
        string key = "building_" + type;
        if (cache.TryGetValue(key, out Texture2D tex))
        {
            return tex;
        }

        tex = CreateIcon(IconSize, IconSize);
        DrawIconBackground(tex);

        switch (type)
        {
            case BuildingType.Headquarters:
                DrawHQIcon(tex);
                break;
            case BuildingType.Barracks:
                DrawBarracksIcon(tex);
                break;
            case BuildingType.Factory:
                DrawFactoryIcon(tex);
                break;
            case BuildingType.Airfield:
                DrawAirfieldIcon(tex);
                break;
        }

        DrawIconFrame(tex);
        tex.Apply();
        cache[key] = tex;
        return tex;
    }

    public static Texture2D GetUnitIcon(UnitType type)
    {
        string key = "unit_" + type;
        if (cache.TryGetValue(key, out Texture2D tex))
        {
            return tex;
        }

        tex = CreateIcon(IconSize, IconSize);
        DrawIconBackground(tex);

        switch (type)
        {
            case UnitType.Infantry:
                DrawInfantryIcon(tex);
                break;
            case UnitType.Tank:
                DrawTankIcon(tex);
                break;
            case UnitType.Aircraft:
                DrawAircraftIcon(tex);
                break;
        }

        DrawIconFrame(tex);
        tex.Apply();
        cache[key] = tex;
        return tex;
    }

    public static Texture2D GetResourceIcon()
    {
        string key = "resource_gold";
        if (cache.TryGetValue(key, out Texture2D tex))
        {
            return tex;
        }

        tex = CreateIcon(32, 32);
        DrawResourceIcon(tex);
        tex.Apply();
        cache[key] = tex;
        return tex;
    }

    public static Texture2D GetActionIcon(string action)
    {
        string key = "action_" + action;
        if (cache.TryGetValue(key, out Texture2D tex))
        {
            return tex;
        }

        tex = CreateIcon(48, 48);
        DrawIconBackground(tex);

        switch (action)
        {
            case "move":
                DrawMoveIcon(tex);
                break;
            case "attack":
                DrawAttackIcon(tex);
                break;
            case "stop":
                DrawStopIcon(tex);
                break;
            case "build":
                DrawBuildIcon(tex);
                break;
        }

        DrawIconFrame(tex);
        tex.Apply();
        cache[key] = tex;
        return tex;
    }

    public static Texture2D GetPanelBackground()
    {
        string key = "panel_bg";
        if (cache.TryGetValue(key, out Texture2D tex))
        {
            return tex;
        }

        tex = CreateIcon(8, 8);
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                float t = (float)y / 7f;
                Color c = Color.Lerp(new Color(0.04f, 0.05f, 0.08f, 0.95f),
                    new Color(0.10f, 0.12f, 0.17f, 0.95f), t);
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        cache[key] = tex;
        return tex;
    }

    public static Texture2D GetButtonNormal()
    {
        string key = "btn_normal";
        if (cache.TryGetValue(key, out Texture2D tex))
        {
            return tex;
        }

        tex = CreateIcon(64, 64);
        DrawButtonBackground(tex, new Color(0.14f, 0.16f, 0.22f, 0.9f),
            new Color(0.20f, 0.23f, 0.30f, 0.9f));
        DrawButtonBorder(tex, FrameGoldDark);
        tex.Apply();
        cache[key] = tex;
        return tex;
    }

    public static Texture2D GetButtonHover()
    {
        string key = "btn_hover";
        if (cache.TryGetValue(key, out Texture2D tex))
        {
            return tex;
        }

        tex = CreateIcon(64, 64);
        DrawButtonBackground(tex, new Color(0.20f, 0.22f, 0.30f, 0.95f),
            new Color(0.28f, 0.32f, 0.40f, 0.95f));
        DrawButtonBorder(tex, FrameGold);
        tex.Apply();
        cache[key] = tex;
        return tex;
    }

    public static Texture2D GetButtonActive()
    {
        string key = "btn_active";
        if (cache.TryGetValue(key, out Texture2D tex))
        {
            return tex;
        }

        tex = CreateIcon(64, 64);
        DrawButtonBackground(tex, new Color(0.30f, 0.28f, 0.18f, 0.95f),
            new Color(0.38f, 0.35f, 0.22f, 0.95f));
        DrawButtonBorder(tex, FrameGoldBright);
        tex.Apply();
        cache[key] = tex;
        return tex;
    }

    public static Texture2D GetProgressBarBg()
    {
        string key = "progress_bg";
        if (cache.TryGetValue(key, out Texture2D tex))
        {
            return tex;
        }

        tex = CreateIcon(4, 4);
        FillRect(tex, 0, 0, 4, 4, new Color(0.05f, 0.06f, 0.08f, 0.9f));
        tex.Apply();
        cache[key] = tex;
        return tex;
    }

    public static Texture2D GetProgressBarFill()
    {
        string key = "progress_fill";
        if (cache.TryGetValue(key, out Texture2D tex))
        {
            return tex;
        }

        tex = CreateIcon(4, 4);
        FillRect(tex, 0, 0, 4, 4, new Color(0.28f, 0.72f, 0.32f, 0.95f));
        tex.Apply();
        cache[key] = tex;
        return tex;
    }

    public static Texture2D GetHealthBarBg()
    {
        string key = "hp_bg";
        if (cache.TryGetValue(key, out Texture2D tex))
        {
            return tex;
        }

        tex = CreateIcon(2, 2);
        FillRect(tex, 0, 0, 2, 2, new Color(0.25f, 0.02f, 0.02f, 0.85f));
        tex.Apply();
        cache[key] = tex;
        return tex;
    }

    public static Texture2D GetHealthBarFill()
    {
        string key = "hp_fill";
        if (cache.TryGetValue(key, out Texture2D tex))
        {
            return tex;
        }

        tex = CreateIcon(2, 2);
        FillRect(tex, 0, 0, 2, 2, new Color(0.18f, 0.82f, 0.22f, 0.95f));
        tex.Apply();
        cache[key] = tex;
        return tex;
    }

    public static Texture2D GetSolidTexture(Color color)
    {
        string key = "solid_" + ColorUtility.ToHtmlStringRGBA(color);
        if (cache.TryGetValue(key, out Texture2D tex))
        {
            return tex;
        }

        tex = CreateIcon(2, 2);
        FillRect(tex, 0, 0, 2, 2, color);
        tex.Apply();
        cache[key] = tex;
        return tex;
    }

    public static Texture2D GetTooltipBg()
    {
        string key = "tooltip_bg";
        if (cache.TryGetValue(key, out Texture2D tex))
        {
            return tex;
        }

        tex = CreateIcon(16, 16);
        int w = tex.width;
        int h = tex.height;
        FillRect(tex, 0, 0, w, h, new Color(0.04f, 0.05f, 0.08f, 0.96f));
        DrawRectBorder(tex, 0, 0, w, h, FrameGoldDark);
        DrawRectBorder(tex, 1, 1, w - 2, h - 2, new Color(0.35f, 0.3f, 0.15f, 0.5f));
        tex.Apply();
        cache[key] = tex;
        return tex;
    }

    public static void ReleaseAll()
    {
        foreach (var pair in cache)
        {
            if (pair.Value != null)
            {
                Object.Destroy(pair.Value);
            }
        }
        cache.Clear();
    }

    private static Texture2D CreateIcon(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        return tex;
    }

    private static void DrawIconBackground(Texture2D tex)
    {
        int w = tex.width;
        int h = tex.height;
        for (int y = 0; y < h; y++)
        {
            float t = (float)y / (h - 1);
            Color bg = Color.Lerp(IconBgDark, IconBgGrad, t * 0.6f);
            for (int x = 0; x < w; x++)
            {
                tex.SetPixel(x, y, bg);
            }
        }
    }

    private static void DrawIconFrame(Texture2D tex)
    {
        int w = tex.width;
        int h = tex.height;

        DrawRectBorder(tex, 0, 0, w, h, FrameGoldDark);
        DrawRectBorder(tex, 1, 1, w - 2, h - 2, FrameGold);
        DrawRectBorder(tex, 2, 2, w - 4, h - 4, FrameGoldDark);

        for (int i = 2; i < w - 2; i++)
        {
            tex.SetPixel(i, h - 3, Color.Lerp(FrameGold, FrameGoldBright, 0.6f));
        }
        for (int i = 2; i < h - 2; i++)
        {
            tex.SetPixel(2, i, Color.Lerp(FrameGold, FrameGoldBright, 0.3f));
        }
    }

    private static void DrawButtonBackground(Texture2D tex, Color top, Color bottom)
    {
        int w = tex.width;
        int h = tex.height;
        for (int y = 0; y < h; y++)
        {
            float t = (float)y / (h - 1);
            Color c = Color.Lerp(top, bottom, t);
            for (int x = 0; x < w; x++)
            {
                tex.SetPixel(x, y, c);
            }
        }
    }

    private static void DrawButtonBorder(Texture2D tex, Color borderColor)
    {
        int w = tex.width;
        int h = tex.height;
        DrawRectBorder(tex, 0, 0, w, h, borderColor);
        Color inner = Color.Lerp(borderColor, Color.black, 0.4f);
        DrawRectBorder(tex, 1, 1, w - 2, h - 2, inner);
    }

    #region Building Icons

    private static void DrawHQIcon(Texture2D tex)
    {
        int w = tex.width;
        int h = tex.height;
        Color wall = new Color(0.55f, 0.58f, 0.65f, 1f);
        Color wallDark = new Color(0.35f, 0.38f, 0.42f, 1f);
        Color roofColor = new Color(0.40f, 0.45f, 0.55f, 1f);
        Color flagPole = new Color(0.70f, 0.65f, 0.50f, 1f);
        Color flagRed = new Color(0.85f, 0.25f, 0.22f, 1f);
        Color windowYellow = new Color(0.90f, 0.80f, 0.40f, 0.9f);
        Color ground = new Color(0.22f, 0.28f, 0.18f, 1f);

        int margin = 3;
        int bx = margin + 6;
        int bw = w - 2 * bx;
        int by = margin + 4;
        int bh = h - 2 * margin - 14;

        FillRect(tex, margin + 3, margin + 3, w - 2 * margin - 6, 4, ground);

        FillRect(tex, bx, by, bw, bh, wall);
        FillRect(tex, bx, by, bw, 3, wallDark);

        int cx = w / 2;
        int towerW = bw / 3;
        int towerH = bh + 8;
        int towerX = cx - towerW / 2;
        FillRect(tex, towerX, by, towerW, towerH, wallDark);
        FillRect(tex, towerX, by + towerH - 3, towerW, 3, roofColor);

        FillRect(tex, bx - 2, by + bh - 2, bw + 4, 3, roofColor);

        int tw = 6;
        int th = 6;
        FillRect(tex, bx, by + bh - th, tw, th, wallDark);
        FillRect(tex, bx + bw - tw, by + bh - th, tw, th, wallDark);
        FillRect(tex, bx, by + bh, tw, 2, roofColor);
        FillRect(tex, bx + bw - tw, by + bh, tw, 2, roofColor);

        int flagBase = by + towerH;
        int poleH = 12;
        DrawVerticalLine(tex, cx, flagBase, flagBase + poleH, flagPole);
        FillRect(tex, cx + 1, flagBase + poleH - 5, 6, 4, flagRed);

        int wy = by + 4;
        int winSize = 2;
        int winSpacing = 5;
        for (int i = 0; i < 3; i++)
        {
            int wx = bx + 4 + i * winSpacing;
            FillRect(tex, wx, wy, winSize, winSize, windowYellow);
        }
        for (int i = 0; i < 3; i++)
        {
            int wx = cx + 2 + i * winSpacing;
            FillRect(tex, wx, wy, winSize, winSize, windowYellow);
        }

        FillRect(tex, cx - 2, by + 1, 4, 5, new Color(0.25f, 0.2f, 0.15f, 1f));
    }

    private static void DrawBarracksIcon(Texture2D tex)
    {
        int w = tex.width;
        int h = tex.height;
        Color wall = new Color(0.50f, 0.48f, 0.44f, 1f);
        Color wallDark = new Color(0.32f, 0.30f, 0.28f, 1f);
        Color roofColor = new Color(0.42f, 0.36f, 0.30f, 1f);
        Color door = new Color(0.22f, 0.18f, 0.14f, 1f);
        Color starColor = new Color(0.90f, 0.82f, 0.40f, 1f);
        Color ground = new Color(0.22f, 0.28f, 0.18f, 1f);

        int margin = 3;
        FillRect(tex, margin + 3, margin + 3, w - 2 * margin - 6, 4, ground);

        int bx = margin + 5;
        int bw = w - 2 * bx;
        int by = margin + 5;
        int bh = h - 2 * margin - 18;

        FillRect(tex, bx, by, bw, bh, wall);

        for (int i = 0; i < bw; i++)
        {
            int rx = bx + i;
            int roofH = 6 - Mathf.Abs(i - bw / 2) * 6 / (bw / 2);
            if (roofH < 1) { roofH = 1; }
            FillRect(tex, rx, by + bh, 1, roofH, roofColor);
        }

        FillRect(tex, bx, by, bw, 2, wallDark);

        int cx = w / 2;
        FillRect(tex, cx - 3, by + 1, 6, 8, door);

        int sx = cx;
        int sy = by + bh + 3;
        DrawStar5(tex, sx, sy, 4, starColor);

        FillRect(tex, bx + 3, by + 4, 2, 3, new Color(0.75f, 0.68f, 0.40f, 0.7f));
        FillRect(tex, bx + bw - 5, by + 4, 2, 3, new Color(0.75f, 0.68f, 0.40f, 0.7f));
    }

    private static void DrawFactoryIcon(Texture2D tex)
    {
        int w = tex.width;
        int h = tex.height;
        Color wall = new Color(0.50f, 0.52f, 0.55f, 1f);
        Color wallDark = new Color(0.30f, 0.32f, 0.35f, 1f);
        Color chimney = new Color(0.35f, 0.30f, 0.28f, 1f);
        Color smoke = new Color(0.60f, 0.58f, 0.55f, 0.6f);
        Color doorColor = new Color(0.20f, 0.18f, 0.16f, 1f);
        Color gearColor = new Color(0.72f, 0.62f, 0.30f, 0.9f);
        Color ground = new Color(0.22f, 0.28f, 0.18f, 1f);

        int margin = 3;
        FillRect(tex, margin + 3, margin + 3, w - 2 * margin - 6, 4, ground);

        int bx = margin + 5;
        int bw = w - 2 * bx;
        int by = margin + 5;
        int bh = h - 2 * margin - 18;

        FillRect(tex, bx, by, bw, bh, wall);
        FillRect(tex, bx, by, bw, 2, wallDark);
        FillRect(tex, bx, by + bh - 2, bw, 2, wallDark);

        int chW = 4;
        int chH = 12;
        int ch1x = bx + 6;
        int ch2x = bx + 14;
        int chBase = by + bh;

        FillRect(tex, ch1x, chBase, chW, chH, chimney);
        FillRect(tex, ch2x, chBase, chW - 1, chH + 4, chimney);

        int smokeY1 = chBase + chH;
        DrawSmoke(tex, ch1x + chW / 2, smokeY1, smoke);
        int smokeY2 = chBase + chH + 4;
        DrawSmoke(tex, ch2x + (chW - 1) / 2, smokeY2, smoke);

        int cx = w / 2;
        FillRect(tex, cx - 3, by + 1, 6, 7, doorColor);

        int gx = bx + bw - 10;
        int gy = by + bh / 2;
        DrawGear(tex, gx, gy, 4, gearColor);
    }

    private static void DrawAirfieldIcon(Texture2D tex)
    {
        int w = tex.width;
        int h = tex.height;
        Color runway = new Color(0.32f, 0.34f, 0.38f, 1f);
        Color runwayLine = new Color(0.90f, 0.85f, 0.60f, 0.8f);
        Color hangar = new Color(0.45f, 0.48f, 0.52f, 1f);
        Color hangarDark = new Color(0.30f, 0.32f, 0.36f, 1f);
        Color ground = new Color(0.25f, 0.30f, 0.22f, 1f);
        Color planeColor = new Color(0.60f, 0.65f, 0.72f, 0.9f);

        int margin = 3;

        FillRect(tex, margin + 3, margin + 3, w - 2 * margin - 6, w - 2 * margin - 6, ground);

        int cx = w / 2;
        int rw = 6;
        int ry = margin + 5;
        int rh = h - 2 * margin - 12;
        FillRect(tex, cx - rw / 2, ry, rw, rh, runway);

        for (int i = 0; i < rh; i += 5)
        {
            FillRect(tex, cx, ry + i, 1, 3, runwayLine);
        }

        int hx = margin + 5;
        int hy = margin + 8;
        int hw = 14;
        int hh = 10;
        FillRect(tex, hx, hy, hw, hh, hangar);

        for (int i = 0; i < hw; i++)
        {
            int arcH = 3 - Mathf.Abs(i - hw / 2) * 3 / (hw / 2);
            if (arcH > 0)
            {
                FillRect(tex, hx + i, hy + hh, 1, arcH, hangarDark);
            }
        }

        FillRect(tex, hx + 2, hy + 1, hw - 4, 2, hangarDark);

        int px = w - margin - 16;
        int py = h / 2 + 4;
        FillRect(tex, px + 2, py, 8, 2, planeColor);
        FillRect(tex, px, py - 2, 12, 1, planeColor);
        FillRect(tex, px + 4, py + 2, 1, 3, planeColor);
        FillRect(tex, px + 7, py + 2, 1, 3, planeColor);
    }

    #endregion

    #region Unit Icons

    private static void DrawInfantryIcon(Texture2D tex)
    {
        int w = tex.width;
        int h = tex.height;
        Color skin = new Color(0.82f, 0.72f, 0.58f, 1f);
        Color uniform = new Color(0.35f, 0.42f, 0.32f, 1f);
        Color uniformDark = new Color(0.25f, 0.30f, 0.22f, 1f);
        Color helmet = new Color(0.30f, 0.35f, 0.28f, 1f);
        Color boots = new Color(0.18f, 0.15f, 0.12f, 1f);
        Color weapon = new Color(0.28f, 0.26f, 0.24f, 1f);

        int cx = w / 2;
        int baseY = 8;

        FillCircle(tex, cx, baseY + 38, 4, helmet);
        FillCircle(tex, cx, baseY + 36, 3, skin);

        FillRect(tex, cx - 4, baseY + 16, 8, 16, uniform);
        FillRect(tex, cx - 3, baseY + 18, 6, 12, uniformDark);

        FillRect(tex, cx - 7, baseY + 22, 4, 2, uniform);
        FillRect(tex, cx + 3, baseY + 22, 4, 2, uniform);
        FillRect(tex, cx - 8, baseY + 20, 2, 3, skin);
        FillRect(tex, cx + 6, baseY + 20, 2, 3, skin);

        FillRect(tex, cx - 3, baseY + 5, 3, 12, uniformDark);
        FillRect(tex, cx + 1, baseY + 5, 3, 12, uniformDark);

        FillRect(tex, cx - 4, baseY + 3, 3, 4, boots);
        FillRect(tex, cx + 1, baseY + 3, 3, 4, boots);

        FillRect(tex, cx + 6, baseY + 20, 2, 22, weapon);
        FillRect(tex, cx + 5, baseY + 40, 4, 2, weapon);
    }

    private static void DrawTankIcon(Texture2D tex)
    {
        int w = tex.width;
        int h = tex.height;
        Color hull = new Color(0.40f, 0.44f, 0.38f, 1f);
        Color hullDark = new Color(0.28f, 0.30f, 0.26f, 1f);
        Color turret = new Color(0.36f, 0.40f, 0.34f, 1f);
        Color barrel = new Color(0.24f, 0.26f, 0.22f, 1f);
        Color track = new Color(0.18f, 0.18f, 0.16f, 1f);
        Color trackDetail = new Color(0.25f, 0.24f, 0.22f, 1f);
        Color highlight = new Color(0.55f, 0.58f, 0.50f, 0.6f);

        int cx = w / 2;
        int baseY = 10;

        FillRect(tex, cx - 22, baseY + 4, 44, 7, track);
        for (int i = 0; i < 8; i++)
        {
            FillRect(tex, cx - 20 + i * 5, baseY + 5, 1, 5, trackDetail);
        }

        FillRect(tex, cx - 18, baseY + 10, 36, 14, hull);
        FillRect(tex, cx - 16, baseY + 22, 32, 2, hullDark);
        FillRect(tex, cx - 18, baseY + 10, 36, 2, highlight);

        FillRect(tex, cx - 8, baseY + 24, 16, 10, turret);
        FillRect(tex, cx - 8, baseY + 32, 16, 2, hullDark);

        FillRect(tex, cx - 2, baseY + 34, 4, 16, barrel);
        FillRect(tex, cx - 3, baseY + 48, 6, 2, barrel);

        FillCircle(tex, cx, baseY + 28, 3, hullDark);
        tex.SetPixel(cx, baseY + 28, highlight);
    }

    private static void DrawAircraftIcon(Texture2D tex)
    {
        int w = tex.width;
        int h = tex.height;
        Color body = new Color(0.48f, 0.52f, 0.58f, 1f);
        Color bodyDark = new Color(0.32f, 0.36f, 0.40f, 1f);
        Color wing = new Color(0.42f, 0.46f, 0.52f, 1f);
        Color cockpit = new Color(0.70f, 0.82f, 0.92f, 0.9f);
        Color tailColor = new Color(0.38f, 0.42f, 0.48f, 1f);
        Color exhaust = new Color(0.28f, 0.26f, 0.24f, 0.8f);

        int cx = w / 2;
        int baseY = 7;

        FillRect(tex, cx - 2, baseY + 4, 4, 42, body);
        FillRect(tex, cx - 3, baseY + 12, 6, 30, body);

        int noseY = baseY + 44;
        FillRect(tex, cx - 1, noseY, 2, 5, bodyDark);
        tex.SetPixel(cx, noseY + 5, bodyDark);

        FillRect(tex, cx - 2, baseY + 38, 4, 4, cockpit);

        int wingY = baseY + 22;
        for (int i = 0; i < 16; i++)
        {
            int offset = i + 4;
            int wh = 3 - i / 6;
            if (wh < 1) { wh = 1; }
            FillRect(tex, cx + offset, wingY - wh / 2, 1, wh, wing);
            FillRect(tex, cx - offset - 1, wingY - wh / 2, 1, wh, wing);
        }

        int tailWingY = baseY + 8;
        for (int i = 0; i < 8; i++)
        {
            FillRect(tex, cx + 3 + i, tailWingY, 1, 2, tailColor);
            FillRect(tex, cx - 4 - i, tailWingY, 1, 2, tailColor);
        }

        FillRect(tex, cx - 1, baseY + 4, 2, 5, tailColor);
        FillRect(tex, cx, baseY + 9, 1, 2, bodyDark);

        FillRect(tex, cx - 1, baseY + 2, 2, 3, exhaust);
    }

    #endregion

    #region Special Icons

    private static void DrawResourceIcon(Texture2D tex)
    {
        int w = tex.width;
        int h = tex.height;
        Color bgColor = new Color(0.08f, 0.10f, 0.14f, 0.0f);
        Color goldBright = new Color(0.95f, 0.85f, 0.40f, 1f);
        Color goldMid = new Color(0.80f, 0.68f, 0.28f, 1f);
        Color goldDark = new Color(0.55f, 0.45f, 0.18f, 1f);
        Color goldShadow = new Color(0.35f, 0.28f, 0.10f, 1f);

        FillRect(tex, 0, 0, w, h, bgColor);

        int cx = w / 2;
        int cy = h / 2;

        FillCircle(tex, cx, cy, 11, goldDark);
        FillCircle(tex, cx, cy, 10, goldMid);
        FillCircle(tex, cx, cy + 1, 8, goldBright);
        FillCircle(tex, cx, cy, 8, goldMid);

        FillCircle(tex, cx, cy, 6, goldBright);
        FillCircle(tex, cx, cy - 1, 5, goldMid);

        FillRect(tex, cx - 2, cy - 4, 4, 8, goldDark);
        FillRect(tex, cx - 4, cy - 2, 8, 4, goldDark);
        FillRect(tex, cx - 1, cy - 3, 2, 6, goldBright);
        FillRect(tex, cx - 3, cy - 1, 6, 2, goldBright);

        tex.SetPixel(cx + 3, cy + 3, goldBright);
        tex.SetPixel(cx - 3, cy + 3, goldShadow);
    }

    private static void DrawMoveIcon(Texture2D tex)
    {
        int w = tex.width;
        int h = tex.height;
        Color arrowColor = new Color(0.40f, 0.82f, 0.40f, 1f);
        int cx = w / 2;
        int cy = h / 2;

        FillRect(tex, cx - 2, cy - 10, 4, 20, arrowColor);
        for (int i = 0; i < 7; i++)
        {
            FillRect(tex, cx - i, cy + 10 + i, 1, 1, arrowColor);
            FillRect(tex, cx + i, cy + 10 + i, 1, 1, arrowColor);
            FillRect(tex, cx - i - 1, cy + 10 + i, 1, 1, arrowColor);
            FillRect(tex, cx + i + 1, cy + 10 + i, 1, 1, arrowColor);
        }
    }

    private static void DrawAttackIcon(Texture2D tex)
    {
        int w = tex.width;
        int h = tex.height;
        Color swordColor = new Color(0.85f, 0.35f, 0.28f, 1f);
        Color bladeColor = new Color(0.78f, 0.80f, 0.82f, 1f);
        int cx = w / 2;
        int cy = h / 2;

        for (int i = -12; i <= 12; i++)
        {
            int x = cx + i;
            int y = cy + i;
            if (x >= 3 && x < w - 3 && y >= 3 && y < h - 3)
            {
                tex.SetPixel(x, y, bladeColor);
                tex.SetPixel(x + 1, y, bladeColor);
            }
        }

        for (int i = -12; i <= 12; i++)
        {
            int x = cx + i;
            int y = cy - i;
            if (x >= 3 && x < w - 3 && y >= 3 && y < h - 3)
            {
                tex.SetPixel(x, y, swordColor);
                tex.SetPixel(x + 1, y, swordColor);
            }
        }

        FillCircle(tex, cx, cy, 3, new Color(0.90f, 0.80f, 0.35f, 1f));
    }

    private static void DrawStopIcon(Texture2D tex)
    {
        int w = tex.width;
        int h = tex.height;
        Color stopColor = new Color(0.88f, 0.30f, 0.25f, 1f);
        int cx = w / 2;
        int cy = h / 2;

        FillCircle(tex, cx, cy, 12, stopColor);
        FillCircle(tex, cx, cy, 9, IconBgDark);
        FillRect(tex, cx - 8, cy - 1, 16, 3, stopColor);
    }

    private static void DrawBuildIcon(Texture2D tex)
    {
        int w = tex.width;
        int h = tex.height;
        Color hammerHead = new Color(0.55f, 0.58f, 0.62f, 1f);
        Color hammerHandle = new Color(0.55f, 0.40f, 0.22f, 1f);
        int cx = w / 2;
        int cy = h / 2;

        for (int i = -10; i <= 10; i++)
        {
            int x = cx + i;
            int y = cy + i;
            if (x >= 3 && x < w - 3 && y >= 3 && y < h - 3)
            {
                tex.SetPixel(x, y, hammerHandle);
                tex.SetPixel(x + 1, y, hammerHandle);
            }
        }

        FillRect(tex, cx + 4, cy + 6, 10, 6, hammerHead);
        FillRect(tex, cx + 5, cy + 12, 8, 2, hammerHead);
    }

    #endregion

    #region Drawing Primitives

    private static void FillRect(Texture2D tex, int x, int y, int rw, int rh, Color color)
    {
        int texW = tex.width;
        int texH = tex.height;
        for (int py = y; py < y + rh; py++)
        {
            for (int px = x; px < x + rw; px++)
            {
                if (px >= 0 && px < texW && py >= 0 && py < texH)
                {
                    if (color.a < 1f)
                    {
                        Color existing = tex.GetPixel(px, py);
                        tex.SetPixel(px, py, Color.Lerp(existing, color, color.a));
                    }
                    else
                    {
                        tex.SetPixel(px, py, color);
                    }
                }
            }
        }
    }

    private static void FillCircle(Texture2D tex, int cx, int cy, int radius, Color color)
    {
        int texW = tex.width;
        int texH = tex.height;
        int rSq = radius * radius;
        for (int py = cy - radius; py <= cy + radius; py++)
        {
            for (int px = cx - radius; px <= cx + radius; px++)
            {
                int dx = px - cx;
                int dy = py - cy;
                if (dx * dx + dy * dy <= rSq && px >= 0 && px < texW && py >= 0 && py < texH)
                {
                    if (color.a < 1f)
                    {
                        Color existing = tex.GetPixel(px, py);
                        tex.SetPixel(px, py, Color.Lerp(existing, color, color.a));
                    }
                    else
                    {
                        tex.SetPixel(px, py, color);
                    }
                }
            }
        }
    }

    private static void DrawRectBorder(Texture2D tex, int x, int y, int rw, int rh, Color color)
    {
        for (int i = x; i < x + rw; i++)
        {
            SafeSetPixel(tex, i, y, color);
            SafeSetPixel(tex, i, y + rh - 1, color);
        }
        for (int i = y; i < y + rh; i++)
        {
            SafeSetPixel(tex, x, i, color);
            SafeSetPixel(tex, x + rw - 1, i, color);
        }
    }

    private static void DrawVerticalLine(Texture2D tex, int x, int yStart, int yEnd, Color color)
    {
        for (int y = yStart; y <= yEnd; y++)
        {
            SafeSetPixel(tex, x, y, color);
        }
    }

    private static void SafeSetPixel(Texture2D tex, int x, int y, Color color)
    {
        if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
        {
            tex.SetPixel(x, y, color);
        }
    }

    private static void DrawStar5(Texture2D tex, int cx, int cy, int size, Color color)
    {
        FillRect(tex, cx - 1, cy + size - 1, 2, 1, color);
        FillRect(tex, cx - size, cy, 2 * size, 1, color);
        FillRect(tex, cx - size + 1, cy - 1, 2 * size - 2, 1, color);
        FillRect(tex, cx - 2, cy + 1, 4, 1, color);
        FillRect(tex, cx - 3, cy - 2, 2, 1, color);
        FillRect(tex, cx + 1, cy - 2, 2, 1, color);
        FillRect(tex, cx - 1, cy + 2, 2, 1, color);
    }

    private static void DrawSmoke(Texture2D tex, int cx, int baseY, Color color)
    {
        Color c1 = new Color(color.r, color.g, color.b, color.a * 0.7f);
        Color c2 = new Color(color.r, color.g, color.b, color.a * 0.4f);
        Color c3 = new Color(color.r, color.g, color.b, color.a * 0.2f);

        FillCircle(tex, cx, baseY + 2, 2, c1);
        FillCircle(tex, cx + 1, baseY + 5, 3, c2);
        FillCircle(tex, cx - 1, baseY + 8, 2, c3);
    }

    private static void DrawGear(Texture2D tex, int cx, int cy, int size, Color color)
    {
        FillCircle(tex, cx, cy, size, color);
        FillCircle(tex, cx, cy, size - 2, IconBgGrad);

        Color toothColor = new Color(color.r, color.g, color.b, 0.8f);
        SafeSetPixel(tex, cx, cy + size + 1, toothColor);
        SafeSetPixel(tex, cx, cy - size - 1, toothColor);
        SafeSetPixel(tex, cx + size + 1, cy, toothColor);
        SafeSetPixel(tex, cx - size - 1, cy, toothColor);

        FillCircle(tex, cx, cy, 1, color);
    }

    #endregion
}

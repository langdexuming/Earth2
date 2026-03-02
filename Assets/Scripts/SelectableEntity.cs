using UnityEngine;

public abstract class SelectableEntity : MonoBehaviour
{
    [Header("Team And Health")]
    public int team = 0;
    public int maxHealth = 100;
    public int health = 100;

    [Header("Selection")]
    public bool isSelected = false;
    [SerializeField] private Color selectionColor = new Color(0.35f, 0.95f, 0.35f, 1f);
    [SerializeField] [Range(0f, 1f)] private float teamTintStrength = 0.7f;

    private Renderer[] cachedRenderers;
    private Color[] baseColors;

    protected virtual void Awake()
    {
        health = Mathf.Clamp(health, 1, maxHealth);
        CacheRenderers();
    }

    protected virtual void Start()
    {
        CacheRenderers();
        UpdateSelectionVisual();
    }

    private void CacheRenderers()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>();
        baseColors = new Color[cachedRenderers.Length];

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Renderer targetRenderer = cachedRenderers[i];
            if (targetRenderer != null && targetRenderer.material != null)
            {
                baseColors[i] = targetRenderer.material.color;
            }
        }
    }

    protected void SetTeamTint(Color tint)
    {
        CacheRenderers();
        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Renderer targetRenderer = cachedRenderers[i];
            if (targetRenderer == null || targetRenderer.material == null)
            {
                continue;
            }

            Color sourceColor = i < baseColors.Length ? baseColors[i] : Color.white;
            Color tintedColor = Color.Lerp(sourceColor, tint, teamTintStrength);
            targetRenderer.material.color = tintedColor;
            baseColors[i] = tintedColor;
        }

        UpdateSelectionVisual();
    }

    public virtual void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateSelectionVisual();
    }

    public virtual void TakeDamage(int damage)
    {
        health -= Mathf.Max(1, damage);
        if (health <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        if (SelectionManager.Instance != null)
        {
            SelectionManager.Instance.DeselectEntity(this);
        }

        Destroy(gameObject);
    }

    private void UpdateSelectionVisual()
    {
        if (cachedRenderers == null || baseColors == null)
        {
            return;
        }

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Renderer targetRenderer = cachedRenderers[i];
            if (targetRenderer == null || targetRenderer.material == null)
            {
                continue;
            }

            Color baseColor = i < baseColors.Length ? baseColors[i] : Color.white;
            targetRenderer.material.color = isSelected
                ? Color.Lerp(baseColor, selectionColor, 0.5f)
                : baseColor;
        }
    }
}

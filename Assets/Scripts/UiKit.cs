using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Shared visual language for every Starfall screen: palette, procedural sprites and tiny motion helpers.
public static class UiKit
{
    public static readonly Color Ink = Rgb(0x04070D);
    public static readonly Color Deep = Rgb(0x070E18);
    public static readonly Color Panel = Rgb(0x0C1A28);
    public static readonly Color PanelSoft = Rgb(0x142636);
    public static readonly Color PanelRaise = Rgb(0x17293B);
    public static readonly Color Line = Rgb(0x22404F);
    public static readonly Color Cyan = Rgb(0x49E9D8);
    public static readonly Color CyanDeep = Rgb(0x1D8E8C);
    public static readonly Color Gold = Rgb(0xFFC55C);
    public static readonly Color Pink = Rgb(0xFF5688);
    public static readonly Color Violet = Rgb(0xA98CFF);
    public static readonly Color TextMain = Rgb(0xEAF5FB);
    public static readonly Color TextDim = Rgb(0xA3B8C8);
    public static readonly Color TextMute = Rgb(0x647D92);

    public static Color Rgb(int hex, float alpha = 1f)
    {
        return new Color(((hex >> 16) & 0xFF) / 255f, ((hex >> 8) & 0xFF) / 255f, (hex & 0xFF) / 255f, alpha);
    }

    public static Color Alpha(Color color, float alpha) { color.a = alpha; return color; }
    public static Color Shade(Color color, float amount) => Color.Lerp(color, Color.black, amount);
    public static Color Light(Color color, float amount) => Color.Lerp(color, Color.white, amount);
    public static Color Mix(Color a, Color b, float t) => Color.Lerp(a, b, t);

    // Letter tracking for small latin kickers: "STARFALL" -> "S T A R F A L L".
    public static string Track(string text)
    {
        var builder = new System.Text.StringBuilder(text.Length * 2);
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == ' ') { builder.Append("   "); continue; }
            builder.Append(text[i]);
            if (i < text.Length - 1 && text[i + 1] != ' ') builder.Append('\u2009');
        }
        return builder.ToString();
    }

    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    private static Sprite Get(string key, System.Func<Sprite> create)
    {
        if (cache.TryGetValue(key, out Sprite sprite) && sprite != null) return sprite;
        sprite = create();
        cache[key] = sprite;
        return sprite;
    }

    private static Sprite Build(string name, int width, int height, float border, System.Func<float, float, float> alpha)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = name,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontSave
        };
        var pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            float v = (y + .5f) / height;
            for (int x = 0; x < width; x++)
            {
                float a = Mathf.Clamp01(alpha((x + .5f) / width, v));
                pixels[y * width + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        sprite.name = name;
        sprite.hideFlags = HideFlags.DontSave;
        return sprite;
    }

    // --- distance helpers -------------------------------------------------
    private static float ChamferAlpha(float u, float v, int width, int height, float cut, float stroke)
    {
        float ax = Mathf.Abs(u - .5f) * width, ay = Mathf.Abs(v - .5f) * height;
        float hx = width * .5f, hy = height * .5f;
        float d = Mathf.Min(Mathf.Min(hx - ax, hy - ay), (hx + hy - cut - ax - ay) * .70710678f);
        return stroke <= 0f ? Mathf.Clamp01(d + .5f) : Mathf.Clamp01(1f - Mathf.Abs(d - stroke * .5f) / (stroke * .5f + .55f));
    }

    private static float RoundedAlpha(float u, float v, int width, int height, float radius, float stroke)
    {
        float ax = Mathf.Abs(u - .5f) * width, ay = Mathf.Abs(v - .5f) * height;
        float qx = ax - (width * .5f - radius), qy = ay - (height * .5f - radius);
        float d = Mathf.Min(Mathf.Max(qx, qy), 0f) + Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f)) - radius;
        return stroke <= 0f ? Mathf.Clamp01(-d + .5f) : Mathf.Clamp01(1f - Mathf.Abs(d + stroke * .5f) / (stroke * .5f + .55f));
    }

    private static float HexAlpha(float u, float v, int width, int height, float stroke)
    {
        float x = (u - .5f) * width, y = (v - .5f) * height;
        float radius = height * .48f;
        float d = float.MinValue;
        for (int i = 0; i < 6; i++)
        {
            float angle = Mathf.Deg2Rad * (60f * i + 30f);
            d = Mathf.Max(d, x * Mathf.Cos(angle) + y * Mathf.Sin(angle));
        }
        d = radius * .8660254f - d;
        return stroke <= 0f ? Mathf.Clamp01(d + .5f) : Mathf.Clamp01(1f - Mathf.Abs(d - stroke * .5f) / (stroke * .5f + .55f));
    }

    private static float SegmentAlpha(float px, float py, float ax, float ay, float bx, float by, float thickness)
    {
        float vx = bx - ax, vy = by - ay;
        float wx = px - ax, wy = py - ay;
        float t = Mathf.Clamp01((wx * vx + wy * vy) / Mathf.Max(.0001f, vx * vx + vy * vy));
        float dx = wx - vx * t, dy = wy - vy * t;
        return Mathf.Clamp01((thickness - Mathf.Sqrt(dx * dx + dy * dy)) / 1.2f);
    }

    // --- sprites ----------------------------------------------------------
    public static Sprite Solid => Get("solid", () => Build("Solid", 8, 8, 0f, (u, v) => 1f));

    public static Sprite PanelFill => Get("panel-fill", () => Build("Panel", 72, 72, 20f, (u, v) => ChamferAlpha(u, v, 72, 72, 17f, 0f)));
    public static Sprite PanelEdge => Get("panel-edge", () => Build("Panel edge", 72, 72, 20f, (u, v) => ChamferAlpha(u, v, 72, 72, 17f, 2f)));

    public static Sprite Soft => Get("soft", () => Build("Soft", 64, 64, 18f, (u, v) => RoundedAlpha(u, v, 64, 64, 14f, 0f)));
    public static Sprite SoftEdge => Get("soft-edge", () => Build("Soft edge", 64, 64, 18f, (u, v) => RoundedAlpha(u, v, 64, 64, 14f, 2f)));

    public static Sprite SoftSmall => Get("soft-small", () => Build("Soft small", 24, 24, 9f, (u, v) => RoundedAlpha(u, v, 24, 24, 7f, 0f)));
    public static Sprite SoftSmallEdge => Get("soft-small-edge", () => Build("Soft small edge", 24, 24, 9f, (u, v) => RoundedAlpha(u, v, 24, 24, 7f, 1.6f)));

    public static Sprite Stroke => Get("stroke", () => Build("Stroke", 16, 16, 5f, (u, v) =>
    {
        float x = u * 16f, y = v * 16f;
        float d = Mathf.Min(Mathf.Min(x, 16f - x), Mathf.Min(y, 16f - y));
        return Mathf.Clamp01((1.2f - d) / 1.2f);
    }));

    public static Sprite GradientUp => Get("grad-up", () => Build("Gradient up", 4, 128, 0f, (u, v) => v * v));
    public static Sprite GradientDown => Get("grad-down", () => Build("Gradient down", 4, 128, 0f, (u, v) => (1f - v) * (1f - v)));
    public static Sprite GradientRight => Get("grad-right", () => Build("Gradient right", 128, 4, 0f, (u, v) => u * u));
    public static Sprite GradientLeft => Get("grad-left", () => Build("Gradient left", 128, 4, 0f, (u, v) => (1f - u) * (1f - u)));

    public static Sprite Glow => Get("glow", () => Build("Glow", 128, 128, 0f, (u, v) =>
    {
        float x = (u - .5f) * 2f, y = (v - .5f) * 2f;
        return Mathf.Pow(Mathf.Clamp01(1f - Mathf.Sqrt(x * x + y * y)), 2.5f);
    }));

    public static Sprite Vignette => Get("vignette", () => Build("Vignette", 256, 256, 0f, (u, v) =>
    {
        float x = (u - .5f) * 2f, y = (v - .5f) * 2f;
        float r = Mathf.Sqrt(x * x + y * y) / 1.4142136f;
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.3f, 1f, r));
    }));

    public static Sprite Scanlines => Get("scanlines", () => Build("Scanlines", 512, 512, 0f, (u, v) =>
    {
        int y = Mathf.Clamp(Mathf.FloorToInt(v * 512f), 0, 511);
        return y % 4 == 0 ? 1f : 0f;
    }));

    public static Sprite Grid => Get("grid", () => Build("Grid", 64, 64, 0f, (u, v) =>
    {
        int x = Mathf.Clamp(Mathf.FloorToInt(u * 64f), 0, 63), y = Mathf.Clamp(Mathf.FloorToInt(v * 64f), 0, 63);
        return x == 0 || y == 0 ? 1f : 0f;
    }));

    public static Sprite Dot => Get("dot", () => Build("Dot", 16, 16, 0f, (u, v) =>
    {
        float x = (u - .5f) * 2f, y = (v - .5f) * 2f;
        return Mathf.Clamp01((1f - (x * x + y * y)) * 1.8f);
    }));

    public static Sprite Diamond => Get("diamond", () => Build("Diamond", 32, 32, 0f, (u, v) =>
    {
        float x = Mathf.Abs((u - .5f) * 2f), y = Mathf.Abs((v - .5f) * 2f);
        float f = Mathf.Pow(x, .55f) + Mathf.Pow(y, .55f);
        return Mathf.Clamp01((1.12f - f) * 7f);
    }));

    public static Sprite Chevron => Get("chevron", () => Build("Chevron", 24, 24, 0f, (u, v) =>
    {
        float x = u * 24f, y = v * 24f;
        return Mathf.Max(SegmentAlpha(x, y, 8f, 5f, 16f, 12f, 2.4f), SegmentAlpha(x, y, 16f, 12f, 8f, 19f, 2.4f));
    }));

    public static Sprite Bracket => Get("bracket", () => Build("Bracket", 24, 24, 0f, (u, v) =>
    {
        float x = u * 24f, y = v * 24f;
        return Mathf.Max(SegmentAlpha(x, y, 1f, 22f, 20f, 22f, 1.6f), SegmentAlpha(x, y, 1f, 22f, 1f, 4f, 1.6f));
    }));

    public static Sprite Ring => Get("ring", () => Build("Ring", 128, 128, 0f, (u, v) =>
    {
        float x = (u - .5f) * 2f, y = (v - .5f) * 2f;
        float r = Mathf.Sqrt(x * x + y * y);
        return Mathf.Clamp01(1f - Mathf.Abs(r - .82f) / .022f);
    }));

    public static Sprite HexFill => Get("hex-fill", () => Build("Hex", 128, 128, 0f, (u, v) => HexAlpha(u, v, 128, 128, 0f)));
    public static Sprite HexEdge => Get("hex-edge", () => Build("Hex edge", 128, 128, 0f, (u, v) => HexAlpha(u, v, 128, 128, 2.4f)));
}

// Hover feedback: scales the card, brightens edge/fill/label. Runs on unscaled time so it works while paused.
public sealed class UiHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public RectTransform body;
    public Graphic edge;
    public Color edgeIdle, edgeHot;
    public Graphic fill;
    public Color fillIdle, fillHot;
    public Text label;
    public Color labelIdle, labelHot;
    public float hoverScale = 1.02f, speed = 13f;
    private bool hovered, pressed;
    private float blend;

    private void Awake() { if (body == null) body = (RectTransform)transform; }
    public void OnPointerEnter(PointerEventData eventData) { hovered = true; }
    public void OnPointerExit(PointerEventData eventData) { hovered = false; pressed = false; }
    public void OnPointerDown(PointerEventData eventData) { pressed = true; }
    public void OnPointerUp(PointerEventData eventData) { pressed = false; }

    private void Update()
    {
        blend = Mathf.MoveTowards(blend, hovered ? 1f : 0f, Time.unscaledDeltaTime * speed);
        if (body != null)
        {
            float scale = 1f + (hoverScale - 1f) * blend - (pressed ? .012f : 0f);
            body.localScale = new Vector3(scale, scale, 1f);
        }
        if (edge != null) edge.color = Color.Lerp(edgeIdle, edgeHot, blend);
        if (fill != null) fill.color = Color.Lerp(fillIdle, fillHot, blend);
        if (label != null) label.color = Color.Lerp(labelIdle, labelHot, blend);
    }
}

// Quick fade/scale-in for whole screens.
public sealed class UiFade : MonoBehaviour
{
    public float duration = .22f;
    private CanvasGroup group;
    private float time;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
    }

    private void Update()
    {
        time += Time.unscaledDeltaTime;
        float p = Mathf.Clamp01(time / Mathf.Max(.01f, duration));
        if (group != null) group.alpha = Mathf.SmoothStep(0f, 1f, p);
        if (p >= 1f) Destroy(this);
    }
}

public sealed class UiSpin : MonoBehaviour
{
    public float degreesPerSecond = 12f;
    private void Update() { transform.Rotate(0f, 0f, degreesPerSecond * Time.unscaledDeltaTime); }
}

public sealed class UiPulse : MonoBehaviour
{
    public Graphic target;
    public float min = .2f, max = .55f, speed = 1.3f;

    private void Start() { if (target == null) target = GetComponent<Graphic>(); }

    private void Update()
    {
        if (target == null) return;
        Color color = target.color;
        color.a = Mathf.Lerp(min, max, (Mathf.Sin(Time.unscaledTime * speed) + 1f) * .5f);
        target.color = color;
    }
}

public sealed class UiTwinkle : MonoBehaviour
{
    public float speed = .6f, min = .25f, max = 1f, phase;
    private Graphic graphic;
    private float baseAlpha;

    private void Awake()
    {
        graphic = GetComponent<Graphic>();
        if (graphic != null) baseAlpha = graphic.color.a;
    }

    private void Update()
    {
        if (graphic == null) return;
        Color color = graphic.color;
        color.a = baseAlpha * Mathf.Lerp(min, max, (Mathf.Sin(Time.unscaledTime * speed + phase) + 1f) * .5f);
        graphic.color = color;
    }
}

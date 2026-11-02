using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class Effects
{
    private static Sprite white;
    private static Material lineMaterial;

    public static SpriteRenderer Sprite(string name, Transform parent, Sprite sprite, Color color, Vector3 localPosition, Vector3 scale, int order)
    {
        var obj = new GameObject(name);
        if (parent != null) obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPosition; obj.transform.localScale = scale;
        var renderer = obj.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color; renderer.sortingOrder = order; return renderer;
    }

    public static void Burst(Vector3 position, Color color, int count, float scale)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = Random.value * Mathf.PI * 2, speed = Random.Range(1.2f, 4.5f);
            var obj = TakeFromPool(SparkPool);
            if (obj == null)
            {
                obj = new GameObject("Spark");
                obj.AddComponent<SpriteRenderer>();
                obj.AddComponent<ParticleFade>();
            }
            else obj.SetActive(true);
            obj.transform.SetPositionAndRotation(position, Quaternion.identity);
            obj.transform.localScale = Vector3.one * Random.Range(.045f, scale);
            var renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = GetWhite(); renderer.color = color; renderer.sortingOrder = 24;
            obj.GetComponent<ParticleFade>().Setup(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed, Random.Range(.22f, .48f));
        }
    }

    private static Canvas cachedCanvas;
    private static Font cachedLabelFont;
    private static readonly Stack<GameObject> SparkPool = new Stack<GameObject>();
    private static readonly Stack<GameObject> LabelPool = new Stack<GameObject>();

    private static Canvas UiCanvas()
    {
        if (cachedCanvas != null) return cachedCanvas;
        cachedCanvas = Object.FindFirstObjectByType<Canvas>();
        return cachedCanvas;
    }

    private static Font LabelFont()
    {
        if (cachedLabelFont == null) cachedLabelFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return cachedLabelFont;
    }

    private static GameObject TakeFromPool(Stack<GameObject> pool)
    {
        while (pool.Count > 0)
        {
            var item = pool.Pop();
            if (item != null) return item;
        }
        return null;
    }

    private static void ReturnToPool(Stack<GameObject> pool, GameObject item, int cap)
    {
        if (item == null) return;
        item.SetActive(false);
        if (pool.Count < cap) pool.Push(item);
        else Object.Destroy(item);
    }

    public static void ReleaseSpark(GameObject item) => ReturnToPool(SparkPool, item, 256);
    public static void ReleaseLabel(GameObject item) => ReturnToPool(LabelPool, item, 64);

    public static void FloatingNumber(Vector3 position, string text, Color tint, bool heavy)
    {
        var canvas = UiCanvas(); if (canvas == null) return;
        var obj = TakeFromPool(LabelPool);
        if (obj == null)
        {
            obj = new GameObject("Damage number", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            obj.AddComponent<FloatingLabel>();
        }
        else obj.SetActive(true);
        obj.transform.SetParent(canvas.transform, false); obj.transform.SetAsLastSibling();
        var rect = obj.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(160, 36); rect.localScale = Vector3.one;
        var label = obj.GetComponent<Text>(); label.font = LabelFont(); label.text = text; label.fontSize = heavy ? 20 : 15; label.alignment = TextAnchor.MiddleCenter; label.fontStyle = heavy ? FontStyle.Bold : FontStyle.Normal; label.color = tint; label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Overflow; label.verticalOverflow = VerticalWrapMode.Overflow;
        var shadow = obj.GetComponent<Shadow>(); if (shadow == null) shadow = obj.AddComponent<Shadow>(); shadow.effectColor = new Color(0, 0, 0, .7f); shadow.effectDistance = new Vector2(1f, -1f);
        obj.GetComponent<FloatingLabel>().Setup(position);
    }

    public static void Ring(Vector3 position, Color tint, float size, float duration)
    {
        var obj = new GameObject("Pulse ring"); obj.transform.position = position; obj.transform.localScale = Vector3.one * .18f;
        var renderer = obj.AddComponent<SpriteRenderer>(); renderer.sprite = GetRing(); renderer.color = new Color(tint.r, tint.g, tint.b, .52f); renderer.sortingOrder = 22;
        obj.AddComponent<RingFade>().Setup(size, duration);
    }

    public static void BossDefeat(Vector3 position, int stage)
    {
        Color primary = stage <= 1 ? GameRules.Pink : stage == 2 ? GameRules.Gold : GameRules.Violet;
        Burst(position, primary, 72 + stage * 18, .18f);
        Burst(position, GameRules.Cyan, 34 + stage * 10, .12f);
        Ring(position, primary, 4.8f + stage * .65f, .9f);
        Ring(position, Color.white, 2.2f + stage * .35f, .42f);
        FloatingNumber(position + Vector3.up * 1.2f, "BOSS DOWN  /  WARDEN " + stage.ToString("00"), GameRules.Gold, true);
        SoundBank.Play(SoundCue.BossDefeat, .65f);
        if (GameBootstrap.Game != null) GameBootstrap.Game.ShakeCamera(.34f + stage * .08f, .58f);
    }

    public static void PlayerDeath(Vector3 position, Sprite sprite, Vector3 scale, Color accent)
    {
        Burst(position, accent, 42, .22f);
        Burst(position, GameRules.Pink, 22, .14f);
        Ring(position, accent, 3.6f, .7f);
        Ring(position, Color.white, 1.5f, .32f);
        if (sprite != null)
        {
            var ghost = Sprite("Fallen pilot", null, sprite, new Color(accent.r, accent.g, accent.b, .82f), position + Vector3.up * .08f, scale, 25);
            ghost.transform.rotation = Quaternion.identity;
            ghost.gameObject.AddComponent<ParticleFade>().Setup(new Vector2(0, 1.15f), .7f);
        }
        SoundBank.Play(SoundCue.BossDefeat, .32f);
    }

    public static void WeaponEvolution(Vector3 position, Color tint)
    {
        Burst(position, tint, 30, .2f);
        Ring(position, tint, 2.4f, .55f);
        Ring(position, Color.white, 1.15f, .3f);
        FloatingNumber(position + Vector3.up * .8f, "WEAPON EVOLVED", tint, true);
        SoundBank.Play(SoundCue.Upgrade, .42f);
        if (GameBootstrap.Game != null) GameBootstrap.Game.ShakeCamera(.12f, .22f);
    }

    public static void Arc(Vector3 start, Vector3 end, Color tint)
    {
        var obj = new GameObject("Lightning arc"); var line = obj.AddComponent<LineRenderer>(); line.useWorldSpace = true; line.positionCount = 9; line.startWidth = .085f; line.endWidth = .025f; line.numCapVertices = 3; line.material = GetLineMaterial(); line.startColor = tint; line.endColor = new Color(tint.r, tint.g, tint.b, .3f); line.sortingOrder = 30;
        Vector2 delta = end - start; Vector2 normal = delta.sqrMagnitude < .01f ? Vector2.up : new Vector2(-delta.y, delta.x).normalized;
        for (int i = 0; i < 9; i++) line.SetPosition(i, Vector3.Lerp(start, end, i / 8f) + (Vector3)normal * (i == 0 || i == 8 ? 0 : Random.Range(-.28f, .28f)));
        obj.AddComponent<LineFade>().Setup(.16f);
    }

    // Evolved weapons use their own visual language. These helpers deliberately
    // avoid the ordinary Burst/Ring/Arc effects so an evolved attack reads as a
    // new weapon rather than a recoloured copy of its base form.
    public static void EvolvedPulseVolley(Vector3 origin, Vector2 direction)
    {
        Color core = new Color(.55f, 1f, 1f, .95f);
        for (int i = -1; i <= 1; i++)
        {
            Vector2 ray = Quaternion.Euler(0, 0, i * 16f) * direction;
            CreateBeam(origin + (Vector3)ray * .24f, origin + (Vector3)ray * 1.15f, core, .11f, .06f, .13f);
            CreateGlyph(origin + (Vector3)ray * .42f, core, .12f, .09f);
        }
        CreateGlyph(origin + (Vector3)direction * .74f, Color.white, .18f, .14f);
    }

    public static void EvolvedScatterVolley(Vector3 origin, Vector2 direction)
    {
        Color ember = new Color(1f, .56f, .16f, .95f);
        Vector2 side = new Vector2(-direction.y, direction.x);
        CreateBeam(origin + (Vector3)side * .18f, origin + (Vector3)(direction * .82f + side * .42f), ember, .09f, .025f, .18f);
        CreateBeam(origin - (Vector3)side * .18f, origin + (Vector3)(direction * .82f - side * .42f), new Color(1f, .9f, .3f, .9f), .09f, .025f, .18f);
        for (int i = 0; i < 5; i++)
        {
            Vector2 ray = Quaternion.Euler(0, 0, Random.Range(-24f, 24f)) * direction;
            CreateGlyph(origin + (Vector3)ray * Random.Range(.24f, .62f), ember, Random.Range(.06f, .12f), .2f);
        }
    }

    public static Transform CreateEvolvedBlade(Vector3 position, float scale)
    {
        var root = new GameObject("Evolved void blade").transform;
        root.position = position;
        var blade = Sprite("Void blade core", root, GameBootstrap.Game.Shape("void_blade"), new Color(.86f, .68f, 1f, .98f), Vector3.zero, Vector3.one * scale, 13);
        Sprite("Void blade edge", root, GameBootstrap.Game.Shape("void_blade"), new Color(.35f, .9f, 1f, .42f), new Vector3(0, 0, -.01f), Vector3.one * (scale * 1.26f), 12);
        root.gameObject.AddComponent<BladeTrail>().Setup(blade, new Color(.7f, .4f, 1f, .7f));
        return root;
    }

    public static void EvolvedBladeDispel(Vector3 position)
    {
        Color violet = new Color(.82f, .5f, 1f, .9f);
        for (int i = 0; i < 6; i++)
        {
            float angle = i * 60f * Mathf.Deg2Rad;
            Vector2 ray = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            CreateBeam(position + (Vector3)ray * .12f, position + (Vector3)ray * .62f, violet, .065f, .015f, .22f);
        }
        CreateGlyph(position, Color.white, .19f, .2f);
    }

    public static void EvolvedBladeHit(Vector3 position, Vector2 direction)
    {
        Vector2 side = new Vector2(-direction.y, direction.x);
        Vector3 center = position + (Vector3)direction * .2f;
        CreateBeam(center - (Vector3)side * .34f, center + (Vector3)side * .34f, new Color(.9f, .66f, 1f, .94f), .09f, .015f, .18f);
        CreateBeam(center - (Vector3)side * .24f + (Vector3)direction * .1f, center + (Vector3)side * .24f - (Vector3)direction * .1f, new Color(.4f, .92f, 1f, .8f), .045f, .008f, .14f);
        CreateGlyph(center, Color.white, .12f, .12f);
    }

    public static void EvolvedArc(Vector3 start, Vector3 end, int branch)
    {
        Color electric = branch % 2 == 0 ? new Color(.35f, 1f, .94f, .96f) : new Color(.7f, .47f, 1f, .96f);
        Vector2 delta = end - start;
        Vector2 normal = delta.sqrMagnitude < .01f ? Vector2.up : new Vector2(-delta.y, delta.x).normalized;
        CreateBeam(start, end, electric, .13f, .035f, .2f, 11, normal, .42f);
        CreateBeam(start + (Vector3)normal * .04f, end - (Vector3)normal * .04f, Color.white, .035f, .012f, .16f, 7, normal, -.24f);
        CreateGlyph(end, electric, .13f, .16f);
    }

    public static void EvolvedCryoLaunch(Vector3 origin, Vector2 direction)
    {
        Color ice = new Color(.52f, .94f, 1f, .95f);
        Vector2 side = new Vector2(-direction.y, direction.x);
        CreateBeam(origin - (Vector3)side * .24f, origin + (Vector3)direction * .78f - (Vector3)side * .08f, ice, .07f, .025f, .24f);
        CreateBeam(origin + (Vector3)side * .24f, origin + (Vector3)direction * .78f + (Vector3)side * .08f, Color.white, .07f, .025f, .24f);
        for (int i = 0; i < 4; i++)
        {
            Vector2 ray = Quaternion.Euler(0, 0, (i - 1.5f) * 32f) * direction;
            CreateGlyph(origin + (Vector3)ray * Random.Range(.25f, .65f), ice, Random.Range(.055f, .105f), .26f);
        }
    }

    public static void EvolvedCryoSlow(Vector3 position)
    {
        Color ice = new Color(.52f, .94f, 1f, .8f);
        for (int i = 0; i < 4; i++)
        {
            float angle = (45f + i * 90f) * Mathf.Deg2Rad;
            Vector2 ray = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            CreateBeam(position + (Vector3)ray * .12f, position + (Vector3)ray * .43f, ice, .055f, .008f, .34f);
            CreateGlyph(position + (Vector3)ray * .3f, Color.white, .065f, .32f);
        }
    }

    public static void EvolvedCryoTick(Vector3 position)
    {
        for (int i = 0; i < 3; i++)
        {
            Vector2 shard = Random.insideUnitCircle.normalized * Random.Range(.12f, .3f);
            CreateGlyph(position + (Vector3)shard, new Color(.6f, .96f, 1f, .85f), Random.Range(.045f, .075f), .22f);
        }
    }

    public static void EvolvedProjectileImpact(Vector3 position, string projectileShape)
    {
        if (projectileShape == "pulse_nova")
        {
            for (int i = 0; i < 6; i++)
            {
                Vector2 ray = Quaternion.Euler(0, 0, i * 60f) * Vector2.right;
                CreateBeam(position, position + (Vector3)ray * .48f, new Color(.4f, 1f, 1f, .82f), .06f, .008f, .18f);
            }
            CreateGlyph(position, Color.white, .16f, .18f);
        }
        else if (projectileShape == "ember_bolt")
        {
            for (int i = 0; i < 7; i++) CreateGlyph(position, new Color(1f, .5f + Random.value * .35f, .12f, .9f), Random.Range(.05f, .11f), .2f);
        }
        else if (projectileShape == "cryo_lance_evolved")
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 ray = Quaternion.Euler(0, 0, i * 45f) * Vector2.right;
                CreateBeam(position, position + (Vector3)ray * .58f, new Color(.48f, .9f, 1f, .86f), .065f, .008f, .25f);
            }
            CreateGlyph(position, Color.white, .2f, .22f);
        }
    }

    private static void CreateBeam(Vector3 start, Vector3 end, Color color, float startWidth, float endWidth, float life, int points = 2, Vector2 normal = default(Vector2), float jitter = 0f)
    {
        var obj = new GameObject("Evolved weapon beam"); var line = obj.AddComponent<LineRenderer>(); line.useWorldSpace = true; line.positionCount = Mathf.Max(2, points); line.startWidth = startWidth; line.endWidth = endWidth; line.numCapVertices = 3; line.material = GetLineMaterial(); line.startColor = color; line.endColor = new Color(color.r, color.g, color.b, .12f); line.sortingOrder = 31;
        for (int i = 0; i < line.positionCount; i++)
        {
            float t = i / (float)(line.positionCount - 1); Vector3 point = Vector3.Lerp(start, end, t);
            if (i > 0 && i < line.positionCount - 1 && jitter != 0) point += (Vector3)normal * Random.Range(-jitter, jitter);
            line.SetPosition(i, point);
        }
        obj.AddComponent<LineFade>().Setup(life);
    }

    private static void CreateGlyph(Vector3 position, Color color, float scale, float life)
    {
        var obj = new GameObject("Evolved weapon glyph"); obj.transform.position = position; obj.transform.localScale = Vector3.one * scale;
        var renderer = obj.AddComponent<SpriteRenderer>(); renderer.sprite = GetWhite(); renderer.color = color; renderer.sortingOrder = 32;
        obj.AddComponent<ParticleFade>().Setup(Random.insideUnitCircle * .7f, life);
    }

    public static void Telegraph(Vector3 start, Vector3 end, float duration)
    {
        var obj = new GameObject("Charge warning"); var line = obj.AddComponent<LineRenderer>(); line.useWorldSpace = true; line.positionCount = 2; line.SetPosition(0, start); line.SetPosition(1, end); line.startWidth = .12f; line.endWidth = .46f; line.material = GetLineMaterial(); line.startColor = new Color(1, .2f, .38f, .1f); line.endColor = new Color(1, .24f, .4f, .48f); line.sortingOrder = 18;
        obj.AddComponent<LineFade>().Setup(duration);
    }

    public static void Afterimage(SpriteRenderer source, Vector3 position)
    {
        if (source == null) return;
        var obj = new GameObject("Dash echo"); obj.transform.position = position; obj.transform.rotation = source.transform.rotation; obj.transform.localScale = source.transform.lossyScale;
        var renderer = obj.AddComponent<SpriteRenderer>(); renderer.sprite = source.sprite; renderer.flipX = source.flipX; renderer.color = new Color(.3f, .95f, 1f, .55f); renderer.sortingOrder = 9;
        obj.AddComponent<ParticleFade>().Setup(Vector2.zero, .24f);
    }

    private static Sprite GetWhite()
    {
        if (white != null) return white;
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false); tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white }); tex.Apply(); white = UnityEngine.Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(.5f, .5f), 2); return white;
    }
    private static Material GetLineMaterial()
    {
        if (lineMaterial == null) lineMaterial = new Material(Shader.Find("Sprites/Default"));
        return lineMaterial;
    }
    private static Sprite ring;
    private static Sprite GetRing()
    {
        if (ring != null) return ring;
        var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++) { float r = Vector2.Distance(new Vector2(x, y), new Vector2(63.5f, 63.5f)); texture.SetPixel(x, y, r > 56 && r < 60 ? Color.white : Color.clear); }
        texture.Apply(); return ring = UnityEngine.Sprite.Create(texture, new Rect(0, 0, 128, 128), new Vector2(.5f, .5f), 128);
    }
}

public sealed class ParticleFade : MonoBehaviour
{
    private Vector2 velocity; private float remaining; private float lifetime; private SpriteRenderer spriteRenderer;
    public void Setup(Vector2 move, float life) { velocity = move; lifetime = remaining = life; spriteRenderer = GetComponent<SpriteRenderer>(); }
    private void Update()
    {
        float dt = Time.deltaTime; transform.position += (Vector3)(velocity * dt); velocity *= 1 - dt * 2.8f; remaining -= dt;
        if (spriteRenderer != null) spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, Mathf.Clamp01(remaining / lifetime));
        transform.localScale *= 1 - dt * 1.4f; if (remaining <= 0) Effects.ReleaseSpark(gameObject);
    }
}

public sealed class BladeTrail : MonoBehaviour
{
    private SpriteRenderer source;
    private Color tint;
    private float timer;
    public void Setup(SpriteRenderer blade, Color trailColor) { source = blade; tint = trailColor; }
    private void Update()
    {
        timer -= Time.deltaTime;
        if (source == null || timer > 0) return;
        timer = .045f;
        var echo = new GameObject("Void blade afterimage"); echo.transform.position = source.transform.position; echo.transform.rotation = source.transform.rotation; echo.transform.localScale = source.transform.lossyScale;
        var renderer = echo.AddComponent<SpriteRenderer>(); renderer.sprite = source.sprite; renderer.color = tint; renderer.sortingOrder = 11;
        echo.AddComponent<ParticleFade>().Setup(Vector2.zero, .16f);
    }
}

public sealed class RingFade : MonoBehaviour
{
    private float duration, size; private SpriteRenderer spriteRenderer; private float remaining;
    public void Setup(float finalSize, float life) { size = finalSize; duration = remaining = life; spriteRenderer = GetComponent<SpriteRenderer>(); }
    private void Update()
    {
        if (GameBootstrap.Game != null && GameBootstrap.Game.State != RunState.Playing && Time.timeScale == 0) return;
        remaining -= Time.deltaTime; float t = 1 - remaining / duration; transform.localScale = Vector3.one * Mathf.Lerp(.15f, size, t);
        if (spriteRenderer != null) spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, Mathf.Clamp01(1 - t));
        if (remaining <= 0) Object.Destroy(gameObject);
    }
}

public sealed class LineFade : MonoBehaviour
{
    private float remaining, lifetime; private LineRenderer line;
    public void Setup(float duration) { remaining = lifetime = duration; line = GetComponent<LineRenderer>(); }
    private void Update() { remaining -= Time.deltaTime; if (line != null) line.startColor = new Color(line.startColor.r, line.startColor.g, line.startColor.b, Mathf.Clamp01(remaining / lifetime)); if (remaining <= 0) Object.Destroy(gameObject); }
}

public sealed class FloatingLabel : MonoBehaviour
{
    private Vector3 source; private RectTransform rect, canvasRect; private Canvas labelCanvas; private float age; private Camera cam;
    public void Setup(Vector3 worldPosition)
    {
        source = worldPosition; rect = GetComponent<RectTransform>(); age = 0f; cam = Camera.main;
        labelCanvas = GetComponentInParent<Canvas>();
        canvasRect = labelCanvas != null ? labelCanvas.transform as RectTransform : null;
    }
    private void Update()
    {
        if (GameBootstrap.Game == null || GameBootstrap.Game.State != RunState.Playing) { Effects.ReleaseLabel(gameObject); return; }
        if (cam == null) cam = Camera.main;
        if (cam == null) { Effects.ReleaseLabel(gameObject); return; }
        age += Time.unscaledDeltaTime; Vector3 point = cam.WorldToScreenPoint(source + Vector3.up * age * .48f);
        if (canvasRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, point, labelCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : labelCanvas.worldCamera, out Vector2 local))
            rect.anchoredPosition = local;
        else rect.position = point;
        var label = GetComponent<Text>(); label.color = new Color(label.color.r, label.color.g, label.color.b, Mathf.Clamp01((.75f - age) * 4));
        if (age > .75f) Effects.ReleaseLabel(gameObject);
    }
}

public enum SoundCue { Shot, Scatter, Arc, Dash, Hurt, Pickup, Upgrade, BossDefeat }

public static class SoundBank
{
    // Gameplay SFX (shots, hits, pickups...) sit a little louder than before; background music keeps its own volume.
    private const float SfxGain = 1.3f;
    private static AudioSource source;
    private static AudioSource musicSource;
    private static string currentMusic;
    private static readonly Dictionary<SoundCue, AudioClip> clips = new Dictionary<SoundCue, AudioClip>();

    public static void SetMusic(string key)
    {
        if (currentMusic == key) return;
        currentMusic = key;
        if (musicSource == null)
        {
            var obj = new GameObject("Background music"); Object.DontDestroyOnLoad(obj);
            musicSource = obj.AddComponent<AudioSource>(); musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0; musicSource.loop = true; musicSource.volume = .18f;
        }
        musicSource.Stop();
        musicSource.clip = string.IsNullOrEmpty(key) ? null : Resources.Load<AudioClip>("Audio/" + key);
        if (musicSource.clip != null) musicSource.Play();
    }

    public static void Play(SoundCue cue, float volume)
    {
        if (source == null)
        {
            var obj = new GameObject("Procedural sound"); Object.DontDestroyOnLoad(obj); source = obj.AddComponent<AudioSource>(); source.playOnAwake = false; source.spatialBlend = 0;
        }
        volume = Mathf.Clamp01(volume * SfxGain);
        if (clips.TryGetValue(cue, out AudioClip cached)) { source.PlayOneShot(cached, volume); return; }
        int count = cue == SoundCue.BossDefeat ? 5 : cue == SoundCue.Scatter ? 2 : cue == SoundCue.Arc ? 3 : 1;
        float root = cue == SoundCue.BossDefeat ? 190 : cue == SoundCue.Dash ? 100 : cue == SoundCue.Hurt ? 140 : cue == SoundCue.Pickup ? 740 : cue == SoundCue.Upgrade ? 510 : cue == SoundCue.Arc ? 400 : cue == SoundCue.Scatter ? 250 : 600;
        int sampleRate = 22050; int length = Mathf.RoundToInt(sampleRate * (cue == SoundCue.Dash ? .11f : .065f));
        if (cue == SoundCue.BossDefeat) length = Mathf.RoundToInt(sampleRate * .48f);
        var clip = AudioClip.Create("Procedural " + cue, length, 1, sampleRate, false); var samples = new float[length];
        for (int i = 0; i < count; i++)
        {
            float frequency = root * (i == 0 ? 1 : i == 1 ? 1.25f : 1.5f);
            for (int s = 0; s < length; s++)
            {
                float t = s / (float)sampleRate; float envelope = Mathf.Exp(-t * (cue == SoundCue.BossDefeat ? 5 : cue == SoundCue.Dash ? 22 : 40));
                float sweep = cue == SoundCue.BossDefeat ? frequency * (1 + t * 3.8f) : cue == SoundCue.Dash ? frequency * (1 - t * 6) : frequency * (1 + t * 1.5f);
                samples[s] += Mathf.Sin(2 * Mathf.PI * sweep * t) * envelope * (1 - i * .18f) / count;
            }
        }
        clip.SetData(samples, 0); clips[cue] = clip; source.PlayOneShot(clip, volume);
    }
}

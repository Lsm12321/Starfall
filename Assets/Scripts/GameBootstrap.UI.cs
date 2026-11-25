using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public partial class GameBootstrap
{
    private static void SetText(Text text, string value)
    {
        if (text != null && text.text != value) text.text = value;
    }

    private T NewUI<T>(string name, Transform parent) where T : Component
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        if (typeof(T) != typeof(RectTransform)) go.AddComponent<T>();
        go.transform.SetParent(parent, false);
        return go.GetComponent<T>();
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
    }

    private void BuildUI()
    {
        var eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
            eventSystem = new GameObject("Event System", typeof(EventSystem), typeof(StandaloneInputModule)).GetComponent<EventSystem>();
        if (eventSystem.TryGetComponent<StandaloneInputModule>(out var inputModule) && inputModule.inputOverride == null)
            inputModule.inputOverride = eventSystem.gameObject.AddComponent<StarfallUiInput>();
        var go = new GameObject("Starfall UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 1000;
        var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1440, 810); scaler.matchWidthOrHeight = .5f;
        hudRoot = new GameObject("HUD"); hudRoot.transform.SetParent(canvas.transform, false); Stretch(hudRoot.AddComponent<RectTransform>());
        BuildHud();
        overlay = NewUI<RectTransform>("State overlay", canvas.transform); Stretch(overlay);
    }

    // ------------------------------------------------------------------ HUD
    private GameObject bossWarningPanel;
    private CanvasGroup bossWarningGroup;
    private Text bossWarningTitle, bossWarningSub;
    private float bossWarningUntil;
    private int warnedBossStage;
    private const float BossWarningSeconds = 1.4f;

    private void BuildHud()
    {
        Art(hudRoot.transform, "Screen vignette", UiKit.Vignette, new Color(0, 0, 0, .36f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        BuildTelemetry();
        BuildVitals();
        BuildExperience();
        BuildBossPanel();
        BuildBossWarning();
        BuildBanner();
        BuildHint();
        if (touchControls) BuildTouchControls();
    }

    private void BuildBossWarning()
    {
        var panel = Node(hudRoot.transform, "Boss warning", AnchorCC, new Vector2(0, -32), new Vector2(680, 160));
        bossWarningPanel = panel.gameObject;
        bossWarningGroup = panel.gameObject.AddComponent<CanvasGroup>();
        Art(panel, "Backing", UiKit.Soft, new Color(.055f, .012f, .028f, .8f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        Art(panel, "Signal wash", UiKit.GradientRight, new Color(1f, .05f, .12f, .045f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        var lineColor = UiKit.Alpha(GameRules.Pink, .82f);
        var lineSize = new Vector2(420, 2);
        Art(panel, "Top lock line", UiKit.Solid, lineColor, AnchorTC, new Vector2(0, -16), lineSize);
        Art(panel, "Bottom lock line", UiKit.Solid, lineColor, AnchorBC, new Vector2(0, 16), lineSize);
        Art(panel, "Lock mark", UiKit.Diamond, UiKit.Alpha(GameRules.Pink, .9f), AnchorCC, new Vector2(-246, 0), new Vector2(8, 8));
        Art(panel, "Lock mark", UiKit.Diamond, UiKit.Alpha(GameRules.Pink, .9f), AnchorCC, new Vector2(246, 0), new Vector2(8, 8));
        bossWarningTitle = MakeText(panel, "Warning title", "WARNING", 52, TextAnchor.MiddleCenter,
            AnchorCC, AnchorCC, AnchorCC, new Vector2(0, 10), new Vector2(600, 68), Color.white, FontStyle.Bold, displayFont);
        GlowText(bossWarningTitle, UiKit.Alpha(GameRules.Pink, .3f), 1.5f);
        bossWarningSub = MakeText(panel, "Warning subtitle", "BOSS INCOMING", 13, TextAnchor.MiddleCenter,
            AnchorCC, AnchorCC, AnchorCC, new Vector2(0, -44), new Vector2(600, 24), UiKit.TextMain, FontStyle.Bold, null);
        bossWarningPanel.SetActive(false);
    }

    private void ShowBossWarning(int stage)
    {
        warnedBossStage = stage;
        bossWarningUntil = Time.unscaledTime + BossWarningSeconds;
        if (bossWarningPanel == null) return;
        bossWarningPanel.SetActive(true);
        bossWarningGroup.alpha = 0f;
        bossWarningPanel.transform.localScale = Vector3.one;
        SoundBank.Play(SoundCue.Dash, .3f);
        SoundBank.Play(SoundCue.Hurt, .16f);
    }

    private void BuildTelemetry()
    {
        float telemetryWidth = touchControls ? 252f : 220f;
        var panel = Frame(hudRoot.transform, "Top telemetry", AnchorTR, new Vector2(-22, -20), new Vector2(telemetryWidth, 82), GameRules.Gold, .9f, false);
        stageEdge = panel.Find("Edge").GetComponent<Image>();
        Art(panel, "Accent", UiKit.Solid, UiKit.Alpha(GameRules.Gold, .9f), AnchorTL, new Vector2(12, -12), new Vector2(2, 20));
        clockText = Lt(panel, "00:00", 26, new Vector2(14, -8), new Vector2(120, 32), UiKit.TextMain, FontStyle.Bold, displayFont);
        DropShadow(clockText, -2f);
        Rt(panel, "等级", 9, new Vector2(-12, -7), new Vector2(70, 12), UiKit.TextMute, FontStyle.Bold);
        levelText = Rt(panel, "01", 16, new Vector2(-12, -20), new Vector2(70, 20), GameRules.Gold, FontStyle.Bold, displayFont);
        stageNameText = Lt(panel, GameRules.StageName(1), 12, new Vector2(14, -43), new Vector2(touchControls ? 224f : 192f, 22), UiKit.TextMain, FontStyle.Bold);
        waveFill = Bar(panel, "Wave progress", 14f, 14f, -68f, 3f, new Color(.09f, .15f, .18f, .72f), GameRules.Cyan, new Color(1, 1, 1, .06f), out var waveGlow, out _, 12);
        waveGlow.color = UiKit.Alpha(GameRules.Cyan, .08f);
    }

    private void BuildVitals()
    {
        var panel = Frame(hudRoot.transform, "Vitals", AnchorTL, new Vector2(22, -20), new Vector2(260, 84), GameRules.Pink, .88f, false);
        healthEdge = panel.Find("Edge").GetComponent<Image>();
        Art(panel, "Pulse dot", UiKit.Diamond, UiKit.Alpha(GameRules.Pink, .95f), AnchorTL, new Vector2(13, -9), new Vector2(5, 5));
        Lt(panel, "生命", 12, new Vector2(24, -5), new Vector2(150, 16), UiKit.TextDim, FontStyle.Bold);
        healthText = Rt(panel, "100 / 100", 16, new Vector2(-12, -4), new Vector2(150, 18), UiKit.TextMain, FontStyle.Bold, displayFont);
        hpFill = Bar(panel, "Health fill", 14f, 14f, -29f, 7f, new Color(.16f, .07f, .1f, .85f), GameRules.Pink, new Color(1, 1, 1, .18f), out hpGlow, out hpGhost, 6);
        HLine(panel, "Divider", 14f, -43f, 1f, new Color(1, 1, 1, .07f));
        Art(panel, "Dash dot", UiKit.Diamond, UiKit.Alpha(GameRules.Cyan, .9f), AnchorTL, new Vector2(13, -49), new Vector2(5, 5));
        Lt(panel, "冲刺", 12, new Vector2(24, -45), new Vector2(120, 16), UiKit.TextDim, FontStyle.Bold);
        dashText = Rt(panel, "就绪", 13, new Vector2(-12, -45), new Vector2(140, 16), GameRules.Cyan, FontStyle.Bold);
        Image dashGlow;
        dashFill = Bar(panel, "Dash cooldown", 14f, 14f, -67f, 5f, new Color(.08f, .13f, .16f, .9f), GameRules.Cyan, new Color(1, 1, 1, .12f), out dashGlow, out _, 0);
    }

    private void BuildExperience()
    {
        var root = Node(hudRoot.transform, "XP progress", Vector2.zero, new Vector2(1, 0), new Vector2(.5f, 0), Vector2.zero, new Vector2(0, 5));
        Art(root, "Track", UiKit.Solid, new Color(.05f, .1f, .14f, .95f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        xpGlow = Fill(root, "Glow", UiKit.Glow, UiKit.Alpha(GameRules.Cyan, .34f), new Vector2(0, 6), new Vector2(0, 34));
        xpFill = Fill(root, "Fill", UiKit.Solid, GameRules.Cyan); xpFill.type = Image.Type.Filled; xpFill.fillMethod = Image.FillMethod.Horizontal; xpFill.fillAmount = 0f;
        Art(root, "Highlight", UiKit.Solid, new Color(1, 1, 1, .35f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(.5f, 1), Vector2.zero, new Vector2(-2, 1));
        xpText = Rt(hudRoot.transform, "XP  0 / 50", 9, new Vector2(-24, 12), new Vector2(200, 14), UiKit.TextMute, FontStyle.Bold);
    }

    private Sprite SquarePortrait(string key)
    {
        Texture2D texture = Resources.Load<Texture2D>(ArtPath(key));
        if (texture == null) return null;
        int size = Mathf.RoundToInt(texture.width * .72f);
        int x = (texture.width - size) / 2;
        int y = Mathf.RoundToInt(texture.height * .38f);
        return Sprite.Create(texture, new Rect(x, y, size, size), new Vector2(.5f, .5f), 128);
    }

    private void BuildArmory()
    {
        var panel = Frame(hudRoot.transform, "Armory telemetry", AnchorTR, new Vector2(-22, -20), new Vector2(220, 256), GameRules.Cyan, .9f);
        var portraitFrame = Node(panel, "Portrait frame", AnchorTL, new Vector2(12, -10), new Vector2(32, 32));
        Art(portraitFrame, "Fill", UiKit.SoftSmall, new Color(1, 1, 1, .05f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        Fill(portraitFrame, "Edge", UiKit.SoftSmallEdge, UiKit.Alpha(GameRules.Cyan, .55f));
        pilotPortrait = Art(portraitFrame, "Pilot portrait", null, Color.white, new Vector2(0, 0), new Vector2(1, 1), AnchorCC, Vector2.zero, new Vector2(-6, -6));
        pilotPortrait.preserveAspect = true;
        pilotText = Lt(panel, "RIN", 11, new Vector2(52, -11), new Vector2(156, 14), GameRules.Cyan, FontStyle.Bold);
        Lt(panel, UiKit.Track("ARMORY / LOADOUT"), 8, new Vector2(52, -25), new Vector2(160, 12), UiKit.TextMute, FontStyle.Bold);
        HLine(panel, "Divider", 12f, -48f, 1f, new Color(1, 1, 1, .08f));
        for (int i = 0; i < 5; i++)
        {
            float y = -56f - i * 39f;
            var row = Node(panel, "Weapon row", new Vector2(0, 1), new Vector2(1, 1), new Vector2(.5f, 1), new Vector2(0, y), new Vector2(-16f, 36f));
            Art(row, "Row fill", UiKit.Soft, new Color(1, 1, 1, .025f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
            var iconFrame = Node(row, "Icon frame", AnchorTL, new Vector2(7, -5), new Vector2(26, 26));
            Art(iconFrame, "Fill", UiKit.SoftSmall, new Color(.03f, .07f, .1f, .95f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
            Fill(iconFrame, "Edge", UiKit.SoftSmallEdge, new Color(1, 1, 1, .1f));
            weaponIcons[i] = Art(iconFrame, "Weapon glyph", null, Color.white, new Vector2(0, 0), new Vector2(1, 1), AnchorCC, Vector2.zero, new Vector2(-7, -7));
            weaponIcons[i].preserveAspect = true;
            weaponLabels[i] = Lm(row, "—", touchControls ? 12 : 10, new Vector2(39, 0), new Vector2(52, 18), UiKit.TextMute, FontStyle.Bold);
            weaponRanks[i] = Rm(row, string.Empty, touchControls ? 10 : 8, new Vector2(-8, 0), new Vector2(46, 18), UiKit.TextMute, FontStyle.Bold);
            for (int p = 0; p < 5; p++)
                weaponPips[i, p] = Art(row, "Rank pip", UiKit.Solid, new Color(1, 1, 1, .07f), AnchorTL, new Vector2(39 + p * 6f, -23f), new Vector2(4, 3));
            var evoChip = Node(row, "Evo chip", AnchorTL, new Vector2(94, -9), new Vector2(26, 18));
            weaponEvoChips[i] = Art(evoChip, "Fill", UiKit.SoftSmall, new Color(1, 1, 1, .05f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
            Fill(evoChip, "Edge", UiKit.SoftSmallEdge, UiKit.Alpha(GameRules.Gold, .45f));
            weaponEvoLabels[i] = Mc(evoChip, "EVO", 6, GameRules.Gold, FontStyle.Bold);
            evoChip.gameObject.SetActive(false);
        }
    }

    private void BuildBossPanel()
    {
        var boss = Frame(hudRoot.transform, "Boss health", AnchorTC, new Vector2(25f, -18), new Vector2(520f, 66), GameRules.Pink, .95f, false);
        bossFrameEdge = boss.Find("Edge").GetComponent<Image>();
        var portrait = Node(boss, "Boss portrait frame", AnchorTL, new Vector2(12, -9), new Vector2(48, 48));
        Art(portrait, "Fill", UiKit.Solid, new Color(.02f, .03f, .05f, .96f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        bossPortrait = Art(portrait, "Boss portrait", null, Color.white, Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, new Vector2(-5, -5));
        bossPortrait.preserveAspect = true;
        bossHeader = MakeText(boss, "Boss title", "噬影守望者", 13, TextAnchor.MiddleLeft, AnchorTL, AnchorTL, AnchorTL, new Vector2(72, -9), new Vector2(330, 20), GameRules.Pink, FontStyle.Bold, null);
        bossText = Lm(boss, "PHASE I", 9, new Vector2(72, -27), new Vector2(330, 14), UiKit.TextDim);
        bossFill = Bar(boss, "Boss health fill", 72f, 22f, -50f, 7f, new Color(.18f, .06f, .1f, .9f), GameRules.Pink, new Color(1, 1, 1, .18f), out bossGlow, out _, 4);
        bossPanel = boss.gameObject;
        bossPanel.SetActive(false);
    }

    private void BuildBanner()
    {
        bannerRoot = Node(hudRoot.transform, "Banner", AnchorTC, new Vector2(45f, -122), new Vector2(460f, 32));
        bannerGroup = bannerRoot.gameObject.AddComponent<CanvasGroup>();
        bannerGroup.alpha = 0f; bannerGroup.blocksRaycasts = false;
        Art(bannerRoot, "Signal line", UiKit.Solid, new Color(1, 1, 1, .18f), new Vector2(0, 0), new Vector2(1, 0), new Vector2(.5f, 0), Vector2.zero, new Vector2(-24, 1));
        bannerAccent = Art(bannerRoot, "Accent", UiKit.Solid, GameRules.Cyan, new Vector2(0, 0), new Vector2(1, 0), new Vector2(.5f, 0), Vector2.zero, new Vector2(0, 2));
        bannerText = Mc(bannerRoot, "", 12, Color.white, FontStyle.Bold);
        bannerText.gameObject.name = "Banner text";
    }

    private void BuildHint()
    {
        hintText = Mc(hudRoot.transform, "WASD / 方向键  移动     空格  冲刺     自动攻击     C  角色     ESC  暂停", 10, UiKit.TextMute, FontStyle.Bold);
        hintText.rectTransform.anchorMin = new Vector2(.5f, 0); hintText.rectTransform.anchorMax = new Vector2(.5f, 0);
        if (touchControls)
        {
            hintText.text = "左侧任意位置拖动移动     右下按钮冲刺     武器自动攻击";
            hintText.fontSize = MobileTextSize(14);
            DropShadow(hintText, -1.5f);
        }
        hintText.rectTransform.pivot = new Vector2(.5f, 0); hintText.rectTransform.anchoredPosition = new Vector2(0f, touchControls ? 28f : 18f);
        hintText.rectTransform.sizeDelta = new Vector2(touchControls ? 720f : 560f, 20);
    }

    // --------------------------------------------------------------- helpers
    private RectTransform Node(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size) => Node(parent, name, anchor, anchor, anchor, pos, size);
    private RectTransform Node(Transform parent, string name, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var rect = NewUI<RectTransform>(name, parent);
        rect.anchorMin = min; rect.anchorMax = max; rect.pivot = pivot; rect.anchoredPosition = pos; rect.sizeDelta = size;
        return rect;
    }

    private Image Art(Transform parent, string name, Sprite sprite, Color color, Vector2 anchor, Vector2 pos, Vector2 size) => Art(parent, name, sprite, color, anchor, anchor, anchor, pos, size);

    private Image Art(Transform parent, string name, Sprite sprite, Color color, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var image = NewUI<Image>(name, parent);
        var rect = image.rectTransform;
        rect.anchorMin = min; rect.anchorMax = max; rect.pivot = pivot; rect.anchoredPosition = pos; rect.sizeDelta = size;
        image.sprite = sprite; image.color = color; image.raycastTarget = false;
        if (sprite != null && sprite.border.sqrMagnitude > 0f) image.type = Image.Type.Sliced;
        return image;
    }

    private Image Fill(Transform parent, string name, Sprite sprite, Color color, Vector2 offset = default, Vector2 grow = default)
    {
        var image = Art(parent, name, sprite, color, Vector2.zero, Vector2.one, AnchorCC, offset, grow);
        return image;
    }

    private RectTransform Frame(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color accent, float fillAlpha = .9f)
    {
        return Frame(parent, name, anchor, pos, size, accent, fillAlpha, true);
    }

    private RectTransform Frame(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color accent, float fillAlpha, bool brackets)
    {
        var root = Node(parent, name, anchor, pos, size);
        Art(root, "Shadow", UiKit.PanelFill, new Color(0, 0, 0, .34f), Vector2.zero, Vector2.one, AnchorCC, new Vector2(0, -6), new Vector2(16, 18));
        Art(root, "Body", UiKit.PanelFill, UiKit.Alpha(UiKit.Panel, fillAlpha), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        Art(root, "Edge", UiKit.PanelEdge, UiKit.Alpha(accent, .24f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        Art(root, "Top sheen", UiKit.GradientRight, new Color(1, 1, 1, .035f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(.5f, 1), new Vector2(0, -1), new Vector2(-26, 2));
        if (brackets)
        {
            Bracket(root, "Bracket TL", AnchorTL, new Vector2(7, -7), 0f, accent, 18f);
            Bracket(root, "Bracket TR", AnchorTR, new Vector2(-7, -7), -90f, accent, 18f);
            Bracket(root, "Bracket BR", AnchorBR, new Vector2(-7, 7), 180f, accent, 18f);
            Bracket(root, "Bracket BL", AnchorBL, new Vector2(7, 7), 90f, accent, 18f);
        }
        return root;
    }

    private void Bracket(RectTransform parent, string name, Vector2 anchor, Vector2 pos, float rotation, Color color, float length)
    {
        var image = Art(parent, name, UiKit.Bracket, UiKit.Alpha(color, .32f), anchor, pos, new Vector2(length, length));
        image.rectTransform.localRotation = Quaternion.Euler(0, 0, rotation);
    }

    private Image HLine(Transform parent, string name, float inset, float y, float height, Color color)
    {
        return Art(parent, name, UiKit.Solid, color, new Vector2(0, 1), new Vector2(1, 1), new Vector2(.5f, 1), new Vector2(0, y), new Vector2(-inset * 2f, height));
    }

    private Image Bar(Transform parent, string name, float leftInset, float rightInset, float y, float height, Color track, Color accent, Color ghostColor, out Image glow, out Image ghost, int ticks)
    {
        var root = Node(parent, name, new Vector2(0, 1), new Vector2(1, 1), new Vector2(.5f, 1), new Vector2((leftInset - rightInset) * .5f, y), new Vector2(-(leftInset + rightInset), height));
        glow = Fill(root, "Glow", UiKit.Glow, UiKit.Alpha(accent, .18f), new Vector2(0, -2), new Vector2(30, 34));
        glow.transform.SetAsFirstSibling();
        Art(root, "Track", UiKit.Solid, track, Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        ghost = Fill(root, "Ghost", UiKit.Solid, ghostColor); ghost.type = Image.Type.Filled; ghost.fillMethod = Image.FillMethod.Horizontal; ghost.fillAmount = 0f;
        var fill = Fill(root, "Fill", UiKit.Solid, accent); fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillAmount = 1f;
        Art(root, "Highlight", UiKit.Solid, new Color(1, 1, 1, .16f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(.5f, 1), Vector2.zero, new Vector2(-2, 1));
        for (int i = 1; i < ticks; i++)
        {
            float t = i / (float)ticks;
            Art(root, "Tick", UiKit.Solid, new Color(.01f, .03f, .05f, .55f), new Vector2(t, 0), new Vector2(t, 1), new Vector2(.5f, .5f), Vector2.zero, new Vector2(1, 0));
        }
        return fill;
    }

    private int MobileTextSize(int size) => touchControls ? Mathf.Max(1, Mathf.RoundToInt(size * 1.3f)) : size;

    private Text MakeText(Transform parent, string name, string value, int size, TextAnchor align, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 dims, Color color, FontStyle style, Font font)
    {
        var text = NewUI<Text>(name, parent);
        var rect = text.rectTransform;
        rect.anchorMin = min; rect.anchorMax = max; rect.pivot = pivot; rect.anchoredPosition = pos; rect.sizeDelta = dims;
        int baseSize = size;
        size = MobileTextSize(size);
        text.text = value; text.font = font != null ? font : uiFont; text.fontSize = size; text.alignment = align; text.color = color; text.fontStyle = style;
        text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
        text.lineSpacing = 1.14f; text.raycastTarget = false;
        if (touchControls)
        {
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = baseSize;
            text.resizeTextMaxSize = size;
        }
        text.gameObject.AddComponent<LocalizedText>().Bind(text);
        return text;
    }

    private Text Lt(Transform p, string v, int s, Vector2 pos, Vector2 dims, Color c, FontStyle st = FontStyle.Normal, Font f = null) => MakeText(p, "Text", v, s, TextAnchor.UpperLeft, AnchorTL, AnchorTL, AnchorTL, pos, dims, c, st, f);
    private Text Lm(Transform p, string v, int s, Vector2 pos, Vector2 dims, Color c, FontStyle st = FontStyle.Normal, Font f = null) => MakeText(p, "Text", v, s, TextAnchor.MiddleLeft, AnchorTL, AnchorTL, AnchorTL, pos, dims, c, st, f);
    private Text Rt(Transform p, string v, int s, Vector2 pos, Vector2 dims, Color c, FontStyle st = FontStyle.Normal, Font f = null) => MakeText(p, "Text", v, s, TextAnchor.UpperRight, AnchorTR, AnchorTR, AnchorTR, pos, dims, c, st, f);
    private Text Rm(Transform p, string v, int s, Vector2 pos, Vector2 dims, Color c, FontStyle st = FontStyle.Normal, Font f = null) => MakeText(p, "Text", v, s, TextAnchor.MiddleRight, AnchorTR, AnchorTR, AnchorTR, pos, dims, c, st, f);
    private Text Ct(Transform p, string v, int s, Vector2 pos, Vector2 dims, Color c, FontStyle st = FontStyle.Normal, Font f = null) => MakeText(p, "Text", v, s, TextAnchor.UpperCenter, AnchorTC, AnchorTC, AnchorTC, pos, dims, c, st, f);
    private Text Mc(Transform p, string v, int s, Color c, FontStyle st = FontStyle.Normal, Font f = null) => MakeText(p, "Text", v, s, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero, c, st, f);

    private static void DropShadow(Text text, float y = -2f)
    {
        var shadow = text.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0, 0, 0, .6f); shadow.effectDistance = new Vector2(0, y);
    }

    private static void GlowText(Text text, Color color, float distance = 1.6f)
    {
        var outline = text.gameObject.AddComponent<Outline>(); outline.effectColor = color; outline.effectDistance = new Vector2(distance, distance);
    }

    private Button Cta(Transform parent, string name, string caption, Vector2 anchor, Vector2 pos, Vector2 size, Color accent, bool filled, UnityEngine.Events.UnityAction action, int fontSize = 14)
    {
        if (touchControls) { size.y = Mathf.Max(size.y, 68f); fontSize = Mathf.Max(fontSize, 16); }
        var root = Node(parent, name, anchor, pos, size);
        var button = root.gameObject.AddComponent<Button>();
        var body = root.gameObject.AddComponent<Image>();
        body.sprite = UiKit.Soft; body.type = Image.Type.Sliced;
        body.color = filled ? UiKit.Alpha(accent, .94f) : UiKit.Alpha(UiKit.PanelSoft, .82f);
        body.raycastTarget = true; button.targetGraphic = body; button.transition = Selectable.Transition.None;
        var edge = Fill(root, "Edge", UiKit.SoftEdge, UiKit.Alpha(accent, filled ? .95f : .5f));
        var labelColor = filled ? UiKit.Ink : UiKit.TextMain;
        var label = caption == null ? null : Mc(root, caption, fontSize, labelColor, FontStyle.Bold);
        button.onClick.AddListener(action);
        var hover = root.gameObject.AddComponent<UiHover>();
        hover.body = root; hover.edge = edge;
        hover.edgeIdle = UiKit.Alpha(accent, filled ? .95f : .5f); hover.edgeHot = UiKit.Alpha(accent, 1f);
        hover.fill = body; hover.fillIdle = body.color; hover.fillHot = filled ? UiKit.Light(accent, .12f) : UiKit.Alpha(UiKit.Shade(accent, .45f), .95f);
        hover.label = label; hover.labelIdle = labelColor; hover.labelHot = filled ? UiKit.Ink : Color.white;
        hover.hoverScale = 1.025f;
        return button;
    }

    // ---------------------------------------------------------- title screen
}

public sealed class StarfallUiInput : BaseInput
{
    public override bool GetButtonDown(string buttonName)
    {
        // Space confirms a focused menu control, but cannot confirm an upgrade.
        return base.GetButtonDown(buttonName) ||
            (buttonName == "Submit" && GameBootstrap.Game?.State != RunState.Upgrade && Input.GetKeyDown(KeyCode.Space));
    }
}

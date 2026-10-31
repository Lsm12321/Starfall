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
        if (FindFirstObjectByType<EventSystem>() == null) new GameObject("Event System", typeof(EventSystem), typeof(StandaloneInputModule));
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
        Art(hudRoot.transform, "Screen vignette", UiKit.Vignette, new Color(0, 0, 0, .55f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        BuildTelemetry();
        BuildVitals();
        BuildStagePanel();
        BuildExperience();
        BuildBossPanel();
        BuildBossWarning();
        BuildBanner();
        BuildHint();
        if (touchControls) BuildTouchControls();
    }

    private void BuildBossWarning()
    {
        var panel = Node(hudRoot.transform, "Boss warning", AnchorCC, new Vector2(0, 30), new Vector2(680, 160));
        bossWarningPanel = panel.gameObject;
        bossWarningGroup = panel.gameObject.AddComponent<CanvasGroup>();
        Art(panel, "Backing", UiKit.Soft, new Color(.075f, .008f, .022f, .94f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        Art(panel, "Band", UiKit.GradientRight, new Color(1f, .05f, .12f, .22f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        Art(panel, "Top line", UiKit.Solid, UiKit.Alpha(GameRules.Pink, 1f), AnchorTC, new Vector2(0, -18), new Vector2(540, 3));
        Art(panel, "Bottom line", UiKit.Solid, UiKit.Alpha(GameRules.Pink, .8f), AnchorBC, new Vector2(0, 18), new Vector2(420, 2));
        Art(panel, "Left mark", UiKit.Diamond, UiKit.Alpha(GameRules.Pink, 1f), AnchorCC, new Vector2(-310, 0), new Vector2(10, 10));
        Art(panel, "Right mark", UiKit.Diamond, UiKit.Alpha(GameRules.Pink, 1f), AnchorCC, new Vector2(310, 0), new Vector2(10, 10));
        bossWarningTitle = Ct(panel, "WARNING", 46, new Vector2(0, -24), new Vector2(600, 56), Color.white, FontStyle.Bold, displayFont);
        GlowText(bossWarningTitle, UiKit.Alpha(GameRules.Pink, .5f), 2f);
        bossWarningSub = Ct(panel, "强敌接近  /  BOSS INCOMING", 13, new Vector2(0, -86), new Vector2(600, 20), UiKit.TextMain, FontStyle.Bold);
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
        float panelWidth = touchControls ? 296f : 326f;
        float panelHeight = touchControls ? 110f : 120f;
        var panel = Frame(hudRoot.transform, "Top telemetry", touchControls ? AnchorTR : AnchorTL, touchControls ? new Vector2(-22, -92) : new Vector2(22, -20), new Vector2(panelWidth, panelHeight), GameRules.Cyan, .9f);
        Art(panel, "Accent", UiKit.Solid, UiKit.Alpha(GameRules.Cyan, .9f), AnchorTL, new Vector2(12, -16), new Vector2(2, 24));
        Lt(panel, UiKit.Track("STARFALL  /  SURVIVAL LOG"), 9, new Vector2(22, -11), new Vector2(panelWidth - 44f, 14), UiKit.Alpha(GameRules.Cyan, .92f), FontStyle.Bold);
        clockText = Lt(panel, "00:00", 29, new Vector2(18, -24), new Vector2(158, 36), UiKit.TextMain, FontStyle.Bold, displayFont);
        DropShadow(clockText, -2f);
        HLine(panel, "Divider", 18f, -62f, 1f, new Color(1, 1, 1, .07f));
        Lt(panel, UiKit.Track("ELIMINATIONS"), 8, new Vector2(18, -66), new Vector2(130, 12), UiKit.TextMute, FontStyle.Bold);
        killText = Lt(panel, "000", 18, new Vector2(18, -76), new Vector2(110, 24), UiKit.TextMain, FontStyle.Bold, displayFont);
        Rt(panel, UiKit.Track("LEVEL"), 8, new Vector2(-14, -66), new Vector2(90, 12), UiKit.TextMute, FontStyle.Bold);
        levelText = Rt(panel, "01", 18, new Vector2(-14, -76), new Vector2(90, 24), GameRules.Gold, FontStyle.Bold, displayFont);
    }

    private void BuildStagePanel()
    {
        // Stage information replaces the old upper-right loadout panel.
        var panel = Frame(hudRoot.transform, "Stage telemetry", AnchorTR, new Vector2(-22, touchControls ? -212 : -20), new Vector2(240, 126), GameRules.Gold, .9f);
        stageEdge = panel.Find("Edge").GetComponent<Image>();
        Art(panel, "Accent", UiKit.Solid, UiKit.Alpha(GameRules.Gold, .9f), AnchorTL, new Vector2(12, -16), new Vector2(2, 24));
        stageSectorText = Lt(panel, UiKit.Track("SECTOR 01"), 8, new Vector2(14, -9), new Vector2(90, 12), UiKit.TextMute, FontStyle.Bold);
        stageIndexText = Rt(panel, "STAGE 01 / 03", 8, new Vector2(-14, -9), new Vector2(120, 12), GameRules.Gold, FontStyle.Bold);
        waveText = Lt(panel, GameRules.StageName(1), 12, new Vector2(14, -23), new Vector2(212, 34), UiKit.TextMain, FontStyle.Bold);
        HLine(panel, "Divider", 14f, -60f, 1f, new Color(1, 1, 1, .07f));
        waveLabelText = Lt(panel, UiKit.Track("WAVE 01 / 18"), 8, new Vector2(14, -64), new Vector2(150, 12), UiKit.TextMute, FontStyle.Bold);
        Image waveGlow;
        waveFill = Bar(panel, "Wave progress", 14f, 14f, -78f, 4f, new Color(.09f, .15f, .18f, .85f), GameRules.Cyan, new Color(1, 1, 1, .1f), out waveGlow, out _, 18);
        waveGlow.color = UiKit.Alpha(GameRules.Cyan, .14f);
        Lt(panel, "BOSS", 7, new Vector2(14, -89), new Vector2(40, 10), UiKit.TextMute, FontStyle.Bold);
        for (int i = 0; i < stagePips.Length; i++)
            stagePips[i] = Art(panel, "Boss pip", UiKit.Diamond, new Color(1, 1, 1, .14f), AnchorTL, new Vector2(52f + i * 18f, -88f), new Vector2(9f, 9f));
    }

    private void BuildVitals()
    {
        // Compact combat vitals live directly under the survival clock: 304x96 against the
        // 326x138 telemetry frame, so they read as a secondary block instead of a second HUD.
        var panel = Frame(hudRoot.transform, "Vitals", AnchorTL, new Vector2(22, touchControls ? -20 : -166), new Vector2(touchControls ? 276 : 304, touchControls ? 84 : 96), GameRules.Pink, .88f);
        healthEdge = panel.Find("Edge").GetComponent<Image>();
        Art(panel, "Pulse dot", UiKit.Diamond, UiKit.Alpha(GameRules.Pink, .95f), AnchorTL, new Vector2(13, -9), new Vector2(5, 5));
        Lt(panel, UiKit.Track("INTEGRITY"), touchControls ? 10 : 7, new Vector2(24, -7), new Vector2(150, 12), UiKit.TextDim, FontStyle.Bold);
        healthText = Rt(panel, "100 / 100", touchControls ? 16 : 12, new Vector2(-12, -4), new Vector2(150, 18), UiKit.TextMain, FontStyle.Bold, displayFont);
        hpFill = Bar(panel, "Health fill", 14f, 14f, -29f, 8f, new Color(.16f, .07f, .1f, .85f), GameRules.Pink, new Color(1, 1, 1, .22f), out hpGlow, out hpGhost, 8);
        HLine(panel, "Divider", 14f, -43f, 1f, new Color(1, 1, 1, .07f));
        Art(panel, "Dash dot", UiKit.Diamond, UiKit.Alpha(GameRules.Cyan, .9f), AnchorTL, new Vector2(13, -49), new Vector2(5, 5));
        Lt(panel, UiKit.Track("DASH"), touchControls ? 10 : 7, new Vector2(24, -47), new Vector2(120, 12), UiKit.TextDim, FontStyle.Bold);
        dashText = Rt(panel, "READY", touchControls ? 13 : 9, new Vector2(-12, -45), new Vector2(140, 16), GameRules.Cyan, FontStyle.Bold);
        Image dashGlow;
        dashFill = Bar(panel, "Dash cooldown", 14f, 14f, -67f, 6f, new Color(.08f, .13f, .16f, .9f), GameRules.Cyan, new Color(1, 1, 1, .16f), out dashGlow, out _, 0);
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
        Texture2D texture = Resources.Load<Texture2D>("Art/" + key);
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
        var boss = Frame(hudRoot.transform, "Boss health", AnchorTC, new Vector2(touchControls ? 25f : 60f, -18), new Vector2(touchControls ? 520f : 700f, 88), GameRules.Pink, .95f);
        bossFrameEdge = boss.Find("Edge").GetComponent<Image>();
        var hex = Node(boss, "Boss portrait frame", AnchorTL, new Vector2(12, -10), new Vector2(68, 68));
        Art(hex, "Glow", UiKit.Glow, UiKit.Alpha(GameRules.Pink, .3f), new Vector2(0, 0), new Vector2(1, 1), AnchorCC, Vector2.zero, new Vector2(30, 30));
        Art(hex, "Hex fill", UiKit.HexFill, new Color(.015f, .025f, .04f, .97f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        bossPortrait = Art(hex, "Boss portrait", null, Color.white, new Vector2(0, 0), new Vector2(1, 1), AnchorCC, Vector2.zero, new Vector2(-8, -8));
        bossPortrait.preserveAspect = true;
        Art(hex, "Hex edge", UiKit.HexEdge, UiKit.Alpha(GameRules.Pink, .75f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        bossHeader = MakeText(boss, "Boss title", "THE DREAD WARDEN", 13, TextAnchor.MiddleLeft, AnchorTL, AnchorTL, AnchorTL, new Vector2(96, -11), new Vector2(touchControls ? 370 : 470, 22), GameRules.Pink, FontStyle.Bold, null);
        bossText = Lm(boss, "PHASE I  /  HUNTER", 10, new Vector2(96, -35), new Vector2(touchControls ? 330 : 430, 16), UiKit.TextDim);
        Rt(boss, UiKit.Track("ARENA 01"), 9, new Vector2(-18, -13), new Vector2(120, 14), UiKit.TextMute, FontStyle.Bold);
        bossFill = Bar(boss, "Boss health fill", 94f, 22f, -58f, 9f, new Color(.18f, .06f, .1f, .9f), GameRules.Pink, new Color(1, 1, 1, .25f), out bossGlow, out _, 4);
        bossPanel = boss.gameObject;
        bossPanel.SetActive(false);
    }

    private void BuildBanner()
    {
        bannerRoot = Node(hudRoot.transform, "Banner", AnchorTC, new Vector2(touchControls ? 45f : 60f, -122), new Vector2(touchControls ? 520f : 620f, 40));
        bannerGroup = bannerRoot.gameObject.AddComponent<CanvasGroup>();
        bannerGroup.alpha = 0f; bannerGroup.blocksRaycasts = false;
        Art(bannerRoot, "Backdrop", UiKit.Soft, new Color(.015f, .03f, .045f, .8f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        Art(bannerRoot, "Edge", UiKit.SoftEdge, new Color(1, 1, 1, .06f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        bannerAccent = Art(bannerRoot, "Accent", UiKit.Solid, GameRules.Cyan, new Vector2(0, 0), new Vector2(1, 0), new Vector2(.5f, 0), Vector2.zero, new Vector2(0, 2));
        bannerText = Mc(bannerRoot, "", 13, Color.white, FontStyle.Bold);
        bannerText.gameObject.name = "Banner text";
    }

    private void BuildHint()
    {
        hintText = Mc(hudRoot.transform, "WASD / 方向键  移动     空格  冲刺     自动攻击     C  角色     ESC  暂停", 10, UiKit.TextMute, FontStyle.Bold);
        hintText.rectTransform.anchorMin = new Vector2(.5f, 0); hintText.rectTransform.anchorMax = new Vector2(.5f, 0);
        if (touchControls) { hintText.text = "左侧摇杆移动     右侧点击冲刺     武器自动攻击"; hintText.fontSize = 12; }
        hintText.rectTransform.pivot = new Vector2(.5f, 0); hintText.rectTransform.anchoredPosition = new Vector2(touchControls ? 0f : 170f, 18);
        hintText.rectTransform.sizeDelta = new Vector2(touchControls ? 640f : 560f, 18);
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
        var root = Node(parent, name, anchor, pos, size);
        Art(root, "Shadow", UiKit.PanelFill, new Color(0, 0, 0, .5f), Vector2.zero, Vector2.one, AnchorCC, new Vector2(0, -6), new Vector2(16, 18));
        Art(root, "Body", UiKit.PanelFill, UiKit.Alpha(UiKit.Panel, fillAlpha), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        Art(root, "Edge", UiKit.PanelEdge, UiKit.Alpha(accent, .36f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        Art(root, "Top sheen", UiKit.GradientRight, new Color(1, 1, 1, .06f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(.5f, 1), new Vector2(0, -1), new Vector2(-26, 2));
        Bracket(root, "Bracket TL", AnchorTL, new Vector2(7, -7), 0f, accent, 18f);
        Bracket(root, "Bracket TR", AnchorTR, new Vector2(-7, -7), -90f, accent, 18f);
        Bracket(root, "Bracket BR", AnchorBR, new Vector2(-7, 7), 180f, accent, 18f);
        Bracket(root, "Bracket BL", AnchorBL, new Vector2(7, 7), 90f, accent, 18f);
        return root;
    }

    private void Bracket(RectTransform parent, string name, Vector2 anchor, Vector2 pos, float rotation, Color color, float length)
    {
        var image = Art(parent, name, UiKit.Bracket, UiKit.Alpha(color, .5f), anchor, pos, new Vector2(length, length));
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

    private Text MakeText(Transform parent, string name, string value, int size, TextAnchor align, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 dims, Color color, FontStyle style, Font font)
    {
        var text = NewUI<Text>(name, parent);
        var rect = text.rectTransform;
        rect.anchorMin = min; rect.anchorMax = max; rect.pivot = pivot; rect.anchoredPosition = pos; rect.sizeDelta = dims;
        text.text = value; text.font = font != null ? font : uiFont; text.fontSize = size; text.alignment = align; text.color = color; text.fontStyle = style;
        text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
        text.lineSpacing = 1.14f; text.raycastTarget = false;
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
        var label = Mc(root, caption, fontSize, filled ? UiKit.Ink : UiKit.TextMain, FontStyle.Bold);
        button.onClick.AddListener(action);
        var hover = root.gameObject.AddComponent<UiHover>();
        hover.body = root; hover.edge = edge;
        hover.edgeIdle = UiKit.Alpha(accent, filled ? .95f : .5f); hover.edgeHot = UiKit.Alpha(accent, 1f);
        hover.fill = body; hover.fillIdle = body.color; hover.fillHot = filled ? UiKit.Light(accent, .12f) : UiKit.Alpha(UiKit.Shade(accent, .45f), .95f);
        hover.label = label; hover.labelIdle = label.color; hover.labelHot = filled ? UiKit.Ink : Color.white;
        hover.hoverScale = 1.025f;
        return button;
    }

    // ---------------------------------------------------------- title screen
}

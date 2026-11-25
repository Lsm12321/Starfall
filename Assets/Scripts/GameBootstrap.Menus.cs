using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private const float MobilePageScale = 1.06f;

    private void ShowStatePanel(RunState state)
    {
        if (statePanel != null) Destroy(statePanel);
        State = state;
        statePanel = NewUI<Image>("State screen", overlay).gameObject; Stretch((RectTransform)statePanel.transform);
        var tint = state == RunState.Menu ? new Color(.012f, .02f, .032f, 1f) : state == RunState.Paused ? new Color(.01f, .018f, .03f, .82f) : new Color(.012f, .022f, .036f, .94f);
        statePanel.GetComponent<Image>().color = tint; statePanel.GetComponent<Image>().raycastTarget = true;
        if (state == RunState.Menu) { BuildMenu(); return; }
        BuildModal(state == RunState.Victory, state == RunState.Defeat);
    }

    // Follows the actual (safe-area fitted) viewport so phone menus never spill past the canvas.
    private float MobileMenuWidth => touchControls ? Mathf.Max(1440f, 810f * ViewportAspect) : 1440f;

    private void BuildMenu()
    {
        lastWave = 1;
        var panel = (RectTransform)statePanel.transform;
        Sprite illustration = SpriteResource("arena");
        if (illustration != null)
        {
            var art = Art(panel, "Title illustration", illustration, new Color(.62f, .78f, .88f, .95f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
            var fitter = art.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = illustration.rect.width / illustration.rect.height;
            if (touchControls) art.rectTransform.localScale = Vector3.one * 1.08f;
        }
        Art(panel, "Scrim base", UiKit.Solid, new Color(.012f, .02f, .032f, .96f), new Vector2(0, 0), new Vector2(.3f, 1), AnchorCL, Vector2.zero, Vector2.zero);
        Art(panel, "Scrim fade", UiKit.GradientLeft, new Color(.012f, .02f, .032f, .96f), new Vector2(.3f, 0), new Vector2(.82f, 1), AnchorCL, Vector2.zero, Vector2.zero);
        Art(panel, "Scrim top", UiKit.GradientUp, new Color(.012f, .02f, .032f, .82f), new Vector2(0, .74f), new Vector2(1, 1), AnchorCC, Vector2.zero, Vector2.zero);
        Art(panel, "Scrim bottom", UiKit.GradientDown, new Color(.006f, .012f, .02f, .96f), new Vector2(0, 0), new Vector2(1, .44f), AnchorCC, Vector2.zero, Vector2.zero);
        Art(panel, "Vignette", UiKit.Vignette, new Color(0, 0, 0, .52f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        Art(panel, "Scanlines", UiKit.Scanlines, new Color(0, 0, 0, .14f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        var grid = Art(panel, "Archive grid", UiKit.Grid, UiKit.Alpha(GameRules.Cyan, .018f), new Vector2(0, 0), new Vector2(.62f, 1), AnchorCC, Vector2.zero, Vector2.zero);
        grid.type = Image.Type.Tiled;
        BuildParticles(panel, 26);

        if (touchControls)
        {
            panel = Node(panel, "Phone menu layout", AnchorCC, Vector2.zero, new Vector2(MobileMenuWidth, 810));
            panel.localScale = Vector3.one * .96f;
        }

        var left = Node(panel, "Menu left", AnchorTL, Vector2.zero, new Vector2(1440, 810));
        left.localScale = Vector3.one * (touchControls ? 1.11f : 1.04f);
        float menuShift = touchControls ? 44f : 0f;

        Art(left, "Title glow", UiKit.Glow, UiKit.Alpha(GameRules.Cyan, .045f), AnchorTL, new Vector2(56, -96), new Vector2(720, 320));
        Art(left, "Kicker mark", UiKit.Diamond, UiKit.Alpha(GameRules.Cyan, .95f), AnchorTL, new Vector2(97, -106), new Vector2(8, 8));
        Lt(left, UiKit.Track("SURVIVOR FILE 001   ·   ASHEN GATE"), 10, new Vector2(114, -102), new Vector2(470, 16), UiKit.Alpha(GameRules.Cyan, .95f), FontStyle.Bold);
        var title = Lt(left, "STARFALL", 80, new Vector2(92, -128), new Vector2(640, 96), UiKit.TextMain, FontStyle.Bold, displayFont);
        GlowText(title, UiKit.Alpha(GameRules.Cyan, .18f), 1.5f);
        DropShadow(title, -4f);
        Lt(left, "星 陨 · 最 后 的 微 光", 23, new Vector2(96, -228 - menuShift), new Vector2(520, 34), GameRules.Gold, FontStyle.Bold);
        var rule = Art(left, "Rule", UiKit.GradientRight, UiKit.Alpha(GameRules.Cyan, .8f), AnchorTL, new Vector2(96, -272 - menuShift), new Vector2(300, 2));
        Art(left, "Rule dot", UiKit.Diamond, GameRules.Cyan, AnchorTL, new Vector2(88, -270 - menuShift), new Vector2(7, 7));
        Lt(left, "收集星尘，组合武器，穿过不断增强的敌潮，击败苏醒的遗迹守望者。", 15, new Vector2(96, -298 - menuShift), new Vector2(492, 74), UiKit.TextDim, FontStyle.Normal);
        var deploy = Cta(left, "Select pilot", "选择角色", AnchorTL, new Vector2(96, -392 - menuShift), new Vector2(344, 64), GameRules.Cyan, false, ShowCharacterSelect, 15);
        var deployHover = deploy.GetComponent<UiHover>();
        deployHover.fillHot = UiKit.Alpha(UiKit.Mix(UiKit.PanelSoft, GameRules.Cyan, .14f), .94f);
        deployHover.edgeHot = UiKit.Alpha(GameRules.Cyan, .8f);
        var engraving = Fill(deploy.transform, "Orbital engraving", UiKit.PilotButtonEngraving, UiKit.Rgb(0x76AEB9, .29f));
        engraving.rectTransform.SetAsFirstSibling();
        Art(deploy.transform, "Chevron", UiKit.Chevron, GameRules.Cyan, AnchorCR, new Vector2(-22, 0), new Vector2(10, 16));
        Lt(left, touchControls ? "左侧拖动  移动     右下按钮  冲刺     武器自动攻击" : "WASD / 方向键  移动     空格  冲刺     武器自动攻击", 12, new Vector2(96, -480 - menuShift), new Vector2(520, 20), UiKit.TextMute, FontStyle.Bold);
        MenuStat(left, "BEST KILLS", PlayerPrefs.GetInt("Starfall.BestKills", 0).ToString("000"), new Vector2(96, -522 - menuShift), GameRules.Cyan);
        MenuStat(left, "CLEARS", PlayerPrefs.GetInt("Starfall.Wins", 0).ToString("00"), new Vector2(286, -522 - menuShift), GameRules.Gold);
        AddSettingsButton(panel);
    }

    private void MenuStat(Transform parent, string label, string value, Vector2 pos, Color accent)
    {
        var chip = Node(parent, "Menu stat", AnchorTL, pos, new Vector2(176, 58));
        Art(chip, "Rule", UiKit.Solid, UiKit.Alpha(accent, .28f), AnchorTL, new Vector2(0, -4), new Vector2(176, 1));
        Art(chip, "Mark", UiKit.Diamond, UiKit.Alpha(accent, .9f), AnchorTL, new Vector2(12, -13), new Vector2(6, 6));
        Lt(chip, UiKit.Track(label), 9, new Vector2(26, -9), new Vector2(140, 14), UiKit.TextMute, FontStyle.Bold);
        Lt(chip, value, 22, new Vector2(12, -23), new Vector2(150, 30), UiKit.TextMain, FontStyle.Bold, displayFont);
    }

    private void BuildParticles(RectTransform parent, int count)
    {
        var rng = new System.Random(20261023);
        var root = Node(parent, "Dust", Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        for (int i = 0; i < count; i++)
        {
            float x = (float)rng.NextDouble(), y = (float)rng.NextDouble();
            float size = 2f + (float)rng.NextDouble() * 4f;
            float alpha = .1f + (float)rng.NextDouble() * .45f;
            bool warm = i % 5 == 0;
            var dot = Art(root, "Mote", UiKit.Dot, UiKit.Alpha(warm ? GameRules.Gold : GameRules.Cyan, alpha), new Vector2(x, y), Vector2.zero, new Vector2(size, size));
            var twinkle = dot.gameObject.AddComponent<UiTwinkle>();
            twinkle.speed = .3f + (float)rng.NextDouble() * .8f; twinkle.phase = (float)rng.NextDouble() * 6.28f; twinkle.min = .2f;
        }
    }

    private void BuildModal(bool victory, bool defeat)
    {
        var panel = (RectTransform)statePanel.transform;
        bool paused = State == RunState.Paused;
        Color accent = paused || victory ? GameRules.Cyan : defeat ? GameRules.Pink : GameRules.Gold;
        Art(panel, "Result scrim", UiKit.Solid, new Color(.004f, .009f, .016f, .9f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        Art(panel, "Vignette", UiKit.Vignette, new Color(0, 0, 0, .4f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        Art(panel, "Backdrop glow", UiKit.Glow, UiKit.Alpha(accent, paused ? .07f : .14f), AnchorCC, new Vector2(0, -10), new Vector2(1240, 820));

        if (touchControls)
        {
            panel = Node(panel, "Phone result layout", AnchorCC, Vector2.zero, new Vector2(1440, 810));
            panel.localScale = Vector3.one * MobilePageScale;
        }
        var modal = Frame(panel, "Modal", AnchorCC, Vector2.zero, new Vector2(1100, 640), accent, 1f);
        // GradientUp is opaque at the top and fades out, so the wash no longer leaves a hard band across the portrait.
        Art(modal, "Accent wash", UiKit.GradientUp, UiKit.Alpha(accent, victory ? .08f : .06f), new Vector2(0, .58f), new Vector2(1, 1), AnchorCC, Vector2.zero, Vector2.zero);

        Art(modal, "Emblem glow", UiKit.Glow, UiKit.Alpha(accent, paused ? .08f : .18f), AnchorTC, new Vector2(0, -4), new Vector2(220, 160));
        var emblem = Node(modal, "Emblem", AnchorTC, new Vector2(0, -14), new Vector2(50, 50));
        var ring = Art(emblem, "Ring", UiKit.Ring, UiKit.Alpha(accent, .9f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        ring.gameObject.AddComponent<UiSpin>().degreesPerSecond = 14f;
        Art(emblem, "Core", UiKit.Diamond, UiKit.Alpha(accent, .95f), AnchorCC, Vector2.zero, new Vector2(13, 13));
        Art(emblem, "Core inner", UiKit.Diamond, UiKit.Alpha(Color.white, .85f), AnchorCC, Vector2.zero, new Vector2(6, 6));

        Ct(modal, victory ? UiKit.Track("RUN COMPLETE  /  SECTOR CLEARED") : defeat ? UiKit.Track("RUN OVER  /  SIGNAL LOST") : UiKit.Track("暂停"), 11, new Vector2(0, -70), new Vector2(700, 16), accent, FontStyle.Bold);
        var title = Ct(modal, victory ? "THE NIGHT BREAKS" : defeat ? "SIGNAL LOST" : "PAUSED", 46, new Vector2(0, touchControls ? -84 : -90), new Vector2(900, touchControls ? 72 : 60), UiKit.TextMain, FontStyle.Bold, displayFont);
        GlowText(title, UiKit.Alpha(accent, .26f), 2f);
        Ct(modal, victory ? "三位守望者尽数陨落 · 航道已经打开" : defeat ? "灰烬之门吞没了又一名跑者" : "深呼吸，跑者", 15, new Vector2(0, touchControls ? -164 : -148), new Vector2(720, 22), UiKit.TextDim);
        HLine(modal, "Divider", 40f, touchControls ? -186f : -170f, 1f, new Color(1, 1, 1, .08f));

        var portraitPosition = new Vector2(30, touchControls ? -198 : -186);
        var portraitSize = paused ? new Vector2(224, 336) : new Vector2(200, 300);
        var portrait = Art(modal, "Result portrait", SpriteResource(CurrentCharacter.Portrait), Color.white, AnchorTL, portraitPosition, portraitSize);
        portrait.preserveAspect = true;
        Art(modal, "Result portrait frame", UiKit.Stroke, UiKit.Alpha(accent, .32f), AnchorTL, portraitPosition + new Vector2(-4, 4), portraitSize + new Vector2(8, 8));
        float nameY = portraitPosition.y - portraitSize.y - 10f;
        float nameHeight = (touchControls ? 40f : 30f) + (paused ? 6f : 0f);
        Lt(modal, CurrentCharacter.Name, paused ? 30 : 26, new Vector2(30, nameY), new Vector2(280, nameHeight), UiKit.TextMain, FontStyle.Bold);
        Lt(modal, CurrentCharacter.CallSign, 12, new Vector2(31, nameY - (touchControls ? 34f : 30f) - (paused ? 6f : 0f)), new Vector2(280, 20), accent, FontStyle.Bold);
        if (!paused)
        {
            string[] finalStats =
            {
                "伤害  ×" + Stats.Damage.ToString("0.00"),
                "攻速  ×" + Stats.Haste.ToString("0.00"),
                "暴击  " + Mathf.RoundToInt(Stats.Crit * 100) + "%",
                "暴伤  " + Mathf.RoundToInt(Stats.CritDamage * 100) + "%",
                "护甲  " + Mathf.RoundToInt(Mathf.Clamp(Stats.Armor, 0f, .5f) * 100) + "%",
                "生命  " + (Player != null ? Player.Health : 0) + " / " + Stats.MaxHealth,
            };
            for (int i = 0; i < finalStats.Length; i++)
                Lt(modal, finalStats[i], touchControls ? 14 : 12, new Vector2(30 + (i % 2) * 140, (touchControls ? -562 : -556) - (i / 2) * (touchControls ? 24 : 21)), new Vector2(138, 22), i % 2 == 0 ? UiKit.TextDim : UiKit.TextMain, FontStyle.Bold);
        }

        var stats = Node(modal, "Run stats", AnchorTL, new Vector2(330, touchControls ? -198 : -186), new Vector2(740, 84));
        if (paused)
        {
            PauseStatLine(stats, "用时", GameRules.TimeLabel(Elapsed), -270, accent);
            PauseStatLine(stats, "击破", Kills.ToString("000"), -90, accent);
            PauseStatLine(stats, "等级", Level.ToString("00"), 90, accent);
            PauseStatLine(stats, "首领", BossesDefeated + " / " + GameRules.BossCount, 270, accent);
        }
        else
        {
            StatTile(stats, "Time", "用时", GameRules.TimeLabel(Elapsed), -270, accent, 170);
            StatTile(stats, "Kills", "击破", Kills.ToString("000"), -90, accent, 170);
            StatTile(stats, "Level", "等级", Level.ToString("00"), 90, accent, 170);
            StatTile(stats, "Bosses", "首领", BossesDefeated + " / " + GameRules.BossCount, 270, accent, 170);
        }

        float totalDamage = 0f, maxDamage = 0f;
        for (int i = 0; i < 5; i++) { totalDamage += Stats.WeaponDamage[i]; maxDamage = Mathf.Max(maxDamage, Stats.WeaponDamage[i]); }
        Lt(modal, UiKit.Track("本局武器伤害"), 10, new Vector2(330, touchControls ? -294 : -282), new Vector2(420, 18), UiKit.TextMute, FontStyle.Bold);
        Rt(modal, "TOTAL  " + Mathf.RoundToInt(totalDamage).ToString("N0"), 10, new Vector2(-40, touchControls ? -295 : -283), new Vector2(300, 18), accent, FontStyle.Bold);

        for (int i = 0; i < 5; i++)
        {
            bool unlocked = Stats.Weapons[i] > 0;
            float damage = Stats.WeaponDamage[i];
            float ratio = maxDamage > 0f ? Mathf.Clamp01(damage / maxDamage) : 0f;
            float percent = totalDamage > 0f ? damage / totalDamage * 100f : 0f;
            var row = Node(modal, "Result weapon", AnchorTL, new Vector2(330, (touchControls ? -314 : -302) - i * 34), new Vector2(740, 30));
            if (!paused)
                Art(row, "Row", UiKit.SoftSmall, new Color(1, 1, 1, .025f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
            var icon = Art(row, "Icon", GetSprite("Icons/" + GameRules.IconAsset(i)), unlocked ? Color.white : new Color(1, 1, 1, .15f), AnchorTL, new Vector2(6, -4), new Vector2(22, 22));
            icon.preserveAspect = true;
            Lm(row, WeaponName(i), 14, new Vector2(34, 0), new Vector2(96, 28), unlocked ? UiKit.TextMain : UiKit.TextMute, FontStyle.Bold);
            Art(row, "Track", UiKit.Solid, new Color(1, 1, 1, .06f), AnchorTL, new Vector2(130, -13), new Vector2(370, 6));
            if (ratio > 0f)
                Art(row, "Fill", UiKit.Solid, UiKit.Alpha(WeaponTint(i), .9f), AnchorTL, new Vector2(130, -13), new Vector2(370f * ratio, 6));
            Rm(row, damage > 0f ? Mathf.RoundToInt(damage).ToString("N0") : "—", 12, new Vector2(-100, 0), new Vector2(110, 28), damage > 0f ? UiKit.TextMain : UiKit.TextMute, FontStyle.Bold);
            Rm(row, totalDamage > 0f ? percent.ToString("0.0") + "%" : string.Empty, 10, new Vector2(-10, 0), new Vector2(80, 28), UiKit.TextMute, FontStyle.Bold);
        }

        if (!paused)
        {
            int ownedWeapons = 0, evolvedWeapons = 0;
            for (int i = 0; i < 5; i++) { if (Stats.Weapons[i] > 0) ownedWeapons++; if (Stats.EvolvedWeapons[i]) evolvedWeapons++; }
            string summary = CurrentCharacter.Name + "  ·  武器 " + ownedWeapons + "/5  ·  进化 " + evolvedWeapons + "/5" +
                (victory ? "  ·  SECTOR CLEARED" : defeat ? "  ·  下次会走得更远" : string.Empty);
            Lt(modal, summary, 13, new Vector2(334, touchControls ? -488 : -476), new Vector2(736, 22), UiKit.TextMute, FontStyle.Bold).gameObject.name = "Result summary";
        }

        Button primaryAction = paused
            ? Cta(modal, "Resume", "继续探索", AnchorTL, new Vector2(590, touchControls ? -522 : -510), new Vector2(480, 54), accent, true, Resume, 17)
            : Cta(modal, "Play again", "再次探索", AnchorTL, new Vector2(590, touchControls ? -522 : -510), new Vector2(480, 54), accent, true, PlayAgain, 17);
        Cta(modal, "Back to title", "返回主菜单", AnchorTL, new Vector2(330, touchControls ? -522 : -510), new Vector2(240, 54), new Color(.45f, .57f, .64f), false, RestartRun, 15);
        primaryAction.transform.SetAsLastSibling();
    }

    private void StatTile(Transform parent, string name, string label, string value, float x, Color accent, float width = 168)
    {
        var tile = Node(parent, name, AnchorTC, new Vector2(x, 0), new Vector2(width, 90));
        Art(tile, "Fill", UiKit.Soft, new Color(1, 1, 1, .04f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        Fill(tile, "Edge", UiKit.SoftEdge, UiKit.Alpha(accent, .22f));
        Art(tile, "Mark", UiKit.Diamond, UiKit.Alpha(accent, .9f), AnchorTC, new Vector2(0, -10), new Vector2(6, 6));
        Ct(tile, label, 12, new Vector2(0, -19), new Vector2(width - 16, 18), UiKit.TextDim, FontStyle.Bold);
        Ct(tile, value, 30, new Vector2(0, -40), new Vector2(width - 16, 38), UiKit.TextMain, FontStyle.Bold, displayFont);
    }

    private void PauseStatLine(Transform parent, string label, string value, float x, Color accent)
    {
        var line = Node(parent, "Pause stat", AnchorTC, new Vector2(x, 0), new Vector2(170, 84));
        Art(line, "Rule", UiKit.Solid, UiKit.Alpha(accent, .3f), AnchorTC, new Vector2(0, -2), new Vector2(96, 1));
        Ct(line, label, 11, new Vector2(0, -14), new Vector2(160, 18), UiKit.TextDim, FontStyle.Bold);
        Ct(line, value, 28, new Vector2(0, -38), new Vector2(170, 34), UiKit.TextMain, FontStyle.Bold, displayFont);
    }

    // ------------------------------------------------------- pilot sheet
    private void OpenPilotSheet()
    {
        if (State != RunState.Playing || Player == null || deathSequence) return;
        State = RunState.CharacterSheet; Time.timeScale = 0f;
        // Update() skips RefreshUI while the sheet is open, so hide the transient HUD text here.
        if (bannerRoot != null) bannerRoot.gameObject.SetActive(false);
        if (hintText != null) hintText.enabled = false;
        if (sheetPanel != null) Destroy(sheetPanel);
        sheetPanel = NewUI<Image>("Pilot sheet", overlay).gameObject;
        Stretch((RectTransform)sheetPanel.transform);
        var background = sheetPanel.GetComponent<Image>();
        background.color = new Color(.01f, .018f, .03f, .87f); background.raycastTarget = true;
        var root = (RectTransform)sheetPanel.transform;
        Art(root, "Vignette", UiKit.Vignette, new Color(0, 0, 0, .6f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);

        var sheetRoot = Node(root, touchControls ? "Phone sheet layout" : "Sheet layout", AnchorCC, Vector2.zero, new Vector2(1440, 810));
        sheetRoot.localScale = Vector3.one * 1.10f;

        Color accent = CurrentCharacter.Accent;
        var card = Frame(sheetRoot, "Pilot sheet frame", AnchorCC, Vector2.zero, new Vector2(1000, 620), accent, .97f);
        Lt(card, UiKit.Track("作战状态"), 10, new Vector2(30, -22), new Vector2(420, 16), UiKit.Alpha(accent, .95f), FontStyle.Bold);
        var close = Node(card, "Close pilot sheet", AnchorTR, new Vector2(-24f, -20f), new Vector2(42f, 42f));
        var closeBody = Art(close, "Hit area", UiKit.Solid, new Color(1f, 1f, 1f, .001f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        closeBody.raycastTarget = true;
        var closeButton = close.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = closeBody; closeButton.transition = Selectable.Transition.None;
        closeButton.onClick.AddListener(ClosePilotSheet);
        var x1 = Art(close, "X 1", UiKit.Solid, UiKit.TextMain, AnchorCC, Vector2.zero, new Vector2(2f, 20f));
        var x2 = Art(close, "X 2", UiKit.Solid, UiKit.TextMain, AnchorCC, Vector2.zero, new Vector2(2f, 20f));
        x1.rectTransform.localRotation = Quaternion.Euler(0, 0, 45f);
        x2.rectTransform.localRotation = Quaternion.Euler(0, 0, -45f);

        var portrait = Art(card, "Sheet portrait", SpriteResource(CurrentCharacter.Portrait), Color.white, AnchorTL, new Vector2(36, -64), new Vector2(280, 420));
        portrait.preserveAspect = true;
        Art(card, "Sheet portrait frame", UiKit.Stroke, UiKit.Alpha(accent, .35f), AnchorTL, new Vector2(32, -60), new Vector2(288, 428));
        float hpRatio = Player != null ? Mathf.Clamp01(Player.Health / (float)Stats.MaxHealth) : 0f;
        Art(card, "HP track", UiKit.Solid, new Color(0, 0, 0, .55f), AnchorTL, new Vector2(44, -492), new Vector2(264, 5));
        Art(card, "HP fill", UiKit.Solid, UiKit.Alpha(accent, .92f), AnchorTL, new Vector2(44, -492), new Vector2(264f * hpRatio, 5));
        Lt(card, "HP  " + (Player != null ? Player.Health : 0) + " / " + Stats.MaxHealth, 9, new Vector2(44, -502), new Vector2(264, 14), UiKit.TextDim, FontStyle.Bold);
        Lt(card, "LV " + Level, 10, new Vector2(238, -76), new Vector2(68, 20), accent, FontStyle.Bold);
        Lt(card, CurrentCharacter.Name, 26, new Vector2(36, -532), new Vector2(300, 32), UiKit.TextMain, FontStyle.Bold);
        Lt(card, CurrentCharacter.CallSign, 11, new Vector2(37, -568), new Vector2(300, 16), accent, FontStyle.Bold);
        Lt(card, "时间 " + GameRules.TimeLabel(Elapsed) + "      等级 " + Level + "      击杀 " + Kills, 11, new Vector2(36, -594), new Vector2(300, 16), UiKit.TextDim, FontStyle.Bold);

        Art(card, "Sheet divider", UiKit.Solid, new Color(1, 1, 1, .06f), AnchorTL, new Vector2(338, -70), new Vector2(1, 470));
        var viewport = Node(card, "Sheet viewport", AnchorTL, new Vector2(356, -62), new Vector2(612, 500));
        var viewportBg = viewport.gameObject.AddComponent<Image>();
        viewportBg.color = new Color(0, 0, 0, 0); viewportBg.raycastTarget = true;
        viewport.gameObject.AddComponent<RectMask2D>();
        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f; scroll.inertia = true; scroll.decelerationRate = .12f;
        var content = Node(viewport, "Sheet content", AnchorTL, Vector2.zero, new Vector2(612, 520));
        content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(.5f, 1); content.anchoredPosition = Vector2.zero;
        scroll.viewport = viewport; scroll.content = content;
        var stats = new[]
        {
            new KeyValuePair<string, string>("伤害", "×" + Stats.Damage.ToString("0.00")),
            new KeyValuePair<string, string>("攻击速度", "×" + Stats.Haste.ToString("0.00")),
            new KeyValuePair<string, string>("移动速度", Stats.Speed.ToString("0.00")),
            new KeyValuePair<string, string>("暴击率", Mathf.RoundToInt(Stats.Crit * 100) + "%"),
            new KeyValuePair<string, string>("暴击伤害", Mathf.RoundToInt(Stats.CritDamage * 100) + "%"),
            new KeyValuePair<string, string>("护甲", Mathf.RoundToInt(Mathf.Clamp(Stats.Armor, 0f, .5f) * 100) + "%"),
            new KeyValuePair<string, string>("每秒回复", Stats.Regeneration.ToString("0.0")),
            new KeyValuePair<string, string>("拾取范围", Stats.Magnet.ToString("0.0") + " 米"),
            new KeyValuePair<string, string>("穿透", Stats.Pierce.ToString()),
            new KeyValuePair<string, string>("生命", Player.Health + " / " + Stats.MaxHealth),
        };
        for (int i = 0; i < stats.Length; i++)
        {
            float x = (i % 5) * 122f, y = -(i / 5) * 72f;
            var tile = Node(content, "Sheet stat", AnchorTL, new Vector2(x, y), new Vector2(114, 64));
            Art(tile, "Rule", UiKit.Solid, UiKit.Alpha(accent, .28f), AnchorTC, new Vector2(0, -2), new Vector2(76, 1));
            Ct(tile, stats[i].Key, 9, new Vector2(0, -10), new Vector2(108, 14), UiKit.TextMute, FontStyle.Bold);
            Ct(tile, stats[i].Value, 18, new Vector2(0, -30), new Vector2(108, 26), UiKit.TextMain, FontStyle.Bold, displayFont);
        }

        Art(content, "Weapons mark", UiKit.Solid, UiKit.Alpha(accent, .8f), AnchorTL, new Vector2(2, -152), new Vector2(3, 12));
        Lt(content, UiKit.Track("武器"), 9, new Vector2(12, -152), new Vector2(300, 14), UiKit.TextMute, FontStyle.Bold);
        HLine(content, "Weapons rule", 2f, -168f, 1f, new Color(1, 1, 1, .05f));
        for (int i = 0; i < 5; i++)
        {
            int level = Stats.Weapons[i]; bool unlocked = level > 0; bool evolved = Stats.EvolvedWeapons[i];
            var row = Node(content, "Sheet weapon", AnchorTL, new Vector2(0, -172 - i * 34), new Vector2(612, 30));
            Art(row, "Rule", UiKit.Solid, new Color(1, 1, 1, .055f), new Vector2(0, 0), new Vector2(1, 0), new Vector2(.5f, 0), Vector2.zero, new Vector2(0, 1));
            var icon = Art(row, "Icon", GetSprite("Icons/" + GameRules.IconAsset(i)), unlocked ? (evolved ? Color.Lerp(Color.white, GameRules.Gold, .25f) : Color.white) : new Color(1, 1, 1, .18f), AnchorTL, new Vector2(6, -4), new Vector2(22, 22));
            icon.preserveAspect = true;
            Lm(row, unlocked ? WeaponName(i) : "—  未解锁", 12, new Vector2(36, 0), new Vector2(110, 26), unlocked ? (evolved ? GameRules.Gold : UiKit.TextMain) : UiKit.TextMute, FontStyle.Bold);
            if (unlocked)
            {
                Rm(row, "Lv " + level + "/" + GameRules.MaxWeaponRank, 9, new Vector2(-8, 0), new Vector2(70, 26), UiKit.TextMain, FontStyle.Bold);
                if (evolved)
                {
                    var evo = Node(row, "Evo", AnchorTL, new Vector2(150, -7), new Vector2(40, 16));
                    Art(evo, "Fill", UiKit.SoftSmall, new Color(1, 1, 1, .05f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
                    Fill(evo, "Edge", UiKit.SoftSmallEdge, UiKit.Alpha(GameRules.Gold, .5f));
                    Mc(evo, "已进化", 8, GameRules.Gold, FontStyle.Bold);
                }
            }
        }

        Art(content, "Bonuses mark", UiKit.Solid, UiKit.Alpha(accent, .8f), AnchorTL, new Vector2(2, -348), new Vector2(3, 12));
        Lt(content, UiKit.Track("当前加成"), 9, new Vector2(12, -348), new Vector2(300, 14), UiKit.TextMute, FontStyle.Bold);
        HLine(content, "Bonuses rule", 2f, -364f, 1f, new Color(1, 1, 1, .05f));
        var bonuses = new List<string> { "角色被动  " + CurrentCharacter.Passive };
        if (bossRewardsClaimed[0]) bonuses.Add("超载核心  最终伤害 +30% · 最终攻速 +20% · 暴击率 +20%");
        if (bossRewardsClaimed[1]) bonuses.Add("凤凰协议  生命 +30 · 护甲 +10% · 回复 +2 · 受击 30% 免疫");
        if (bossRewardsClaimed[2]) bonuses.Add("星核共鸣  暴击 +30% · 暴击伤害 +50%");
        if (Stats.BoostDamageMultiplier > 1.001f) bonuses.Add("武装补强  伤害 ×" + Stats.BoostDamageMultiplier.ToString("0.00"));
        if (Stats.BoostHasteMultiplier > 1.001f) bonuses.Add("武装补强  攻速 ×" + Stats.BoostHasteMultiplier.ToString("0.00"));
        bonuses.Add("属性加成  伤害 +" + Mathf.RoundToInt(Stats.DamageBonus * 100) + "% · 攻速 +" + Mathf.RoundToInt(Stats.HasteBonus * 100) + "% · 移速 +" + Mathf.RoundToInt(Stats.SpeedBonus * 100) + "%");
        int ownedWeapons = 0, evolvedWeapons = 0;
        for (int w = 0; w < 5; w++) { if (Stats.Weapons[w] > 0) ownedWeapons++; if (Stats.EvolvedWeapons[w]) evolvedWeapons++; }
        if (evolvedWeapons > 0)
        {
            string[] shortNames = { "脉冲", "散射", "光刃", "电弧", "冰枪" };
            var evolvedNames = new List<string>();
            for (int w = 0; w < 5; w++) if (Stats.EvolvedWeapons[w]) evolvedNames.Add(shortNames[w]);
            bonuses.Add("已进化  " + string.Join("、", evolvedNames));
        }
        bonuses.Add("武器 " + ownedWeapons + " / 5 · 已进化 " + evolvedWeapons + " 件");
        for (int i = 0; i < bonuses.Count; i++)
        {
            Art(content, "Bonus bullet", UiKit.Diamond, UiKit.Alpha(accent, .8f), AnchorTL, new Vector2(3, -361 - i * 19), new Vector2(5, 5));
            Lt(content, bonuses[i], 10, new Vector2(16, -366 - i * 19), new Vector2(588, 18), i == 0 ? UiKit.TextDim : UiKit.TextMain);
        }

        float contentHeight = Mathf.Max(500f, 366f + Mathf.Max(0, bonuses.Count - 1) * 19f + 44f);
        content.sizeDelta = new Vector2(0, contentHeight);
        if (contentHeight > 500f)
        {
            var barRect = Node(card, "Sheet scrollbar", AnchorTL, new Vector2(974, -66), new Vector2(5, 492));
            var barBg = barRect.gameObject.AddComponent<Image>();
            barBg.color = new Color(1, 1, 1, .05f); barBg.raycastTarget = true;
            var bar = barRect.gameObject.AddComponent<Scrollbar>();
            var handleRect = Node(barRect, "Handle", AnchorTL, Vector2.zero, new Vector2(5, 80));
            var handle = handleRect.gameObject.AddComponent<Image>();
            handle.color = UiKit.Alpha(accent, .55f);
            bar.handleRect = handleRect; bar.targetGraphic = handle;
            bar.direction = Scrollbar.Direction.BottomToTop; bar.size = .55f;
            scroll.verticalScrollbar = bar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            Rt(card, "滚轮 / 拖动查看全部加成", 9, new Vector2(-36, -584), new Vector2(260, 14), UiKit.TextMute, FontStyle.Bold);
        }
    }

    private void ClosePilotSheet()
    {
        if (State != RunState.CharacterSheet) return;
        if (sheetPanel != null) { Destroy(sheetPanel); sheetPanel = null; }
        State = RunState.Playing; Time.timeScale = 1f;
    }

    // --------------------------------------------------------- pilot archive
    private void ShowCharacterSelect()
    {
        if (statePanel != null) Destroy(statePanel);
        State = RunState.CharacterSelect;
        statePanel = NewUI<Image>("Pilot selection", overlay).gameObject;
        Stretch((RectTransform)statePanel.transform);
        statePanel.GetComponent<Image>().color = UiKit.Ink;
        var panel = (RectTransform)statePanel.transform;

        Sprite backdrop = SpriteResource("arena");
        if (backdrop != null)
        {
            var art = Art(panel, "Archive backdrop", backdrop, new Color(.22f, .36f, .46f, .2f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
            var fitter = art.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = backdrop.rect.width / backdrop.rect.height;
        }
        Art(panel, "Wash", UiKit.Solid, new Color(.012f, .024f, .038f, .96f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        var grid = Art(panel, "Grid", UiKit.Grid, UiKit.Alpha(GameRules.Cyan, .016f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero); grid.type = Image.Type.Tiled;
        Art(panel, "Vignette", UiKit.Vignette, new Color(0, 0, 0, .44f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        Art(panel, "Scanlines", UiKit.Scanlines, new Color(0, 0, 0, .08f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);

        var archive = Node(panel, "Phone archive layout", AnchorCC, new Vector2(0f, touchControls ? 0f : 26f), new Vector2(1440, touchControls ? 810f / MobilePageScale : 810f));
        if (touchControls) archive.localScale = Vector3.one * MobilePageScale;
        panel = archive;

        Art(panel, "Header halo", UiKit.Glow, UiKit.Alpha(GameRules.Cyan, .10f), AnchorTL, new Vector2(240, -40), new Vector2(640, 240));
        Art(panel, "Head mark", UiKit.Diamond, UiKit.Alpha(GameRules.Cyan, .95f), AnchorTL, new Vector2(58, -38), new Vector2(7, 7));
        Lt(panel, UiKit.Track("PILOT ARCHIVE"), 11, new Vector2(74, -34), new Vector2(560, 18), UiKit.Alpha(GameRules.Cyan, .95f), FontStyle.Bold);
        Art(panel, "Title accent", UiKit.Solid, GameRules.Cyan, AnchorTL, new Vector2(48, -54), new Vector2(3, 44));
        Lt(panel, "选择你的共鸣者", 42, new Vector2(60, -50), new Vector2(touchControls ? 780 : 620, touchControls ? 72 : 56), UiKit.TextMain, FontStyle.Bold);
        float headerShift = touchControls ? 22f : 0f;
        characterFeatureIndex = Rt(panel, "01 / 05", 30, new Vector2(-66, -40), new Vector2(150, 38), GameRules.Cyan, FontStyle.Bold, displayFont);
        Rt(panel, UiKit.Track("ACTIVE FILE"), 10, new Vector2(-66, -76), new Vector2(140, 16), UiKit.TextMute, FontStyle.Bold);
        HLine(panel, "Header divider", 56f, -136f - headerShift, 1f, new Color(1, 1, 1, .08f));

        var feature = Node(panel, "Featured pilot", AnchorTL, new Vector2(48, -148 - headerShift), new Vector2(452, 590));
        // The portrait is the visual anchor. Keep one editorial rule and a soft wash instead of a card frame.
        characterFeatureEdge = Art(feature, "Edge", UiKit.Solid, UiKit.Alpha(CurrentCharacter.Accent, .58f), Vector2.zero, new Vector2(0, 1), new Vector2(0, .5f), Vector2.zero, new Vector2(3, -128));
        characterFeatureGlow = Art(feature, "Portrait glow", UiKit.Glow, UiKit.Alpha(CurrentCharacter.Accent, .08f), AnchorTC, new Vector2(0, -16), new Vector2(430, 470));
        characterFeaturePortrait = Art(feature, "Featured portrait", null, Color.white, AnchorTC, new Vector2(0, -16), new Vector2(320, 420));
        characterFeaturePortrait.preserveAspect = true;
        Art(feature, "Portrait scrim", UiKit.GradientDown, new Color(.012f, .02f, .032f, .86f), AnchorTC, new Vector2(0, -352), new Vector2(320, 86));

        characterFeatureName = Lt(feature, "", 34, new Vector2(26, -442), new Vector2(400, 50), UiKit.TextMain, FontStyle.Bold);
        characterFeatureCallSign = Lt(feature, "", 13, new Vector2(27, -494), new Vector2(400, 20), GameRules.Cyan, FontStyle.Bold);
        HLine(feature, "Divider", 26f, -514f, 1f, new Color(1, 1, 1, .07f));
        for (int s = 0; s < 3; s++)
        {
            float y = -522f - s * 22f;
            var row = Node(feature, "Stat row", AnchorTL, new Vector2(26, y), new Vector2(400, 20));
            Lt(row, new[] { "生存", "机动", "输出" }[s], touchControls ? 13 : 11, new Vector2(0, touchControls ? 2 : 3), new Vector2(58, 20), UiKit.TextDim);
            for (int p = 0; p < 5; p++)
                characterStatSegments[s, p] = Art(row, "Segment", UiKit.Solid, new Color(1, 1, 1, .08f), AnchorTL, new Vector2(64 + p * 36f, 6), new Vector2(30, 8));
            characterStatValues[s] = Rm(row, "2 / 5", 10, new Vector2(-4, 0), new Vector2(50, 20), UiKit.TextMute, FontStyle.Bold);
        }

        var dossier = Node(panel, "Pilot dossier", AnchorTL, new Vector2(516, -148 - headerShift), new Vector2(876, 304));
        Art(dossier, "Dossier rule", UiKit.Solid, new Color(1, 1, 1, .16f), Vector2.zero, new Vector2(0, 1), new Vector2(0, .5f), Vector2.zero, new Vector2(3, -4));
        // Phone body copy is set one step larger so the dossier stays readable at arm's length.
        int bodySize = touchControls ? 16 : 13, detailSize = touchControls ? 15 : 12;
        characterFeatureQuote = Lt(dossier, "", touchControls ? 24 : 21, new Vector2(28, -20), new Vector2(800, 32), GameRules.Cyan);
        characterFeatureStory = Lt(dossier, "", bodySize, new Vector2(28, -58), new Vector2(810, 54), UiKit.TextDim);
        HLine(dossier, "Divider", 26f, -118f, 1f, new Color(1, 1, 1, .07f));
        var weaponBox = Node(dossier, "Weapon dossier", AnchorTL, new Vector2(24, -104), new Vector2(402, touchControls ? 172 : 160));
        Art(weaponBox, "Rule", UiKit.Solid, new Color(1, 1, 1, .10f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(.5f, 1), new Vector2(0, -2), new Vector2(0, 1));
        var weaponChip = Node(weaponBox, "Weapon icon frame", AnchorTL, new Vector2(12, -12), new Vector2(42, 42));
        Art(weaponChip, "Fill", UiKit.SoftSmall, new Color(.02f, .05f, .07f, .95f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        Fill(weaponChip, "Edge", UiKit.SoftSmallEdge, new Color(1, 1, 1, .12f));
        characterFeatureWeaponIcon = Art(weaponChip, "Weapon icon", null, Color.white, new Vector2(0, 0), new Vector2(1, 1), AnchorCC, Vector2.zero, new Vector2(-12, -12));
        characterFeatureWeaponIcon.preserveAspect = true;
        Lt(weaponBox, UiKit.Track("本命武器"), 9, new Vector2(64, -11), new Vector2(320, 16), UiKit.TextMute, FontStyle.Bold);
        characterFeatureWeapon = Lt(weaponBox, "", 17, new Vector2(64, -26), new Vector2(320, 24), UiKit.TextMain, FontStyle.Bold);
        characterFeatureWeaponDesc = Lt(weaponBox, "", detailSize, new Vector2(14, -54), new Vector2(376, touchControls ? 108 : 88), UiKit.TextDim);

        var passiveBox = Node(dossier, "Passive dossier", AnchorTL, new Vector2(444, -104), new Vector2(402, touchControls ? 172 : 160));
        Art(passiveBox, "Rule", UiKit.Solid, new Color(1, 1, 1, .10f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(.5f, 1), new Vector2(0, -2), new Vector2(0, 1));
        var passiveChip = Node(passiveBox, "Passive icon frame", AnchorTL, new Vector2(12, -12), new Vector2(42, 42));
        Art(passiveChip, "Fill", UiKit.SoftSmall, new Color(.02f, .05f, .07f, .95f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        Fill(passiveChip, "Edge", UiKit.SoftSmallEdge, new Color(1, 1, 1, .12f));
        Art(passiveChip, "Passive glyph", UiKit.Diamond, Color.white, new Vector2(.5f, .5f), new Vector2(.5f, .5f), AnchorCC, Vector2.zero, new Vector2(18, 18));
        Lt(passiveBox, UiKit.Track("专属被动"), 9, new Vector2(64, -11), new Vector2(320, 16), UiKit.TextMute, FontStyle.Bold);
        characterFeaturePassive = Lt(passiveBox, "", 17, new Vector2(64, -26), new Vector2(320, 24), UiKit.TextMain, FontStyle.Bold);
        characterFeaturePassiveDesc = Lt(passiveBox, "", detailSize, new Vector2(14, -54), new Vector2(376, touchControls ? 108 : 88), UiKit.TextDim);

        for (int i = 0; i < CharacterRoster.All.Length; i++)
        {
            int index = i; CharacterProfile profile = CharacterRoster.All[i];
            var card = Node(panel, "Character card", AnchorBL, AnchorBL, AnchorCC, new Vector2(596 + i * 179f, touchControls ? 70f : 196f), new Vector2(160, 266));
            var button = card.gameObject.AddComponent<Button>();
            var body = card.gameObject.AddComponent<Image>(); body.sprite = UiKit.Solid;
            body.color = new Color(1, 1, 1, .002f); body.raycastTarget = true;
            button.targetGraphic = body; button.transition = Selectable.Transition.None; button.onClick.AddListener(() => SelectCharacter(index));
            characterButtons[i] = button; characterCardBodies[i] = body;
            var glow = Fill(card, "Glow", UiKit.Glow, UiKit.Alpha(profile.Accent, 0f), Vector2.zero, new Vector2(8, 8)); characterCardGlows[i] = glow;
            var edge = Art(card, "Edge", UiKit.Solid, new Color(1, 1, 1, .045f), new Vector2(0, 0), new Vector2(1, 0), new Vector2(.5f, 0), Vector2.zero, new Vector2(-20, 1)); characterCardEdges[i] = edge;
            var bar = Art(card, "Accent bar", UiKit.Solid, new Color(1, 1, 1, .12f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(.5f, 1), new Vector2(0, 0), new Vector2(-20, 2)); characterCardBars[i] = bar;
            var portrait = Art(card, "Pilot portrait", SpriteResource(profile.Portrait), Color.white, new Vector2(0, 1), new Vector2(1, 1), new Vector2(.5f, 1), new Vector2(0, -5), new Vector2(-14, 214));
            portrait.preserveAspect = true;
            Art(card, "Portrait scrim", UiKit.GradientDown, new Color(.012f, .02f, .032f, .62f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(.5f, 1), new Vector2(0, -179), new Vector2(-14, 40));
            var chip = Node(card, "Index chip", AnchorTL, new Vector2(8, -8), new Vector2(26, 18));
            Art(chip, "Rule", UiKit.Solid, new Color(1, 1, 1, .18f), new Vector2(0, 0), new Vector2(1, 0), new Vector2(.5f, 0), Vector2.zero, new Vector2(0, 1));
            characterCardIndices[i] = Mc(chip, (i + 1).ToString("00"), 9, UiKit.TextDim, FontStyle.Bold);
            Lt(card, profile.Name, touchControls ? 18 : 15, new Vector2(9, touchControls ? -208 : -222), new Vector2(142, 28), UiKit.TextMain, FontStyle.Bold);
            Lt(card, profile.Weapon, touchControls ? 14 : 11, new Vector2(9, -248), new Vector2(142, 22), profile.Accent, FontStyle.Bold);
            var hover = card.gameObject.AddComponent<UiHover>();
            hover.body = card; hover.edge = edge; hover.edgeIdle = edge.color; hover.edgeHot = profile.Accent; hover.hoverScale = 1.03f;
        }

        Cta(panel, "Deploy pilot", "以当前角色出击", AnchorBR, new Vector2(-48, 16), new Vector2(300, 52), GameRules.Cyan, true, StartSelectedRun, 17);
        Cta(panel, "Back to title", "返回标题", AnchorBR, new Vector2(-366, 16), new Vector2(190, 44), new Color(.45f, .62f, .69f), false, () => ShowStatePanel(RunState.Menu), 14);

        UpdateCharacterSelection();
    }

    private void AddSettingsButton(Transform parent)
    {
        float size = touchControls ? 68f : 48f;
        var button = Cta(parent, "Settings", null, AnchorTR, new Vector2(-28, touchControls ? -28 : -24),
            Vector2.one * size, GameRules.Cyan, false, OpenSettings);
        Art(button.transform, "Settings gear", GetSprite("Icons/settings-gear"), UiKit.TextMain,
            AnchorCC, Vector2.zero, Vector2.one * (touchControls ? 30f : 24f));
    }

    // -------------------------------------------------------------- upgrades
}

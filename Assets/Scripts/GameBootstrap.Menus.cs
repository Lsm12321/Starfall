using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
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
        }
        Art(panel, "Scrim base", UiKit.Solid, new Color(.012f, .02f, .032f, .96f), new Vector2(0, 0), new Vector2(.3f, 1), AnchorCL, Vector2.zero, Vector2.zero);
        Art(panel, "Scrim fade", UiKit.GradientLeft, new Color(.012f, .02f, .032f, .96f), new Vector2(.3f, 0), new Vector2(.82f, 1), AnchorCL, Vector2.zero, Vector2.zero);
        Art(panel, "Scrim top", UiKit.GradientUp, new Color(.012f, .02f, .032f, .82f), new Vector2(0, .74f), new Vector2(1, 1), AnchorCC, Vector2.zero, Vector2.zero);
        Art(panel, "Scrim bottom", UiKit.GradientDown, new Color(.006f, .012f, .02f, .96f), new Vector2(0, 0), new Vector2(1, .44f), AnchorCC, Vector2.zero, Vector2.zero);
        Art(panel, "Vignette", UiKit.Vignette, new Color(0, 0, 0, .72f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        Art(panel, "Scanlines", UiKit.Scanlines, new Color(0, 0, 0, .45f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        var grid = Art(panel, "Archive grid", UiKit.Grid, UiKit.Alpha(GameRules.Cyan, .035f), new Vector2(0, 0), new Vector2(.62f, 1), AnchorCC, Vector2.zero, Vector2.zero);
        grid.type = Image.Type.Tiled;
        BuildParticles(panel, 46);

        if (touchControls)
        {
            panel = Node(panel, "Phone menu layout", AnchorCC, Vector2.zero, new Vector2(1440, 810));
            panel.localScale = Vector3.one * .86f;
        }

        Art(panel, "Title glow", UiKit.Glow, UiKit.Alpha(GameRules.Cyan, .13f), AnchorTL, new Vector2(56, -96), new Vector2(720, 320));
        Art(panel, "Kicker mark", UiKit.Diamond, UiKit.Alpha(GameRules.Cyan, .95f), AnchorTL, new Vector2(97, -106), new Vector2(8, 8));
        Lt(panel, UiKit.Track("SURVIVOR FILE 001   ·   ASHEN GATE"), 10, new Vector2(114, -102), new Vector2(470, 16), UiKit.Alpha(GameRules.Cyan, .95f), FontStyle.Bold);
        var title = Lt(panel, "STARFALL", 80, new Vector2(92, -128), new Vector2(640, 96), UiKit.TextMain, FontStyle.Bold, displayFont);
        GlowText(title, UiKit.Alpha(GameRules.Cyan, .18f), 1.5f);
        DropShadow(title, -4f);
        Lt(panel, "星 陨 · 最 后 的 微 光", 23, new Vector2(96, -228), new Vector2(520, 34), GameRules.Gold, FontStyle.Bold);
        var rule = Art(panel, "Rule", UiKit.GradientRight, UiKit.Alpha(GameRules.Cyan, .8f), AnchorTL, new Vector2(96, -272), new Vector2(300, 2));
        Art(panel, "Rule dot", UiKit.Diamond, GameRules.Cyan, AnchorTL, new Vector2(88, -270), new Vector2(7, 7));
        Lt(panel, "收集星尘，组合武器，穿过不断增强的敌潮，击败苏醒的遗迹守望者。", 15, new Vector2(96, -298), new Vector2(492, 74), UiKit.TextDim, FontStyle.Normal);
        var deploy = Cta(panel, "Select pilot", "选择驾驶员  /  SELECT PILOT", AnchorTL, new Vector2(96, -392), new Vector2(344, 64), GameRules.Cyan, false, ShowCharacterSelect, 15);
        Art(deploy.transform, "Chevron", UiKit.Chevron, GameRules.Cyan, AnchorCR, new Vector2(-22, 0), new Vector2(10, 16));
        Lt(panel, UiKit.Track(touchControls ? "LEFT PAD  MOVE     DASH  BUTTON     AUTO ATTACK" : "WASD / ARROWS  MOVE     SPACE  DASH     AUTO ATTACK"), 10, new Vector2(96, -482), new Vector2(520, 18), UiKit.TextMute, FontStyle.Bold);
        MenuStat(panel, "BEST KILLS", PlayerPrefs.GetInt("Starfall.BestKills", 0).ToString("000"), new Vector2(96, -522), GameRules.Cyan);
        MenuStat(panel, "CLEARS", PlayerPrefs.GetInt("Starfall.Wins", 0).ToString("00"), new Vector2(286, -522), GameRules.Gold);
        Lt(panel, "© 2026 STARFALL STUDIO   ·   BUILD 1.0.0", 9, new Vector2(96, -742), new Vector2(420, 14), new Color(1, 1, 1, .22f), FontStyle.Normal);
        Rt(panel, touchControls ? "STARFALL STUDIO  /  MOBILE" : "STARFALL STUDIO  /  UNITY 6", 9, new Vector2(-26, -26), new Vector2(300, 14), new Color(1, 1, 1, .28f), FontStyle.Bold);
        AddUtilityButtons(panel);
    }

    private void MenuStat(Transform parent, string label, string value, Vector2 pos, Color accent)
    {
        var chip = Node(parent, "Menu stat", AnchorTL, pos, new Vector2(176, 58));
        Art(chip, "Fill", UiKit.Soft, new Color(1, 1, 1, .045f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        Fill(chip, "Edge", UiKit.SoftEdge, UiKit.Alpha(accent, .18f));
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
        Color accent = victory ? GameRules.Cyan : defeat ? GameRules.Pink : GameRules.Gold;
        Art(panel, "Result scrim", UiKit.Solid, new Color(.004f, .009f, .016f, .9f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        Art(panel, "Vignette", UiKit.Vignette, new Color(0, 0, 0, .55f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        Art(panel, "Backdrop glow", UiKit.Glow, UiKit.Alpha(accent, .22f), AnchorCC, new Vector2(0, -10), new Vector2(1240, 820));

        if (touchControls)
        {
            panel = Node(panel, "Phone result layout", AnchorCC, Vector2.zero, new Vector2(1440, 810));
            panel.localScale = Vector3.one * .86f;
        }
        Art(panel, "Modal backing", UiKit.Soft, new Color(.026f, .047f, .067f, 1f), AnchorCC, Vector2.zero, new Vector2(1098, 638));
        var modal = Frame(panel, "Modal", AnchorCC, Vector2.zero, new Vector2(1100, 640), accent, 1f);
        Art(modal, "Accent wash", UiKit.GradientDown, UiKit.Alpha(accent, victory ? .10f : .08f), new Vector2(0, .58f), new Vector2(1, 1), AnchorCC, Vector2.zero, Vector2.zero);

        Art(modal, "Emblem glow", UiKit.Glow, UiKit.Alpha(accent, .28f), AnchorTC, new Vector2(0, -4), new Vector2(220, 160));
        var emblem = Node(modal, "Emblem", AnchorTC, new Vector2(0, -18), new Vector2(58, 58));
        var ring = Art(emblem, "Ring", UiKit.Ring, UiKit.Alpha(accent, .9f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        ring.gameObject.AddComponent<UiSpin>().degreesPerSecond = 14f;
        Art(emblem, "Core", UiKit.Diamond, UiKit.Alpha(accent, .95f), AnchorCC, Vector2.zero, new Vector2(13, 13));
        Art(emblem, "Core inner", UiKit.Diamond, UiKit.Alpha(Color.white, .85f), AnchorCC, Vector2.zero, new Vector2(6, 6));

        Ct(modal, victory ? UiKit.Track("RUN COMPLETE  /  SECTOR CLEARED") : defeat ? UiKit.Track("RUN OVER  /  SIGNAL LOST") : UiKit.Track("STARFALL  /  PAUSED"), 9, new Vector2(0, -78), new Vector2(700, 14), accent, FontStyle.Bold);
        var title = Ct(modal, victory ? "THE NIGHT BREAKS" : defeat ? "SIGNAL LOST" : "PAUSED", 40, new Vector2(0, -90), new Vector2(900, 52), UiKit.TextMain, FontStyle.Bold, displayFont);
        GlowText(title, UiKit.Alpha(accent, .26f), 2f);
        Ct(modal, victory ? "三位守望者尽数陨落 · 航道已经打开" : defeat ? "灰烬之门吞没了又一名跑者" : "深呼吸，跑者", 13, new Vector2(0, -144), new Vector2(720, 20), UiKit.TextDim);
        HLine(modal, "Divider", 40f, -170f, 1f, new Color(1, 1, 1, .08f));

        // Character portrait and final stats.
        var portrait = Art(modal, "Result portrait", SpriteResource(CurrentCharacter.Portrait), Color.white, AnchorTL, new Vector2(30, -186), new Vector2(200, 300));
        portrait.preserveAspect = true;
        Art(modal, "Result portrait frame", UiKit.Stroke, UiKit.Alpha(accent, .32f), AnchorTL, new Vector2(26, -182), new Vector2(208, 308));
        Lt(modal, CurrentCharacter.Name, 22, new Vector2(30, -496), new Vector2(280, 28), UiKit.TextMain, FontStyle.Bold);
        Lt(modal, CurrentCharacter.CallSign, 10, new Vector2(31, -524), new Vector2(280, 14), accent, FontStyle.Bold);
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
            Lt(modal, finalStats[i], 10, new Vector2(30 + (i % 2) * 128, -550 - (i / 2) * 19), new Vector2(126, 16), i % 2 == 0 ? UiKit.TextDim : UiKit.TextMain, FontStyle.Bold);

        var stats = Node(modal, "Run stats", AnchorTL, new Vector2(330, -186), new Vector2(740, 84));
        StatTile(stats, "Time", "TIME", GameRules.TimeLabel(Elapsed), -270, accent, 170);
        StatTile(stats, "Kills", "ELIMINATIONS", Kills.ToString("000"), -90, accent, 170);
        StatTile(stats, "Level", "LEVEL", Level.ToString("00"), 90, accent, 170);
        StatTile(stats, "Bosses", "BOSSES", BossesDefeated + " / " + GameRules.BossCount, 270, accent, 170);

        float totalDamage = 0f, maxDamage = 0f;
        for (int i = 0; i < 5; i++) { totalDamage += Stats.WeaponDamage[i]; maxDamage = Mathf.Max(maxDamage, Stats.WeaponDamage[i]); }
        Lt(modal, UiKit.Track("WEAPON DAMAGE  /  本局武器伤害"), 9, new Vector2(330, -282), new Vector2(420, 14), UiKit.TextMute, FontStyle.Bold);
        Rt(modal, "TOTAL  " + Mathf.RoundToInt(totalDamage).ToString("N0"), 9, new Vector2(-40, -283), new Vector2(300, 14), accent, FontStyle.Bold);

        for (int i = 0; i < 5; i++)
        {
            bool unlocked = Stats.Weapons[i] > 0;
            float damage = Stats.WeaponDamage[i];
            float ratio = maxDamage > 0f ? Mathf.Clamp01(damage / maxDamage) : 0f;
            float percent = totalDamage > 0f ? damage / totalDamage * 100f : 0f;
            var row = Node(modal, "Result weapon", AnchorTL, new Vector2(330, -302 - i * 34), new Vector2(740, 30));
            Art(row, "Row", UiKit.SoftSmall, new Color(1, 1, 1, .025f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
            var icon = Art(row, "Icon", GetSprite("Icons/" + GameRules.IconAsset(i)), unlocked ? Color.white : new Color(1, 1, 1, .15f), AnchorTL, new Vector2(6, -4), new Vector2(22, 22));
            icon.preserveAspect = true;
            Lm(row, WeaponShort(i), 10, new Vector2(34, 0), new Vector2(90, 26), unlocked ? UiKit.TextMain : UiKit.TextMute, FontStyle.Bold);
            Art(row, "Track", UiKit.Solid, new Color(1, 1, 1, .06f), AnchorTL, new Vector2(130, -13), new Vector2(370, 6));
            if (ratio > 0f)
                Art(row, "Fill", UiKit.Solid, UiKit.Alpha(WeaponTint(i), .9f), AnchorTL, new Vector2(130, -13), new Vector2(370f * ratio, 6));
            Rm(row, damage > 0f ? Mathf.RoundToInt(damage).ToString("N0") : "—", 10, new Vector2(-100, 0), new Vector2(110, 26), damage > 0f ? UiKit.TextMain : UiKit.TextMute, FontStyle.Bold);
            Rm(row, totalDamage > 0f ? percent.ToString("0.0") + "%" : string.Empty, 9, new Vector2(-10, 0), new Vector2(80, 26), UiKit.TextMute, FontStyle.Bold);
        }

        int ownedWeapons = 0, evolvedWeapons = 0;
        for (int i = 0; i < 5; i++) { if (Stats.Weapons[i] > 0) ownedWeapons++; if (Stats.EvolvedWeapons[i]) evolvedWeapons++; }
        string summary = CurrentCharacter.Name + "  ·  武器 " + ownedWeapons + "/5  ·  进化 " + evolvedWeapons + "/5" +
            (victory ? "  ·  SECTOR CLEARED" : defeat ? "  ·  下次会走得更远" : string.Empty);
        Ct(modal, summary, 10, new Vector2(0, -476), new Vector2(900, 16), UiKit.TextMute, FontStyle.Bold);

        if (State == RunState.Paused) Cta(modal, "Resume", "继续探索  /  RESUME", AnchorTC, new Vector2(0, -506), new Vector2(320, 48), accent, true, Resume, 14);
        else Cta(modal, "Play again", "再次探索  /  PLAY AGAIN", AnchorTC, new Vector2(0, -506), new Vector2(320, 48), accent, true, PlayAgain, 14);
        Cta(modal, "Back to title", "返回主菜单", AnchorTC, new Vector2(0, -566), new Vector2(320, 40), new Color(.45f, .57f, .64f), false, RestartRun, 12);
        Ct(modal, "ESC  " + (State == RunState.Paused ? "继续" : "返回标题"), 9, new Vector2(0, -608), new Vector2(300, 14), UiKit.TextMute, FontStyle.Bold);
        AddUtilityButtons(panel);
    }

    private void StatTile(Transform parent, string name, string label, string value, float x, Color accent, float width = 168)
    {
        var tile = Node(parent, name, AnchorTC, new Vector2(x, 0), new Vector2(width, 84));
        Art(tile, "Fill", UiKit.Soft, new Color(1, 1, 1, .04f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        Fill(tile, "Edge", UiKit.SoftEdge, UiKit.Alpha(accent, .22f));
        Art(tile, "Mark", UiKit.Diamond, UiKit.Alpha(accent, .9f), AnchorTC, new Vector2(0, -10), new Vector2(6, 6));
        Ct(tile, UiKit.Track(label), 9, new Vector2(0, -21), new Vector2(width - 16, 14), UiKit.TextMute, FontStyle.Bold);
        Ct(tile, value, 26, new Vector2(0, -38), new Vector2(width - 16, 34), UiKit.TextMain, FontStyle.Bold, displayFont);
    }

    // ------------------------------------------------------- pilot sheet
    private void OpenPilotSheet()
    {
        if (State != RunState.Playing || Player == null || deathSequence) return;
        State = RunState.CharacterSheet; Time.timeScale = 0f;
        if (sheetPanel != null) Destroy(sheetPanel);
        sheetPanel = NewUI<Image>("Pilot sheet", overlay).gameObject;
        Stretch((RectTransform)sheetPanel.transform);
        var background = sheetPanel.GetComponent<Image>();
        background.color = new Color(.01f, .018f, .03f, .87f); background.raycastTarget = true;
        var root = (RectTransform)sheetPanel.transform;
        Art(root, "Vignette", UiKit.Vignette, new Color(0, 0, 0, .6f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);

        Color accent = CurrentCharacter.Accent;
        var card = Frame(root, "Pilot sheet frame", AnchorCC, Vector2.zero, new Vector2(touchControls ? 1120 : 1000, touchControls ? 680 : 620), accent, .97f);
        Lt(card, UiKit.Track("RUN STATUS  /  作战状态"), 10, new Vector2(30, -22), new Vector2(420, 16), UiKit.Alpha(accent, .95f), FontStyle.Bold);
        Rt(card, "C / ESC  关闭", 9, new Vector2(-30, -24), new Vector2(200, 14), UiKit.TextMute, FontStyle.Bold);

        var portrait = Art(card, "Sheet portrait", SpriteResource(CurrentCharacter.Portrait), Color.white, AnchorTL, new Vector2(36, -64), new Vector2(280, 420));
        portrait.preserveAspect = true;
        Art(card, "Sheet portrait frame", UiKit.Stroke, UiKit.Alpha(accent, .35f), AnchorTL, new Vector2(32, -60), new Vector2(288, 428));
        Art(card, "Portrait bottom scrim", UiKit.GradientDown, new Color(.012f, .02f, .032f, .9f), AnchorTL, new Vector2(36, -400), new Vector2(280, 84));
        float hpRatio = Player != null ? Mathf.Clamp01(Player.Health / (float)Stats.MaxHealth) : 0f;
        Art(card, "HP track", UiKit.Solid, new Color(0, 0, 0, .55f), AnchorTL, new Vector2(44, -462), new Vector2(264, 9));
        Art(card, "HP fill", UiKit.Solid, UiKit.Alpha(accent, .92f), AnchorTL, new Vector2(44, -462), new Vector2(264f * hpRatio, 9));
        Lt(card, "HP  " + (Player != null ? Player.Health : 0) + " / " + Stats.MaxHealth, 9, new Vector2(44, -448), new Vector2(264, 14), UiKit.TextDim, FontStyle.Bold);
        var levelChip = Node(card, "Sheet level", AnchorTL, new Vector2(238, -76), new Vector2(68, 22));
        Art(levelChip, "Fill", UiKit.SoftSmall, new Color(.015f, .025f, .04f, .92f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        Fill(levelChip, "Edge", UiKit.SoftSmallEdge, UiKit.Alpha(accent, .45f));
        Mc(levelChip, "LV " + Level, 9, UiKit.TextMain, FontStyle.Bold);
        Lt(card, CurrentCharacter.Name, 28, new Vector2(36, -494), new Vector2(320, 36), UiKit.TextMain, FontStyle.Bold);
        Lt(card, CurrentCharacter.CallSign, 11, new Vector2(37, -532), new Vector2(320, 16), accent, FontStyle.Bold);
        Lt(card, "时间 " + GameRules.TimeLabel(Elapsed) + "      等级 " + Level + "      击杀 " + Kills, 11, new Vector2(36, -560), new Vector2(320, 16), UiKit.TextDim, FontStyle.Bold);
        Lt(card, "被动  " + PassiveTitle(CurrentCharacter.Id), 10, new Vector2(36, -582), new Vector2(320, 14), UiKit.TextMute);

        Art(card, "Sheet divider", UiKit.Solid, new Color(1, 1, 1, .06f), AnchorTL, new Vector2(338, -70), new Vector2(1, 470));
        Art(card, "Sheet content panel", UiKit.Soft, new Color(1, 1, 1, .018f), AnchorTL, new Vector2(350, -62), new Vector2(620, 500));
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
            new KeyValuePair<string, string>("拾取范围", Stats.Magnet.ToString("0.0") + "m"),
            new KeyValuePair<string, string>("穿透", Stats.Pierce.ToString()),
            new KeyValuePair<string, string>("生命", Player.Health + " / " + Stats.MaxHealth),
        };
        for (int i = 0; i < stats.Length; i++)
        {
            float x = (i % 5) * 122f, y = -(i / 5) * 72f;
            var tile = Node(content, "Sheet stat", AnchorTL, new Vector2(x, y), new Vector2(114, 64));
            Art(tile, "Fill", UiKit.Soft, new Color(1, 1, 1, .04f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
            Fill(tile, "Edge", UiKit.SoftEdge, UiKit.Alpha(accent, .16f));
            Ct(tile, stats[i].Key, 9, new Vector2(0, -10), new Vector2(108, 14), UiKit.TextMute, FontStyle.Bold);
            Ct(tile, stats[i].Value, 18, new Vector2(0, -30), new Vector2(108, 26), UiKit.TextMain, FontStyle.Bold, displayFont);
        }

        Art(content, "Weapons mark", UiKit.Solid, UiKit.Alpha(accent, .8f), AnchorTL, new Vector2(2, -152), new Vector2(3, 12));
        Lt(content, UiKit.Track("WEAPONS  /  武器"), 9, new Vector2(12, -152), new Vector2(300, 14), UiKit.TextMute, FontStyle.Bold);
        HLine(content, "Weapons rule", 2f, -168f, 1f, new Color(1, 1, 1, .05f));
        for (int i = 0; i < 5; i++)
        {
            int level = Stats.Weapons[i]; bool unlocked = level > 0; bool evolved = Stats.EvolvedWeapons[i];
            var row = Node(content, "Sheet weapon", AnchorTL, new Vector2(0, -172 - i * 34), new Vector2(612, 30));
            Art(row, "Fill", UiKit.SoftSmall, new Color(1, 1, 1, i % 2 == 0 ? .035f : .02f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
            var icon = Art(row, "Icon", GetSprite("Icons/" + GameRules.IconAsset(i)), unlocked ? (evolved ? Color.Lerp(Color.white, GameRules.Gold, .25f) : Color.white) : new Color(1, 1, 1, .18f), AnchorTL, new Vector2(6, -4), new Vector2(22, 22));
            icon.preserveAspect = true;
            Lm(row, unlocked ? WeaponShort(i) : "—  LOCKED", 10, new Vector2(36, 0), new Vector2(90, 26), unlocked ? (evolved ? GameRules.Gold : UiKit.TextMain) : UiKit.TextMute, FontStyle.Bold);
            if (unlocked)
            {
                Rm(row, "Lv " + level + "/" + GameRules.MaxWeaponRank, 9, new Vector2(-8, 0), new Vector2(70, 26), UiKit.TextMain, FontStyle.Bold);
                if (evolved)
                {
                    var evo = Node(row, "Evo", AnchorTL, new Vector2(130, -7), new Vector2(26, 16));
                    Art(evo, "Fill", UiKit.SoftSmall, new Color(1, 1, 1, .05f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
                    Fill(evo, "Edge", UiKit.SoftSmallEdge, UiKit.Alpha(GameRules.Gold, .5f));
                    Mc(evo, "EVO", 6, GameRules.Gold, FontStyle.Bold);
                }
            }
        }

        Art(content, "Bonuses mark", UiKit.Solid, UiKit.Alpha(accent, .8f), AnchorTL, new Vector2(2, -348), new Vector2(3, 12));
        Lt(content, UiKit.Track("BONUSES  /  当前加成"), 9, new Vector2(12, -348), new Vector2(300, 14), UiKit.TextMute, FontStyle.Bold);
        HLine(content, "Bonuses rule", 2f, -364f, 1f, new Color(1, 1, 1, .05f));
        var bonuses = new List<string> { "角色被动  " + CurrentCharacter.Passive };
        if (bossRewardsClaimed[0]) bonuses.Add("超载核心  最终伤害 +20% · 最终攻速 +15% · 暴击率 +20%");
        if (bossRewardsClaimed[1]) bonuses.Add("凤凰协议  生命 +60 · 护甲 +15% · 移速 +18% · 回复 +1");
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
        var grid = Art(panel, "Grid", UiKit.Grid, UiKit.Alpha(GameRules.Cyan, .03f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero); grid.type = Image.Type.Tiled;
        Art(panel, "Vignette", UiKit.Vignette, new Color(0, 0, 0, .75f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        Art(panel, "Scanlines", UiKit.Scanlines, new Color(0, 0, 0, .4f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);

        if (touchControls)
        {
            panel = Node(panel, "Phone archive layout", AnchorCC, Vector2.zero, new Vector2(1440, 810));
            panel.localScale = Vector3.one * .82f;
        }

        Art(panel, "Header halo", UiKit.Glow, UiKit.Alpha(GameRules.Cyan, .10f), AnchorTL, new Vector2(240, -40), new Vector2(640, 240));
        Art(panel, "Head mark", UiKit.Diamond, UiKit.Alpha(GameRules.Cyan, .95f), AnchorTL, new Vector2(58, -38), new Vector2(7, 7));
        Lt(panel, UiKit.Track("PILOT ARCHIVE  /  ASHEN GATE EXPEDITION"), 10, new Vector2(74, -34), new Vector2(560, 16), UiKit.Alpha(GameRules.Cyan, .95f), FontStyle.Bold);
        Art(panel, "Title accent", UiKit.Solid, GameRules.Cyan, AnchorTL, new Vector2(48, -54), new Vector2(3, 44));
        Lt(panel, "选择你的共鸣者", 36, new Vector2(60, -50), new Vector2(560, 50), UiKit.TextMain, FontStyle.Bold);
        Lt(panel, "五名驾驶员 · 五种本命武器 · 一套专属被动", 12, new Vector2(60, -100), new Vector2(600, 20), UiKit.TextDim);
        characterFeatureIndex = Rt(panel, "01 / 05", 26, new Vector2(-66, -40), new Vector2(140, 32), GameRules.Cyan, FontStyle.Bold, displayFont);
        Rt(panel, UiKit.Track("ACTIVE FILE"), 9, new Vector2(-66, -72), new Vector2(140, 14), UiKit.TextMute, FontStyle.Bold);
        HLine(panel, "Header divider", 56f, -136f, 1f, new Color(1, 1, 1, .08f));

        var feature = Frame(panel, "Featured pilot", AnchorTL, new Vector2(48, -148), new Vector2(452, 590), CurrentCharacter.Accent, .92f);
        characterFeatureEdge = feature.Find("Edge").GetComponent<Image>();
        characterFeatureGlow = Art(feature, "Portrait glow", UiKit.Glow, UiKit.Alpha(CurrentCharacter.Accent, .3f), AnchorTC, new Vector2(0, -20), new Vector2(410, 500));
        Art(feature, "Portrait frame", UiKit.Stroke, UiKit.Alpha(CurrentCharacter.Accent, .35f), AnchorTC, new Vector2(0, -14), new Vector2(288, 428));
        characterFeaturePortrait = Art(feature, "Featured portrait", null, Color.white, AnchorTC, new Vector2(0, -18), new Vector2(280, 420));
        characterFeaturePortrait.preserveAspect = true;
        Art(feature, "Portrait scrim", UiKit.GradientDown, new Color(.012f, .02f, .032f, .92f), AnchorTC, new Vector2(0, -352), new Vector2(280, 86));
        var indexChip = Node(feature, "File index", AnchorTL, new Vector2(22, -30), new Vector2(46, 22));
        Art(indexChip, "Fill", UiKit.SoftSmall, new Color(.015f, .025f, .04f, .9f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        Fill(indexChip, "Edge", UiKit.SoftSmallEdge, new Color(1, 1, 1, .12f));
        characterFeatureFile = Mc(indexChip, "FILE 01", 8, UiKit.TextDim, FontStyle.Bold);
        var difficultyChip = Node(feature, "Difficulty", AnchorTR, new Vector2(-22, -30), new Vector2(92, 22));
        Art(difficultyChip, "Fill", UiKit.SoftSmall, new Color(.015f, .025f, .04f, .9f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        Fill(difficultyChip, "Edge", UiKit.SoftSmallEdge, new Color(1, 1, 1, .12f));
        characterFeatureDifficulty = Mc(difficultyChip, "作战难度  入门", 8, UiKit.TextDim, FontStyle.Bold);

        characterFeatureName = Lt(feature, "", 30, new Vector2(26, -442), new Vector2(400, 36), UiKit.TextMain, FontStyle.Bold);
        characterFeatureCallSign = Lt(feature, "", 11, new Vector2(27, -482), new Vector2(400, 18), GameRules.Cyan, FontStyle.Bold);
        HLine(feature, "Divider", 26f, -508f, 1f, new Color(1, 1, 1, .07f));
        for (int s = 0; s < 3; s++)
        {
            float y = -518f - s * 24f;
            var row = Node(feature, "Stat row", AnchorTL, new Vector2(26, y), new Vector2(400, 20));
            Lt(row, new[] { "生存", "机动", "输出" }[s], 10, new Vector2(0, 3), new Vector2(58, 16), UiKit.TextDim);
            for (int p = 0; p < 5; p++)
                characterStatSegments[s, p] = Art(row, "Segment", UiKit.Solid, new Color(1, 1, 1, .08f), AnchorTL, new Vector2(64 + p * 36f, 6), new Vector2(30, 8));
            characterStatValues[s] = Rm(row, "2 / 5", 9, new Vector2(-4, 0), new Vector2(50, 20), UiKit.TextMute, FontStyle.Bold);
        }

        var dossier = Frame(panel, "Pilot dossier", AnchorTL, new Vector2(516, -148), new Vector2(876, 292), new Color(.4f, .66f, .7f), .9f);
        characterFeatureQuote = Lt(dossier, "", 20, new Vector2(28, -20), new Vector2(700, 30), GameRules.Cyan);
        characterFeatureStory = Lt(dossier, "", 12, new Vector2(28, -58), new Vector2(800, 50), UiKit.TextDim);
        HLine(dossier, "Divider", 26f, -118f, 1f, new Color(1, 1, 1, .07f));
        var weaponBox = Node(dossier, "Weapon dossier", AnchorTL, new Vector2(24, -132), new Vector2(402, 138));
        Art(weaponBox, "Fill", UiKit.Soft, new Color(1, 1, 1, .035f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        Fill(weaponBox, "Edge", UiKit.SoftEdge, new Color(1, 1, 1, .07f));
        var weaponChip = Node(weaponBox, "Weapon icon frame", AnchorTL, new Vector2(12, -12), new Vector2(42, 42));
        Art(weaponChip, "Fill", UiKit.SoftSmall, new Color(.02f, .05f, .07f, .95f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        Fill(weaponChip, "Edge", UiKit.SoftSmallEdge, new Color(1, 1, 1, .12f));
        characterFeatureWeaponIcon = Art(weaponChip, "Weapon icon", null, Color.white, new Vector2(0, 0), new Vector2(1, 1), AnchorCC, Vector2.zero, new Vector2(-12, -12));
        characterFeatureWeaponIcon.preserveAspect = true;
        Lt(weaponBox, UiKit.Track("SIGNATURE WEAPON  /  本命武器"), 8, new Vector2(64, -11), new Vector2(320, 14), UiKit.TextMute, FontStyle.Bold);
        characterFeatureWeapon = Lt(weaponBox, "", 15, new Vector2(64, -26), new Vector2(320, 22), UiKit.TextMain, FontStyle.Bold);
        characterFeatureWeaponDesc = Lt(weaponBox, "", 11, new Vector2(14, -66), new Vector2(376, 44), UiKit.TextDim);

        var passiveBox = Node(dossier, "Passive dossier", AnchorTL, new Vector2(444, -132), new Vector2(402, 138));
        Art(passiveBox, "Fill", UiKit.Soft, new Color(1, 1, 1, .035f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        Fill(passiveBox, "Edge", UiKit.SoftEdge, new Color(1, 1, 1, .07f));
        var passiveChip = Node(passiveBox, "Passive icon frame", AnchorTL, new Vector2(12, -12), new Vector2(42, 42));
        Art(passiveChip, "Fill", UiKit.SoftSmall, new Color(.02f, .05f, .07f, .95f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        Fill(passiveChip, "Edge", UiKit.SoftSmallEdge, new Color(1, 1, 1, .12f));
        Art(passiveChip, "Passive glyph", UiKit.Diamond, Color.white, new Vector2(.5f, .5f), new Vector2(.5f, .5f), AnchorCC, Vector2.zero, new Vector2(18, 18));
        Lt(passiveBox, UiKit.Track("SIGNATURE TRAIT  /  专属被动"), 8, new Vector2(64, -11), new Vector2(320, 14), UiKit.TextMute, FontStyle.Bold);
        characterFeaturePassive = Lt(passiveBox, "", 15, new Vector2(64, -26), new Vector2(320, 22), UiKit.TextMain, FontStyle.Bold);
        characterFeaturePassiveDesc = Lt(passiveBox, "", 11, new Vector2(14, -66), new Vector2(376, 44), UiKit.TextDim);

        for (int i = 0; i < CharacterRoster.All.Length; i++)
        {
            int index = i; CharacterProfile profile = CharacterRoster.All[i];
            var card = Node(panel, "Character card", AnchorBL, AnchorBL, AnchorCC, new Vector2(630 + i * 162f, 238f), new Vector2(146, 250));
            var button = card.gameObject.AddComponent<Button>();
            var body = card.gameObject.AddComponent<Image>(); body.sprite = UiKit.Soft; body.type = Image.Type.Sliced;
            body.color = new Color(1, 1, 1, .03f); body.raycastTarget = true;
            button.targetGraphic = body; button.transition = Selectable.Transition.None; button.onClick.AddListener(() => SelectCharacter(index));
            characterButtons[i] = button; characterCardBodies[i] = body;
            var glow = Fill(card, "Glow", UiKit.Glow, UiKit.Alpha(profile.Accent, 0f), Vector2.zero, new Vector2(12, 12)); characterCardGlows[i] = glow;
            var edge = Fill(card, "Edge", UiKit.SoftEdge, new Color(1, 1, 1, .07f)); characterCardEdges[i] = edge;
            var bar = Art(card, "Accent bar", UiKit.Solid, new Color(1, 1, 1, .12f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(.5f, 1), new Vector2(0, 0), new Vector2(-20, 2)); characterCardBars[i] = bar;
            var portrait = Art(card, "Pilot portrait", SpriteResource(profile.Portrait), Color.white, new Vector2(0, 1), new Vector2(1, 1), new Vector2(.5f, 1), new Vector2(0, -5), new Vector2(-14, 198));
            portrait.preserveAspect = true;
            Art(card, "Portrait scrim", UiKit.GradientDown, new Color(.012f, .02f, .032f, .8f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(.5f, 1), new Vector2(0, -165), new Vector2(-14, 38));
            var chip = Node(card, "Index chip", AnchorTL, new Vector2(8, -8), new Vector2(26, 18));
            Art(chip, "Fill", UiKit.SoftSmall, new Color(.012f, .02f, .032f, .88f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
            Fill(chip, "Edge", UiKit.SoftSmallEdge, new Color(1, 1, 1, .1f));
            characterCardIndices[i] = Mc(chip, (i + 1).ToString("00"), 8, UiKit.TextDim, FontStyle.Bold);
            Lt(card, profile.Name, 12, new Vector2(9, -207), new Vector2(130, 18), UiKit.TextMain, FontStyle.Bold);
            Lt(card, profile.Weapon, 9, new Vector2(9, -228), new Vector2(130, 14), profile.Accent, FontStyle.Bold);
            var hover = card.gameObject.AddComponent<UiHover>();
            hover.body = card; hover.edge = edge; hover.edgeIdle = edge.color; hover.edgeHot = profile.Accent; hover.hoverScale = 1.03f;
        }

        Cta(panel, "Deploy pilot", "以当前角色出击", AnchorBR, new Vector2(-48, 34), new Vector2(300, 52), GameRules.Cyan, true, StartSelectedRun, 15);
        Cta(panel, "Back to title", "返回标题  /  ESC", AnchorBR, new Vector2(-366, 34), new Vector2(190, 44), new Color(.45f, .62f, .69f), false, () => ShowStatePanel(RunState.Menu), 12);
        MakeText(panel, "Archive hint", "← → / 1 - 5  切换作战员     ·     点击卡片查看档案     ·     Enter 出击", 10, TextAnchor.MiddleLeft, AnchorBL, AnchorBL, AnchorBL, new Vector2(48, 30), new Vector2(600, 18), UiKit.TextDim, FontStyle.Normal, null);
        UpdateCharacterSelection();
    }

    private void AddUtilityButtons(Transform parent)
    {
        var sound = Node(parent, "Sound toggle", AnchorBR, new Vector2(touchControls ? -176 : -158, touchControls ? 32 : 22), touchControls ? new Vector2(184, 60) : new Vector2(136, 36));
        var soundButton = sound.gameObject.AddComponent<Button>();
        var soundBody = sound.gameObject.AddComponent<Image>(); soundBody.sprite = UiKit.Soft; soundBody.type = Image.Type.Sliced; soundBody.color = new Color(.04f, .08f, .11f, .9f);
        soundButton.targetGraphic = soundBody; soundButton.transition = Selectable.Transition.None;
        var soundEdge = Fill(sound, "Edge", UiKit.SoftEdge, UiKit.Alpha(GameRules.Cyan, .25f));
        var dot = Art(sound, "Dot", UiKit.Dot, GameRules.Cyan, AnchorCL, new Vector2(16, 0), new Vector2(6, 6));
        var soundLabel = Mc(sound, AudioListener.volume > 0 ? "音效：开" : "音效：关", touchControls ? 16 : 11, UiKit.TextDim, FontStyle.Normal);
        soundButton.onClick.AddListener(() =>
        {
            AudioListener.volume = AudioListener.volume > 0 ? 0 : 1;
            soundLabel.text = AudioListener.volume > 0 ? "音效：开" : "音效：关";
            dot.color = AudioListener.volume > 0 ? GameRules.Cyan : new Color(1, 1, 1, .2f);
            soundEdge.color = AudioListener.volume > 0 ? UiKit.Alpha(GameRules.Cyan, .25f) : new Color(1, 1, 1, .1f);
            PlayerPrefs.SetInt("Starfall.Muted", AudioListener.volume > 0 ? 0 : 1); PlayerPrefs.Save();
        });
        var soundHover = sound.gameObject.AddComponent<UiHover>(); soundHover.body = sound; soundHover.edge = soundEdge; soundHover.edgeIdle = soundEdge.color; soundHover.edgeHot = UiKit.Alpha(GameRules.Cyan, .8f); soundHover.hoverScale = 1.02f;

        var quit = Node(parent, "Quit", AnchorBR, new Vector2(-22, touchControls ? 32 : 22), touchControls ? new Vector2(152, 60) : new Vector2(124, 36));
        var quitButton = quit.gameObject.AddComponent<Button>();
        var quitBody = quit.gameObject.AddComponent<Image>(); quitBody.sprite = UiKit.Soft; quitBody.type = Image.Type.Sliced; quitBody.color = new Color(.04f, .06f, .09f, .9f);
        quitButton.targetGraphic = quitBody; quitButton.transition = Selectable.Transition.None;
        var quitEdge = Fill(quit, "Edge", UiKit.SoftEdge, new Color(1, 1, 1, .1f));
        Mc(quit, "退出游戏", touchControls ? 16 : 11, UiKit.TextDim);
        quitButton.onClick.AddListener(() => { Application.Quit(); });
        var quitHover = quit.gameObject.AddComponent<UiHover>(); quitHover.body = quit; quitHover.edge = quitEdge; quitHover.edgeIdle = quitEdge.color; quitHover.edgeHot = UiKit.Alpha(GameRules.Pink, .8f); quitHover.hoverScale = 1.02f;
    }

    // -------------------------------------------------------------- upgrades
}

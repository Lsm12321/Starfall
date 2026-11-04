using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private void BuildUpgradeScreen()
    {
        upgradePanel = NewUI<Image>("Upgrade choices", overlay).gameObject; Stretch((RectTransform)upgradePanel.transform);
        upgradePanel.GetComponent<Image>().color = new Color(.012f, .022f, .036f, .93f);
        var panel = (RectTransform)upgradePanel.transform;
        if (touchControls) panel.localScale = Vector3.one * 1.06f;
        Art(panel, "Vignette", UiKit.Vignette, new Color(0, 0, 0, .6f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        upgradeHalo = Art(panel, "Halo", UiKit.Glow, UiKit.Alpha(GameRules.Cyan, .12f), AnchorTC, new Vector2(0, -90), new Vector2(1160, 540));

        upgradeKicker = Ct(panel, UiKit.Track("FIELD SIGNAL"), 10, new Vector2(0, -82), new Vector2(760, 16), GameRules.Cyan, FontStyle.Bold);
        upgradeTitle = upgradeKicker;
        upgradeKickerRect = upgradeKicker.rectTransform;
        upgradeSubtitle = Ct(panel, "选择属性与已有武器技能", 38, new Vector2(0, -98), new Vector2(940, 52), UiKit.TextMain, FontStyle.Bold);
        upgradeTitleRect = upgradeSubtitle.rectTransform;
        DropShadow(upgradeSubtitle, -3f);
        upgradeSummary = Ct(panel, "LEVEL 01  /  FIELD MODIFICATION", 12, new Vector2(0, -152), new Vector2(900, 20), UiKit.TextDim);
        upgradeSummaryRect = upgradeSummary.rectTransform;
        upgradeHeaderRule = Art(panel, "Header rule", UiKit.GradientRight, new Color(1, 1, 1, .16f), AnchorTC, new Vector2(0, -180), new Vector2(240, 1));

        rewardSealRect = Node(panel, "Boss reward seal", AnchorTC, new Vector2(0, -22), new Vector2(104, 104));
        var sealGlow = Art(rewardSealRect, "Glow", UiKit.Glow, UiKit.Alpha(GameRules.Gold, .3f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, new Vector2(74, 74));
        sealGlow.gameObject.AddComponent<UiPulse>();
        bossOrbit = Node(rewardSealRect, "Core orbit", AnchorCC, Vector2.zero, new Vector2(148, 148));
        for (int k = 0; k < 4; k++)
        {
            float angle = k * Mathf.PI * .5f;
            var shard = Art(bossOrbit, "Orbit shard", UiKit.Diamond, UiKit.Gold, AnchorCC, new Vector2(Mathf.Cos(angle) * 70f, Mathf.Sin(angle) * 70f), new Vector2(6, 6));
            shard.color = UiKit.Alpha(GameRules.Gold, .7f);
        }
        bossOrbit.gameObject.AddComponent<UiSpin>().degreesPerSecond = 16f;
        Art(rewardSealRect, "Hex fill", UiKit.HexFill, new Color(.03f, .025f, .04f, .97f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        rewardBossArt = Art(rewardSealRect, "Boss core portrait", null, Color.white, new Vector2(0, 0), new Vector2(1, 1), AnchorCC, Vector2.zero, new Vector2(-10, -10));
        rewardBossArt.preserveAspect = true;
        Art(rewardSealRect, "Hex edge", UiKit.HexEdge, UiKit.Alpha(GameRules.Gold, .85f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        rewardSealRect.gameObject.SetActive(false);

        for (int i = 0; i < 3; i++)
        {
            int index = i;
            var card = Node(panel, "Upgrade card", AnchorCC, Vector2.zero, new Vector2(264, 292));
            var button = card.gameObject.AddComponent<Button>();
            var body = card.gameObject.AddComponent<Image>(); body.sprite = UiKit.Soft; body.type = Image.Type.Sliced;
            body.color = new Color(.04f, .075f, .1f, .98f); body.raycastTarget = true;
            button.targetGraphic = body; button.transition = Selectable.Transition.None; button.onClick.AddListener(() => ChooseUpgrade(index));
            choiceButtons[i] = button; choiceBodies[i] = body; choiceRoots[i] = card;
            choiceGlows[i] = Fill(card, "Glow", UiKit.Glow, new Color(1, 1, 1, 0f), Vector2.zero, new Vector2(30, 40));
            choiceEdges[i] = Fill(card, "Edge", UiKit.SoftEdge, new Color(1, 1, 1, .12f));
            choiceTopBars[i] = Art(card, "Top accent", UiKit.GradientRight, new Color(1, 1, 1, .5f), AnchorTC, new Vector2(-80, 0), new Vector2(160, 2));
            var corners = Node(card, "Boss corners", Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
            Bracket(corners, "Bracket TL", AnchorTL, new Vector2(9, -9), 0f, Color.white, 18f);
            Bracket(corners, "Bracket TR", AnchorTR, new Vector2(-9, -9), -90f, Color.white, 18f);
            Bracket(corners, "Bracket BR", AnchorBR, new Vector2(-9, 9), 180f, Color.white, 18f);
            Bracket(corners, "Bracket BL", AnchorBL, new Vector2(9, 9), 90f, Color.white, 18f);
            choiceCornerSets[i] = corners;
            choiceInnerEdges[i] = Art(card, "Inner edge", UiKit.SoftEdge, new Color(1, 1, 1, 0f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, new Vector2(-12, -12));
            var chip = Node(card, "Icon chip", AnchorTC, new Vector2(0, -24), new Vector2(78, 78));
            choiceChips[i] = chip;
            choiceIconFrames[i] = Art(chip, "Icon frame", UiKit.Soft, new Color(1, 1, 1, .04f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
            choiceChipEdges[i] = Fill(chip, "Edge", UiKit.SoftEdge, new Color(1, 1, 1, .14f));
            choiceHexFills[i] = Art(chip, "Hex fill", UiKit.HexFill, new Color(1, 1, 1, .04f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
            choiceHexEdges[i] = Art(chip, "Hex edge", UiKit.HexEdge, new Color(1, 1, 1, .5f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
            choiceBossRings[i] = Art(chip, "Core ring", UiKit.Ring, new Color(1, 1, 1, .3f), AnchorCC, Vector2.zero, new Vector2(124, 124));
            var ringPulse = choiceBossRings[i].gameObject.AddComponent<UiPulse>();
            ringPulse.min = .16f; ringPulse.max = .46f; ringPulse.speed = 1.1f;
            choiceIcons[i] = Art(chip, "Skill icon art", null, Color.white, new Vector2(0, 0), new Vector2(1, 1), AnchorCC, Vector2.zero, new Vector2(-10, -10));
            choiceIcons[i].preserveAspect = true;
            choiceGlyphs[i] = Mc(chip, "?", 26, Color.white, FontStyle.Bold);
            choiceGlyphs[i].gameObject.SetActive(false);
            var key = Node(card, "Key chip", AnchorTR, new Vector2(-14, -14), new Vector2(24, 20));
            choiceNumberBgs[i] = Art(key, "Fill", UiKit.SoftSmall, new Color(1, 1, 1, .06f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
            Fill(key, "Edge", UiKit.SoftSmallEdge, new Color(1, 1, 1, .1f));
            choiceNumbers[i] = Mc(key, (i + 1).ToString(), 9, UiKit.TextDim, FontStyle.Bold);
            choiceTitles[i] = Ct(card, "", 18, new Vector2(0, -116), new Vector2(228, 44), UiKit.TextMain, FontStyle.Bold);
            choiceTitles[i].gameObject.name = "Choice title";
            choiceDetails[i] = Ct(card, "", 12, new Vector2(0, -160), new Vector2(224, 54), UiKit.TextDim);
            choiceDetails[i].gameObject.name = "Choice details";
            Art(card, "Badge divider", UiKit.Solid, new Color(1, 1, 1, .07f), new Vector2(0, 0), new Vector2(1, 0), new Vector2(.5f, 0), new Vector2(0, 50), new Vector2(-40, 1));
            var badge = Node(card, "Badge", AnchorBC, new Vector2(0, 18), new Vector2(160, 20));
            choiceBadgeBgs[i] = Art(badge, "Fill", UiKit.SoftSmall, new Color(1, 1, 1, .06f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
            Fill(badge, "Edge", UiKit.SoftSmallEdge, new Color(1, 1, 1, .06f));
            choiceBadges[i] = Mc(badge, "STAT MODULE", 8, UiKit.Cyan, FontStyle.Bold);
            var hover = card.gameObject.AddComponent<UiHover>();
            hover.body = card; hover.edge = choiceEdges[i]; hover.hoverScale = 1.025f;
        }

        MakeText(panel, "Upgrade hint", touchControls ? "点击卡牌选择强化" : "点击卡牌，或按 1 / 2 / 3 选择", touchControls ? 16 : 12, TextAnchor.MiddleCenter, AnchorBC, AnchorBC, AnchorBC, new Vector2(0, touchControls ? 46 : 52), new Vector2(560, 24), UiKit.TextDim, FontStyle.Normal, null);
        rerollButton = Cta(panel, "Reroll", "刷新选项  ·  剩余 2", AnchorBC, new Vector2(0, 82), new Vector2(248, 40), GameRules.Cyan, false, Reroll, 12);
        rerollLabel = rerollButton.GetComponentInChildren<Text>();
        upgradePanel.SetActive(false);
    }

    private void SetUpgradeChoices(bool chest) => SetUpgradeChoices(UpgradeFlow.Normal, chest);

    private void SetUpgradeChoices(UpgradeFlow flow, bool chest = false, int bossStage = 0, List<int> avoid = null)
    {
        if (upgradePanel == null) BuildUpgradeScreen();
        currentUpgradeFlow = flow; chestReward = chest; rewardBossStage = bossStage;
        choices.Clear();
        if (flow == UpgradeFlow.Weapon) choices.AddRange(GameRules.WeaponChoices(Stats, avoid));
        else if (flow == UpgradeFlow.Boss) choices.AddRange(GameRules.BossChoices(bossStage, bossRewardsClaimed));
        else choices.AddRange(GameRules.Choices(Stats, avoid, Level));
        bool bossReward = flow == UpgradeFlow.Boss;
        bool weaponFlow = flow == UpgradeFlow.Weapon;
        Color pageAccent = bossReward ? GameRules.BossColor(bossStage) : weaponFlow ? GameRules.Gold : GameRules.Cyan;

        upgradeTitle.text = bossReward ? "BOSS CORE EXTRACTED  /  STAGE " + bossStage.ToString("00") : weaponFlow ? "ARMORY UPLINK  /  WEAPON GROWTH" : chest ? "SUPPLY CACHE RECOVERED" : $"FIELD SIGNAL  /  LEVEL {Level:00}";
        upgradeTitle.color = pageAccent;
        upgradeSubtitle.text = bossReward ? "选择核心共鸣" : weaponFlow ? "选择新武器或进化武器" : chest ? "选择战地补给" : "选择属性与已有武器技能";
        upgradeSubtitle.fontSize = bossReward ? 42 : weaponFlow ? 40 : 36;
        upgradeSubtitle.color = bossReward ? UiKit.Light(pageAccent, .62f) : UiKit.TextMain;
        upgradeSummary.text = bossReward ? GameRules.BossName(bossStage) + "  /  唯一核心奖励 · 还可选择 " + choices.Count + " 种" : weaponFlow ? (choices.Count > 0 && choices[0].Id < 0 ? "武装补强" : "新武器 · 武器进化") : chest ? "战地补给" : "属性模块与武器技能  /  等级 " + Level.ToString("00");
        upgradeSummary.color = bossReward ? pageAccent : weaponFlow ? GameRules.Gold : UiKit.TextDim;
        upgradePanel.GetComponent<Image>().color = bossReward ? new Color(.033f, .015f, .03f, .97f) : weaponFlow ? new Color(.022f, .026f, .038f, .96f) : new Color(.012f, .022f, .036f, .93f);
        upgradeHalo.color = UiKit.Alpha(pageAccent, bossReward ? .24f : weaponFlow ? .16f : .1f);
        upgradeHalo.rectTransform.sizeDelta = bossReward ? new Vector2(1340, 720) : new Vector2(1160, 540);
        upgradeHalo.rectTransform.anchoredPosition = new Vector2(0, bossReward ? -60f : -90f);
        upgradeHeaderRule.color = UiKit.Alpha(pageAccent, bossReward ? .6f : weaponFlow ? .32f : .16f);
        upgradeHeaderRule.rectTransform.sizeDelta = new Vector2(bossReward ? 320 : 240, 1);
        upgradeHeaderRule.rectTransform.anchoredPosition = new Vector2(0, bossReward ? -240f : -180f);
        rewardSealRect.gameObject.SetActive(bossReward);
        if (bossReward) { rewardBossArt.sprite = GetSprite(GameRules.BossVisual(bossStage)); rewardBossArt.color = Color.white; }
        // Boss header keeps a clear margin from the screen top even with the 1.06 phone scale.
        rewardSealRect.anchoredPosition = new Vector2(0, -34f);
        upgradeKickerRect.anchoredPosition = new Vector2(0, bossReward ? -144f : -82f);
        upgradeTitleRect.anchoredPosition = new Vector2(0, bossReward ? -160f : -98f);
        upgradeSummaryRect.anchoredPosition = new Vector2(0, bossReward ? -214f : -152f);

        // Weapon cards carry two-part descriptions (effect + numbers); give them the height so text never reaches the badge rule.
        Vector2 cardSize = bossReward ? new Vector2(344, 360) : weaponFlow ? new Vector2(304, 364) : new Vector2(touchControls ? 304 : 264, touchControls ? 316 : 286);
        float spacing = bossReward ? 372f : weaponFlow ? 336f : touchControls ? 336f : 296f;
        float centerY = bossReward ? -26f : weaponFlow ? -6f : 8f;
        float edgeAlpha = bossReward ? .92f : weaponFlow ? .56f : .24f;
        float glowAlpha = bossReward ? .3f : weaponFlow ? .15f : .05f;
        float barAlpha = bossReward ? 1f : weaponFlow ? .9f : .4f;
        float bodyMix = bossReward ? .13f : weaponFlow ? .09f : .04f;
        for (int i = 0; i < 3; i++)
        {
            choiceButtons[i].gameObject.SetActive(i < choices.Count);
            if (i >= choices.Count) continue;
            var option = choices[i];
            bool evolution = weaponFlow && option.IsEvolution;
            bool twoLineWeapon = weaponFlow && option.Id >= 0;
            choiceTitles[i].text = evolution ? "武器进阶\n" + option.Title : option.Title;
            choiceDetails[i].text = option.Description; choiceGlyphs[i].text = option.Icon; choiceGlyphs[i].color = option.Color;
            Color frameAccent = option.Color;
            choiceBadges[i].text = bossReward ? "唯一核心  ·  每种限选一次" : weaponFlow ? (option.Id < 0 ? "ARSENAL BOOST" : evolution ? "✦  WEAPON EVOLUTION" : "NEW WEAPON") : option.IsWeapon ? "WEAPON SKILL" : chest ? "FIELD SUPPLY" : "STAT MODULE";
            choiceBadges[i].color = bossReward ? UiKit.Light(option.Color, .18f) : evolution ? UiKit.Light(option.Color, .3f) : option.Color;
            choiceIcons[i].sprite = GetSprite("Icons/" + GameRules.IconAsset(option.Id)); choiceIcons[i].color = Color.white;
            choiceIcons[i].gameObject.SetActive(choiceIcons[i].sprite != null);
            choiceBodies[i].color = UiKit.Mix(new Color(.03f, .052f, .075f), option.Color, evolution ? .10f : bodyMix);
            choiceEdges[i].color = UiKit.Alpha(frameAccent, evolution ? .82f : edgeAlpha);
            choiceGlows[i].color = UiKit.Alpha(frameAccent, evolution ? .18f : glowAlpha);
            choiceTopBars[i].color = UiKit.Alpha(frameAccent, evolution ? 1f : barAlpha);
            choiceBadgeBgs[i].color = UiKit.Alpha(frameAccent, bossReward ? .2f : evolution ? .16f : weaponFlow ? .12f : .07f);
            choiceNumbers[i].color = UiKit.Light(frameAccent, .25f);
            choiceNumberBgs[i].color = UiKit.Alpha(frameAccent, .12f);
            choiceIconFrames[i].color = UiKit.Alpha(UiKit.Shade(option.Color, .55f), weaponFlow ? .45f : .28f);
            choiceChipEdges[i].color = UiKit.Alpha(option.Color, weaponFlow ? .7f : .32f);
            choiceHexFills[i].color = UiKit.Alpha(UiKit.Shade(option.Color, .7f), .55f);
            choiceHexEdges[i].color = UiKit.Alpha(option.Color, .9f);
            choiceInnerEdges[i].color = UiKit.Alpha(frameAccent, evolution ? .22f : .24f);
            foreach (Transform mark in choiceCornerSets[i]) mark.GetComponent<Image>().color = UiKit.Alpha(frameAccent, evolution ? .55f : .7f);
            choiceBossRings[i].color = UiKit.Alpha(option.Color, bossReward ? .3f : .55f);
            choiceHexFills[i].gameObject.SetActive(bossReward);
            choiceHexEdges[i].gameObject.SetActive(bossReward);
            choiceBossRings[i].gameObject.SetActive(bossReward || evolution);
            choiceCornerSets[i].gameObject.SetActive(bossReward || evolution);
            choiceInnerEdges[i].gameObject.SetActive(bossReward || evolution);
            choiceIconFrames[i].gameObject.SetActive(!bossReward);
            choiceChipEdges[i].gameObject.SetActive(!bossReward);
            var hover = choiceButtons[i].GetComponent<UiHover>();
            if (hover != null) { hover.edgeIdle = UiKit.Alpha(option.Color, edgeAlpha); hover.edgeHot = UiKit.Alpha(option.Color, .98f); }

            var root = choiceRoots[i]; root.sizeDelta = cardSize; root.anchoredPosition = new Vector2((i - (choices.Count - 1) * .5f) * spacing, centerY);
            choiceChips[i].sizeDelta = bossReward ? new Vector2(104, 104) : weaponFlow ? new Vector2(86, 86) : new Vector2(78, 78);
            choiceBossRings[i].rectTransform.sizeDelta = bossReward ? new Vector2(136, 136) : new Vector2(124, 124);
            float titleY = bossReward ? -148f : weaponFlow ? -124f : -114f;
            float detailsY = bossReward ? -198f : weaponFlow ? -170f : -162f;
            choiceTitles[i].rectTransform.anchoredPosition = new Vector2(0, twoLineWeapon ? -120f : titleY);
            choiceTitles[i].rectTransform.sizeDelta = new Vector2(bossReward ? 300 : weaponFlow ? 272 : touchControls ? 268 : 228, twoLineWeapon ? 58 : 46);
            choiceTitles[i].fontSize = twoLineWeapon ? (touchControls ? 20 : 17) : bossReward ? 23 : weaponFlow ? 21 : touchControls ? 21 : 18;
            choiceDetails[i].rectTransform.anchoredPosition = new Vector2(0, twoLineWeapon ? -188f : detailsY);
            choiceDetails[i].alignment = twoLineWeapon ? TextAnchor.MiddleCenter : TextAnchor.UpperCenter;
            choiceDetails[i].fontSize = touchControls ? 15 : bossReward ? 13 : 12;
            choiceDetails[i].lineSpacing = touchControls ? 1.04f : 1.14f;
            choiceDetails[i].rectTransform.sizeDelta = new Vector2(bossReward ? 300 : weaponFlow ? 272 : touchControls ? 264 : 224, twoLineWeapon ? 116 : bossReward ? 70 : 60);
            var badgeRect = (RectTransform)choiceBadges[i].transform.parent;
            badgeRect.sizeDelta = new Vector2(bossReward ? 224 : weaponFlow ? 192 : 160, bossReward ? 22 : 20);
            choiceBadges[i].fontSize = bossReward ? 9 : 8;
            choiceTopBars[i].rectTransform.sizeDelta = new Vector2(bossReward ? 220 : weaponFlow ? 190 : 160, 2);
        }
        var rerollBody = rerollButton.GetComponent<Image>();
        var rerollEdge = rerollButton.transform.Find("Edge")?.GetComponent<Image>();
        if (rerollBody != null) rerollBody.color = UiKit.Mix(UiKit.PanelSoft, pageAccent, .1f);
        if (rerollEdge != null) rerollEdge.color = UiKit.Alpha(pageAccent, .52f);
        var rerollHover = rerollButton.GetComponent<UiHover>();
        if (rerollHover != null) { rerollHover.fillIdle = rerollBody != null ? rerollBody.color : rerollHover.fillIdle; rerollHover.edgeIdle = UiKit.Alpha(pageAccent, .52f); rerollHover.edgeHot = UiKit.Alpha(pageAccent, 1f); }
        int remainingRerolls = weaponFlow ? weaponRerolls : normalRerolls;
        rerollButton.interactable = flow != UpgradeFlow.Boss && remainingRerolls > 0;
        rerollButton.gameObject.SetActive(flow != UpgradeFlow.Boss);
        rerollLabel.text = (touchControls ? "" : "R  /  ") + "刷新选项  ·  剩余 " + remainingRerolls;
        State = RunState.Upgrade; upgradePanel.SetActive(true); upgradePanel.transform.SetAsLastSibling(); Time.timeScale = 0;
    }
}

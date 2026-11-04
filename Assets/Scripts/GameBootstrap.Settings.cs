using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Settings console: left rail navigation, one register per page, changes saved and applied immediately.
public partial class GameBootstrap
{
    private GameObject settingsPanel;
    private bool settingsOpen;
    private int settingsPage;
    private Text settingsToast;
    private readonly Text[] settingsNavLabels = new Text[4];
    private readonly Text[] settingsNavIndices = new Text[4];
    private readonly Image[] settingsNavMarks = new Image[4];
    private readonly Image[] settingsNavBodies = new Image[4];
    private readonly RectTransform[] settingsPages = new RectTransform[4];
    private readonly List<Action> settingsRefreshers = new List<Action>();

    // Row metrics; touch layouts get larger rows and targets.
    private float SettingsRowHeight => touchControls ? 84f : 70f;
    private float SettingsControlX => touchControls ? 220f : 200f;
    private int SettingsLabelSize => touchControls ? 19 : 17;

    private void OpenSettings()
    {
        if (settingsPanel == null) BuildSettingsPanel();
        settingsOpen = true;
        settingsPanel.SetActive(true);
        settingsPanel.transform.SetAsLastSibling();
        ShowSettingsPage(settingsPage);
        RefreshSettings();
    }

    private void CloseSettings()
    {
        if (!settingsOpen) return;
        settingsOpen = false;
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void CommitSettings()
    {
        GameSettings.Save();
        GameSettings.Apply();
        RefreshSettings();
    }

    private void RefreshSettings()
    {
        foreach (Action refresh in settingsRefreshers) refresh();
    }

    private void BuildSettingsPanel()
    {
        settingsRefreshers.Clear();
        bool touch = touchControls;
        settingsPanel = NewUI<Image>("Settings screen", overlay).gameObject;
        Stretch((RectTransform)settingsPanel.transform);
        var background = settingsPanel.GetComponent<Image>();
        background.color = new Color(.006f, .012f, .022f, .94f);
        background.raycastTarget = true;

        var root = (RectTransform)settingsPanel.transform;
        Art(root, "Vignette", UiKit.Vignette, new Color(0, 0, 0, .6f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        // Canvas is 1440x810 (desktop 16:9) or 1800x810 (touch 20:9) inside the safe area; keep >=30 units of margin.
        float width = touch ? 1440f : 1240f, height = touch ? 750f : 736f;
        var modal = Frame(root, "Settings frame", AnchorCC, Vector2.zero, new Vector2(width, height), GameRules.Cyan, .98f);
        Art(modal, "Header wash", UiKit.GradientUp, UiKit.Alpha(GameRules.Cyan, .07f), new Vector2(0, .84f), new Vector2(1, 1), AnchorCC, Vector2.zero, Vector2.zero);

        // Header: identity on the left, close on the right.
        Lt(modal, UiKit.Track("SYSTEM CONFIG"), 11, new Vector2(44, -30), new Vector2(400, 18), GameRules.Cyan, FontStyle.Bold);
        Lt(modal, "设置", 30, new Vector2(44, -50), new Vector2(300, 44), UiKit.TextMain, FontStyle.Bold);
        float closeSize = touch ? 68f : 52f;
        Cta(modal, "Close settings", "×", AnchorTR, new Vector2(-28, -24), new Vector2(closeSize, closeSize), GameRules.Cyan, false, CloseSettings, touch ? 28 : 24);
        HLine(modal, "Header rule", 28, -112, 1, new Color(1, 1, 1, .08f));

        // Navigation rail.
        const float top = -128f, railX = 28f, footer = 28f;
        float railWidth = touch ? 270f : 236f, bodyHeight = height + top - footer;
        var nav = Node(modal, "Settings navigation", AnchorTL, new Vector2(railX, top), new Vector2(railWidth, bodyHeight));
        Art(nav, "Rail", UiKit.Soft, new Color(.02f, .04f, .06f, .55f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        string[] names = { "音频", "画面", "通用", "数据" };
        float navHeight = touch ? 84f : 62f, navGap = 6f, itemWidth = railWidth - 16f;
        for (int i = 0; i < 4; i++)
        {
            int page = i;
            var item = Node(nav, "Settings nav " + i, AnchorTL, new Vector2(8, -8 - i * (navHeight + navGap)), new Vector2(itemWidth, navHeight));
            var body = item.gameObject.AddComponent<Image>();
            body.sprite = UiKit.Soft; body.type = Image.Type.Sliced; body.raycastTarget = true;
            var button = item.gameObject.AddComponent<Button>();
            button.targetGraphic = body;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => ShowSettingsPage(page));
            settingsNavBodies[i] = body;
            settingsNavMarks[i] = Art(item, "Active mark", UiKit.Solid, GameRules.Cyan, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(0, .5f), Vector2.zero, new Vector2(3, navHeight - 26f));
            settingsNavIndices[i] = Lm(item, (i + 1).ToString("00"), 11, new Vector2(18, 0), new Vector2(28, navHeight), UiKit.TextMute, FontStyle.Bold);
            settingsNavLabels[i] = Lm(item, names[i], touch ? 22 : 19, new Vector2(50, 0), new Vector2(110, navHeight), UiKit.TextDim, FontStyle.Bold);
        }

        // Content register.
        float contentX = railX + railWidth + 44f;
        var content = Node(modal, "Settings content", AnchorTL, new Vector2(contentX, top - 4f), new Vector2(width - contentX - 44f, bodyHeight));
        float row = SettingsRowHeight, first = -56f;

        settingsPages[0] = SettingsPage(content, "音频", "AUDIO");
        SliderRow(settingsPages[0], first, "主音量", () => GameSettings.Master, value => GameSettings.Master = value);
        SliderRow(settingsPages[0], first - row, "音乐", () => GameSettings.Music, value => GameSettings.Music = value);
        SliderRow(settingsPages[0], first - row * 2, "音效", () => GameSettings.Sfx, value => GameSettings.Sfx = value);

        settingsPages[1] = SettingsPage(content, "画面", "VIDEO");
        OptionRow(settingsPages[1], first, "画质", new[] { "低", "中", "高" }, () => GameSettings.Quality, value => GameSettings.Quality = value);
        OptionRow(settingsPages[1], first - row, "帧率", new[] { "30", "60", "120" }, () => GameSettings.Fps == 30 ? 0 : GameSettings.Fps == 120 ? 2 : 1, value => GameSettings.Fps = value == 0 ? 30 : value == 2 ? 120 : 60);
        ToggleRow(settingsPages[1], first - row * 2, "伤害数字", () => GameSettings.DamageNumbers, value => GameSettings.DamageNumbers = value, true);
        OptionRow(settingsPages[1], first - row * 3, "屏幕震动", new[] { "关", "低", "标准" }, () => GameSettings.Shake, value => GameSettings.Shake = value);
        OptionRow(settingsPages[1], first - row * 4, "粒子密度", new[] { "低", "中", "高" }, () => GameSettings.Particles, value => GameSettings.Particles = value);

        settingsPages[2] = SettingsPage(content, "通用", "GENERAL");
        OptionRow(settingsPages[2], first, "语言", new[] { "简体中文", "English" }, () => GameSettings.Language, value => GameSettings.Language = value);
        ToggleRow(settingsPages[2], first - row, "失焦自动暂停", () => GameSettings.AutoPause, value => GameSettings.AutoPause = value);
        var exitRow = SettingsRow(settingsPages[2], first - row * 2, "退出");
        float exitHeight = touch ? 68f : 46f;
        Cta(exitRow, "Exit game", "退出游戏", AnchorTL, new Vector2(SettingsControlX, -(row - exitHeight) * .5f), new Vector2(touch ? 240f : 200f, exitHeight), new Color(1f, .45f, .48f), false, () => Application.Quit(), touch ? 17 : 15);

        settingsPages[3] = SettingsPage(content, "数据", "DATA");
        BuildDataPage(settingsPages[3]);
        ShowSettingsPage(settingsPage);
    }

    // Data: the save feature is not implemented; show that state plainly and keep the entries visibly disabled.
    private void BuildDataPage(RectTransform page)
    {
        bool touch = touchControls;
        float noticeHeight = touch ? 104f : 92f;
        var notice = Node(page, "Data status", new Vector2(0, 1), new Vector2(1, 1), new Vector2(.5f, 1), new Vector2(0, -68), new Vector2(0, noticeHeight));
        Art(notice, "Fill", UiKit.Soft, UiKit.Alpha(UiKit.Gold, .06f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        Fill(notice, "Edge", UiKit.SoftEdge, UiKit.Alpha(UiKit.Gold, .32f));
        var chip = Node(notice, "Status chip", AnchorTL, new Vector2(22, -20), new Vector2(touch ? 80f : 70f, touch ? 30f : 26f));
        Art(chip, "Fill", UiKit.SoftSmall, UiKit.Alpha(UiKit.Gold, .16f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        Mc(chip, "未开放", touch ? 14 : 13, UiKit.Gold, FontStyle.Bold);
        Lm(notice, "存档导入与导出", touch ? 19 : 17, new Vector2(touch ? 118f : 108f, -18), new Vector2(400, touch ? 34f : 30f), UiKit.TextMain, FontStyle.Bold);
        Lt(notice, "将在后续版本接入，当前不可使用。", touch ? 16 : 14, new Vector2(22, touch ? -62f : -56f), new Vector2(700, 24), UiKit.TextDim);

        float buttonY = -68f - noticeHeight - 24f, buttonHeight = touch ? 68f : 50f, buttonWidth = touch ? 260f : 220f;
        UnavailableButton(page, "Export save", "导出存档", new Vector2(0, buttonY), new Vector2(buttonWidth, buttonHeight));
        UnavailableButton(page, "Import save", "导入存档", new Vector2(buttonWidth + 16f, buttonY), new Vector2(buttonWidth, buttonHeight));
        settingsToast = Lm(page, "", touch ? 16 : 14, new Vector2(0, buttonY - buttonHeight - 10f), new Vector2(700, 30), UiKit.Gold, FontStyle.Bold);
        settingsToast.gameObject.SetActive(false);
    }

    private void UnavailableButton(Transform parent, string name, string caption, Vector2 position, Vector2 size)
    {
        var root = Node(parent, name, AnchorTL, position, size);
        var body = root.gameObject.AddComponent<Image>();
        body.sprite = UiKit.Soft; body.type = Image.Type.Sliced; body.raycastTarget = true;
        body.color = new Color(.03f, .05f, .07f, .6f);
        Fill(root, "Edge", UiKit.SoftEdge, new Color(1, 1, 1, .1f));
        Mc(root, caption, touchControls ? 17 : 15, UiKit.TextMute, FontStyle.Bold);
        var button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = body; button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() => SettingsUnavailable(caption));
    }

    private RectTransform SettingsPage(Transform content, string title, string track)
    {
        var page = Node(content, "Settings page " + title, Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        Lt(page, title, 24, new Vector2(0, 0), new Vector2(300, 34), UiKit.TextMain, FontStyle.Bold);
        HLine(page, "Page rule", 0, -46, 1, new Color(1, 1, 1, .1f));
        page.gameObject.SetActive(false);
        return page;
    }

    private void ShowSettingsPage(int page)
    {
        settingsPage = Mathf.Clamp(page, 0, 3);
        for (int i = 0; i < settingsPages.Length; i++)
            if (settingsPages[i] != null) settingsPages[i].gameObject.SetActive(i == settingsPage);
        if (settingsToast != null) settingsToast.gameObject.SetActive(false);
        for (int i = 0; i < settingsNavLabels.Length; i++)
        {
            if (settingsNavLabels[i] == null) continue;
            bool active = i == settingsPage;
            settingsNavBodies[i].color = active ? UiKit.Alpha(GameRules.Cyan, .12f) : new Color(1, 1, 1, .001f);
            settingsNavMarks[i].color = active ? GameRules.Cyan : new Color(1, 1, 1, .1f);
            settingsNavLabels[i].color = active ? GameRules.Cyan : UiKit.TextDim;
            settingsNavIndices[i].color = active ? GameRules.Cyan : UiKit.TextMute;
        }
    }

    // Full-width register line with its label and a hairline separator.
    private RectTransform SettingsRow(Transform parent, float y, string label)
    {
        float height = SettingsRowHeight;
        var row = Node(parent, label + " row", new Vector2(0, 1), new Vector2(1, 1), new Vector2(.5f, 1), new Vector2(0, y), new Vector2(0, height));
        Art(row, "Row rule", UiKit.Solid, new Color(1, 1, 1, .06f), Vector2.zero, new Vector2(1, 0), new Vector2(.5f, 0), Vector2.zero, new Vector2(0, 1));
        Lm(row, label, SettingsLabelSize, Vector2.zero, new Vector2(SettingsControlX - 16f, height), UiKit.TextMain, FontStyle.Bold);
        return row;
    }

    private void SliderRow(Transform parent, float y, string label, Func<int> get, Action<int> set)
    {
        bool touch = touchControls;
        var row = SettingsRow(parent, y, label);
        float valueWidth = 96f, hitHeight = touch ? 60f : 44f, knob = touch ? 32f : 24f;

        var sliderRoot = NewUI<RectTransform>(label + " slider", row);
        sliderRoot.anchorMin = new Vector2(0, .5f); sliderRoot.anchorMax = new Vector2(1, .5f); sliderRoot.pivot = new Vector2(.5f, .5f);
        sliderRoot.offsetMin = new Vector2(SettingsControlX, -hitHeight * .5f);
        sliderRoot.offsetMax = new Vector2(-valueWidth, hitHeight * .5f);
        // Transparent hit area so the whole track row accepts press and drag.
        var hit = sliderRoot.gameObject.AddComponent<Image>();
        hit.color = new Color(1, 1, 1, .001f); hit.raycastTarget = true;
        Art(sliderRoot, "Track", UiKit.Solid, new Color(.16f, .3f, .35f, .85f), new Vector2(0, .5f), new Vector2(1, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(0, 6));
        // Slider drives the fill's vertical anchors to 0..1, so the fill lives in a 6-unit-high area.
        var fillArea = Node(sliderRoot, "Fill area", new Vector2(0, .5f), new Vector2(1, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(0, 6));
        var fill = Art(fillArea, "Fill", UiKit.Solid, GameRules.Cyan, Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        var handleArea = Node(sliderRoot, "Handle area", Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        var handle = Art(handleArea, "Handle", UiKit.Dot, GameRules.Cyan, Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        Art(handle.rectTransform, "Core", UiKit.Dot, UiKit.Panel, AnchorCC, Vector2.zero, new Vector2(knob * .36f, knob * .36f));

        var slider = sliderRoot.gameObject.AddComponent<Slider>();
        slider.transition = Selectable.Transition.None;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.wholeNumbers = false;
        slider.targetGraphic = hit;
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        // Handle height = area height + sizeDelta.y, so this yields a knob-sized circle.
        handle.rectTransform.sizeDelta = new Vector2(knob, knob - hitHeight);
        slider.SetValueWithoutNotify(Mathf.Clamp(get(), 0, 100));

        var value = MakeText(row, "Value", get() + "%", touch ? 22 : 20, TextAnchor.MiddleRight, new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(1, .5f), Vector2.zero, new Vector2(valueWidth - 20f, 40), GameRules.Cyan, FontStyle.Bold, null);
        bool updating = false;
        slider.onValueChanged.AddListener(raw =>
        {
            if (updating) return;
            updating = true;
            int rounded = Mathf.Clamp(Mathf.RoundToInt(raw / 5f) * 5, 0, 100);
            slider.SetValueWithoutNotify(rounded);
            updating = false;
            set(rounded);
            CommitSettings();
        });
        settingsRefreshers.Add(() =>
        {
            int current = Mathf.Clamp(get(), 0, 100);
            slider.SetValueWithoutNotify(current);
            value.text = current + "%";
        });
    }

    private void OptionRow(Transform parent, float y, string label, string[] options, Func<int> get, Action<int> set)
    {
        bool touch = touchControls;
        var row = SettingsRow(parent, y, label);
        int count = options.Length;
        float width = count == 2 ? (touch ? 150f : 128f) : (touch ? 120f : 100f);
        float height = touch ? 60f : 44f, gap = 6f;
        var bodies = new Image[count];
        var edges = new Image[count];
        var labels = new Text[count];
        for (int i = 0; i < count; i++)
        {
            int index = i;
            var button = Node(row, label + " option " + i, AnchorTL, new Vector2(SettingsControlX + i * (width + gap), -(SettingsRowHeight - height) * .5f), new Vector2(width, height));
            var body = button.gameObject.AddComponent<Image>();
            body.sprite = UiKit.Soft; body.type = Image.Type.Sliced; body.raycastTarget = true;
            var edge = Fill(button, "Edge", UiKit.SoftEdge, new Color(1, 1, 1, .12f));
            var text = Mc(button, options[i], touch ? 17 : 15, UiKit.TextDim, FontStyle.Bold);
            var click = button.gameObject.AddComponent<Button>();
            click.targetGraphic = body;
            click.transition = Selectable.Transition.None;
            click.onClick.AddListener(() =>
            {
                set(index);
                CommitSettings();
            });
            bodies[i] = body; edges[i] = edge; labels[i] = text;
        }
        settingsRefreshers.Add(() =>
        {
            int current = get();
            for (int i = 0; i < count; i++)
            {
                bool active = i == current;
                bodies[i].color = active ? UiKit.Alpha(GameRules.Cyan, .2f) : new Color(.03f, .06f, .085f, .85f);
                edges[i].color = active ? UiKit.Alpha(GameRules.Cyan, .85f) : new Color(1, 1, 1, .12f);
                labels[i].color = active ? GameRules.Cyan : UiKit.TextDim;
            }
        });
        settingsRefreshers[settingsRefreshers.Count - 1]();
    }

    private void ToggleRow(Transform parent, float y, string label, Func<bool> get, Action<bool> set, bool offFirst = false)
    {
        string[] options = offFirst ? new[] { "关", "开" } : new[] { "开", "关" };
        OptionRow(parent, y, label, options, () => offFirst ? (get() ? 1 : 0) : (get() ? 0 : 1), value => set(offFirst ? value == 1 : value == 0));
    }

    private void SettingsUnavailable(string action)
    {
        if (settingsToast == null) return;
        settingsToast.text = action + "：功能暂未开放";
        settingsToast.gameObject.SetActive(true);
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) TryAutoPause();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) TryAutoPause();
    }

    private void TryAutoPause()
    {
        if (!GameSettings.AutoPause || settingsOpen) return;
        if (State != RunState.Playing || deathSequence || victoryPending || pendingBossRewardStage > 0) return;
        Pause();
    }
}

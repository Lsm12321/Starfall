using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private float labelRefreshTimer;
    private int lastBossHudStage = -1;
    private void RefreshUI()
    {
        UpdateGameplayUiScale();
        bool active = Player != null;
        if (hudRoot.activeSelf != active) hudRoot.SetActive(active);
        if (!active || clockText == null) return;
        float dt = Mathf.Min(.1f, Time.unscaledDeltaTime);
        labelRefreshTimer -= Time.unscaledDeltaTime;
        bool labels = labelRefreshTimer <= 0f;
        if (labels) labelRefreshTimer = .1f;

        if (labels)
        {
            SetText(clockText, GameRules.TimeLabel(Elapsed));
            SetText(waveText, GameRules.StageName(Mathf.Clamp(BossesDefeated + 1, 1, GameRules.BossCount)));
            SetText(waveLabelText, UiKit.Track("WAVE " + Wave.ToString("00") + " / 18"));
            SetText(killText, Kills.ToString("000"));
            SetText(levelText, Level.ToString("00"));
            SetText(stageIndexText, "STAGE " + Mathf.Clamp(BossesDefeated + 1, 1, GameRules.BossCount).ToString("00") + " / " + GameRules.BossCount.ToString("00"));
            SetText(stageSectorText, UiKit.Track("SECTOR " + Mathf.Clamp(BossesDefeated + 1, 1, GameRules.BossCount).ToString("00")));
            SetText(healthText, $"{Player.Health:000} / {Player.MaxHealth:000}");
            SetText(xpText, $"XP   {Xp} / {XpNeeded}");
            SetText(pilotText, CurrentCharacter.CallSign);
            for (int i = 0; i < stagePips.Length; i++)
            {
                if (stagePips[i] == null) continue;
                Color stageColor = GameRules.BossColor(i + 1);
                stagePips[i].color = i < BossesDefeated ? stageColor : i == BossesDefeated ? UiKit.Alpha(stageColor, .9f) : new Color(1, 1, 1, .12f);
            }
            if (stageEdge != null) stageEdge.color = UiKit.Alpha(GameRules.BossColor(Mathf.Clamp(BossesDefeated + 1, 1, GameRules.BossCount)), .4f);
            if (hintText != null)
            {
                // Control hints are an onboarding aid: full strength for the opening seconds, then they step back.
                // Touch controls are self-labelled, so the phone hint leaves the battlefield entirely.
                float settle = Mathf.Clamp01((Elapsed - 12f) / 2.5f);
                Color hint = touchControls ? UiKit.TextDim : UiKit.TextMute;
                hint.a *= Mathf.Lerp(1f, touchControls ? 0f : .45f, settle);
                hintText.color = hint;
                // Only while actually playing: overlays (upgrades, cores, sheet) must not show HUD text bleeding through.
                bool showHint = hint.a > .01f && State == RunState.Playing;
                if (hintText.enabled != showHint) hintText.enabled = showHint;
            }
        }

        float hpTarget = Mathf.Clamp01(Player.Health / (float)Player.MaxHealth);
        hpDisplay = Mathf.Lerp(hpDisplay, hpTarget, 1f - Mathf.Exp(-dt * 16f));
        if (hpGhostDisplay < hpTarget) hpGhostDisplay = hpTarget;
        else hpGhostDisplay = Mathf.Max(hpTarget, hpGhostDisplay - dt * .38f);
        hpFill.fillAmount = hpDisplay; hpGhost.fillAmount = hpGhostDisplay;
        float lowHealth = Mathf.Clamp01((.34f - hpTarget) / .34f);
        if (lowHealth > .01f)
        {
            float pulse = .5f + .5f * Mathf.Sin(Time.unscaledTime * 6.5f);
            hpGlow.color = UiKit.Alpha(GameRules.Pink, .12f + .28f * lowHealth * pulse);
            healthEdge.color = UiKit.Alpha(GameRules.Pink, .34f + .46f * lowHealth * pulse);
        }
        else
        {
            hpGlow.color = UiKit.Alpha(GameRules.Pink, .14f);
            healthEdge.color = UiKit.Alpha(GameRules.Pink, .34f);
        }

        waveFill.fillAmount = WaveProgress;
        float dashTarget = Player.DashReady ? 1f : 1f - Player.DashCooldown / Player.DashCooldownMax;
        dashDisplay = Mathf.Lerp(dashDisplay, dashTarget, 1f - Mathf.Exp(-dt * 18f));
        dashFill.fillAmount = dashDisplay;
        if (labels)
        {
            SetText(dashText, Player.DashReady ? "Ready" : Player.DashCooldown.ToString("0.0") + "s");
            dashText.color = Player.DashReady ? GameRules.Cyan : UiKit.TextDim;
        }

        xpDisplay = Mathf.Lerp(xpDisplay, Xp / (float)XpNeeded, 1f - Mathf.Exp(-dt * 12f));
        xpFill.fillAmount = xpDisplay;

        if (labels)
        {
            for (int i = 0; i < 5; i++)
            {
                if (weaponLabels[i] == null) continue;
                bool unlocked = Stats.Weapons[i] > 0; int rank = Stats.Weapons[i]; bool evolved = Stats.EvolvedWeapons[i];
                SetText(weaponLabels[i], unlocked ? WeaponShort(i) : "—  LOCKED");
                SetText(weaponRanks[i], unlocked ? "Lv " + rank + "/" + GameRules.MaxWeaponRank : string.Empty);
                weaponLabels[i].color = unlocked ? (evolved ? GameRules.Gold : UiKit.TextMain) : UiKit.TextMute;
                weaponRanks[i].color = unlocked ? (evolved ? GameRules.Gold : UiKit.TextMain) : UiKit.TextMute;
                weaponEvoChips[i].transform.parent.gameObject.SetActive(unlocked && evolved);
                weaponIcons[i].sprite = GetSprite("Icons/" + GameRules.IconAsset(i));
                weaponIcons[i].color = unlocked ? (evolved ? Color.Lerp(Color.white, GameRules.Gold, .25f) : Color.white) : new Color(1, 1, 1, .2f);
                for (int p = 0; p < 5; p++)
                    weaponPips[i, p].color = unlocked && p < rank ? UiKit.Alpha(WeaponTint(i), .95f) : new Color(1, 1, 1, .07f);
            }
        }

        // The boss bar is only visible while a boss is alive; after a kill it disappears until the next boss spawns.
        bool bossEncounterActive = Boss != null;
        if (bossPanel.activeSelf != bossEncounterActive) bossPanel.SetActive(bossEncounterActive);
        int displayBossStage = Boss != null ? Boss.BossStage : pendingBossRewardStage > 0 ? pendingBossRewardStage : State == RunState.Upgrade && currentUpgradeFlow == UpgradeFlow.Boss ? rewardBossStage : Mathf.Clamp(BossesDefeated + 1, 1, GameRules.BossCount);
        if (bossEncounterActive)
        {
            // Header and portrait update the frame the encounter changes; no per-frame string work.
            if (lastBossHudStage != displayBossStage)
            {
                lastBossHudStage = displayBossStage;
                Color bossColor = GameRules.BossColor(displayBossStage);
                SetText(bossHeader, BossName(displayBossStage));
                bossHeader.color = bossColor;
                bossPortrait.sprite = GetSprite(GameRules.BossVisual(displayBossStage));
                bossPortrait.color = Color.white;
                bossFrameEdge.color = UiKit.Alpha(bossColor, .4f);
            }
        }
        else lastBossHudStage = -1;
        float bossTarget = Boss != null ? Mathf.Clamp01(Boss.Health / (float)Boss.MaxHealth) : 0f;
        bossDisplay = Boss == null ? bossTarget : Mathf.Lerp(bossDisplay, bossTarget, 1f - Mathf.Exp(-dt * 14f));
        bossFill.fillAmount = bossDisplay;
        if (labels)
        {
            if (Boss != null)
            {
                SetText(bossText, "PHASE " + Boss.BossStage.ToString("00") + "  /  " + (Boss.PhaseTwo ? "PHASE II  ·  ENRAGED" : "PHASE I  ·  HUNTER"));
                bossText.color = Boss.PhaseTwo ? GameRules.Pink : UiKit.TextDim;
            }
            else if (victoryPending) { SetText(bossText, "ALL THREE BOSSES DOWN  /  GATE SECURED"); }
            else if (bossTransitionTimer > 0) { SetText(bossText, "STAGE " + (BossesDefeated + 1).ToString("00") + "  /  BOSS IN " + GameRules.TimeLabel(bossTransitionTimer)); }
            else if (State == RunState.Upgrade && currentUpgradeFlow == UpgradeFlow.Boss) { SetText(bossText, "CORE EXPOSED  /  SELECT RESONANCE"); }
        }

        float remaining = bannerUntil - Time.unscaledTime;
        bool showBanner = !string.IsNullOrEmpty(bannerMessage) && remaining > -.2f && State == RunState.Playing;
        if (bannerRoot.gameObject.activeSelf != showBanner) bannerRoot.gameObject.SetActive(showBanner);
        if (showBanner)
        {
            float age = 2.5f - remaining;
            bannerGroup.alpha = Mathf.Clamp01(age / .18f) * Mathf.Clamp01(remaining / .5f);
            bannerRoot.anchoredPosition = new Vector2(0, -122f - (1f - Mathf.Clamp01(age / .24f)) * 10f);
            SetText(bannerText, bannerMessage);
            bannerText.color = Color.Lerp(bannerColor, Color.white, .3f);
            bannerAccent.color = bannerColor;
        }

        if (bossWarningPanel != null && bossWarningPanel.activeSelf)
        {
            float remain = bossWarningUntil - Time.unscaledTime;
            if (remain <= 0f) bossWarningPanel.SetActive(false);
            else
            {
                float age = BossWarningSeconds - remain;
                bossWarningGroup.alpha = Mathf.Clamp01(age / .18f) * Mathf.Clamp01(remain / .35f);
                float pulse = .5f + .5f * Mathf.Sin(Time.unscaledTime * 12f);
                bossWarningTitle.color = Color.Lerp(Color.white, GameRules.Gold, .22f * pulse);
                bossWarningPanel.transform.localScale = Vector3.one * (1f + .02f * pulse);
            }
        }
        if (touchRoot != null) touchRoot.SetActive(State == RunState.Playing && !deathSequence && !victoryPending && pendingBossRewardStage == 0);
        RefreshTouchHud();
    }

    private string WeaponShort(int id) => new[] { "PULSE", "SCATTER", "ORBITAL", "ARC", "CRYO" }[id];
    // Chinese-first names for result and status tables; English stays only as small kicker labels elsewhere.
    private string WeaponName(int id) => new[] { "星芒脉冲", "余烬散射", "环轨光刃", "连锁电弧", "冰晶长枪" }[id];
    private string WeaponGlyph(int id) => new[] { "P", "S", "O", "Z", "❄" }[id];
    private Color WeaponTint(int id) => GameRules.WeaponColor(id);
}

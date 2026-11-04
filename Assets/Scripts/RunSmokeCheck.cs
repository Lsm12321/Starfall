#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Only runs in a development player when explicitly launched with --smoke-test.
public sealed class RunSmokeCheck : MonoBehaviour
{
    private readonly List<string> checks = new List<string>();
    private readonly List<string> errors = new List<string>();
    private readonly List<string> exceptions = new List<string>();
    private string folder;
    private float started;
    private bool complete;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (!Environment.GetCommandLineArgs().Contains("--smoke-test")) return;
        PlayerPrefs.SetInt("Starfall.Settings.Language", 0); PlayerPrefs.Save();
        var obj = new GameObject("Development smoke check"); DontDestroyOnLoad(obj); obj.AddComponent<RunSmokeCheck>();
    }
    private void Awake()
    {
        folder = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith("--smoke-root="))?.Substring(13) ?? Application.persistentDataPath;
        Directory.CreateDirectory(folder); started = Time.realtimeSinceStartup; Application.logMessageReceived += OnLog;
    }
    private void OnLog(string message, string trace, LogType type)
    { if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) exceptions.Add(message + "\n" + trace); }
    private void Update()
    {
        if (complete) return;
        // Keep running after a failed check so the report and screenshots cover the whole run.
        if (Time.realtimeSinceStartup - started > 55) { errors.Add("Smoke test exceeded 55 seconds."); Finish(); }
    }
    private void Check(bool condition, string name) { checks.Add((condition ? "PASS " : "FAIL ") + name); if (!condition) errors.Add(name); }
    private void Invoke(GameBootstrap game, string method, params object[] args)
    {
        var target = typeof(GameBootstrap).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).First(m => m.Name == method && m.GetParameters().Length == args.Length);
        target.Invoke(game, args);
    }
    private IEnumerator Capture(string name)
    {
        // Capture the presented frame, including the letterbox camera and real viewport.
        // Rendering just the main camera into a texture ignores its screen rect.
        yield return new WaitForEndOfFrame();
        var image = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Path.Combine(folder, name + ".png"), image.EncodeToPNG());
        Destroy(image);
        yield return null;
    }
    private IEnumerator Start()
    {
        yield return null; yield return null;
        var game = GameBootstrap.Game;
        // Regression runs verify the Chinese baseline first; the English route is tested at the end.
        GameSettings.Language = 0; GameSettings.Apply(); yield return null;
        File.WriteAllText(Path.Combine(folder, "viewport.txt"),
            $"Screen: {Screen.width} x {Screen.height}\nSystem safe area: {Screen.safeArea}\nCamera pixel rect: {Camera.main.pixelRect}\nTouch: {game.TouchControls}\n");
        Check(game != null && game.State == RunState.Menu, "Menu initializes");
        var musicKeys = new[] { "music_menu", "music_stage1", "music_stage2", "music_stage3", "music_boss1", "music_boss2", "music_boss3", "music_victory", "music_defeat" };
        Check(musicKeys.All(key => Resources.Load<AudioClip>("Audio/" + key) != null), "All ACE-Step background music clips load");
        yield return Capture("01-menu");
        if (game.TouchControls)
        {
            ValidateMobileLayout("Phone menu layout", "Select pilot", "Settings");
            ValidateSafeArea("Phone menu", "Select pilot", "Settings");
        }
        yield return ValidateSettings(game);
        var select = FindObjectsByType<Button>(FindObjectsSortMode.None).First(b => b.name == "Select pilot");
        select.onClick.Invoke(); yield return null;
        Check(game.State == RunState.CharacterSelect, "Pilot archive opens");
        if (game.TouchControls)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            Check(!FindObjectsByType<RectTransform>(FindObjectsSortMode.None).Any(r => r.name == "Armory telemetry"), "Mobile combat has no upper-right loadout panel");
            ValidateMobileLayout("Phone archive layout", "Featured pilot", "Pilot dossier", "Character card", "Deploy pilot", "Back to title");
            ValidateSafeArea("Phone archive", "Featured pilot", "Deploy pilot", "Back to title");
        }
        Check(CharacterRoster.All.Length == 5 && CharacterRoster.All.Count(p => p.Gender.StartsWith("女")) == 4, "Pilot roster has a one-to-four gender split");
        var loadouts = CharacterRoster.All.Select(p => { var stats = new RunStats(); CharacterRoster.ApplyLoadout(p.Id, stats); return stats; }).ToArray();
        Check(loadouts.All(s => s.Weapons.Count(level => level == 1) == 1) && CharacterRoster.All.Select(p => p.WeaponId).Distinct().Count() == 5, "Every pilot has one distinct signature weapon");
        Check(loadouts[0].Crit > .19f && loadouts[0].Magnet > 3.5f && loadouts[1].Haste > 1.15f && loadouts[2].ChillDuration > 2.0f && loadouts[2].Pierce >= 1 && loadouts[3].BladeRadiusBonus > .49f && loadouts[3].BladeSpeedMultiplier > 1.19f && loadouts[4].LightningDamage > 1.24f, "All five pilot passives affect combat stats");
        var reiPortrait = game.GetSprite("portrait_rei"); var yunaPortrait = game.GetSprite("portrait_yuna");
        Check(reiPortrait != null && yunaPortrait != null && reiPortrait.texture.width > 100 && yunaPortrait.texture.height > 100, "Five ComfyUI pilot portraits loaded");
        var requiredIcons = Enumerable.Range(0, 15).Select(GameRules.IconAsset).Concat(new[] { -1, -2, -3, -4, -5, -6 }.Select(GameRules.IconAsset)).Distinct();
        var missingIcons = requiredIcons.Where(name => Resources.Load<Texture2D>("Art/UI/Icons/" + name) == null).ToArray();
        Check(missingIcons.Length == 0, "Weapon, stat, reinforcement and boss reward icons load");
        var missingPilots = CharacterRoster.All.Where(p => { var sprite = game.GetSprite("pilot_sprite_" + p.Id.ToString().ToLowerInvariant()); return sprite == null || sprite.texture.width != 512; }).ToArray();
        Check(missingPilots.Length == 0 && CharacterRoster.All.All(p => game.GetSprite("pilot_sprite_" + p.Id.ToString().ToLowerInvariant()).texture.height == 640), "All five Blender-rendered pilot models load at full portrait ratio");
        var newEnemyKinds = new[] { EnemyKind.PhaseLancer, EnemyKind.EchoWisp, EnemyKind.RiftBrute, EnemyKind.NovaReaver, EnemyKind.SiegeOrb, EnemyKind.NullMaw };
        Check(newEnemyKinds.All(kind => Resources.Load<Texture2D>("Art/Enemies/" + GameRules.EnemyVisual(kind)) != null), "Six additional tier-two and tier-three enemy models load");
        Check(Enumerable.Range(1, 3).All(stage => Resources.Load<Texture2D>($"Art/Arena/arena-floor-stage{stage}") != null) && Enumerable.Range(1, 3).All(stage => Resources.Load<Texture2D>("Art/Bosses/" + GameRules.BossVisual(stage)) != null) && GameRules.BossVisual(1) != GameRules.BossVisual(2) && GameRules.BossVisual(2) != GameRules.BossVisual(3), "Three stage arenas and all unique boss artworks load");
        Check(GameRules.BossVisual(1) == "starfall_boss_star_eater" && GameRules.BossVisual(3) == "starfall_boss_warden", "Bosses one and three swap their in-game models");
        Check(GameRules.EnemyTierForBossesDefeated(0) == 1 && GameRules.EnemyTierForBossesDefeated(1) == 2 && GameRules.EnemyTierForBossesDefeated(2) == 3 && GameRules.SelectEnemyKind(2, 280, .45f) == EnemyKind.PhaseLancer && GameRules.SelectEnemyKind(2, 280, .05f) == EnemyKind.NovaReaver, "Later enemy tiers retain earlier enemies and add new elite families");
        Check(GameRules.BossIntermissionSeconds(1) >= 120 && GameRules.BossIntermissionSeconds(2) >= 150, "Boss encounters are separated by long, playable minion phases");
        var pilotCards = FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b => b.name == "Character card").ToArray();
        Check(pilotCards.Length == 5, "Five selectable pilots shown");
        Check(pilotCards.All(b => { var image = b.GetComponentsInChildren<Image>().FirstOrDefault(i => i.name == "Pilot portrait"); return image != null && image.preserveAspect && image.rectTransform.rect.height > image.rectTransform.rect.width; }), "Archive cards preserve the portrait aspect ratio");
        var featuredPortrait = FindObjectsByType<Image>(FindObjectsSortMode.None).FirstOrDefault(i => i.name == "Featured portrait");
        Check(featuredPortrait != null && featuredPortrait.preserveAspect && featuredPortrait.sprite != null, "Selected pilot dossier uses a large aspect-preserving portrait");
        var rinCard = pilotCards.FirstOrDefault(b => b.GetComponentsInChildren<Text>().Any(t => t.text.Contains("白濑 凛")));
        Check(rinCard != null, "Cryo pilot card is discoverable");
        yield return Capture("01-characters");
        if (rinCard != null) rinCard.onClick.Invoke(); yield return null;
        Check(game.CurrentCharacter.Id == CharacterKind.Rin, "Pilot selection changes loadout");
        var start = FindObjectsByType<Button>(FindObjectsSortMode.None).First(b => b.GetComponentInChildren<Text>().text.Contains("出击"));
        start.onClick.Invoke(); yield return null;
        Check(game.State == RunState.Playing && game.Player != null, "Begin button starts run");
        if (game.TouchControls)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return ValidateMobileUi(game);
        }
        var arenaFloor = (SpriteRenderer)typeof(GameBootstrap).GetField("arenaFloorRenderer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
        Check(game.ArenaStage == 1 && arenaFloor.sprite.texture == Resources.Load<Texture2D>("Art/Arena/arena-floor-stage1") && FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None).Any(i => i.name == "Boss portrait" && i.preserveAspect), "Stage-one arena art and aspect-preserving boss portrait are wired into the HUD");
        var pilotModel = game.Player.GetComponentsInChildren<SpriteRenderer>().FirstOrDefault(r => r.name == "Pilot model");
        Check(pilotModel != null && pilotModel.sprite == game.GetSprite("pilot_sprite_rin") && pilotModel.sprite.texture.width == 512, "Selected pilot uses her distinct in-game sprite model");
        var weaponSlotIcons = FindObjectsByType<Image>(FindObjectsSortMode.None).Where(i => i.name == "Weapon glyph").ToArray();
        Check(weaponSlotIcons.Length == 0, "Upper-right combat loadout panel is removed");
        Check(game.Stats.Weapons[(int)WeaponKind.Cryo] == 1 && game.Stats.ChillDuration > 1.1f, "Cryo pilot starts with signature weapon and passive");
        Invoke(game, "SpawnEnemy", EnemyKind.Brute, new Vector2(3, 0));
        var cryoTarget = game.Nearby(new Vector2(3, 0), .1f).FirstOrDefault(); yield return new WaitForSeconds(.35f);
        Check(cryoTarget != null && cryoTarget.IsChilled, "Cryo projectiles apply the slow status");
        Invoke(game, "SpawnEnemy", EnemyKind.Grunt, new Vector2(1.5f, 0));
        yield return new WaitForSeconds(3.3f);
        Check(game.Kills > 0, "Automatic targeting and projectile kills enemy");
        int kills = game.Kills; var pickup = FindObjectsByType<XpPickup>(FindObjectsSortMode.None).FirstOrDefault();
        if (pickup != null) game.Player.transform.position = pickup.transform.position;
        yield return new WaitForSeconds(.25f);
        Check(game.Xp > 0, "XP drops move into player and are collected");
        game.AddXp(game.XpNeeded); yield return null;
        Check(game.State == RunState.Upgrade && Time.timeScale == 0, "Level-up pauses battle");
        var cards = FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b => b.name == "Upgrade card").ToArray();
        Check(cards.Length == 3, "Three clickable upgrade cards");
        Check(cards.All(b => { var icon = b.GetComponentsInChildren<Image>().FirstOrDefault(img => img.name == "Skill icon art"); return icon != null && icon.sprite != null; }), "Each upgrade choice displays its dedicated icon");
        var titles = cards.Select(b => b.GetComponentsInChildren<Text>().First(t => t.name == "Choice title").text).ToArray();
        Check(titles.Distinct().Count() == 3, "Upgrade choices are unique");
        var choiceField = typeof(GameBootstrap).GetField("choices", BindingFlags.Instance | BindingFlags.NonPublic);
        var normalChoices = (List<UpgradeOption>)choiceField.GetValue(game);
        Check(normalChoices.All(option => option.Flow == UpgradeFlow.Normal && (option.IsWeapon ? game.Stats.Weapons[option.Id] > 0 : option.Id >= 5 && option.Id <= 14)), "Normal upgrades contain attributes or skills for already owned weapons");
        var survivalFour = new RunStats(); CharacterRoster.ApplyLoadout(CharacterKind.Mei, survivalFour);
        var survivalTwo = new RunStats(); CharacterRoster.ApplyLoadout(CharacterKind.Yuna, survivalTwo);
        var survivalThree = new RunStats(); CharacterRoster.ApplyLoadout(CharacterKind.Rei, survivalThree);
        Check(survivalFour.MaxHealth == 120 && survivalTwo.MaxHealth == 90 && survivalThree.MaxHealth == 100, "Survival stat sets base health to 120 / 100 / 90");
        var rinLoadout = new RunStats(); CharacterRoster.ApplyLoadout(CharacterKind.Rin, rinLoadout);
        Check(Mathf.Approximately(rinLoadout.ChillDuration, 2.1f) && rinLoadout.Pierce == 1, "Zero-domain resonance grants +1s chill and +1 pierce");
        var meiLoadout = new RunStats(); CharacterRoster.ApplyLoadout(CharacterKind.Mei, meiLoadout);
        Check(Mathf.Approximately(meiLoadout.BladeRadiusBonus, .5f) && Mathf.Approximately(meiLoadout.BladeSpeedMultiplier, 1.2f), "Orbit calibration grants +0.5m radius and +20% blade speed");
        var healthOnly = new RunStats();
        for (int i = 0; i < healthOnly.Weapons.Length; i++) healthOnly.Weapons[i] = GameRules.MaxWeaponRank;
        for (int i = 5; i < healthOnly.Ranks.Length; i++) healthOnly.Ranks[i] = GameRules.MaxRank(i);
        healthOnly.Ranks[14] = 0;
        Check(GameRules.Choices(healthOnly).Any(option => option.Id == 14 && option.Description.Contains("生命上限 +20") && option.Description.Contains("恢复 40")), "Health expansion adds +20 maximum health and restores 40 health");
        var coresAfterOverload = GameRules.BossChoices(2, new[] { true, false, false });
        var coreAfterTwo = GameRules.BossChoices(3, new[] { true, false, true });
        Check(coresAfterOverload.Count == 2 && coresAfterOverload.All(option => option.RewardId != 0) && coreAfterTwo.Count == 1 && coreAfterTwo[0].RewardId == 1, "Boss core drafts exclude rewards already taken during the run");
        Check(GameRules.XpNeeded(1) > 27 && GameRules.XpNeeded(12) > GameRules.XpNeeded(11), "XP thresholds rise to slow repeated upgrade screens");
        var categoryIds = normalChoices.Select(option => option.IsWeapon || option.Id == 5 || option.Id == 6 || option.Id == 8 || option.Id == 10 || option.Id == 11 ? 0 : option.Id == 12 || option.Id == 13 || option.Id == 14 ? 1 : 2).Distinct().Count();
        Check(categoryIds == 3, "Normal choices cover three different stat categories");
        var weaponSkillStats = new RunStats(); weaponSkillStats.Weapons[0] = 2;
        for (int i = 5; i <= 13; i++) weaponSkillStats.Ranks[i] = GameRules.MaxRank(i);
        weaponSkillStats.Ranks[5] = 0;
        var ownedWeaponOnlyDraft = GameRules.Choices(weaponSkillStats);
        Check(ownedWeaponOnlyDraft.Any(option => option.IsWeapon && option.Id == 0) && ownedWeaponOnlyDraft.All(option => !option.IsWeapon || weaponSkillStats.Weapons[option.Id] > 0), "Normal choices can upgrade an owned weapon but cannot unlock one");
        var rerollAvoid = new List<int> { 5, 6, 7, 8 };
        Check(GameRules.Choices(weaponSkillStats, rerollAvoid).All(option => !rerollAvoid.Contains(option.Id)), "Normal rerolls exclude the previous option ids");
        var weaponDraftStats = new RunStats(); weaponDraftStats.Weapons[0] = 1; weaponDraftStats.Weapons[1] = 1;
        var weaponAvoid = new List<int> { 0, 1 };
        Check(GameRules.WeaponChoices(weaponDraftStats, weaponAvoid).All(option => !weaponAvoid.Contains(option.Id)), "Weapon rerolls exclude the previous option ids");
        var weaponDraftSamples = Enumerable.Range(0, 40).Select(_ => GameRules.WeaponChoices(weaponDraftStats)).ToArray();
        Check(weaponDraftSamples.All(draft => draft.Count == 3 && draft.Select(option => option.Id).Distinct().Count() == 3 && draft.All(option => option.Flow == UpgradeFlow.Weapon) && draft.All(option => !option.IsWeapon || weaponDraftStats.Weapons[option.Id] == 0 || option.IsEvolution)) && weaponDraftSamples.Any(draft => draft.Any(option => option.Id >= 0 && weaponDraftStats.Weapons[option.Id] == 0)) && weaponDraftSamples.Any(draft => draft.Any(option => option.IsEvolution && weaponDraftStats.Weapons[option.Id] > 0)), "Weapon draft offers only new weapons and evolutions, never weapon rank upgrades");
        var limitedWeaponStats = new RunStats(); for (int i = 0; i < 5; i++) limitedWeaponStats.Weapons[i] = 1; limitedWeaponStats.Weapons[4] = 0;
        bool remainingNewWeaponDrawn = Enumerable.Range(0, 40).Any(_ => GameRules.WeaponChoices(limitedWeaponStats).Any(option => option.Id == 4));
        Check(remainingNewWeaponDrawn && GameRules.WeaponChoices(limitedWeaponStats).Count == 3, "Weapon draft keeps the remaining new weapon alongside owned upgrades");
        var threeLeftStats = new RunStats(); for (int i = 0; i < 5; i++) threeLeftStats.Weapons[i] = 1; threeLeftStats.EvolvedWeapons[0] = true; threeLeftStats.EvolvedWeapons[1] = true;
        var threeBefore = GameRules.WeaponChoices(threeLeftStats);
        var threeAfter = GameRules.WeaponChoices(threeLeftStats, threeBefore.Select(option => option.Id).ToList());
        Check(threeAfter.Count == 3 && threeAfter.Select(option => option.Id).OrderBy(id => id).SequenceEqual(threeBefore.Select(option => option.Id).OrderBy(id => id)) && !threeAfter.Select(option => option.Id).SequenceEqual(threeBefore.Select(option => option.Id)), "Weapon reroll reshuffles the final three choices instead of freezing them");
        Check(GameRules.EnemySpawnInterval(230, 2) < GameRules.EnemySpawnInterval(0, 0) && GameRules.EnemyTimeMultiplier(230) < 1.4f && GameRules.BossEnemyMultiplier(2) < 1.3f, "Enemy density grows faster than the gradual stage stat curve");
        var normalRerollField = typeof(GameBootstrap).GetField("normalRerolls", BindingFlags.Instance | BindingFlags.NonPublic);
        var weaponRerollField = typeof(GameBootstrap).GetField("weaponRerolls", BindingFlags.Instance | BindingFlags.NonPublic);
        Check((int)normalRerollField.GetValue(game) == 3 && (int)weaponRerollField.GetValue(game) == 1, "Rerolls start with three normal charges and one weapon charge");
        var beforeRerollIds = ((List<UpgradeOption>)choiceField.GetValue(game)).Select(option => option.Id).ToList();
        FindObjectsByType<Button>(FindObjectsSortMode.None).First(b => b.name == "Reroll").onClick.Invoke();
        Check(game.State == RunState.Upgrade, "Reroll refreshes choices while preserving pause");
        var afterRerollIds = ((List<UpgradeOption>)choiceField.GetValue(game)).Select(option => option.Id).ToList();
        Check(afterRerollIds.Count > 0 && afterRerollIds.All(id => !beforeRerollIds.Contains(id)), "Rerolled choices avoid every option from the previous roll");
        Check((int)normalRerollField.GetValue(game) == 2 && (int)weaponRerollField.GetValue(game) == 1, "A normal reroll spends only the normal charge");
        var capped = new RunStats();
        for (int i = 0; i < 5; i++) { capped.Weapons[i] = GameRules.MaxWeaponRank; capped.EvolvedWeapons[i] = true; }
        for (int i = 5; i < 15; i++) capped.Ranks[i] = GameRules.MaxRank(i);
        Check(GameRules.Choices(capped).Count == 3, "Capped upgrade pool still produces three safe recovery options");
        var reinforcements = GameRules.WeaponChoices(capped);
        Check(reinforcements.Count == 3 && reinforcements.Select(option => option.Id).SequenceEqual(new[] { -1, -2, -3 }) && reinforcements[0].Description.Contains("伤害 +10%") && reinforcements[1].Description.Contains("攻击速度 +8%") && reinforcements[2].Description.Contains("暴击率 +6%"), "Fully evolved armory offers the three specified armament boosts");
        float damageCurve = 0f, hasteCurve = 0f, speedCurve = 0f;
        for (int rank = 0; rank < GameRules.MaxUpgradeRank; rank++)
        {
            damageCurve += GameRules.DamageUpgradeAtRank(rank);
            hasteCurve += GameRules.HasteUpgradeAtRank(rank);
            speedCurve += GameRules.SpeedUpgradeAtRank(rank);
        }
        Check(damageCurve > 2.84f && damageCurve < 2.86f && hasteCurve > 1.73f && hasteCurve < 1.75f && speedCurve > .69f && speedCurve < .71f,
            "Damage, haste and speed upgrades use the additive xlsm values");
        float armorCurve = 0f, regenerationCurve = 0f;
        for (int rank = 0; rank < GameRules.MaxRank(13); rank++) armorCurve += GameRules.ArmorUpgradeAtRank(rank);
        for (int rank = 0; rank < GameRules.MaxRank(12); rank++) regenerationCurve += GameRules.RegenerationUpgradeAtRank(rank);
        Check(armorCurve > .39f && armorCurve < .41f && regenerationCurve > 11.1f && regenerationCurve < 11.3f && GameRules.MaxRank(13) == 5,
            "Armor and regeneration use the seven-rank xlsm values");
        var zones = new RunStats
        {
            DamageBonus = .5f, HasteBonus = .25f, SpeedBonus = .1f,
            TalentHasteMultiplier = 1.16f, CoreDamageMultiplier = 1.30f,
            CoreSpeedMultiplier = 1.18f, BoostDamageMultiplier = 1.1f, BoostHasteMultiplier = 1.08f
        };
        Check(Mathf.Approximately(zones.Damage, 1.5f * 1.30f * 1.1f) && Mathf.Approximately(zones.Haste, 1.25f * 1.16f * 1.08f) && Mathf.Approximately(zones.Speed, 5.2f * 1.1f * 1.18f),
            "Additive upgrades stay separate from talent, boss core and armament multipliers");
        float pulseStart = GameRules.PulseDamage(1) / GameRules.PulseInterval(1, 1);
        float pulseMax = GameRules.PulseDamage(GameRules.MaxWeaponRank) / GameRules.PulseInterval(GameRules.MaxWeaponRank, 1);
        float scatterStart = GameRules.ScatterDamage(1) * GameRules.ScatterPellets(1) / GameRules.ScatterInterval(1, 1);
        float scatterMax = GameRules.ScatterDamage(GameRules.MaxWeaponRank) * GameRules.ScatterPellets(GameRules.MaxWeaponRank) / GameRules.ScatterInterval(GameRules.MaxWeaponRank, 1);
        float arcStart = GameRules.ArcDamage(1) * GameRules.ArcTargets(1) / GameRules.ArcInterval(1, 1);
        float arcMax = GameRules.ArcDamage(GameRules.MaxWeaponRank) * GameRules.ArcTargets(GameRules.MaxWeaponRank) / GameRules.ArcInterval(GameRules.MaxWeaponRank, 1);
        float cryoStart = GameRules.CryoDamage(1) / GameRules.CryoInterval(1, 1);
        float cryoMax = GameRules.CryoDamage(GameRules.MaxWeaponRank) * 1.4f / GameRules.CryoInterval(GameRules.MaxWeaponRank, 1);
        float bladeStart = GameRules.OrbitDpsBudget(1);
        float bladeMax = GameRules.OrbitDpsBudget(GameRules.MaxWeaponRank);
        Check(GameRules.CryoDamage(1) == 35 && GameRules.CryoInterval(1, 1) < 1.5f && GameRules.CryoSlowDuration(GameRules.MaxWeaponRank, 1.1f) > GameRules.CryoSlowDuration(1, 1.1f) && GameRules.CryoEvolutionTickDamage(1) == 3 && GameRules.CryoEvolutionTickDamage(5) == 5 && GameRules.CryoEvolutionTickInterval == .25f && GameRules.CryoProjectileSpeed(1) >= 18f && GameRules.CryoPierce == 4 && GameRules.CryoProjectileLength(1) > 4f && GameRules.OrbitBlades(1) == 3 && GameRules.OrbitDamage(1) == 20 && GameRules.ScatterDamage(1) == 16 && GameRules.PulseDamage(1) == 18,
            "Requested sniper-style cryo weapon damage, fire rate, projectile length, speed and penetration are applied");
        Check(Enumerable.Range(2, 4).All(level => GameRules.ScatterDamage(level) - GameRules.ScatterDamage(level - 1) == 2), "Ember Scatter gains exactly two damage per rank");
        Check(Enumerable.Range(0, 5).Select(GameRules.WeaponColor).Distinct().Count() == 5, "All five signature weapons use distinct colors");
        Check(GameRules.MaxWeaponRank == 5 && pulseMax / pulseStart < 8.5f && scatterMax / scatterStart < 20f && arcMax / arcStart < 13f && cryoMax / cryoStart < 9f && bladeMax / bladeStart < 6f && GameRules.ArcDamage(1) * 1.25f >= 22 && GameRules.OrbitDamage(1) >= 20,
            "All five signature weapon rank curves support the extended level cap");
        Check(GameRules.MaxUpgradeRank == 7 && GameRules.Describe(5, new RunStats()).Description.Contains("+25%") && GameRules.Describe(6, new RunStats()).Description.Contains("+15%") && GameRules.Describe(7, new RunStats()).Description.Contains("+10%") && GameRules.Describe(8, new RunStats()).Description.Contains("+20%") && GameRules.Describe(10, new RunStats()).Description.Contains("+10%") && GameRules.Describe(12, new RunStats()).Description.Contains("1.00"),
            "Upgrade cards describe the additive xlsm values");
        var evolutionStats = new RunStats();
        for (int i = 0; i < 5; i++) { evolutionStats.Weapons[i] = 1; evolutionStats.EvolvedWeapons[i] = true; }
        evolutionStats.EvolvedWeapons[0] = false;
        var evolutionOption = GameRules.WeaponChoices(evolutionStats).FirstOrDefault(option => option.IsEvolution && option.Id == 0);
        Check(evolutionOption != null && evolutionOption.IsEvolution && evolutionOption.Description.Contains("三颗子弹"), "Any owned weapon rank exposes a one-time evolution option");
        evolutionStats.EvolvedWeapons[0] = true;
        Check(!GameRules.WeaponChoices(evolutionStats).Any(option => option.IsEvolution && option.Id == 0), "An evolved weapon cannot be offered for evolution again");
        Check(GameRules.RollDamageImmunity(1f) && !GameRules.RollDamageImmunity(0f), "Damage immunity probability handles guaranteed and zero chance");
        var overflowCrit = new RunStats { Crit = 1.10f, CritDamage = 2.40f, ResonanceOverflow = true };
        Check(overflowCrit.RollDamage(100, out bool overflowIsCritical) == 260 && overflowIsCritical, "Starcore Resonance converts critical overflow at 2% bonus critical damage per excess 1%");
        var cappedCrit = new RunStats { Crit = 1.10f, CritDamage = 2.40f };
        Check(cappedCrit.RollDamage(100, out bool cappedIsCritical) == 240 && cappedIsCritical, "Critical overflow does nothing without Starcore Resonance");
        Check(GameRules.OrbitHitIntervalEvolved < GameRules.OrbitHitInterval && GameRules.DescribeEvolution(0).Description.Contains("三颗子弹"), "Evolved pulse fires a three-way volley and evolved orbit hits faster");
        CheckCardText("Stat draft");
        yield return Capture("03-upgrades");
        int healthBeforeExpansion = game.Stats.MaxHealth;
        typeof(PlayerActor).GetField("health", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(game.Player, 55f);
        typeof(GameBootstrap).GetProperty("Xp").SetValue(game, 0);
        normalChoices.Clear(); normalChoices.Add(GameRules.Describe(14, game.Stats));
        Invoke(game, "ChooseUpgrade", 0); yield return null;
        Check(game.Stats.MaxHealth == healthBeforeExpansion + 20 && game.Stats.Ranks[14] == 1 && game.Player.Health == 95, "Selecting health expansion raises the cap and heals immediately");
        Check(game.State == RunState.Playing && Time.timeScale == 1, "Upgrade choice resumes game");
        typeof(GameBootstrap).GetProperty("Level").SetValue(game, 4); typeof(GameBootstrap).GetProperty("Xp").SetValue(game, 0);
        int unlockedBeforeWeaponOffer = game.Stats.Weapons.Count(rank => rank > 0);
        game.AddXp(game.XpNeeded); yield return null;
        var weaponChoices = (List<UpgradeOption>)choiceField.GetValue(game);
        cards = FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b => b.name == "Upgrade card").ToArray();
        Check(game.State == RunState.Upgrade && weaponChoices.Count == 3 && weaponChoices.All(option => option.Flow == UpgradeFlow.Weapon), "Every fourth level opens the separate weapon draft");
        Check(weaponChoices.Any(option => option.Id >= 0 && game.Stats.Weapons[option.Id] == 0) && weaponChoices.All(option => !option.IsWeapon || game.Stats.Weapons[option.Id] == 0 || option.IsEvolution), "Weapon draft excludes weapon upgrades and allows new weapons or evolutions");
        Check(cards.All(b => ((RectTransform)b.transform).sizeDelta.x > 246), "Weapon draft uses a wider card layout than stat upgrades");
        CheckCardText("Weapon draft");
        yield return Capture("03-weapons");
        var selectedWeaponOption = weaponChoices[0];
        int selectedWeaponBefore = selectedWeaponOption.Id >= 0 ? game.Stats.Weapons[selectedWeaponOption.Id] : 0;
        var selectedWeaponCard = cards.FirstOrDefault(card => card.GetComponentsInChildren<Text>().Any(text => text.name == "Choice title" && text.text.Contains(selectedWeaponOption.Title))) ?? cards[0];
        selectedWeaponCard.onClick.Invoke(); yield return null;
        bool selectedNewWeapon = selectedWeaponOption.Id >= 0 && selectedWeaponBefore == 0 && game.Stats.Weapons[selectedWeaponOption.Id] == 1;
        bool selectedEvolution = selectedWeaponOption.Id >= 0 && selectedWeaponOption.IsEvolution && selectedWeaponBefore > 0 && game.Stats.EvolvedWeapons[selectedWeaponOption.Id] && game.Stats.Weapons[selectedWeaponOption.Id] == selectedWeaponBefore;
        Check(game.State == RunState.Playing && (selectedNewWeapon || selectedEvolution), "Weapon selection unlocks a new weapon or evolves an owned weapon without changing its rank");
        var originalWeapons = (int[])game.Stats.Weapons.Clone();
        var originalEvolvedWeapons = (bool[])game.Stats.EvolvedWeapons.Clone();
        float baseDamage = game.Stats.Damage, baseHaste = game.Stats.Haste, baseCrit = game.Stats.Crit;
        for (int i = 0; i < 5; i++) { game.Stats.Weapons[i] = GameRules.MaxWeaponRank; game.Stats.EvolvedWeapons[i] = true; }
        for (int boost = 0; boost < 3; boost++)
        {
            typeof(GameBootstrap).GetProperty("Level").SetValue(game, 8); typeof(GameBootstrap).GetProperty("Xp").SetValue(game, 0);
            game.AddXp(game.XpNeeded); yield return null;
            var boosts = (List<UpgradeOption>)choiceField.GetValue(game);
            Check(boosts.Count == 3 && boosts.Select(option => option.Id).SequenceEqual(new[] { -1, -2, -3 }), "Fully unlocked armory offers three reinforcement choices");
            Invoke(game, "ChooseUpgrade", boost); yield return null;
        }
        Check(Mathf.Approximately(game.Stats.Damage, baseDamage * 1.1f) && Mathf.Approximately(game.Stats.Haste, baseHaste * 1.08f) && Mathf.Approximately(game.Stats.Crit, baseCrit + .06f), "Each armament reinforcement applies its specified stat bonus");
        Array.Copy(originalWeapons, game.Stats.Weapons, originalWeapons.Length);
        Array.Copy(originalEvolvedWeapons, game.Stats.EvolvedWeapons, originalEvolvedWeapons.Length);
        game.Stats.BoostDamageMultiplier = 1f; game.Stats.BoostHasteMultiplier = 1f; game.Stats.Crit = baseCrit;
        int health = game.Player.Health; game.Player.Dash(Vector2.up); game.Player.TakeDamage(20);
        Check(game.Player.Health == health && !game.Player.DashReady, "Dash grants invulnerability and enters cooldown");
        Check(Mathf.Approximately(game.Player.DashCooldownMax, 3.84f), "Low-mobility pilot (2) has a 20% longer dash cooldown");
        Check(Mathf.Approximately(CharacterRoster.DashCooldownMultiplier(4), .85f) && Mathf.Approximately(CharacterRoster.DashCooldownMultiplier(3), 1f), "High-mobility pilots get a 15% shorter dash cooldown");
        yield return new WaitForSeconds(.95f);
        game.Player.TakeDamage(10);
        Check(game.Player.Health < health, "Enemy damage reduces health outside invulnerability");
        yield return new WaitForSeconds(.85f);
        game.Stats.DamageImmunityChance = 1f; int healthBeforeImmuneHit = game.Player.Health; game.Player.TakeDamage(10);
        Check(game.Player.Health == healthBeforeImmuneHit, "Guaranteed Phoenix immunity prevents damage");
        game.Stats.DamageImmunityChance = 0;
        Invoke(game, "Pause"); float elapsed = game.Elapsed; yield return new WaitForSecondsRealtime(.3f);
        Check(game.State == RunState.Paused && game.Elapsed == elapsed, "Pause freezes game timer");
        CheckResultLayout("Pause");
        Invoke(game, "Resume");
        Invoke(game, "SpawnChest"); yield return null;
        var chest = FindFirstObjectByType<SupplyChest>();
        game.Player.transform.position = chest.transform.position;
        yield return null; yield return null;
        Check(game.State == RunState.Upgrade, "Supply cache heals and offers a reward");
        Invoke(game, "ChooseUpgrade", 0);
        typeof(GameBootstrap).GetProperty("Level").SetValue(game, 20);
        typeof(GameBootstrap).GetProperty("Xp").SetValue(game, 0);
        for (int i = 0; i < 5; i++) game.Stats.Weapons[i] = 3;
        game.Player.transform.position = new Vector2(-1, -1);
        typeof(GameBootstrap).GetProperty("Elapsed").SetValue(game, 112f);
        Invoke(game, "SpawnEnemy", EnemyKind.Brute, new Vector2(4, 2));
        Invoke(game, "SpawnEnemy", EnemyKind.Spitter, new Vector2(-5, 2));
        Invoke(game, "SpawnEnemy", EnemyKind.Runner, new Vector2(2, 4));
        yield return new WaitForSeconds(1.5f);
        game.Stats.EvolvedWeapons[(int)WeaponKind.Pulse] = true;
        yield return Capture("02-combat");
        var hudFlags = BindingFlags.Instance | BindingFlags.NonPublic;
        var bannerNode = (RectTransform)typeof(GameBootstrap).GetField("bannerRoot", hudFlags).GetValue(game);
        var hintNode = (Text)typeof(GameBootstrap).GetField("hintText", hudFlags).GetValue(game);
        game.ShowBanner("SMOKE BANNER", GameRules.Cyan); yield return null;
        Invoke(game, "OpenPilotSheet"); yield return null;
        Check(game.State == RunState.CharacterSheet && Time.timeScale == 0, "C opens the pilot status sheet");
        Check(!bannerNode.gameObject.activeSelf && !hintNode.enabled, "Pilot sheet hides the combat banner and control hint");
        yield return Capture("06-pilot-sheet");
        Invoke(game, "ClosePilotSheet"); yield return null;
        Check(game.State == RunState.Playing && Time.timeScale == 1, "Closing the pilot sheet resumes the run");
        Check(bannerNode.gameObject.activeSelf, "Closing the pilot sheet restores a still-running banner");
        typeof(GameBootstrap).GetField("bannerUntil", hudFlags).SetValue(game, 0f);
        Invoke(game, "ShowBossWarning", 1);
        yield return new WaitForSecondsRealtime(.3f);
        yield return Capture("09-warning");
        yield return new WaitForSecondsRealtime(2.5f);
        typeof(GameBootstrap).GetProperty("Elapsed").SetValue(game, 181f);
        yield return new WaitForSeconds(1.4f);
        Check(game.Boss != null && game.Boss.Kind == EnemyKind.Boss && game.Boss.BossStage == 1, "First timed boss spawns");
        var bossPortrait = FindObjectsByType<Image>(FindObjectsSortMode.None).FirstOrDefault(i => i.name == "Boss portrait");
        Check(bossPortrait != null && bossPortrait.sprite == game.GetSprite(GameRules.BossVisual(1)) && bossPortrait.transform.parent.gameObject.activeSelf, "Boss HUD shows the matching first-boss portrait during combat");
        if (game.Boss != null)
        {
            Vector2 testMobPosition = (Vector2)game.Player.transform.position + Vector2.right * 3.5f;
            Invoke(game, "SpawnEnemy", EnemyKind.Grunt, testMobPosition);
            var stageOneMob = game.Nearby(testMobPosition, .05f).FirstOrDefault();
            int stageOneMobHealth = stageOneMob != null ? stageOneMob.MaxHealth : 0;
            if (stageOneMob != null)
            {
                var targetOrder = new List<EnemyActor> { stageOneMob };
                Vector2 playerPosition = game.Player.transform.position;
                Vector3 bossPosition = game.Boss.transform.position;
                game.Boss.transform.position = playerPosition + Vector2.right * 6f;
                PlayerActor.PrioritizeBossTarget(targetOrder, game.Boss, playerPosition, GameRules.AutoTargetRange);
                bool nearbyBossFirst = targetOrder.Count > 0 && targetOrder[0] == game.Boss;
                targetOrder.Clear(); targetOrder.Add(stageOneMob); targetOrder.Add(game.Boss);
                game.Boss.transform.position = playerPosition + Vector2.right * (GameRules.AutoTargetRange + 1f);
                PlayerActor.PrioritizeBossTarget(targetOrder, game.Boss, playerPosition, GameRules.AutoTargetRange);
                Check(nearbyBossFirst && targetOrder[0] == stageOneMob, "Auto-target prioritizes a nearby boss and falls back to the nearest enemy when the boss is far");
                game.Boss.transform.position = bossPosition;
            }
            game.Boss.Hit(game.Boss.MaxHealth / 2 + 20, false, Vector2.zero);
            yield return new WaitForSeconds(.2f);
            Check(game.Boss.PhaseTwo, "Boss enters second phase below half health");
            yield return Capture("04-boss");
            float damageBeforeReward = game.Stats.Damage; float hasteBeforeReward = game.Stats.Haste; float critBeforeReward = game.Stats.Crit; int maxHealthBeforeReward = game.Stats.MaxHealth;
            int firstBossHealth = game.Boss.MaxHealth;
            game.Boss.Hit(100000, true, Vector2.zero); yield return null;
            Check(game.State == RunState.Playing && game.Boss == null && game.BossesDefeated == 1 && game.BossTransitionRemaining == 0, "Boss death waits for the core reward before stage transition");
            Check(Mathf.Approximately(game.Stats.Damage, damageBeforeReward) && game.Stats.MaxHealth == maxHealthBeforeReward, "Boss power is selected, not granted automatically");
            yield return new WaitForSecondsRealtime(1.35f);
            Check(game.State == RunState.Upgrade && game.BossTransitionRemaining == 0, "Boss defeat opens a dedicated reward draft");
            var bossChoices = (List<UpgradeOption>)choiceField.GetValue(game);
            cards = FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b => b.name == "Upgrade card").ToArray();
            Check(bossChoices.Count == 3 && bossChoices.All(option => option.Flow == UpgradeFlow.Boss) && game.GetSprite(GameRules.BossVisual(1)) != null, "Boss draft presents three core rewards and boss art");
            Check(bossChoices[0].Title == "超载核心" && bossChoices[1].Title == "凤凰协议" && bossChoices[2].Title == "星核共鸣" && cards.All(b => b.GetComponentsInChildren<Image>().FirstOrDefault(img => img.name == "Skill icon art")?.sprite != null), "Boss rewards display their unique icons and identities");
            Check(cards.All(b => ((RectTransform)b.transform).sizeDelta.y > 310), "Boss draft has the largest reward-card layout");
            CheckCardText("Boss core draft");
            yield return Capture("04-boss-reward");
            Invoke(game, "ChooseUpgrade", 0); yield return null;
            Check(Mathf.Approximately(game.Stats.Damage, damageBeforeReward * 1.30f) && Mathf.Approximately(game.Stats.Haste, hasteBeforeReward * 1.20f) && Mathf.Approximately(game.Stats.Crit, critBeforeReward + .20f) && game.Stats.MaxHealth == maxHealthBeforeReward && game.Player.Health == game.Player.MaxHealth, "Overload core multiplies final damage +30%, haste +20% and adds +20% critical chance");
            Check(game.BossTransitionRemaining >= GameRules.StageTwoIntermission - 1 && game.BossesDefeated == 1, "Core selection begins a two-minute tier-two minion phase");
            Check(game.ArenaStage == 2 && arenaFloor.sprite.texture == Resources.Load<Texture2D>("Art/Arena/arena-floor-stage2"), "Defeating the first boss switches to the stage-two arena art");
            Invoke(game, "SpawnEnemy", EnemyKind.Grunt, testMobPosition);
            var stageTwoMob = game.Nearby(testMobPosition, .05f).FirstOrDefault();
            int stageTwoMobHealth = stageTwoMob != null ? stageTwoMob.MaxHealth : 0;
            Check(stageTwoMobHealth > stageOneMobHealth && stageTwoMobHealth <= Mathf.CeilToInt(stageOneMobHealth * 1.5f), "Stage two minions strengthen without an excessive jump");
            typeof(GameBootstrap).GetField("bossTransitionTimer", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(game, .18f);
            float wait = 0;
            while (game.Boss == null && wait < 7)
            {
                if (game.State == RunState.Upgrade) { Invoke(game, "ChooseUpgrade", 0); yield return null; }
                else yield return new WaitForSecondsRealtime(.1f);
                wait += .1f;
            }
            Check(game.Boss != null && game.Boss.BossStage == 2 && game.Boss.MaxHealth == 27000 && game.GetSprite(GameRules.BossVisual(2)) != null, "Redesigned mobile second boss has exactly 27000 health");
            Check(bossPortrait.sprite == game.GetSprite(GameRules.BossVisual(2)), "Boss HUD portrait changes to the second boss");
            int secondBossHealth = game.Boss != null ? game.Boss.MaxHealth : 0;
            yield return new WaitForSecondsRealtime(1.85f);
            if (game.Boss != null)
            {
                Vector3 beforeEchoJump = game.Boss.transform.position;
                typeof(EnemyActor).GetField("echoTimer", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(game.Boss, .04f);
                yield return new WaitForSeconds(1.1f);
                Check(Vector3.Distance(game.Boss.transform.position, beforeEchoJump) > 2f, "Echo Herald telegraphs and performs a distinct arena jump");
            }
            if (game.Boss != null) game.Boss.Hit(1000000, true, Vector2.zero);
            yield return new WaitForSecondsRealtime(1.35f);
            Check(game.State == RunState.Upgrade && game.BossesDefeated == 2, "Second boss opens its own reward draft");
            var secondBossChoices = (List<UpgradeOption>)choiceField.GetValue(game);
            Check(secondBossChoices.Count == 2 && secondBossChoices.All(option => option.RewardId != 0), "Second core draft cannot repeat the Overload core");
            int phoenixHealthBefore = game.Stats.MaxHealth; float phoenixArmorBefore = game.Stats.Armor; float phoenixRegenBefore = game.Stats.Regeneration; float phoenixImmunityBefore = game.Stats.DamageImmunityChance;
            Invoke(game, "ChooseUpgrade", 0); yield return null;
            Check(game.Stats.MaxHealth == phoenixHealthBefore + 30 && Mathf.Approximately(game.Stats.Armor, phoenixArmorBefore + .10f) && Mathf.Approximately(game.Stats.Regeneration, phoenixRegenBefore + 2f) && Mathf.Approximately(game.Stats.DamageImmunityChance, phoenixImmunityBefore + .30f), "Phoenix protocol grants all four specified defensive effects");
            Check(game.BossTransitionRemaining >= GameRules.StageThreeIntermission - 1, "Second core selection begins a two-and-a-half-minute tier-three minion phase");
            Check(game.ArenaStage == 3 && arenaFloor.sprite.texture == Resources.Load<Texture2D>("Art/Arena/arena-floor-stage3"), "Defeating the second boss switches to the stage-three arena art");
            Invoke(game, "SpawnEnemy", EnemyKind.Grunt, testMobPosition);
            var stageThreeMob = game.Nearby(testMobPosition, .05f).FirstOrDefault();
            Check(stageThreeMob != null && stageThreeMob.MaxHealth > stageTwoMobHealth && stageThreeMob.MaxHealth <= Mathf.CeilToInt(stageTwoMobHealth * 1.9f), "Stage three minions grow steadily again");
            typeof(GameBootstrap).GetField("bossTransitionTimer", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(game, .18f);
            wait = 0;
            while (game.Boss == null && wait < 7)
            {
                if (game.State == RunState.Upgrade) { Invoke(game, "ChooseUpgrade", 0); yield return null; }
                else yield return new WaitForSecondsRealtime(.1f);
                wait += .1f;
            }
            Check(game.Boss != null && game.Boss.BossStage == 3 && game.Boss.MaxHealth == 120000 && game.Boss.IsAnchored && game.GetSprite(GameRules.BossVisual(3)) != null, "Final boss has exactly 120000 health, is colossal, unique, and anchored");
            Check(game.Boss != null && (game.Boss.transform.position - Vector3.zero).sqrMagnitude < .001f, "Final boss spawns at the center of the screen arena");
            Check(bossPortrait.sprite == game.GetSprite(GameRules.BossVisual(3)), "Boss HUD portrait changes to the colossal final boss");
            if (game.Boss != null)
            {
                Vector3 anchoredPosition = game.Boss.transform.position;
                yield return new WaitForSeconds(.5f);
                Check((game.Boss.transform.position - anchoredPosition).sqrMagnitude < .001f, "Final boss never moves from its arena position");
            }
            yield return new WaitForSecondsRealtime(2.35f);
            if (game.Boss != null) game.Boss.Hit(1000000, true, Vector2.zero);
            yield return null;
            Check(game.Boss == null && game.BossesDefeated == 3, "Final boss dies without opening a core draft");
            yield return new WaitForSecondsRealtime(1.35f);
            Check(game.State != RunState.Upgrade && game.State == RunState.Playing, "Final boss skips the core draft");
            yield return new WaitForSecondsRealtime(.8f);
            Check(game.BossesDefeated == 3 && game.State == RunState.Victory && Time.timeScale == 0, "Defeating the final boss ends the run directly");
            Check(!game.HasClaimedBossCore(2), "Final boss no longer awards a third core");
            CheckResultLayout("Victory");
            yield return Capture("05-victory");
        }
        Invoke(game, "RestartRun"); yield return new WaitForSecondsRealtime(.7f);
        game = GameBootstrap.Game;
        Check(game != null && game.State == RunState.Menu && game.Player == null, "Scene restart recreates menu cleanly");
        game.StartRun(); yield return new WaitForSeconds(.1f);
        game.Player.TakeDamage(100000);
        yield return new WaitForSecondsRealtime(.35f);
        yield return Capture("08-death");
        yield return new WaitForSecondsRealtime(.8f);
        Check(game.State == RunState.Defeat, "Player death opens defeat screen");
        CheckResultLayout("Defeat");
        yield return Capture("07-defeat");
        Invoke(game, "PlayAgain"); yield return new WaitForSecondsRealtime(.7f);
        Check(GameBootstrap.Game.State == RunState.Playing && GameBootstrap.Game.Player != null, "Play again starts a fresh run directly");
        Check(exceptions.Count == 0, "No runtime exceptions");
        Finish();
    }
    private IEnumerator ValidateMobileUi(GameBootstrap game)
    {
        var nodes = FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        RectTransform Node(string name) => nodes.First(r => r.name == name);
        Rect Bounds(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            var min = RectTransformUtility.WorldToScreenPoint(Camera.main, corners[0]);
            var max = RectTransformUtility.WorldToScreenPoint(Camera.main, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
        var viewport = Camera.main.pixelRect;
        var telemetry = Bounds(Node("Top telemetry"));
        var boss = Bounds(Node("Boss health"));
        var dash = Bounds(Node("Dash touch button"));
        Check(!boss.Overlaps(telemetry), "Mobile boss bar stays clear of the merged top HUD panel");
        Check(telemetry.xMax > viewport.xMax - viewport.width * .08f, "Merged time/stage panel sits in the upper-right corner");
        foreach (string name in new[] { "Movement joystick", "Dash touch button", "Vitals", "Top telemetry" })
        {
            var bounds = Bounds(Node(name));
            Check(viewport.Contains(bounds.min) && viewport.Contains(bounds.max), "Mobile viewport contains " + name);
        }
        var stick = Node("Movement joystick touch").GetComponent<VirtualJoystick>();
        ValidateSafeAreaFallbacks(game);
        ValidateSafeArea("Phone HUD", "Dash touch button", "Pause button", "Open pilot sheet", "Vitals", "Top telemetry");
        // Touches outside the viewport (safe-area margins / letterbox) or in its right-side action zone never grab the stick.
        var rejected = new List<Vector2> { new Vector2(viewport.xMin + viewport.width * .8f, viewport.center.y) };
        if (viewport.xMin >= 2f) rejected.Add(new Vector2(viewport.xMin - 1f, viewport.center.y));
        if (viewport.yMin >= 2f) rejected.Add(new Vector2(viewport.xMin + viewport.width * .2f, viewport.yMin - 1f));
        bool ignored = true;
        foreach (var point in rejected)
        {
            var probe = new PointerEventData(EventSystem.current) { pointerId = 20, position = point };
            stick.OnPointerDown(probe);
            probe.position = point + new Vector2(viewport.width * .25f, 0f); stick.OnDrag(probe);
            ignored &= game.TouchMove == Vector2.zero;
            stick.OnPointerUp(probe);
        }
        Check(ignored, "Stick ignores touches outside the viewport and in its right-side action zone");
        var edge = new PointerEventData(EventSystem.current) { pointerId = 21, position = new Vector2(viewport.xMin + 2f, viewport.center.y) };
        stick.OnPointerDown(edge);
        edge.position += new Vector2(viewport.width * .25f, 0f); stick.OnDrag(edge);
        Check(game.TouchMove.x > .5f, "Stick accepts a touch at the left edge of the safe viewport");
        stick.OnPointerUp(edge);
        var stickRect = (RectTransform)stick.transform;
        Vector2 StickPoint(Vector2 local) => RectTransformUtility.WorldToScreenPoint(Camera.main, stickRect.TransformPoint(local));
        var pointer = new PointerEventData(EventSystem.current) { pointerId = 10, position = StickPoint(Vector2.zero) };
        stick.OnPointerDown(pointer);
        pointer.position = StickPoint(new Vector2(84, 0)); stick.OnDrag(pointer);
        Check(game.TouchMove.x > .95f && Mathf.Abs(game.TouchMove.y) < .01f, "Dragging stick reaches full movement speed");
        var second = new PointerEventData(EventSystem.current) { pointerId = 11, position = StickPoint(new Vector2(-84, 0)) };
        stick.OnPointerDown(second); stick.OnPointerUp(second);
        Check(game.TouchMove.x > .95f, "A second finger cannot steal or release the movement stick");
        pointer.position = StickPoint(new Vector2(30, 0)); stick.OnDrag(pointer);
        Check(game.TouchMove.x > .1f && game.TouchMove.x < .8f, "Stick supports controlled analog movement");
        stick.OnPointerUp(pointer);
        Check(game.TouchMove == Vector2.zero, "Releasing stick stops movement");
        stick.OnPointerDown(pointer);
        Effects.Burst(game.Player.transform.position, GameRules.Pink, 36, .18f);
        game.RequestPause(); yield return null;
        Check(game.State == RunState.Paused && game.TouchMove == Vector2.zero, "Pause action opens menu and clears movement");
        yield return Capture("10-phone-pause");
        Invoke(game, "Resume");
    }

    // Test oracle for the safe area: the development-only --safe-area-insets override, else the system value.
    private static Rect ExpectedSafeArea()
    {
        int width = Mathf.Max(1, Screen.width), height = Mathf.Max(1, Screen.height);
        const string prefix = "--safe-area-insets=";
        string arg = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        if (arg != null)
        {
            var parts = arg.Substring(prefix.Length).Split(',');
            var v = new float[4];
            if (parts.Length == 4 && Enumerable.Range(0, 4).All(i => float.TryParse(parts[i].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v[i])))
                return Rect.MinMaxRect(v[0], v[1], width - v[2], height - v[3]);
        }
        return Screen.safeArea;
    }

    private static bool Inside(Rect outer, Rect inner, float tolerance = 1f) =>
        inner.xMin >= outer.xMin - tolerance && inner.yMin >= outer.yMin - tolerance && inner.xMax <= outer.xMax + tolerance && inner.yMax <= outer.yMax + tolerance;

    private static bool IsFiniteRect(Rect r) =>
        new[] { r.x, r.y, r.width, r.height }.All(f => !float.IsNaN(f) && !float.IsInfinity(f));

    // The locked 20:9 camera (and the camera-space canvas riding on it) must sit inside the safe area,
    // as large as the safe area allows, with the named tap targets fully inside it.
    private void ValidateSafeArea(string label, params string[] elementNames)
    {
        Canvas.ForceUpdateCanvases();
        Rect safe = ExpectedSafeArea(), view = Camera.main.pixelRect;
        Check(Inside(safe, view), label + " camera viewport stays inside the safe area");
        bool largest = Mathf.Abs(view.width - safe.width) <= 2f || Mathf.Abs(view.height - safe.height) <= 2f;
        Check(largest && Mathf.Abs(view.width / Mathf.Max(1f, view.height) - 20f / 9f) < .02f, label + " viewport is the largest 20:9 rect in the safe area");
        var nodes = FindObjectsByType<RectTransform>(FindObjectsSortMode.None);
        foreach (string name in elementNames)
        {
            var rect = nodes.FirstOrDefault(r => r.name == name);
            Check(rect != null && Inside(safe, ScreenRect(rect)), label + " keeps " + name + " inside the safe area");
        }
    }

    private void ValidateSafeAreaFallbacks(GameBootstrap game)
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        int width = Mathf.Max(1, Screen.width), height = Mathf.Max(1, Screen.height);
        var screen = new Rect(0f, 0f, width, height);
        var resolve = typeof(GameBootstrap).GetMethod("ResolveSafeArea", flags);
        var invalid = new[] { new Rect(float.NaN, 0f, width, height), new Rect(0f, 0f, 0f, 0f), new Rect(width * .4f, 0f, -width, height), new Rect(-500f, -500f, width * 3f, height * 3f) };
        Check(invalid.All(bad =>
        {
            var resolved = (Rect)resolve.Invoke(game, new object[] { width, height, bad });
            return IsFiniteRect(resolved) && resolved.width >= width * .5f && resolved.height >= height * .5f && Inside(screen, resolved, 0f);
        }), "Invalid, collapsed or oversized safe areas resolve to a usable on-screen rect");
        // Same resolution, different safe area: the cached lock must re-fit instead of keeping a stale rect.
        Rect expected = Camera.main.rect;
        Camera.main.rect = new Rect(0f, 0f, 1f, 1f);
        typeof(GameBootstrap).GetField("viewportRawSafeArea", flags).SetValue(game, new Rect(1f, 1f, 1f, 1f));
        Invoke(game, "ApplyAspectLock", false);
        Check(Camera.main.rect == expected, "A safe-area-only change re-fits the viewport without a resolution change");
    }

    // Settings: entry, four pages, unique active page, layout bounds, key controls, live-save interactions, close paths.
    // Settings write PlayerPrefs, so the local preference values are backed up and restored afterwards.
    private static readonly string[] SettingsKeys = { "Master", "Music", "Sfx", "Quality", "Fps", "DamageNumbers", "Shake", "Particles", "Language", "AutoPause" };
    private IEnumerator ValidateSettings(GameBootstrap game)
    {
        var backup = SettingsKeys.Select(k => "Starfall.Settings." + k).ToDictionary(k => k, k => PlayerPrefs.HasKey(k) ? (int?)PlayerPrefs.GetInt(k) : null);
        bool touch = game.TouchControls;
        string mode = touch ? "touch" : "desktop";
        var entry = FindObjectsByType<Button>(FindObjectsSortMode.None).First(b => b.name == "Settings");
        var entryRect = (RectTransform)entry.transform;
        Check(entryRect.rect.height >= (touch ? 64f : 44f) && entryRect.rect.width >= 150f, "Title menu Settings entry has a comfortable " + mode + " hit area");
        Check(Camera.main.pixelRect.Contains(ScreenRect(entryRect).min) && Camera.main.pixelRect.Contains(ScreenRect(entryRect).max), "Title menu Settings entry stays inside the viewport");
        entry.onClick.Invoke(); yield return null;
        RectTransform Find(string name) => FindObjectsByType<RectTransform>(FindObjectsSortMode.None).FirstOrDefault(r => r.name == name);
        var frame = Find("Settings frame");
        Check(frame != null, "Settings panel opens from the menu");
        if (frame == null) yield break;
        Canvas.ForceUpdateCanvases();
        Rect frameRect = ScreenRect(frame), view = Camera.main.pixelRect;
        Check(frameRect.xMin >= view.xMin - 1 && frameRect.yMin >= view.yMin - 1 && frameRect.xMax <= view.xMax + 1 && frameRect.yMax <= view.yMax + 1, "Settings frame fits inside the " + mode + " viewport");
        var close = Find("Close settings");
        Check(close != null && close.rect.width >= (touch ? 64f : 44f) && close.rect.height >= (touch ? 64f : 44f), "Settings close button meets the " + mode + " target size");
        string[] pages = { "音频", "画面", "通用", "数据" };
        string[] files = { "audio", "video", "general", "data" };
        for (int i = 0; i < 4; i++)
        {
            var nav = Find("Settings nav " + i);
            Check(nav != null && nav.rect.height >= (touch ? 64f : 44f), "Settings nav " + i + " meets the " + mode + " target size");
            if (nav == null) continue;
            nav.GetComponent<Button>().onClick.Invoke(); yield return null;
            Canvas.ForceUpdateCanvases();
            var active = FindObjectsByType<RectTransform>(FindObjectsSortMode.None).Where(r => r.name.StartsWith("Settings page ")).ToArray();
            Check(active.Length == 1 && active[0].name == "Settings page " + pages[i], "Settings page " + pages[i] + " is the only active page");
            if (active.Length != 1) continue;
            var page = active[0];
            bool inside = page.GetComponentsInChildren<RectTransform>().Where(r => r.rect.width > 0 && r.rect.height > 0 && r.name != "Shadow")
                .All(r => { var b = ScreenRect(r); return b.xMin >= frameRect.xMin - 1 && b.xMax <= frameRect.xMax + 1 && b.yMin >= frameRect.yMin - 1 && b.yMax <= frameRect.yMax + 1; });
            Check(inside, "Settings page " + pages[i] + " content stays inside the frame");
            bool fits = frame.GetComponentsInChildren<Text>()
                .Where(t => t.isActiveAndEnabled && !string.IsNullOrEmpty(t.text)).All(t => t.preferredWidth <= t.rectTransform.rect.width + 1f && t.preferredHeight <= Mathf.Max(t.rectTransform.rect.height, t.fontSize * 1.6f) + 2f);
            Check(fits, "Settings page " + pages[i] + " labels fit on one line without clipping");
            bool targets = page.GetComponentsInChildren<Button>().Select(b => (RectTransform)b.transform).All(r => r.rect.height >= (touch ? 56f : 40f));
            Check(targets, "Settings page " + pages[i] + " buttons meet the " + mode + " target height");
            yield return Capture("12-settings-" + (i + 1) + "-" + files[i]);
        }

        // Key controls exist on each page and settings save/apply immediately.
        Find("Settings nav 0").GetComponent<Button>().onClick.Invoke(); yield return null;
        var sliders = FindObjectsByType<Slider>(FindObjectsSortMode.None).Where(s => s.name.EndsWith(" slider")).ToArray();
        Check(sliders.Length == 3 && sliders.All(s => s.GetComponent<Image>() != null && s.GetComponent<Image>().raycastTarget), "Audio page has three pointer-draggable sliders");
        var master = sliders.FirstOrDefault(s => s.name == "主音量 slider");
        if (master != null)
        {
            master.value = 37f; yield return null;
            var valueText = master.transform.parent.GetComponentsInChildren<Text>().FirstOrDefault(t => t.name == "Value");
            Check(GameSettings.Master == 35 && PlayerPrefs.GetInt("Starfall.Settings.Master") == 35 && Mathf.Abs(AudioListener.volume - .35f) < .01f && valueText != null && valueText.text == "35%", "Master slider snaps to 5, saves, applies and refreshes its readout");
            var fill = master.fillRect; var handle = master.handleRect;
            Check(fill.rect.height <= 8f && handle.rect.height <= (touch ? 34f : 26f) && Mathf.Abs(handle.rect.width - handle.rect.height) < 1f, "Slider fill stays a thin track and the knob stays round");
        }
        Find("Settings nav 1").GetComponent<Button>().onClick.Invoke(); yield return null;
        Check(new[] { "画质", "帧率", "伤害数字", "屏幕震动", "粒子密度" }.All(n => Find(n + " option 0") != null), "Video page lists quality, frame rate, damage numbers, shake and particles");
        var high = Find("画质 option 2");
        if (high != null)
        {
            high.GetComponent<Button>().onClick.Invoke(); yield return null;
            Check(GameSettings.Quality == 2 && PlayerPrefs.GetInt("Starfall.Settings.Quality") == 2 && high.GetComponentInChildren<Text>().color == GameRules.Cyan, "Quality option saves and highlights the selected segment");
        }
        var damageOff = Find("伤害数字 option 0");
        if (damageOff != null)
        {
            damageOff.GetComponent<Button>().onClick.Invoke(); yield return null;
            Check(!GameSettings.DamageNumbers && !Effects.ShowDamageNumbers && PlayerPrefs.GetInt("Starfall.Settings.DamageNumbers") == 0, "Damage-number toggle saves and applies immediately");
        }
        Find("Settings nav 2").GetComponent<Button>().onClick.Invoke(); yield return null;
        Check(Find("语言 option 1") != null && Find("失焦自动暂停 option 0") != null && Find("Exit game") != null, "General page lists language, auto-pause and exit");
        Find("Settings nav 3").GetComponent<Button>().onClick.Invoke(); yield return null;
        Check(Find("Data status") != null && FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t => t.text == "未开放"), "Data page states that saves are not available yet");
        var import = Find("Import save");
        Check(Find("Export save") != null && import != null, "Data page keeps the export and import entries visible");
        if (import != null) { import.GetComponent<Button>().onClick.Invoke(); yield return null; }
        Check(FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t => t.text.Contains("功能暂未开放")), "Data buttons remain visible and report unavailable");
        yield return Capture("12-settings-5-data-toast");
        Find("Settings nav 0").GetComponent<Button>().onClick.Invoke(); yield return null;
        Check(!FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t => t.text.Contains("功能暂未开放")), "Unavailable notice clears when switching pages");

        Find("Close settings").GetComponent<Button>().onClick.Invoke(); yield return null;
        Check(Find("Settings frame") == null && game.State == RunState.Menu, "Settings panel closes back to the prior screen");
        // Esc routes through CloseSettings while settings are open (GameBootstrap.Update); key input itself is not simulated.
        entry.onClick.Invoke(); yield return null;
        Check(Find("Settings page 音频") != null, "Reopening settings returns to the last viewed page");
        Invoke(game, "CloseSettings"); yield return null;
        Check(Find("Settings frame") == null && game.State == RunState.Menu, "Esc close path returns to the title menu");

        // English route: switch language through the real settings UI and capture the localized shell.
        entry.onClick.Invoke(); yield return null;
        Find("Settings nav 2").GetComponent<Button>().onClick.Invoke(); yield return null;
        Find("语言 option 1").GetComponent<Button>().onClick.Invoke(); yield return null;
        Check(GameSettings.Language == 1 && FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t => t.text == "SETTINGS"), "Language selector applies English to the settings UI");
        yield return Capture("13-settings-en");
        Find("Close settings").GetComponent<Button>().onClick.Invoke(); yield return null;
        yield return Capture("14-menu-en");
        FindObjectsByType<Button>(FindObjectsSortMode.None).First(b => b.name == "Select pilot").onClick.Invoke(); yield return null;
        yield return Capture("15-characters-en");
        FindObjectsByType<Button>(FindObjectsSortMode.None).First(b => b.name == "Deploy pilot").onClick.Invoke(); yield return null;
        game.AddXp(game.XpNeeded); yield return null;
        yield return Capture("16-upgrade-en");
        Invoke(game, "ShowStatePanel", RunState.Menu); yield return null;
        GameSettings.Language = 0; GameSettings.Apply(); yield return null;

        foreach (var pair in backup)
        {
            if (pair.Value.HasValue) PlayerPrefs.SetInt(pair.Key, pair.Value.Value); else PlayerPrefs.DeleteKey(pair.Key);
        }
        PlayerPrefs.Save();
        GameSettings.Load(); GameSettings.Apply();
        Check(backup.All(p => p.Value.HasValue ? PlayerPrefs.GetInt(p.Key) == p.Value.Value : !PlayerPrefs.HasKey(p.Key)), "Local settings preferences restored after the settings test");
    }

    private void ValidateMobileLayout(string layoutName, params string[] elementNames)
    {
        var nodes = FindObjectsByType<RectTransform>(FindObjectsSortMode.None);
        var layout = nodes.FirstOrDefault(r => r.name == layoutName);
        Check(layout != null && layout.lossyScale.x < 1f, layoutName + " uses a compact phone layout scale");
        var viewport = Camera.main.pixelRect;
        foreach (string name in elementNames)
        {
            var rect = nodes.FirstOrDefault(r => r.name == name);
            if (rect == null) { Check(false, "Mobile layout contains " + name); continue; }
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            var min = RectTransformUtility.WorldToScreenPoint(Camera.main, corners[0]);
            var max = RectTransformUtility.WorldToScreenPoint(Camera.main, corners[2]);
            Rect bounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            Check(viewport.Contains(bounds.min) && viewport.Contains(bounds.max), "Mobile layout keeps " + name + " inside the screen");
            if (name == "Character card") Check(bounds.width >= 100f && bounds.height >= 170f, "Mobile pilot cards remain easy to tap");
        }
    }

    private static Rect ScreenRect(RectTransform rect)
    {
        var root = rect.GetComponentInParent<Canvas>().rootCanvas;
        Camera camera = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        var min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        var max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    // Result / pause modal: summary, actions and the corner utility buttons must not collide.
    private void CheckResultLayout(string label)
    {
        Canvas.ForceUpdateCanvases();
        var rects = FindObjectsByType<RectTransform>(FindObjectsSortMode.None);
        var modal = rects.FirstOrDefault(r => r.name == "Modal");
        var primary = rects.FirstOrDefault(r => (r.name == "Play again" || r.name == "Resume") && modal != null && r.IsChildOf(modal));
        var back = rects.FirstOrDefault(r => r.name == "Back to title" && modal != null && r.IsChildOf(modal));
        var summary = rects.FirstOrDefault(r => r.name == "Result summary");
        if (modal == null || primary == null || back == null || summary == null) { Check(false, label + " result layout nodes exist"); return; }
        Rect primaryRect = ScreenRect(primary), backRect = ScreenRect(back), summaryRect = ScreenRect(summary), modalRect = ScreenRect(modal);
        Check(!summaryRect.Overlaps(primaryRect) && !summaryRect.Overlaps(backRect) && !primaryRect.Overlaps(backRect), label + " summary and action buttons do not overlap");
        Check(modalRect.Contains(primaryRect.min) && modalRect.Contains(primaryRect.max) && modalRect.Contains(backRect.min) && modalRect.Contains(backRect.max), label + " action buttons stay inside the result frame");
        foreach (string corner in new[] { "Settings" })
        {
            var node = rects.FirstOrDefault(r => r.name == corner);
            if (node != null) Check(!ScreenRect(node).Overlaps(modalRect), label + " " + corner + " stays clear of the result frame");
        }
    }

    // Upgrade cards: description text must end above the badge divider (52 units from the card bottom).
    private void CheckCardText(string label)
    {
        Canvas.ForceUpdateCanvases();
        var cards = FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b => b.name == "Upgrade card").Select(b => (RectTransform)b.transform).ToArray();
        bool fits = cards.Length > 0 && cards.All(card =>
        {
            var details = card.GetComponentsInChildren<Text>().FirstOrDefault(t => t.name == "Choice details");
            if (details == null) return false;
            float bottom = -details.rectTransform.anchoredPosition.y + details.preferredHeight;
            return bottom <= card.sizeDelta.y - 52f;
        });
        Check(fits, label + " card descriptions end above the badge divider");
    }

    private void Finish()
    {
        if (complete) return; complete = true;
        bool clean = errors.Count == 0 && exceptions.Count == 0;
        var details = new List<string>(errors); details.AddRange(exceptions);
        File.WriteAllText(Path.Combine(folder, "smoke-check.txt"), string.Join("\n", checks) + "\n\n" + (clean ? "RESULT: PASS" : "RESULT: FAIL\n" + string.Join("\n", details)));
        Application.Quit(errors.Count == 0 && exceptions.Count == 0 ? 0 : 1);
    }
    private void OnDestroy() { Application.logMessageReceived -= OnLog; }
}
#endif

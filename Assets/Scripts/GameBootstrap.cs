using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public partial class GameBootstrap : MonoBehaviour
{
    public static GameBootstrap Game { get; private set; }
    public RunState State { get; private set; } = RunState.Menu;
    public CharacterKind SelectedCharacter { get; private set; } = CharacterKind.Rei;
    public CharacterProfile CurrentCharacter => CharacterRoster.Get(SelectedCharacter);
    public RunStats Stats { get; private set; } = new RunStats();
    public PlayerActor Player { get; private set; }
    public EnemyActor Boss { get; private set; }
    public int BossStage { get; private set; }
    public int BossesDefeated { get; private set; }
    public int ArenaStage { get; private set; } = 1;
    public float BossTransitionRemaining => Mathf.Max(0, bossTransitionTimer);
    public bool HasClaimedBossCore(int rewardId) => rewardId >= 0 && rewardId < bossRewardsClaimed.Length && bossRewardsClaimed[rewardId];
    public float Elapsed { get; private set; }
    public int Kills { get; private set; }
    public int Level { get; private set; } = 1;
    public int Xp { get; private set; }
    public int XpNeeded => GameRules.XpNeeded(Level);
    public int Wave => Mathf.Min(18, Mathf.FloorToInt(Elapsed / 36f) + 1);
    public float WaveProgress => Mathf.Repeat(Elapsed, 36f) / 36f;

    private readonly List<EnemyActor> enemies = new List<EnemyActor>();
    private readonly List<EnemyActor> nearbyBuffer = new List<EnemyActor>(64);
    private readonly List<UpgradeOption> choices = new List<UpgradeOption>();
    private readonly bool[] bossRewardsClaimed = new bool[3];
    private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    private readonly Dictionary<string, Sprite> shapes = new Dictionary<string, Sprite>();
    private readonly Text[] weaponLabels = new Text[5];
    private readonly Text[] weaponRanks = new Text[5];
    private readonly Image[] weaponIcons = new Image[5];
    private readonly Image[,] weaponPips = new Image[5, 5];
    private readonly Image[] weaponEvoChips = new Image[5];
    private readonly Text[] weaponEvoLabels = new Text[5];
    private Image pilotPortrait;
    private readonly Button[] characterButtons = new Button[5];
    private readonly Image[] characterCardBodies = new Image[5];
    private readonly Image[] characterCardEdges = new Image[5];
    private readonly Image[] characterCardGlows = new Image[5];
    private readonly Image[] characterCardBars = new Image[5];
    private readonly Text[] characterCardIndices = new Text[5];
    private Image characterFeaturePortrait, characterFeatureEdge, characterFeatureGlow, characterFeatureWeaponIcon;
    private Text characterFeatureName, characterFeatureCallSign, characterFeatureQuote, characterFeatureStory;
    private Text characterFeatureWeapon, characterFeatureWeaponDesc, characterFeaturePassive, characterFeaturePassiveDesc;
    private Text characterFeatureIndex;
    private readonly Image[,] characterStatSegments = new Image[3, 5];
    private readonly Text[] characterStatValues = new Text[3];
    private readonly Button[] choiceButtons = new Button[3];
    private readonly Text[] choiceTitles = new Text[3];
    private readonly Text[] choiceDetails = new Text[3];
    private readonly Text[] choiceBadges = new Text[3];
    private readonly Text[] choiceGlyphs = new Text[3];
    private readonly Image[] choiceIcons = new Image[3];
    private readonly Image[] choiceBodies = new Image[3];
    private readonly Image[] choiceEdges = new Image[3];
    private readonly Image[] choiceGlows = new Image[3];
    private readonly Image[] choiceTopBars = new Image[3];
    private readonly Image[] choiceBadgeBgs = new Image[3];
    private readonly Image[] choiceNumberBgs = new Image[3];
    private readonly Text[] choiceNumbers = new Text[3];
    private readonly Image[] choiceIconFrames = new Image[3];
    private readonly Image[] choiceChipEdges = new Image[3];
    private readonly Image[] choiceHexFills = new Image[3];
    private readonly Image[] choiceHexEdges = new Image[3];
    private readonly Image[] choiceBossRings = new Image[3];
    private readonly Image[] choiceInnerEdges = new Image[3];
    private readonly RectTransform[] choiceChips = new RectTransform[3];
    private readonly RectTransform[] choiceCornerSets = new RectTransform[3];
    private Image upgradeHalo, upgradeHeaderRule;
    private RectTransform bossOrbit;
    private readonly RectTransform[] choiceRoots = new RectTransform[3];
    private Camera worldCamera;
    private Canvas canvas;
    private RectTransform overlay;
    private GameObject hudRoot, statePanel, upgradePanel, bossPanel, sheetPanel;
    private Text clockText, levelText, killText, healthText, waveText, waveLabelText, stageSectorText, stageIndexText, hintText, bossText, bossHeader, upgradeTitle, upgradeSubtitle, upgradeKicker, upgradeSummary, pilotText, xpText, dashText;
    private Image hpFill, hpGhost, hpGlow, healthEdge, xpFill, xpGlow, waveFill, bossFill, bossGlow, dashFill, rewardBossArt, bossPortrait, bossFrameEdge, stageEdge;
    private readonly Image[] stagePips = new Image[3];
    private RectTransform upgradeKickerRect, upgradeTitleRect, upgradeSummaryRect, rewardSealRect;
    private CanvasGroup bannerGroup;
    private Text bannerText;
    private Image bannerAccent;
    private RectTransform bannerRoot;
    private float hpDisplay = 1f, hpGhostDisplay = 1f, xpDisplay, dashDisplay = 1f, bossDisplay = 1f;
    private SpriteRenderer arenaFloorRenderer;
    private readonly Sprite[] arenaStageSprites = new Sprite[3];
    private float spawnTimer = .7f, chestTimer = 32f;
    private bool bossStarted;
    private float stageStartedAt;
    private bool deathSequence;
    private float deathSequenceTimer;
    private float bossTransitionTimer;
    private bool victoryPending;
    private float victoryTimer;
    private const float CameraOverscan = 2f;
    internal static float ScreenShakeScale = .4f;
    private float cameraShakeTimer, cameraShakeStrength;
    private Vector3 cameraBasePosition;
    private Vector3 lastCameraPlayerPosition;
    private Vector2 cameraPlayerVelocity;
    private bool cameraReady;
    private float bossHitStopTimer;
    private float bossRewardTimer;
    private int pendingBossRewardStage, rewardBossStage;
    private UpgradeFlow currentUpgradeFlow;
    private bool chestReward;
    private int lastWave = 1;
    private string bannerMessage;
    private Color bannerColor = Color.white;
    private float bannerUntil;
    private Font uiFont, displayFont;
    private int normalRerolls = 3, weaponRerolls = 1;
    private Button rerollButton;
    private Text rerollLabel;
    private static bool beginAfterReload;
    private static CharacterKind continueCharacter = CharacterKind.Rei;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<GameBootstrap>() == null) new GameObject("Starfall Game").AddComponent<GameBootstrap>();
    }

    private static void ResetSaveForNewBuild()
    {
        string buildId = Application.version + "|" + (string.IsNullOrEmpty(Application.buildGUID) ? "no-guid" : Application.buildGUID);
        if (PlayerPrefs.GetString("Starfall.SaveBuild", "") == buildId) return;
        // Audio/settings are user preferences, not run progress. Keep them across builds.
        string[] keep = {
            "Starfall.Muted",
            "Starfall.Settings.Master", "Starfall.Settings.Music", "Starfall.Settings.Sfx",
            "Starfall.Settings.Quality", "Starfall.Settings.Fps", "Starfall.Settings.DamageNumbers",
            "Starfall.Settings.Shake", "Starfall.Settings.Particles", "Starfall.Settings.Language",
            "Starfall.Settings.AutoPause"
        };
        var saved = new Dictionary<string, int>();
        foreach (string key in keep) if (PlayerPrefs.HasKey(key)) saved[key] = PlayerPrefs.GetInt(key);
        PlayerPrefs.DeleteAll();
        foreach (var pair in saved) PlayerPrefs.SetInt(pair.Key, pair.Value);
        PlayerPrefs.SetString("Starfall.SaveBuild", buildId);
        PlayerPrefs.Save();
    }

    private void Awake()
    {
        if (Game != null && Game != this) { Destroy(gameObject); return; }
        Game = this;
        ResetSaveForNewBuild();
        InitializeMobileMode();
        var installed = new HashSet<string>(Font.GetOSInstalledFontNames());
        uiFont = PickFont(installed, "Microsoft YaHei UI", "Microsoft YaHei", "Noto Sans CJK SC", "Arial");
        displayFont = PickFont(installed, "Bahnschrift SemiCondensed", "Bahnschrift SemiBold", "Bahnschrift", "Segoe UI Semibold", "Arial");
        if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (displayFont == null) displayFont = uiFont;
        ConfigureFonts();
        GameSettings.Load();
        GameSettings.Apply();
        Time.timeScale = 1f;
        BuildWorld();
        BuildUI();
        InitializeMobilePresentation();
        ShowStatePanel(RunState.Menu);
        if (beginAfterReload) { beginAfterReload = false; StartRun(continueCharacter); }
        RefreshUI();
    }

    private void Update()
    {
        UpdateMusic();
        UpdateGameplayUiScale();
        if (settingsOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) CloseSettings();
            return;
        }
        if (State == RunState.CharacterSheet)
        {
            if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.Escape)) ClosePilotSheet();
            return;
        }
        if (State == RunState.Playing && Input.GetKeyDown(KeyCode.C) && !victoryPending && pendingBossRewardStage == 0)
        { OpenPilotSheet(); return; }
        if (State == RunState.CharacterSelect)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { ShowStatePanel(RunState.Menu); return; }
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) SelectCharacter(((int)SelectedCharacter + CharacterRoster.All.Length - 1) % CharacterRoster.All.Length);
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) SelectCharacter(((int)SelectedCharacter + 1) % CharacterRoster.All.Length);
            else if (Input.GetKeyDown(KeyCode.Alpha1)) SelectCharacter(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) SelectCharacter(1);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) SelectCharacter(2);
            else if (Input.GetKeyDown(KeyCode.Alpha4)) SelectCharacter(3);
            else if (Input.GetKeyDown(KeyCode.Alpha5)) SelectCharacter(4);
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) StartSelectedRun();
        }
        if (State == RunState.Playing)
        {
            if (deathSequence)
            {
                deathSequenceTimer -= Time.unscaledDeltaTime;
                if (deathSequenceTimer <= 0f) { deathSequence = false; Time.timeScale = 1f; Finish(false); }
                RefreshUI();
                return;
            }
            if (victoryPending)
            {
                victoryTimer -= Time.unscaledDeltaTime;
                if (victoryTimer <= 0) { victoryPending = false; Finish(true); }
                RefreshUI();
                return;
            }
            if (bossHitStopTimer > 0)
            {
                bossHitStopTimer -= Time.unscaledDeltaTime;
                if (bossHitStopTimer <= 0) Time.timeScale = 1f;
            }
            if (pendingBossRewardStage > 0)
            {
                bossRewardTimer -= Time.unscaledDeltaTime;
                if (bossRewardTimer <= 0)
                {
                    rewardBossStage = pendingBossRewardStage;
                    pendingBossRewardStage = 0;
                    SetUpgradeChoices(UpgradeFlow.Boss, false, rewardBossStage);
                }
                RefreshUI();
                return;
            }
            Elapsed += Time.deltaTime;
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0)
            {
                SpawnEnemy();
                bool lateStageTwo = ArenaStage == 2 && Elapsed - stageStartedAt > 55f;
                if (lateStageTwo && Random.value < .28f) SpawnEnemy();
                float extraChance = Mathf.Lerp(.08f, .34f, Mathf.Clamp01(Elapsed / 240f)) + BossesDefeated * .06f;
                if (Random.value < extraChance)
                {
                    SpawnEnemy();
                    if (Elapsed > 180f && Random.value < .35f) SpawnEnemy();
                }
                spawnTimer = GameRules.EnemySpawnInterval(Elapsed, BossesDefeated);
            }
            chestTimer -= Time.deltaTime;
            if (chestTimer <= 0) { SpawnChest(); chestTimer = 42f; }
            if (Wave > lastWave)
            {
                // Wave density still ramps up, but the stage banner only appears when the stage actually changes.
                lastWave = Wave;
                for (int i = 0; i < 3 + Wave; i++) SpawnEnemy();
            }
            if (!bossStarted && Elapsed >= GameRules.BossTime - 2.5f && warnedBossStage < 1) ShowBossWarning(1);
            if (!bossStarted && Elapsed >= GameRules.BossTime) SpawnBoss();
            if (bossTransitionTimer > 0)
            {
                int upcomingWarn = BossesDefeated + 1;
                if (bossTransitionTimer <= 2.5f && Boss == null && warnedBossStage < upcomingWarn) ShowBossWarning(upcomingWarn);
                bossTransitionTimer -= Time.deltaTime;
                if (bossTransitionTimer <= 0 && Boss == null && BossesDefeated < GameRules.BossCount) SpawnBoss();
            }
            if (Input.GetKeyDown(KeyCode.Escape)) Pause();
        }
        else if (State == RunState.Paused && Input.GetKeyDown(KeyCode.Escape)) Resume();
        if (State == RunState.Upgrade)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) ChooseUpgrade(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) ChooseUpgrade(1);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) ChooseUpgrade(2);
            else if (Input.GetKeyDown(KeyCode.R)) Reroll();
        }
        RefreshUI();
    }

    private void UpdateMusic()
    {
        string music;
        if (State == RunState.Menu || State == RunState.CharacterSelect) music = "music_menu";
        else if (State == RunState.Victory) music = "music_victory";
        else if (State == RunState.Defeat) music = "music_defeat";
        else
        {
            int bossStage = Boss != null ? Boss.BossStage : pendingBossRewardStage > 0 ? pendingBossRewardStage : State == RunState.Upgrade && currentUpgradeFlow == UpgradeFlow.Boss ? rewardBossStage : 0;
            music = bossStage > 0 ? "music_boss" + bossStage : "music_stage" + ArenaStage;
        }
        SoundBank.SetMusic(music);
    }

    private void LateUpdate()
    {
        UpdateMobileViewport();
        if (worldCamera == null || Player == null || State != RunState.Playing) return;

        float dt = Mathf.Min(.05f, Time.unscaledDeltaTime);
        float halfHeight = worldCamera.orthographicSize;
        float halfWidth = halfHeight * worldCamera.aspect;

        Vector3 playerPosition = Player.transform.position;
        if (!cameraReady)
        {
            cameraReady = true;
            cameraPlayerVelocity = Vector2.zero;
            cameraBasePosition = ClampCameraPosition(PlayerCameraFocus(playerPosition), halfWidth, halfHeight);
            lastCameraPlayerPosition = playerPosition;
            ApplyCameraPosition(dt);
            return;
        }

        float moveDt = Mathf.Max(.0001f, Time.deltaTime);
        Vector2 instantVelocity = ((Vector2)playerPosition - (Vector2)lastCameraPlayerPosition) / moveDt;
        cameraPlayerVelocity = Vector2.Lerp(cameraPlayerVelocity, instantVelocity, 1f - Mathf.Exp(-12f * dt));
        lastCameraPlayerPosition = playerPosition;

        Vector3 focus = PlayerCameraFocus(playerPosition);
        Vector3 desired = cameraBasePosition;
        if (Boss != null)
        {
            // Keep the player and the boss in the same composition without
            // letting a distant boss pull the player completely off-screen.
            Vector2 bossPosition = Boss.transform.position;
            Vector2 towardBoss = bossPosition - (Vector2)playerPosition;
            if (towardBoss.magnitude > 6.5f)
                bossPosition = (Vector2)playerPosition + towardBoss.normalized * 6.5f;
            focus = Vector2.Lerp(playerPosition, bossPosition, TouchControls ? .60f : .65f);
            desired = new Vector3(focus.x, focus.y, -20f);
        }
        else
        {
            float deadX = halfWidth * (TouchControls ? .18f : .14f);
            float deadY = halfHeight * (TouchControls ? .12f : .10f);
            float dx = focus.x - cameraBasePosition.x;
            float dy = focus.y - cameraBasePosition.y;
            if (Mathf.Abs(dx) > deadX)
                desired.x += dx - Mathf.Sign(dx) * deadX;
            if (Mathf.Abs(dy) > deadY)
                desired.y += dy - Mathf.Sign(dy) * deadY;
        }

        desired = ClampCameraPosition(desired, halfWidth, halfHeight);
        float sharpness = Boss != null ? 5.5f : TouchControls ? 10f : 8.5f;
        cameraBasePosition = Vector3.Lerp(cameraBasePosition, desired, 1f - Mathf.Exp(-sharpness * dt));
        ApplyCameraPosition(dt);
    }

    private Vector3 PlayerCameraFocus(Vector3 playerPosition)
    {
        Vector2 lead = cameraPlayerVelocity * (TouchControls ? .30f : .45f);
        lead = Vector2.ClampMagnitude(lead, TouchControls ? .35f : .55f);
        Vector2 focus = (Vector2)playerPosition + lead;
        return new Vector3(focus.x, focus.y, -20f);
    }

    private Vector3 ClampCameraPosition(Vector3 position, float halfWidth, float halfHeight)
    {
        // Let the camera reveal a little of the void outside the arena at the
        // edges. This keeps the player away from the HUD and touch controls
        // instead of pinning them to the screen edge.
        float limitX = Mathf.Max(0f, GameRules.HalfWidth - halfWidth) + CameraOverscan;
        float limitY = Mathf.Max(0f, GameRules.HalfHeight - halfHeight) + CameraOverscan;
        position.x = Mathf.Clamp(position.x, -limitX, limitX);
        position.y = Mathf.Clamp(position.y, -limitY, limitY);
        position.z = -20f;
        return position;
    }

    private void ApplyCameraPosition(float dt)
    {
        Vector2 shake = Vector2.zero;
        if (cameraShakeTimer > 0f)
        {
            cameraShakeTimer -= dt;
            float decay = Mathf.Clamp01(cameraShakeTimer * 4f);
            shake = Random.insideUnitCircle * cameraShakeStrength * decay;
        }
        worldCamera.transform.position = new Vector3(
            cameraBasePosition.x + shake.x,
            cameraBasePosition.y + shake.y,
            -20f);
    }

    private void ResetCameraRig()
    {
        cameraReady = false;
        cameraPlayerVelocity = Vector2.zero;
        if (Player != null) lastCameraPlayerPosition = Player.transform.position;
    }


    private void BuildWorld()
    {
        worldCamera = Camera.main;
        if (worldCamera == null) { var cameraObject = new GameObject("Main Camera"); cameraObject.tag = "MainCamera"; worldCamera = cameraObject.AddComponent<Camera>(); }
        worldCamera.orthographic = true; worldCamera.orthographicSize = 7.2f; worldCamera.transform.position = new Vector3(0, 0, -20); worldCamera.backgroundColor = new Color(.008f, .012f, .022f); worldCamera.clearFlags = CameraClearFlags.SolidColor;
        if (FindFirstObjectByType<AudioListener>() == null) worldCamera.gameObject.AddComponent<AudioListener>();
        CreateArena();
    }

    private void CreateArena()
    {
        Texture2D arenaTexture = Resources.Load<Texture2D>("Art/Arena/arena-floor-stage1");
        if (arenaTexture != null)
        {
            var ground = new GameObject("Blender ruin arena");
            ground.transform.position = new Vector3(0, 0, 5);
            arenaFloorRenderer = ground.AddComponent<SpriteRenderer>();
            arenaFloorRenderer.sprite = ArenaSprite(arenaTexture);
            arenaFloorRenderer.sortingOrder = -30;
            for (int i = 0; i < arenaStageSprites.Length; i++)
            {
                Texture2D stageTexture = Resources.Load<Texture2D>($"Art/Arena/arena-floor-stage{i + 1}");
                if (stageTexture != null) arenaStageSprites[i] = ArenaSprite(stageTexture);
            }
            SetArenaStage(1);
            DrawBoundary();
            Random.InitState(System.Environment.TickCount);
            return;
        }
        const int n = 512;
        var texture = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "Ruin floor" };
        Random.InitState(24419);
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
        {
            float vignette = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x, y), new Vector2(n / 2f, n / 2f)) / 410f);
            float grain = Random.Range(-.012f, .012f);
            Color color = new Color(.075f + vignette * .024f + grain, .105f + vignette * .028f + grain, .12f + vignette * .034f + grain);
            int gx = x % 64, gy = y % 64;
            if (gx < 2 || gy < 2) color *= .62f;
            if ((x * 19 + y * 43) % 1517 < 4) color = Color.Lerp(color, new Color(.07f, .32f, .34f), .3f);
            texture.SetPixel(x, y, color);
        }
        texture.Apply();
        var floor = new GameObject("Ruin tile floor"); floor.transform.position = new Vector3(0, 0, 5); floor.transform.localScale = new Vector3(GameRules.HalfWidth * 2f, GameRules.HalfHeight * 2f, 1);
        arenaFloorRenderer = floor.AddComponent<SpriteRenderer>(); arenaFloorRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, n, n), new Vector2(.5f, .5f), n); arenaFloorRenderer.sortingOrder = -30;
        DrawBoundary();
        CreateDeco();
        Random.InitState(System.Environment.TickCount);
    }

    private Sprite ArenaSprite(Texture2D texture) => Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), texture.width);

    private void SetArenaStage(int stage)
    {
        ArenaStage = Mathf.Clamp(stage, 1, GameRules.BossCount);
        stageStartedAt = Elapsed;
        if (arenaFloorRenderer == null) return;
        Sprite sprite = arenaStageSprites[ArenaStage - 1];
        if (sprite == null) return;
        arenaFloorRenderer.sprite = sprite;
        // ArenaSprite uses the texture width as pixels-per-unit, so the sprite is 1 unit
        // wide. Scale both axes from its native size to keep the art's aspect ratio and
        // let the floor extend beyond the wider arena bounds.
        float nativeWidth = sprite.rect.width / sprite.pixelsPerUnit;
        float nativeHeight = sprite.rect.height / sprite.pixelsPerUnit;
        // Preserve the arena texture's native aspect ratio. Cover the playable
        // rectangle, then let the dark void frame hide the overflow.
        float textureAspect = nativeWidth / nativeHeight;
        float arenaAspect = (GameRules.HalfWidth * 2f) / (GameRules.HalfHeight * 2f);
        float uniformScale = textureAspect >= arenaAspect
            ? (GameRules.HalfHeight * 2f) / nativeHeight
            : (GameRules.HalfWidth * 2f) / nativeWidth;
        arenaFloorRenderer.transform.localScale = new Vector3(uniformScale, uniformScale, 1);
    }

    private void DrawBoundary()
    {
        DrawVoidMask();
        for (int i = 0; i < 4; i++)
        {
            bool horizontal = i < 2;
            bool positive = i == 1 || i == 3;
            float sign = positive ? 1f : -1f;
            float edge = horizontal ? GameRules.HalfHeight * sign : GameRules.HalfWidth * sign;
            Vector3 edgePosition = horizontal ? new Vector3(0, edge, 1) : new Vector3(edge, 0, 1);
            Vector3 edgeScale = horizontal
                ? new Vector3(GameRules.HalfWidth * 2f, .14f, 1)
                : new Vector3(.14f, GameRules.HalfHeight * 2f, 1);
            Vector3 inset = horizontal ? new Vector3(0, -sign * .15f, 0) : new Vector3(-sign * .15f, 0, 0);
            Vector3 insetScale = horizontal
                ? new Vector3(GameRules.HalfWidth * 2f, .30f, 1)
                : new Vector3(.30f, GameRules.HalfHeight * 2f, 1);

            Vector3 glowScale = horizontal
                ? new Vector3(GameRules.HalfWidth * 2f, .34f, 1)
                : new Vector3(.34f, GameRules.HalfHeight * 2f, 1);

            BoundaryStrip("Boundary inner shadow", edgePosition + inset, insetScale, new Color(.005f, .012f, .02f, .82f), -29);
            BoundaryStrip("Boundary glow", edgePosition, glowScale, new Color(.14f, .75f, .72f, .09f), -10);
            BoundaryStrip("Ruin boundary", edgePosition, edgeScale, new Color(.22f, .96f, .90f, .92f), -9);
        }
    }

    private void DrawVoidMask()
    {
        const float depth = 24f;
        float width = GameRules.HalfWidth * 2f + depth * 2f;
        float height = GameRules.HalfHeight * 2f + depth * 2f;
        Color voidColor = new Color(.008f, .012f, .022f, 1f);
        BoundaryStrip("Void top", new Vector3(0, GameRules.HalfHeight + depth * .5f, 1f), new Vector3(width, depth, 1f), voidColor, -28);
        BoundaryStrip("Void bottom", new Vector3(0, -GameRules.HalfHeight - depth * .5f, 1f), new Vector3(width, depth, 1f), voidColor, -28);
        BoundaryStrip("Void left", new Vector3(-GameRules.HalfWidth - depth * .5f, 0, 1f), new Vector3(depth, height, 1f), voidColor, -28);
        BoundaryStrip("Void right", new Vector3(GameRules.HalfWidth + depth * .5f, 0, 1f), new Vector3(depth, height, 1f), voidColor, -28);
    }

    private void BoundaryStrip(string label, Vector3 position, Vector3 scale, Color color, int order)
    {
        var strip = new GameObject(label);
        strip.transform.position = position;
        strip.transform.localScale = scale;
        var renderer = strip.AddComponent<SpriteRenderer>();
        renderer.sprite = Shape("square");
        renderer.color = color;
        renderer.sortingOrder = order;
    }

    private void CreateDeco()
    {
        var parent = new GameObject("Ancient structures").transform;
        for (int i = 0; i < 30; i++)
        {
            float angle = i * 2.39996f, radius = 4.5f + (i % 6) * 1.1f;
            Vector2 position = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * .63f);
            if (Mathf.Abs(position.x) > GameRules.HalfWidth - 1 || Mathf.Abs(position.y) > GameRules.HalfHeight - 1) continue;
            var obj = new GameObject("Crystal flora"); obj.transform.SetParent(parent); obj.transform.position = position; obj.transform.localScale = Vector3.one * Random.Range(.18f, .54f);
            var renderer = obj.AddComponent<SpriteRenderer>(); renderer.sprite = Shape(i % 3 == 0 ? "diamond" : "circle"); renderer.color = i % 3 == 0 ? new Color(.16f, .54f, .52f, .65f) : new Color(.41f, .17f, .31f, .72f); renderer.sortingOrder = -8;
            if (i % 5 == 0)
            {
                var shard = new GameObject("Glow crystal"); shard.transform.SetParent(obj.transform, false); shard.transform.localPosition = new Vector3(.15f, .26f, -.01f); shard.transform.localScale = new Vector3(.13f, .23f, 1);
                var glow = shard.AddComponent<SpriteRenderer>(); glow.sprite = Shape("diamond"); glow.color = GameRules.Cyan; glow.sortingOrder = -7;
            }
        }
    }

    private static readonly Vector2 AnchorTL = new Vector2(0, 1), AnchorTC = new Vector2(.5f, 1), AnchorTR = new Vector2(1, 1);
    private static readonly Vector2 AnchorCL = new Vector2(0, .5f), AnchorCC = new Vector2(.5f, .5f), AnchorCR = new Vector2(1, .5f);
    private static readonly Vector2 AnchorBL = new Vector2(0, 0), AnchorBC = new Vector2(.5f, 0), AnchorBR = new Vector2(1, 0);

    private static Font PickFont(HashSet<string> installed, params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
            if (installed.Contains(names[i])) return Font.CreateDynamicFontFromOSFont(names[i], 18);
        return null;
    }

    private void SelectCharacter(int index)
    {
        if (index < 0 || index >= CharacterRoster.All.Length) return;
        SelectedCharacter = CharacterRoster.All[index].Id;
        UpdateCharacterSelection();
    }

    private void UpdateCharacterSelection()
    {
        CharacterProfile selected = CurrentCharacter;
        if (characterFeaturePortrait != null)
        {
            characterFeaturePortrait.sprite = SpriteResource(selected.Portrait);
            characterFeaturePortrait.color = Color.white;
            characterFeatureEdge.color = UiKit.Alpha(selected.Accent, .62f);
            characterFeatureGlow.color = UiKit.Alpha(selected.Accent, .3f);
            characterFeatureName.text = selected.Name;
            characterFeatureCallSign.text = selected.CallSign;
            characterFeatureQuote.text = CharacterQuote(selected.Id);
            characterFeatureStory.text = selected.Story;
            characterFeatureWeapon.text = selected.Weapon;
            characterFeatureWeaponDesc.text = WeaponDescription(selected.WeaponId);
            characterFeatureWeaponIcon.sprite = GetSprite("Icons/" + GameRules.IconAsset(selected.WeaponId));
            characterFeaturePassive.text = PassiveTitle(selected.Id);
            characterFeaturePassiveDesc.text = selected.Passive;
            characterFeatureIndex.text = ((int)selected.Id + 1).ToString("00") + " / 05";
            for (int s = 0; s < 3; s++)
            {
                int value = StatValue(selected.Id, s);
                for (int p = 0; p < 5; p++)
                    characterStatSegments[s, p].color = p < value ? UiKit.Alpha(selected.Accent, .92f) : new Color(1, 1, 1, .08f);
                characterStatValues[s].text = value + " / 5";
            }
        }
        for (int i = 0; i < characterButtons.Length; i++)
        {
            if (characterButtons[i] == null) continue;
            CharacterProfile profile = CharacterRoster.All[i]; bool isSelected = i == (int)SelectedCharacter;
            characterCardBodies[i].color = isSelected ? UiKit.Mix(new Color(.03f, .055f, .075f), profile.Accent, .16f) : new Color(1, 1, 1, .03f);
            characterCardEdges[i].color = isSelected ? UiKit.Alpha(profile.Accent, .95f) : new Color(1, 1, 1, .07f);
            characterCardGlows[i].color = UiKit.Alpha(profile.Accent, isSelected ? .24f : 0f);
            characterCardBars[i].color = isSelected ? profile.Accent : new Color(1, 1, 1, .12f);
            characterCardIndices[i].color = isSelected ? UiKit.TextMain : UiKit.TextDim;
            var rect = (RectTransform)characterButtons[i].transform;
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, isSelected ? 242f : 238f);
            var hover = characterButtons[i].GetComponent<UiHover>();
            if (hover != null) { hover.edgeIdle = characterCardEdges[i].color; hover.edgeHot = profile.Accent; }
        }
    }

    private static string CharacterQuote(CharacterKind id)
    {
        switch (id)
        {
            case CharacterKind.Rikka: return "\"让每一发炮火，都成为回家的信号。\"";
            case CharacterKind.Rin: return "\"失序只是尚未被计算的寒流。\"";
            case CharacterKind.Mei: return "\"轨道锁定，刃阵开始同步。\"";
            case CharacterKind.Yuna: return "\"听见了吗？星核还在呼吸。\"";
            default: return "\"我会找到下一段航线。\"";
        }
    }

    private static string PassiveTitle(CharacterKind id)
    {
        switch (id)
        {
            case CharacterKind.Rikka: return "热装填";
            case CharacterKind.Rin: return "零域共鸣";
            case CharacterKind.Mei: return "轨道校准";
            case CharacterKind.Yuna: return "过载回路";
            default: return "临界瞄准";
        }
    }

    private static string WeaponDescription(int id)
    {
        switch (id)
        {
            case 0: return "自动锁定最近目标，发射稳定的星弧能量脉冲。";
            case 1: return "扇形投射多枚高热弹体，适合清理近距离敌潮。";
            case 2: return "轨道刃围绕驾驶员旋转，接触敌人并可消除弹幕。";
            case 3: return "电弧在敌群之间跳跃，连续打击多个目标，首个目标额外承受 30% 伤害。";
            default: return "狙击式冰晶长枪，长弹体高速贯穿并施加霜缓。";
        }
    }

    private static int StatValue(CharacterKind id, int category) => CharacterRoster.Stat(id, category);

    private void StartSelectedRun() => StartRun(SelectedCharacter);

    private void ChooseUpgrade(int index)
    {
        if (State != RunState.Upgrade || index < 0 || index >= choices.Count) return;
        var option = choices[index];
        if (option.Flow == UpgradeFlow.Boss)
        {
            ApplyBossReward(rewardBossStage, option.RewardId);
            upgradePanel.SetActive(false); Time.timeScale = 1; State = RunState.Playing;
            if (rewardBossStage < GameRules.BossCount)
            {
                SetArenaStage(rewardBossStage + 1);
                bossTransitionTimer = GameRules.BossIntermissionSeconds(rewardBossStage); bossHitStopTimer = .14f; Time.timeScale = .16f;
                bossHeader.text = BossName(rewardBossStage + 1); bossHeader.color = GameRules.BossColor(rewardBossStage + 1);
                ShowBanner(GameRules.StageName(rewardBossStage + 1) + "  /  新级别敌潮已部署", GameRules.BossColor(rewardBossStage + 1));
            }
            else
            {
                victoryPending = true; victoryTimer = 1.65f; Time.timeScale = .18f;
                ShowBanner("ALL THREE BOSSES DOWN  /  GATE SECURED", GameRules.Cyan);
            }
            if (rewardBossStage < GameRules.BossCount) TryOpenQueuedLevelReward();
            RefreshUI(); return;
        }
        if (option.Flow == UpgradeFlow.Weapon)
        {
            if (option.Id >= 0)
            {
                if (option.Id >= Stats.Weapons.Length) return;
                if (option.IsEvolution && Stats.Weapons[option.Id] > 0 && !Stats.EvolvedWeapons[option.Id])
                {
                    Stats.EvolvedWeapons[option.Id] = true;
                    Effects.WeaponEvolution(Player.transform.position, option.Color);
                    ShowBanner("武器进阶！", option.Color);
                }
                else if (!option.IsEvolution && Stats.Weapons[option.Id] == 0)
                {
                    // Unlock at level one; later weapon skill ranks remain exclusive to normal upgrades.
                    Stats.Weapons[option.Id] = 1;
                    ShowBanner("获得新武器！", option.Color);
                }
            }
            else if (option.Id == -1) Stats.BoostDamageMultiplier *= 1.1f;
            else if (option.Id == -2) Stats.BoostHasteMultiplier *= 1.08f;
            else if (option.Id == -3) Stats.Crit += .06f;
        }
        else if (option.IsWeapon) Stats.Weapons[option.Id] = Mathf.Min(GameRules.MaxWeaponRank, Stats.Weapons[option.Id] + 1);
        else switch (option.Id)
        {
            case 5: Stats.DamageBonus += GameRules.DamageUpgradeAtRank(Stats.Ranks[5]); Stats.Ranks[5]++; break;
            case 6: Stats.HasteBonus += GameRules.HasteUpgradeAtRank(Stats.Ranks[6]); Stats.Ranks[6]++; break;
            case 7: Stats.SpeedBonus += GameRules.SpeedUpgradeAtRank(Stats.Ranks[7]); Stats.Ranks[7]++; break;
            case 8: Stats.CritDamage += GameRules.CritDamageUpgradeAtRank(Stats.Ranks[8]); Stats.Ranks[8]++; break;
            case 9: Stats.Magnet += GameRules.MagnetUpgradeAtRank(Stats.Ranks[9]); Stats.Ranks[9]++; break;
            case 10: Stats.Crit += GameRules.CritUpgradeAtRank(Stats.Ranks[10]); Stats.Ranks[10]++; break;
            case 11: Stats.Pierce += GameRules.PierceUpgradeAtRank(Stats.Ranks[11]); Stats.Ranks[11]++; break;
            case 12: Stats.Regeneration += GameRules.RegenerationUpgradeAtRank(Stats.Ranks[12]); Stats.Ranks[12]++; break;
            case 13: Stats.Armor += GameRules.ArmorUpgradeAtRank(Stats.Ranks[13]); Stats.Ranks[13]++; break;
            case 14:
                int healthRank = Stats.Ranks[14];
                Stats.MaxHealth += GameRules.MaxHealthUpgradeAtRank(healthRank);
                Player.Heal(GameRules.MaxHealthHealAtRank(healthRank));
                Stats.Ranks[14]++;
                break;
            default: Player.Heal(GameRules.EmergencyHeal); break;
        }
        upgradePanel.SetActive(false); Time.timeScale = 1; State = RunState.Playing; Player.Pulse(); RefreshUI();
        TryOpenQueuedLevelReward();
    }

    public void StartRun() => StartRun(SelectedCharacter);

    public void StartRun(CharacterKind character)
    {
        SelectedCharacter = character; Time.timeScale = 1; State = RunState.Playing; if (statePanel != null) statePanel.SetActive(false);
        Stats = new RunStats(); CharacterRoster.ApplyLoadout(SelectedCharacter, Stats);
        if (pilotPortrait != null) pilotPortrait.sprite = SquarePortrait(CurrentCharacter.Portrait);
        Player = new GameObject("Runner").AddComponent<PlayerActor>(); Player.transform.position = Vector3.zero;
        ResetCameraRig();
        Elapsed = 0; Kills = 0; Level = 1; Xp = 0; spawnTimer = .8f; chestTimer = 32f; lastWave = 1; normalRerolls = 3; weaponRerolls = 1;
        bossStarted = false; warnedBossStage = 0; bossWarningUntil = 0f; if (bossWarningPanel != null) bossWarningPanel.SetActive(false); bossTransitionTimer = 0; bossRewardTimer = 0; pendingBossRewardStage = rewardBossStage = 0; victoryPending = false; victoryTimer = 0; deathSequence = false; deathSequenceTimer = 0; bossHitStopTimer = 0; BossStage = 0; BossesDefeated = 0; Boss = null; enemies.Clear(); SetArenaStage(1);
        for (int i = 0; i < bossRewardsClaimed.Length; i++) bossRewardsClaimed[i] = false;
        Time.timeScale = 1f;
        hpDisplay = hpGhostDisplay = dashDisplay = 1f; xpDisplay = 0f; bossDisplay = 1f; bannerUntil = 0f;
        RefreshUI(); ShowBanner(GameRules.StageName(1), GameRules.Cyan);
    }

    private void RestartRun() { Time.timeScale = 1; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
    private void PlayAgain() { beginAfterReload = true; continueCharacter = SelectedCharacter; RestartRun(); }
    private void Reroll()
    {
        if (State != RunState.Upgrade || currentUpgradeFlow == UpgradeFlow.Boss) return;
        var avoid = new List<int>();
        for (int i = 0; i < choices.Count; i++) avoid.Add(choices[i].Id);
        if (currentUpgradeFlow == UpgradeFlow.Weapon)
        {
            if (weaponRerolls <= 0) return;
            weaponRerolls--;
            SetUpgradeChoices(UpgradeFlow.Weapon, false, 0, avoid);
        }
        else
        {
            if (normalRerolls <= 0) return;
            normalRerolls--;
            SetUpgradeChoices(UpgradeFlow.Normal, chestReward, 0, avoid);
        }
    }
    private void Pause() { if (State != RunState.Playing || deathSequence) return; Time.timeScale = 0; ShowStatePanel(RunState.Paused); }
    private void Resume() { if (State != RunState.Paused) return; statePanel.SetActive(false); State = RunState.Playing; Time.timeScale = 1; }

    public void AddXp(int amount)
    {
        if (State != RunState.Playing || victoryPending) return;
        Xp += amount;
        TryOpenQueuedLevelReward();
    }

    private bool TryOpenQueuedLevelReward()
    {
        if (State != RunState.Playing || victoryPending || pendingBossRewardStage > 0 || Xp < XpNeeded) return false;
        Xp -= XpNeeded; Level++;
        bool weaponMilestone = Level > 1 && (Level - 1) % GameRules.WeaponChoiceInterval == 0;
        SetUpgradeChoices(weaponMilestone ? UpgradeFlow.Weapon : UpgradeFlow.Normal);
        return true;
    }

    public List<EnemyActor> Nearby(Vector2 point, float radius, int max = 32, bool sorted = true)
    {
        var found = nearbyBuffer;
        found.Clear();
        float maxDistance = radius * radius;
        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || (enemy.transform.position - (Vector3)point).sqrMagnitude > maxDistance) continue;
            found.Add(enemy);
            if (found.Count >= max) break;
        }
        if (sorted && found.Count > 1)
            found.Sort((a, b) => (a.transform.position - (Vector3)point).sqrMagnitude.CompareTo((b.transform.position - (Vector3)point).sqrMagnitude));
        return found;
    }

    private static string ArtPath(string name)
    {
        if (name == "arena") return "Art/UI/Illustrations/arena";
        if (name.StartsWith("Icons/")) return "Art/UI/" + name;
        if (name.StartsWith("pilot_sprite_")) return "Art/Characters/Sprites/" + name;
        if (name.StartsWith("portrait_")) return "Art/Characters/Portraits/" + name;
        if (name.StartsWith("starfall_boss")) return "Art/Bosses/" + name;
        if (name.StartsWith("starfall_")) return "Art/Enemies/" + name;
        return "Art/" + name;
    }

    public Sprite GetSprite(string name)
    {
        if (!sprites.TryGetValue(name, out Sprite sprite))
        {
            Texture2D texture = Resources.Load<Texture2D>(ArtPath(name));
            if (texture != null)
            {
                int pixelsPerUnit = name.StartsWith("pilot_sprite_") ? 512 : name.StartsWith("starfall_boss_echo_herald") || name.StartsWith("starfall_boss_star_eater") ? 256 : 128;
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), pixelsPerUnit);
            }
            sprites[name] = sprite;
        }
        return sprite != null ? sprite : Shape("circle");
    }

    public Sprite Shape(string name)
    {
        if (shapes.TryGetValue(name, out Sprite cached)) return cached;
        int size = 64; var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, name = "Starfall " + name };
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float xx = (x + .5f - size * .5f) / (size * .5f), yy = (y + .5f - size * .5f) / (size * .5f);
            bool inside = name == "square" ? Mathf.Abs(xx) < .96f && Mathf.Abs(yy) < .96f : name == "diamond" ? Mathf.Abs(xx) + Mathf.Abs(yy) < 1.14f : name == "void_blade" ? Mathf.Abs(xx) < (.18f + Mathf.Abs(yy) * .16f) && Mathf.Abs(yy) < .98f && (Mathf.Abs(yy) > .13f || Mathf.Abs(xx) < .08f) : name == "cryo_lance" || name == "cryo_lance_evolved" ? Mathf.Abs(yy) < .98f && Mathf.Abs(xx) < (.16f + (1f - Mathf.Abs(yy)) * .62f) : name == "pulse_nova" ? xx * xx + yy * yy < .82f || Mathf.Abs(xx) + Mathf.Abs(yy) < .72f : name == "ember_bolt" ? Mathf.Abs(xx) + Mathf.Abs(yy) < 1.04f && yy > -.9f : xx * xx + yy * yy < .91f;
            texture.SetPixel(x, y, inside ? Color.white : Color.clear);
        }
        texture.Apply(); cached = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size); shapes[name] = cached; return cached;
    }

    private Sprite SpriteResource(string key)
    { Texture2D texture = Resources.Load<Texture2D>(ArtPath(key)); return texture == null ? null : Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 128); }

    private void SpawnEnemy()
    {
        enemies.RemoveAll(e => e == null);
        if (Player == null || enemies.Count >= 110) return;
        Vector2 position = Vector2.zero;
        for (int attempt = 0; attempt < 12; attempt++)
        {
            int side = Random.Range(0, 4);
            position = side < 2 ? new Vector2(Random.Range(-GameRules.HalfWidth + 1, GameRules.HalfWidth - 1), (side == 0 ? -1 : 1) * (GameRules.HalfHeight - 1)) : new Vector2((side == 2 ? -1 : 1) * (GameRules.HalfWidth - 1), Random.Range(-GameRules.HalfHeight + 1, GameRules.HalfHeight - 1));
            if (Vector2.Distance(position, Player.transform.position) >= 6) break;
        }
        EnemyKind kind = GameRules.SelectEnemyKind(BossesDefeated, Elapsed, Random.value);
        SpawnEnemy(kind, position);
    }

    private void SpawnEnemy(EnemyKind kind, Vector2 position)
    {
        var enemy = new GameObject(kind.ToString()).AddComponent<EnemyActor>();
        float difficulty = GameRules.EnemyTimeMultiplier(Elapsed) * GameRules.BossEnemyMultiplier(BossesDefeated) * GameRules.EnemyStageHealthMultiplier(BossesDefeated);
        enemy.Setup(kind, position, difficulty, 0, GameRules.EnemyStageAttackMultiplier(BossesDefeated)); enemies.Add(enemy);
    }
    public void SpawnBossAdds(Vector2 center, int count)
    {
        enemies.RemoveAll(e => e == null);
        for (int i = 0; i < count && enemies.Count < 110; i++)
        {
            float angle = (i * 137.5f + Time.time * 35f) * Mathf.Deg2Rad;
            Vector2 position = GameRules.Clamp(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 2.8f);
            SpawnEnemy(BossStage == 2 ? EnemyKind.EchoWisp : EnemyKind.NovaReaver, position);
        }
    }
    private void SpawnBoss()
    {
        BossStage = Mathf.Clamp(BossesDefeated + 1, 1, GameRules.BossCount);
        bossStarted = true; bossTransitionTimer = 0; bossPanel.SetActive(true);
        bossHeader.text = BossName(BossStage); bossHeader.color = GameRules.BossColor(BossStage);
        Vector2 spawn;
        if (BossStage == 3)
        {
            // Stage three is an anchored arena encounter; keep the colossal boss at world/screen center.
            spawn = Vector2.zero;
        }
        else spawn = new Vector2(Player.transform.position.x > 0 ? -9 : 9, Player.transform.position.y > 0 ? -4 : 4);
        Boss = new GameObject(BossName(BossStage)).AddComponent<EnemyActor>(); Boss.Setup(EnemyKind.Boss, spawn, 1f, BossStage); enemies.Add(Boss);
        bossDisplay = 1f;
    }
    private void SpawnChest()
    {
        float angle = Random.value * Mathf.PI * 2f; Vector2 p = GameRules.Clamp((Vector2)Player.transform.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 6f);
        var chest = new GameObject("Supply Cache").AddComponent<SupplyChest>(); chest.transform.position = p;
    }
    public void OpenChest() { if (State != RunState.Playing) return; Player.Heal(12); SetUpgradeChoices(true); }
    public void RegisterKill(EnemyActor enemy)
    {
        if (enemy.Kind != EnemyKind.Boss) { Kills++; return; }
        int stage = Mathf.Clamp(enemy.BossStage, 1, GameRules.BossCount);
        Boss = null; BossesDefeated = Mathf.Max(BossesDefeated, stage);
        ShakeCamera(.34f + stage * .06f, .5f);
        if (stage >= GameRules.BossCount)
        {
            // The final boss ends the run directly; no third core draft page.
            victoryPending = true; victoryTimer = 1.65f; bossHitStopTimer = .14f; Time.timeScale = .18f;
            ShowBanner("ALL THREE BOSSES DOWN  /  GATE SECURED", GameRules.Cyan);
            return;
        }
        pendingBossRewardStage = stage; bossRewardTimer = 1.15f; bossHitStopTimer = .14f; Time.timeScale = .16f;
        ShowBanner(GameRules.BossName(stage) + "  /  核心暴露", GameRules.BossColor(stage));
    }
    private void ApplyBossReward(int stage, int rewardId)
    {
        if (rewardId < 0 || rewardId >= bossRewardsClaimed.Length || bossRewardsClaimed[rewardId]) return;
        bossRewardsClaimed[rewardId] = true;
        switch (rewardId)
        {
            case 0:
                Stats.CoreDamageMultiplier *= 1.30f;
                Stats.CoreHasteMultiplier *= 1.20f;
                Stats.Crit += 0.20f;
                break;
            case 1:
                Stats.MaxHealth += 30;
                Stats.Armor += .10f;
                Stats.Regeneration += 2f;
                Stats.DamageImmunityChance = Mathf.Min(.9f, Stats.DamageImmunityChance + GameRules.PhoenixDamageImmunity);
                break;
            default:
                Stats.Crit += .30f;
                Stats.CritDamage += .50f;
                Stats.ResonanceOverflow = true;
                break;
        }
        if (Player != null) { Player.Heal(Player.MaxHealth); Player.Pulse(); }
        ShowBanner(GameRules.StageName(stage) + "  /  共鸣强化已载入", GameRules.BossColor(stage));
    }
    private static string BossName(int stage)
    {
        switch (stage)
        {
            case 1: return "噬影守望者";
            case 2: return "回响先驱";
            default: return "星蚀巨兽";
        }
    }
    public void ShakeCamera(float strength, float duration)
    {
        cameraShakeStrength = Mathf.Max(cameraShakeStrength, strength * ScreenShakeScale);
        cameraShakeTimer = Mathf.Max(cameraShakeTimer, duration);
    }
    public void Finish(bool victory)
    {
        if (State == RunState.Victory || State == RunState.Defeat) return;
        if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--smoke-test") < 0)
        {
            PlayerPrefs.SetInt("Starfall.BestKills", Mathf.Max(Kills, PlayerPrefs.GetInt("Starfall.BestKills", 0)));
            if (victory) PlayerPrefs.SetInt("Starfall.Wins", PlayerPrefs.GetInt("Starfall.Wins", 0) + 1);
            PlayerPrefs.Save();
        }
        Time.timeScale = 0; ShowStatePanel(victory ? RunState.Victory : RunState.Defeat);
    }
    public void BeginPlayerDeath()
    {
        if (deathSequence || State != RunState.Playing) return;
        deathSequence = true; deathSequenceTimer = .9f; Time.timeScale = .22f;
        if (Player != null)
        {
            Effects.PlayerDeath(Player.transform.position, Player.VisualSprite, Player.VisualScale, CurrentCharacter.Accent);
            Player.HideForDeath();
        }
        ShakeCamera(.55f, .6f);
    }

    public void TrackWeaponDamage(int weaponId, float amount)
    {
        if (weaponId < 0 || weaponId >= Stats.WeaponDamage.Length || amount <= 0f) return;
        Stats.WeaponDamage[weaponId] += amount;
    }

    public void ShowBanner(string message, Color tint)
    {
        bannerMessage = message; bannerColor = tint; bannerUntil = Time.unscaledTime + 2.5f;
        if (bannerText != null) { bannerText.text = message; bannerText.color = Color.Lerp(tint, Color.white, .3f); }
        if (bannerAccent != null) bannerAccent.color = tint;
    }

    private Sprite uiSprite;
    private Sprite UISprite()
    {
        if (uiSprite != null) return uiSprite;
        var texture = new Texture2D(1, 1); texture.SetPixel(0, 0, Color.white); texture.Apply();
        return uiSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
    }

    public Vector2 Separation(EnemyActor self)
    {
        Vector2 displacement = Vector2.zero; Vector2 p = self.transform.position;
        foreach (var other in enemies)
        {
            if (other == null || other == self || other.Kind == EnemyKind.Boss) continue;
            Vector2 difference = p - (Vector2)other.transform.position;
            float radius = self.Radius + other.Radius;
            float distance = difference.magnitude;
            if (distance > .001f && distance < radius) displacement += difference / distance * (radius - distance) * 2.5f;
        }
        return displacement;
    }
}

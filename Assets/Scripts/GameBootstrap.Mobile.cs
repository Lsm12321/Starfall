using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Mobile presentation: 20:9 viewport lock, virtual stick, dash trigger and
// touch-friendly HUD refresh hooks. Desktop keeps the original 16:9 layout.
public partial class GameBootstrap
{
    private const float DesktopAspect = 16f / 9f;
    private const float MobileAspect = 20f / 9f;
    private float LockedAspect => touchControls ? MobileAspect : DesktopAspect;
    private bool touchControls;
    private Vector2 touchMove;
    private Camera letterboxCamera;
    private int viewportWidth, viewportHeight;
    private VirtualJoystick virtualJoystick;
    private MobileActionButton dashTouchButton;
    private Image dashButtonRing;
    private Text dashButtonState;
    private GameObject touchRoot;
    private CanvasScaler uiScaler;
    private bool gameplayUiScale = true;

    public bool TouchControls => touchControls;
    public Vector2 TouchMove => touchMove;

    private static bool HasCommandLineFlag(string flag)
    {
        return Environment.GetCommandLineArgs().Any(arg => string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase));
    }

    private void InitializeMobileMode()
    {
        touchControls = Application.isMobilePlatform || HasCommandLineFlag("--mobile-ui");
        Input.multiTouchEnabled = true;
        if (!Application.isMobilePlatform) return;
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Screen.fullScreen = true;
        Application.targetFrameRate = 60;
    }

    private void ConfigureFonts()
    {
        var bundled = Resources.Load<Font>("Fonts/NotoSansSC-Regular");
        if (bundled == null) return;
        if (Application.isMobilePlatform || touchControls)
        {
            uiFont = bundled;
            displayFont = bundled;
        }
        else if (uiFont == null)
        {
            uiFont = bundled;
            if (displayFont == null) displayFont = bundled;
        }
    }

    private void InitializeMobilePresentation()
    {
        var letterbox = new GameObject("Aspect letterbox");
        letterbox.transform.SetParent(transform, false);
        letterboxCamera = letterbox.AddComponent<Camera>();
        letterboxCamera.clearFlags = CameraClearFlags.SolidColor;
        letterboxCamera.backgroundColor = Color.black;
        letterboxCamera.cullingMask = 0;
        letterboxCamera.depth = worldCamera.depth - 10f;
        letterboxCamera.orthographic = true;
        letterboxCamera.orthographicSize = 1f;
        letterboxCamera.allowHDR = false;
        letterboxCamera.allowMSAA = false;
        letterboxCamera.useOcclusionCulling = false;
        letterboxCamera.transform.position = new Vector3(0, 0, -60);

        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = worldCamera;
            canvas.planeDistance = 1f;
            uiScaler = canvas.GetComponent<CanvasScaler>();
        }
        ApplyAspectLock(true);
    }

    // The in-game HUD uses the phone reference for both desktop and mobile.
    private void UpdateGameplayUiScale()
    {
        if (uiScaler == null) return;
        bool gameplay = State == RunState.Playing;
        if (gameplay == gameplayUiScale) return;
        gameplayUiScale = gameplay;
        uiScaler.referenceResolution = gameplay ? new Vector2(1240f, 697.5f) : new Vector2(1440f, 810f);
        uiScaler.matchWidthOrHeight = gameplay ? 1f : (touchControls ? 1f : .5f);
        Canvas.ForceUpdateCanvases();
    }

    private void ApplyAspectLock(bool force)
    {
        if (worldCamera == null) return;
        int width = Mathf.Max(1, Screen.width), height = Mathf.Max(1, Screen.height);
        if (!force && width == viewportWidth && height == viewportHeight) return;
        viewportWidth = width; viewportHeight = height;
        float aspect = width / (float)height;
        Rect rect;
        if (aspect > LockedAspect)
        {
            float viewWidth = LockedAspect / aspect;
            rect = new Rect((1f - viewWidth) * .5f, 0f, viewWidth, 1f);
        }
        else
        {
            float viewHeight = aspect / LockedAspect;
            rect = new Rect(0f, (1f - viewHeight) * .5f, 1f, viewHeight);
        }
        worldCamera.rect = rect;
        if (letterboxCamera != null) letterboxCamera.rect = new Rect(0f, 0f, 1f, 1f);
        if (canvas != null) Canvas.ForceUpdateCanvases();
    }

    private void UpdateMobileViewport()
    {
        ApplyAspectLock(false);
        if (virtualJoystick != null && State != RunState.Playing && State != RunState.Upgrade) virtualJoystick.ResetInput();
    }

    public void SetTouchMove(Vector2 value)
    {
        touchMove = Vector2.ClampMagnitude(value, 1f);
    }

    public void RequestDash()
    {
        if (State != RunState.Playing || Player == null) return;
        Player.RequestDash();
    }

    public void RequestPause()
    {
        if (State != RunState.Playing) return;
        if (virtualJoystick != null) virtualJoystick.ResetInput();
        Pause();
    }

    private void BuildTouchControls()
    {
        touchRoot = new GameObject("Touch controls", typeof(RectTransform));
        touchRoot.transform.SetParent(hudRoot.transform, false);
        Stretch((RectTransform)touchRoot.transform);

        // Bottom-left virtual stick. The touch zone is larger than the drawn stick.
        var hitZone = Node(touchRoot.transform, "Movement joystick hit", AnchorBL, AnchorBL, AnchorCC, new Vector2(202f, 180f), new Vector2(396f, 396f));
        var hitArea = Art(hitZone, "Hit area", UiKit.Solid, new Color(1f, 1f, 1f, .001f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        hitArea.raycastTarget = true;

        var pad = Node(hitZone, "Movement joystick", AnchorCC, Vector2.zero, new Vector2(236f, 236f));
        pad.localScale = Vector3.one * 1.12f;
        Art(pad, "Stick glow", UiKit.Glow, new Color(.12f, .92f, .86f, .12f), AnchorCC, Vector2.zero, new Vector2(250f, 250f));
        Art(pad, "Stick base", UiKit.Dot, new Color(.018f, .06f, .082f, .7f), AnchorCC, Vector2.zero, new Vector2(214f, 214f));
        Art(pad, "Stick edge", UiKit.Ring, UiKit.Alpha(GameRules.Cyan, .38f), AnchorCC, Vector2.zero, new Vector2(216f, 216f));
        Art(pad, "Stick inner", UiKit.Ring, UiKit.Alpha(GameRules.Cyan, .13f), AnchorCC, Vector2.zero, new Vector2(128f, 128f));
        Art(pad, "Stick cross H", UiKit.Solid, new Color(1f, 1f, 1f, .08f), AnchorCC, Vector2.zero, new Vector2(140f, 1f));
        Art(pad, "Stick cross V", UiKit.Solid, new Color(1f, 1f, 1f, .08f), AnchorCC, Vector2.zero, new Vector2(1f, 140f));

        var knob = Node(pad, "Stick knob", AnchorCC, Vector2.zero, new Vector2(92f, 92f));
        Art(knob, "Knob glow", UiKit.Glow, UiKit.Alpha(GameRules.Cyan, .34f), AnchorCC, Vector2.zero, new Vector2(148f, 148f));
        Art(knob, "Knob body", UiKit.Dot, new Color(.026f, .12f, .145f, .95f), AnchorCC, Vector2.zero, new Vector2(86f, 86f));
        Art(knob, "Knob edge", UiKit.Ring, UiKit.Alpha(GameRules.Cyan, .92f), AnchorCC, Vector2.zero, new Vector2(92f, 92f));
        Art(knob, "Knob core", UiKit.Dot, new Color(1f, 1f, 1f, .6f), AnchorCC, Vector2.zero, new Vector2(10f, 10f));

        virtualJoystick = hitZone.gameObject.AddComponent<VirtualJoystick>();
        virtualJoystick.Init(this, knob, 84f, canvas);

        // Bottom-right dash action.
        var dash = Node(touchRoot.transform, "Dash touch button", AnchorBR, AnchorBR, AnchorCC, new Vector2(-158f, 156f), new Vector2(178f, 178f));
        dash.localScale = Vector3.one * 1.08f;
        var dashHit = Art(dash, "Touch area", UiKit.Ring, new Color(1f, 1f, 1f, .002f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        dashHit.raycastTarget = true;
        Art(dash, "Dash glow", UiKit.Glow, UiKit.Alpha(GameRules.Cyan, .2f), AnchorCC, Vector2.zero, new Vector2(220f, 220f));
        Art(dash, "Dash body", UiKit.Dot, new Color(.02f, .075f, .095f, .86f), AnchorCC, Vector2.zero, new Vector2(168f, 168f));
        Art(dash, "Dash edge", UiKit.Ring, UiKit.Alpha(GameRules.Cyan, .45f), AnchorCC, Vector2.zero, new Vector2(172f, 172f));
        dashButtonRing = Art(dash, "Dash cooldown ring", UiKit.Ring, UiKit.Alpha(GameRules.Cyan, .9f), AnchorCC, Vector2.zero, new Vector2(160f, 160f));
        dashButtonRing.type = Image.Type.Filled;
        dashButtonRing.fillMethod = Image.FillMethod.Radial360;
        dashButtonRing.fillOrigin = (int)Image.Origin360.Top;
        dashButtonRing.fillClockwise = false;
        dashButtonRing.fillAmount = 1f;
        Art(dash, "Dash core", UiKit.Dot, new Color(.035f, .11f, .14f, .6f), AnchorCC, Vector2.zero, new Vector2(134f, 134f));
        MakeText(dash, "Dash label", "冲刺", 23, TextAnchor.MiddleCenter, AnchorCC, AnchorCC, AnchorCC,
            new Vector2(0f, 8f), new Vector2(150f, 26f), UiKit.TextMain, FontStyle.Bold, displayFont);
        Art(dash, "Dash rule", UiKit.Solid, UiKit.Alpha(GameRules.Cyan, .3f), AnchorCC, new Vector2(0f, -9f), new Vector2(58f, 1f));
        dashButtonState = MakeText(dash, "Dash state", "就绪", 14, TextAnchor.MiddleCenter, AnchorCC, AnchorCC, AnchorCC,
            new Vector2(0f, -24f), new Vector2(132f, 22f), GameRules.Cyan, FontStyle.Bold, uiFont);
        dashTouchButton = dash.gameObject.AddComponent<MobileActionButton>();
        dashTouchButton.Init(this, false);

        // Character sheet shortcut, directly under the top-left vitals.
        var sheet = Node(touchRoot.transform, "Open pilot sheet", AnchorTL, new Vector2(22f, -112f), new Vector2(96f, 44f));
        var sheetBody = Art(sheet, "Fill", UiKit.Soft, new Color(.02f, .06f, .08f, .9f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        sheetBody.raycastTarget = true;
        Fill(sheet, "Edge", UiKit.SoftEdge, UiKit.Alpha(GameRules.Cyan, .28f));
        Art(sheet, "Mark", UiKit.Diamond, UiKit.Alpha(GameRules.Cyan, .85f), AnchorTL, new Vector2(10f, -17f), new Vector2(7f, 7f));
        Mc(sheet, "角色", 12, UiKit.TextMain, FontStyle.Bold);
        var sheetButton = sheet.gameObject.AddComponent<Button>();
        sheetButton.targetGraphic = sheetBody; sheetButton.transition = Selectable.Transition.None;
        sheetButton.onClick.AddListener(OpenPilotSheet);

        // Pause sits just below the top-right stage panel, aligned to its right edge.
        var pause = Node(touchRoot.transform, "Pause button", AnchorTR, new Vector2(-22f, -128f), new Vector2(44f, 44f));
        var pauseBody = Art(pause, "Fill", UiKit.Soft, new Color(.02f, .06f, .08f, .9f), Vector2.zero, Vector2.one, AnchorCC, Vector2.zero, Vector2.zero);
        pauseBody.raycastTarget = true;
        Fill(pause, "Edge", UiKit.SoftEdge, UiKit.Alpha(GameRules.Gold, .35f));
        Art(pause, "Pause bar left", UiKit.Solid, UiKit.TextMain, AnchorCC, new Vector2(-5f, 0f), new Vector2(4f, 16f));
        Art(pause, "Pause bar right", UiKit.Solid, UiKit.TextMain, AnchorCC, new Vector2(5f, 0f), new Vector2(4f, 16f));
        pause.gameObject.AddComponent<MobileActionButton>().Init(this, true);
    }

    private void RefreshTouchHud()
    {
        if (Player == null) return;
        if (dashButtonRing != null)
        {
            dashButtonRing.fillAmount = Player.DashReady ? 1f : Mathf.Clamp01(dashDisplay);
            dashButtonRing.color = Player.DashReady ? UiKit.Alpha(GameRules.Cyan, .9f) : UiKit.Alpha(GameRules.Pink, .5f);
        }
        if (dashButtonState != null)
        {
            SetText(dashButtonState, Player.DashReady ? "就绪" : Player.DashCooldown.ToString("0.0") + "s");
            dashButtonState.color = Player.DashReady ? GameRules.Cyan : UiKit.TextDim;
        }
    }
}

public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private GameBootstrap game;
    private RectTransform knob;
    private Canvas canvas;
    private float radius = 84f;
    private int activePointer = -1;
    private bool hasPointer;
    private Vector2 value;

    public void Init(GameBootstrap owner, RectTransform stick, float stickRadius, Canvas parentCanvas)
    {
        game = owner; knob = stick; radius = Mathf.Max(1f, stickRadius); canvas = parentCanvas;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (game == null || game.State != RunState.Playing || hasPointer) return;
        activePointer = eventData.pointerId;
        hasPointer = true;
        UpdateFromScreen(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!hasPointer || eventData.pointerId != activePointer) return;
        UpdateFromScreen(eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!hasPointer || eventData.pointerId != activePointer) return;
        ResetInput();
        activePointer = -1;
    }

    private void UpdateFromScreen(Vector2 screenPosition)
    {
        var rect = (RectTransform)transform;
        Camera camera = canvas != null ? canvas.worldCamera : null;
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPosition, camera, out local)) return;
        Vector2 next = Vector2.ClampMagnitude(local / radius, 1f);
        float magnitude = next.magnitude;
        next = magnitude <= .1f ? Vector2.zero : next.normalized * Mathf.InverseLerp(.1f, 1f, magnitude);
        value = next;
        if (knob != null) knob.anchoredPosition = next * radius;
        if (game != null) game.SetTouchMove(next);
    }

    private bool TryCurrentPointer(out Vector2 screenPosition)
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.fingerId != activePointer) continue;
            screenPosition = touch.position;
            return true;
        }
        if (Input.GetMouseButton(0))
        {
            screenPosition = Input.mousePosition;
            return true;
        }
        screenPosition = Vector2.zero;
        return false;
    }

    public void ResetInput()
    {
        hasPointer = false;
        activePointer = -1;
        value = Vector2.zero;
        if (knob != null) knob.anchoredPosition = Vector2.zero;
        if (game != null) game.SetTouchMove(Vector2.zero);
    }

    private void Update()
    {
        if (!hasPointer || game == null || !Application.isMobilePlatform) return;
        if (game.State == RunState.Upgrade)
        {
            // The upgrade overlay takes pointer events, but the physical touch may
            // still be held. Keep it alive and re-read its position on resume.
            if (!TryCurrentPointer(out _)) ResetInput();
            return;
        }
        if (game.State != RunState.Playing) return;
        if (TryCurrentPointer(out Vector2 position)) UpdateFromScreen(position);
        else ResetInput();
    }

    private void OnDisable() { ResetInput(); }
    private void OnApplicationFocus(bool focused) { if (!focused) ResetInput(); }
}

public sealed class MobileActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private GameBootstrap game;
    private bool pauseAction;
    private bool pressed;
    private Vector3 baseScale;

    public void Init(GameBootstrap owner, bool isPause)
    {
        game = owner; pauseAction = isPause; baseScale = transform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (game == null) return;
        pressed = true;
        if (pauseAction) game.RequestPause(); else game.RequestDash();
    }

    public void OnPointerUp(PointerEventData eventData) { pressed = false; }
    public void OnPointerExit(PointerEventData eventData) { pressed = false; }

    private void Update()
    {
        float target = pressed ? .94f : 1f;
        transform.localScale = Vector3.Lerp(transform.localScale, baseScale * target, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 20f));
    }
    private void OnDisable() { pressed = false; transform.localScale = baseScale; }
}

using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerActor : MonoBehaviour
{
    public int Health => Mathf.CeilToInt(health);
    public int MaxHealth => GameBootstrap.Game.Stats.MaxHealth;
    public const float DashCooldownMaxValue = 3.2f;
    public float DashCooldownMax => DashCooldownMaxValue * (GameBootstrap.Game != null ? GameBootstrap.Game.Stats.DashCooldownMultiplier : 1f);
    public float DashCooldown { get; private set; }
    public bool DashReady => DashCooldown <= 0;
    public bool Invincible => immunity > 0 || dashTime > 0;
    private float health = 100, immunity, dashTime, hitFlash, orbitAngle;
    private readonly float[] timers = new float[5];
    private Vector2 facing = Vector2.down;
    private Vector2 dashDirection;
    private bool dashRequested;
    private SpriteRenderer visual;
    private SpriteRenderer resonance;
    private Transform body;
    private Transform shadow;
    private readonly List<Transform> blades = new List<Transform>();
    private readonly Dictionary<int, float> bladeHits = new Dictionary<int, float>();
    private bool bladeEvolutionVisual;

    private void Awake()
    {
        shadow = Effects.Sprite("Hero shadow", transform, GameBootstrap.Game.Shape("circle"), new Color(0, 0, 0, .28f), new Vector3(0, -.22f, .1f), new Vector3(.95f, .4f, 1), -1).transform;
        body = new GameObject("Animated body").transform; body.SetParent(transform, false);
        Color accent = GameBootstrap.Game.CurrentCharacter.Accent;
        resonance = Effects.Sprite("Pilot resonance", transform, GameBootstrap.Game.Shape("circle"), new Color(accent.r, accent.g, accent.b, .18f), new Vector3(0, -.02f, .02f), new Vector3(1.35f, 1.35f, 1), 2);
        string modelKey = "pilot_sprite_" + GameBootstrap.Game.CurrentCharacter.Id.ToString().ToLowerInvariant();
        visual = Effects.Sprite("Pilot model", body, GameBootstrap.Game.GetSprite(modelKey), Color.white, Vector3.zero, Vector3.one, 10);
        health = MaxHealth;
    }

    private void Update()
    {
        var game = GameBootstrap.Game;
        if (game.State != RunState.Playing || health <= 0f) return;
        float delta = Time.deltaTime;
        Vector2 input = new Vector2(Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? -1 : 0,
            Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? -1 : 0).normalized;
        if (game.TouchControls && game.TouchMove.sqrMagnitude > .0025f) input = game.TouchMove;
        if (input.sqrMagnitude > .01f) facing = input.normalized;
        bool dashNow = Input.GetKeyDown(KeyCode.Space) || dashRequested;
        dashRequested = false;
        if (dashNow && DashReady) Dash(facing);
        DashCooldown = Mathf.Max(0, DashCooldown - delta);
        dashTime -= delta; immunity -= delta; hitFlash -= delta;
        Vector2 movement = dashTime > 0 ? dashDirection * game.Stats.Speed * 3.4f : input * game.Stats.Speed;
        transform.position = GameRules.Clamp((Vector2)transform.position + movement * delta);
        if (dashTime > 0 && Time.frameCount % 3 == 0) Effects.Afterimage(visual, transform.position);
        body.localPosition = new Vector3(0, input.sqrMagnitude > .1f ? Mathf.Abs(Mathf.Sin(Time.time * 13)) * .065f : Mathf.Sin(Time.time * 2) * .025f, 0);
        body.localRotation = Quaternion.Euler(0, 0, input.x * -4f);
        visual.flipX = facing.x > .1f;
        if (resonance != null) resonance.color = new Color(GameBootstrap.Game.CurrentCharacter.Accent.r, GameBootstrap.Game.CurrentCharacter.Accent.g, GameBootstrap.Game.CurrentCharacter.Accent.b, .13f + Mathf.PingPong(Time.time * .16f, .12f));
        visual.color = hitFlash > 0 ? new Color(1, .35f, .4f) : immunity > 0 && Time.frameCount % 10 < 4 ? new Color(.45f, 1, 1, .45f) : Color.white;
        health = Mathf.Min(MaxHealth, health + game.Stats.Regeneration * delta);
        for (int i = 0; i < timers.Length; i++) timers[i] -= delta;
        FireWeapons();
        UpdateBlades(delta);
    }

    public void Dash(Vector2 direction)
    {
        if (!DashReady) return;
        dashDirection = direction.normalized; if (dashDirection.sqrMagnitude < .1f) dashDirection = Vector2.down;
        dashTime = .22f; DashCooldown = DashCooldownMax; immunity = .27f;
        Effects.Burst(transform.position, GameRules.Cyan, 9, .09f); SoundBank.Play(SoundCue.Dash, .24f);
    }

    public void RequestDash() { dashRequested = true; }

    private void FireWeapons()
    {
        var game = GameBootstrap.Game; var stats = game.Stats;
        var targets = game.Nearby(transform.position, GameRules.AutoTargetRange);
        PrioritizeBossTarget(targets, game.Boss, transform.position, GameRules.AutoTargetRange);
        if (targets.Count == 0) return;
        Vector2 targetDirection = ((Vector2)targets[0].transform.position - (Vector2)transform.position).normalized;
        int pulse = stats.Weapons[0];
        if (pulse > 0 && timers[0] <= 0)
        {
            timers[0] = GameRules.PulseInterval(pulse, stats.Haste);
            if (stats.EvolvedWeapons[0])
            {
                float splitPower = GameRules.PulseDamage(pulse) * .75f;
                Shoot(Rotate(targetDirection, -16), splitPower, 15f, 1.3f, 1 + GameRules.PulsePierce + stats.Pierce, new Color(.42f, 1f, 1f), .22f, false, 0, 0, 2.2f, 0, .25f, "pulse_nova", 0);
                Shoot(targetDirection, splitPower, 15f, 1.3f, 1 + GameRules.PulsePierce + stats.Pierce, new Color(.42f, 1f, 1f), .22f, false, 0, 0, 2.2f, 0, .25f, "pulse_nova", 0);
                Shoot(Rotate(targetDirection, 16), splitPower, 15f, 1.3f, 1 + GameRules.PulsePierce + stats.Pierce, new Color(.42f, 1f, 1f), .22f, false, 0, 0, 2.2f, 0, .25f, "pulse_nova", 0);
                Effects.EvolvedPulseVolley(transform.position, targetDirection);
            }
            else
            {
                Shoot(targetDirection, GameRules.PulseDamage(pulse), 15f, 1.3f, 1 + GameRules.PulsePierce + stats.Pierce, GameRules.Cyan, .16f, weaponId: 0);
            }
            SoundBank.Play(SoundCue.Shot, .075f);
        }
        int scatter = stats.Weapons[1];
        if (scatter > 0 && timers[1] <= 0)
        {
            timers[1] = GameRules.ScatterInterval(scatter, stats.Haste);
            bool scatterEvolved = stats.EvolvedWeapons[1];
            int count = GameRules.ScatterPellets(scatter) + (scatterEvolved ? 2 : 0);
            float spread = GameRules.ScatterSpread;
            float power = scatterEvolved ? GameRules.ScatterDamage(scatter) * 1.3f : GameRules.ScatterDamage(scatter);
            int scatterPierce = 2 + stats.Pierce;
            // Longer reach so the wider evolved spread still connects at mid range.
            for (int i = 0; i < count; i++) Shoot(Rotate(targetDirection, (i - (count - 1) * .5f) * spread), power, 13.5f, .72f, scatterPierce, scatterEvolved ? new Color(1f, .9f, .38f) : GameRules.Gold, scatterEvolved ? .27f : .2f, false, 0, 0, 2.2f, 0, .25f, scatterEvolved ? "ember_bolt" : null, 1);
            if (stats.EvolvedWeapons[1]) Effects.EvolvedScatterVolley(transform.position + (Vector3)targetDirection * .15f, targetDirection);
            else Effects.Burst(transform.position + (Vector3)targetDirection * .4f, GameRules.Gold, 3, .08f);
            SoundBank.Play(SoundCue.Scatter, .16f);
        }
        int arc = stats.Weapons[3];
        if (arc > 0 && timers[3] <= 0)
        {
            timers[3] = GameRules.ArcInterval(arc, stats.Haste);
            Vector3 previous = transform.position;
            int baseCount = GameRules.ArcTargets(arc);
            int count = Mathf.Min(targets.Count, stats.EvolvedWeapons[3] ? baseCount + 2 : baseCount);
            float arcPower = stats.EvolvedWeapons[3] ? GameRules.ArcDamage(arc) * baseCount / Mathf.Max(1, count) * 1.5f : GameRules.ArcDamage(arc);
            for (int i = 0; i < count; i++)
            {
                var enemy = targets[i]; if (enemy == null || Vector3.Distance(previous, enemy.transform.position) > 8) continue;
                if (stats.EvolvedWeapons[3]) Effects.EvolvedArc(previous, enemy.transform.position, i);
                else Effects.Arc(previous, enemy.transform.position, new Color(.46f, .77f, 1f));
                bool critical;
                float power = i == 0 ? arcPower * GameRules.ArcFirstTargetBonus : arcPower;
                int arcDamage = stats.RollDamage(power * stats.LightningDamage, out critical);
                int arcDealt = Mathf.Min(arcDamage, Mathf.Max(0, enemy.Health));
                enemy.Hit(arcDamage, critical, Vector2.zero, stats.EvolvedWeapons[3]);
                game.TrackWeaponDamage(3, arcDealt);
                previous = enemy.transform.position;
            }
            SoundBank.Play(SoundCue.Arc, .16f);
        }
        int cryo = stats.Weapons[4];
        if (cryo > 0 && timers[4] <= 0)
        {
            timers[4] = GameRules.CryoInterval(cryo, stats.Haste);
            if (stats.EvolvedWeapons[4])
            {
                Shoot(targetDirection, GameRules.CryoDamage(cryo) * 1.4f, GameRules.CryoProjectileSpeed(cryo), 1.6f, GameRules.CryoPierce + stats.Pierce, new Color(.64f, 1f, 1f), .22f, false, .3f, GameRules.CryoSlowDuration(cryo, stats.ChillDuration), GameRules.CryoProjectileLength(cryo) * 1.08f, GameRules.CryoEvolutionTickDamage(cryo), GameRules.CryoEvolutionTickInterval, "cryo_lance_evolved", 4);
                Effects.EvolvedCryoLaunch(transform.position + (Vector3)targetDirection * .18f, targetDirection);
            }
            else
            {
                Shoot(targetDirection, GameRules.CryoDamage(cryo), GameRules.CryoProjectileSpeed(cryo), 1.6f, GameRules.CryoPierce + stats.Pierce, new Color(.47f, .82f, 1f), .16f, false, .35f, GameRules.CryoSlowDuration(cryo, stats.ChillDuration), GameRules.CryoProjectileLength(cryo), weaponId: 4);
            }
            SoundBank.Play(SoundCue.Scatter, .12f);
        }
    }

    internal static void PrioritizeBossTarget(List<EnemyActor> targets, EnemyActor boss, Vector2 playerPosition, float priorityRange)
    {
        if (boss == null || Vector2.Distance(playerPosition, boss.transform.position) > priorityRange) return;
        int currentIndex = targets.IndexOf(boss);
        if (currentIndex >= 0) targets.RemoveAt(currentIndex);
        targets.Insert(0, boss);
    }

    private static Vector2 Rotate(Vector2 d, float degrees) => Quaternion.Euler(0, 0, degrees) * d;
    private void Shoot(Vector2 direction, float power, float speed, float lifetime, int pierce, Color tint, float size, bool hostile = false, float slowMultiplier = 0, float slowDuration = 0, float lengthScale = 2.2f, int slowDamage = 0, float slowDamageInterval = .25f, string projectileShape = null, int weaponId = -1)
    {
        bool critical; int damage = GameBootstrap.Game.Stats.RollDamage(power, out critical);
        Projectile.Create((Vector2)transform.position + direction * .45f, direction, speed, damage, pierce, lifetime, tint, critical, hostile, size, slowMultiplier, slowDuration, lengthScale, slowDamage, slowDamageInterval, projectileShape, weaponId);
    }

    private void UpdateBlades(float delta)
    {
        var game = GameBootstrap.Game; int level = game.Stats.Weapons[2];
        if (bladeEvolutionVisual != game.Stats.EvolvedWeapons[2])
        {
            for (int i = 0; i < blades.Count; i++) if (blades[i] != null) Destroy(blades[i].gameObject);
            blades.Clear(); bladeEvolutionVisual = game.Stats.EvolvedWeapons[2];
        }
        int count = level == 0 ? 0 : GameRules.OrbitBlades(level) + (game.Stats.EvolvedWeapons[2] ? 2 : 0);
        while (blades.Count < count)
        {
            float bladeScale = game.Stats.EvolvedWeapons[2] ? .98f : 1f;
            Color bladeColor = game.Stats.EvolvedWeapons[2] ? new Color(.82f, .68f, 1f) : GameRules.Violet;
            if (game.Stats.EvolvedWeapons[2]) blades.Add(Effects.CreateEvolvedBlade(Vector3.zero, bladeScale).transform);
            else
            {
                var renderer = Effects.Sprite("Orbit blade", null, game.Shape("diamond"), bladeColor, Vector3.zero, new Vector3(.18f * bladeScale, .62f * bladeScale, 1), 13);
                blades.Add(renderer.transform);
            }
        }
        orbitAngle += delta * GameRules.OrbitSpeed(level) * game.Stats.Haste * game.Stats.BladeSpeedMultiplier;
        for (int i = 0; i < blades.Count; i++)
        {
            if (blades[i] == null) continue;
            float angle = (orbitAngle + i * 360f / count) * Mathf.Deg2Rad; float radius = 1.7f + level * .08f + game.Stats.BladeRadiusBonus;
            Vector2 pos = (Vector2)transform.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            blades[i].position = pos; blades[i].rotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
            if (Projectile.DispelHostileNear(pos, .28f) > 0)
            {
                if (game.Stats.EvolvedWeapons[2]) Effects.EvolvedBladeDispel(pos);
                else
                {
                    Effects.Burst(pos, new Color(.65f, .82f, 1f), 4, .09f);
                    Effects.Ring(pos, GameRules.Violet, .62f, .14f);
                }
            }
            var near = game.Nearby(pos, .68f);
            for (int j = 0; j < near.Count; j++)
            {
                var enemy = near[j]; int id = enemy.GetInstanceID();
                if (bladeHits.TryGetValue(id, out float until) && until > Time.time) continue;
                bladeHits[id] = Time.time + (game.Stats.EvolvedWeapons[2] ? GameRules.OrbitHitIntervalEvolved : GameRules.OrbitHitInterval);
                float baseDamage = GameRules.OrbitDamage(level);
                float bladePower = game.Stats.EvolvedWeapons[2] ? baseDamage * GameRules.OrbitBlades(level) / count * 1.5f : baseDamage;
                bool critical; int bladeDamage = game.Stats.RollDamage(bladePower, out critical);
                int bladeDealt = Mathf.Min(bladeDamage, Mathf.Max(0, enemy.Health));
                enemy.Hit(bladeDamage, critical, (Vector2)(enemy.transform.position - transform.position).normalized * .08f, game.Stats.EvolvedWeapons[2]);
                game.TrackWeaponDamage(2, bladeDealt);
                if (game.Stats.EvolvedWeapons[2]) Effects.EvolvedBladeHit(enemy.transform.position, ((Vector2)enemy.transform.position - pos).normalized);
            }
        }
    }

    public Sprite VisualSprite => visual != null ? visual.sprite : null;
    public Vector3 VisualScale => visual != null ? visual.transform.lossyScale : Vector3.one;
    public void HideForDeath()
    {
        if (visual != null) visual.enabled = false;
        if (resonance != null) resonance.enabled = false;
        if (shadow != null) shadow.gameObject.SetActive(false);
        if (body != null) body.gameObject.SetActive(false);
    }

    public void TakeDamage(int amount)
    {
        if (health <= 0f || Invincible || GameBootstrap.Game.State != RunState.Playing) return;
        var game = GameBootstrap.Game;
        if (game.Stats.DamageImmunityChance > 0 && GameRules.RollDamageImmunity(game.Stats.DamageImmunityChance))
        {
            Effects.FloatingNumber(transform.position + Vector3.up * .65f, "免疫", GameRules.Cyan, true);
            Effects.Ring(transform.position, GameRules.Cyan, .95f, .28f);
            SoundBank.Play(SoundCue.Upgrade, .18f);
            return;
        }
        int damage = Mathf.Max(1, Mathf.RoundToInt(amount * (1 - Mathf.Clamp(game.Stats.Armor, 0, .5f))));
        health = Mathf.Max(0, health - damage); immunity = .8f; hitFlash = .15f;
        Effects.FloatingNumber(transform.position + Vector3.up * .5f, "-" + damage, GameRules.Pink, true);
        Effects.Burst(transform.position, GameRules.Pink, 8, .1f); SoundBank.Play(SoundCue.Hurt, .25f);
        if (health <= 0) GameBootstrap.Game.BeginPlayerDeath();
    }
    public void Heal(int amount)
    {
        float previous = health; health = Mathf.Min(MaxHealth, health + amount);
        int recovered = Mathf.RoundToInt(health - previous);
        if (recovered > 0) Effects.FloatingNumber(transform.position + Vector3.up, "+" + recovered, GameRules.Cyan, false);
    }
    public void Pulse() { Effects.Ring(transform.position, GameRules.Cyan, 1.5f, .35f); SoundBank.Play(SoundCue.Upgrade, .22f); }
    private void OnDestroy() { foreach (var blade in blades) if (blade != null) Destroy(blade.gameObject); }
}

public sealed class EnemyActor : MonoBehaviour
{
    public EnemyKind Kind { get; private set; }
    public int BossStage { get; private set; }
    public int Health { get; private set; }
    public int MaxHealth { get; private set; }
    public bool IsAnchored => Kind == EnemyKind.Boss && BossStage == 3;
    public bool PhaseTwo => Kind == EnemyKind.Boss && Health <= MaxHealth * .5f;
    public bool IsChilled => slowUntil > Time.time;
    public float Radius { get; private set; }
    private float speed, spawnDelay = .6f, fireTimer = 1.5f, ringTimer = 3f, contactTimer, flashTimer, chargeTimer = 6f, chargeWarn, chargeTime, eliteAttackTimer = 3.3f, eliteWarn, eliteDash, echoTimer = 7.5f, echoWarn, slowUntil, slowMultiplier = 1f, slowDamageTimer, slowDamageInterval = .25f, difficultyFactor = 1f;
    private int slowDamage;
    private bool slowDamageUsesEvolutionVisual;
    private int xp, touchDamage;
    private int attackPattern;
    private bool dead, announced, echoRelocated;
    private Vector2 recoil, chargeDirection, eliteDirection, echoDestination;
    private SpriteRenderer visual, shadow;
    private Color tint;

    public void Setup(EnemyKind kind, Vector2 position, float difficulty, int bossStage = 0, float attackScale = 1f)
    {
        Kind = kind; transform.position = GameRules.Clamp(position);
        BossStage = kind == EnemyKind.Boss ? Mathf.Clamp(bossStage, 1, GameRules.BossCount) : 0;
        string asset = kind == EnemyKind.Boss ? GameRules.BossVisual(BossStage).Replace("starfall_", "") : GameRules.EnemyVisual(kind).Replace("starfall_", "");
        float scale = .61f; tint = Color.white;
        switch (kind)
        {
            case EnemyKind.Grunt: MaxHealth = Mathf.RoundToInt(22 * difficulty); speed = 1.45f; Radius = .42f; xp = 3; touchDamage = 12; break;
            case EnemyKind.Runner: MaxHealth = Mathf.RoundToInt(18 * difficulty); speed = 2.5f; Radius = .36f; xp = 3; touchDamage = 10; break;
            case EnemyKind.Brute: MaxHealth = Mathf.RoundToInt(105 * difficulty); speed = .91f; Radius = .72f; xp = 9; touchDamage = 20; scale = .83f; break;
            case EnemyKind.Spitter: MaxHealth = Mathf.RoundToInt(38 * difficulty); speed = 1.25f; Radius = .49f; xp = 5; touchDamage = 12; tint = new Color(.56f, 1, .82f); scale = .72f; break;
            case EnemyKind.PhaseLancer: MaxHealth = Mathf.RoundToInt(74 * difficulty); speed = 2.1f; Radius = .48f; xp = 7; touchDamage = 17; tint = new Color(.35f, .85f, 1f); scale = .78f; eliteAttackTimer = 2.8f; break;
            case EnemyKind.EchoWisp: MaxHealth = Mathf.RoundToInt(62 * difficulty); speed = 1.1f; Radius = .46f; xp = 7; touchDamage = 14; tint = new Color(.65f, .67f, 1f); scale = .82f; fireTimer = 1.8f; break;
            case EnemyKind.RiftBrute: MaxHealth = Mathf.RoundToInt(190 * difficulty); speed = .72f; Radius = .83f; xp = 13; touchDamage = 28; tint = new Color(.7f, .43f, .95f); scale = 1.04f; break;
            case EnemyKind.NovaReaver: MaxHealth = Mathf.RoundToInt(148 * difficulty); speed = 2.75f; Radius = .56f; xp = 11; touchDamage = 24; tint = new Color(1f, .35f, .64f); scale = .88f; eliteAttackTimer = 2.5f; break;
            case EnemyKind.SiegeOrb: MaxHealth = Mathf.RoundToInt(285 * difficulty); speed = .48f; Radius = .92f; xp = 17; touchDamage = 30; tint = new Color(1f, .65f, .28f); scale = 1.18f; fireTimer = 2.1f; break;
            case EnemyKind.NullMaw: MaxHealth = Mathf.RoundToInt(420 * difficulty); speed = .3f; Radius = 1.12f; xp = 22; touchDamage = 36; tint = new Color(.62f, .3f, .94f); scale = 1.35f; ringTimer = 3.2f; break;
            default:
                MaxHealth = BossStage == 2 ? 27000 : BossStage == 3 ? 120000 : 3450;
                speed = BossStage == 2 ? 1.2f : BossStage == 3 ? 0f : 1.2f;
                Radius = BossStage == 2 ? 2.08f : BossStage == 3 ? 2.75f : 1.50f; xp = 0;
                touchDamage = Mathf.RoundToInt((24 + (BossStage - 1) * 9) * (BossStage >= 3 ? .85f : 1f)); scale = BossStage == 2 ? 1.75f : BossStage == 3 ? 1.72f : 1.35f;
                spawnDelay = BossStage == 2 ? 1.6f : BossStage == 3 ? 2.1f : 1.1f;
                fireTimer = BossStage == 1 ? 1.5f : BossStage == 2 ? 1.55f : 1.35f;
                ringTimer = BossStage == 1 ? 4.2f : BossStage == 2 ? 3.3f : 5.4f;
                chargeTimer = BossStage == 1 ? 6f : BossStage == 2 ? 5.6f : float.MaxValue;
                tint = GameRules.BossColor(BossStage);
                break;
        }
        Health = MaxHealth;
        if (kind != EnemyKind.Boss)
        {
            difficultyFactor = difficulty * attackScale;
            speed *= Mathf.Lerp(1f, 1.32f, Mathf.Clamp01((difficulty - 1f) / 2.2f));
            touchDamage = Mathf.Max(1, Mathf.RoundToInt(touchDamage * difficulty * attackScale));
        }
        shadow = Effects.Sprite("Enemy shadow", transform, GameBootstrap.Game.Shape("circle"), new Color(0, 0, 0, .3f), new Vector3(0, -.25f, .1f), new Vector3(Radius * 2.1f, Radius * .75f, 1), 0);
        visual = Effects.Sprite("Enemy body", transform, GameBootstrap.Game.GetSprite("starfall_" + asset), tint, Vector3.zero, Vector3.one * scale, 8);
        Effects.Ring(transform.position, kind == EnemyKind.Boss ? tint : new Color(1, .45f, .3f), Radius * 1.5f, spawnDelay);
    }

    private void Update()
    {
        var game = GameBootstrap.Game;
        if (dead || game.State != RunState.Playing || game.Player == null) return;
        float dt = Time.deltaTime; spawnDelay -= dt;
        if (spawnDelay > 0) { visual.color = new Color(tint.r, tint.g, tint.b, .4f); return; }
        if (slowUntil > Time.time && slowDamage > 0)
        {
            slowDamageTimer -= dt;
            while (slowDamageTimer <= 0 && !dead)
            {
                int tick = Mathf.Max(1, Mathf.RoundToInt(slowDamage * game.Stats.Damage));
                int tickDealt = Mathf.Min(tick, Mathf.Max(0, Health));
                Hit(tick, false, Vector2.zero, slowDamageUsesEvolutionVisual);
                game.TrackWeaponDamage(4, tickDealt);
                if (slowDamageUsesEvolutionVisual) Effects.EvolvedCryoTick(transform.position);
                slowDamageTimer += slowDamageInterval;
            }
            if (dead) return;
        }
        Vector2 player = game.Player.transform.position, position = transform.position;
        Vector2 toward = (player - position).normalized; float distance = Vector2.Distance(player, position);
        float motion = speed;
        if (Kind == EnemyKind.Spitter || Kind == EnemyKind.EchoWisp || Kind == EnemyKind.SiegeOrb)
        {
            float ideal = Kind == EnemyKind.SiegeOrb ? 6.2f : 5.8f;
            motion *= distance > ideal + 1.2f ? 1 : distance < ideal - 1.4f ? -.72f : .12f;
        }
        UpdateEliteLunge(toward, distance, dt, ref motion, ref toward);
        if (Kind == EnemyKind.Boss)
        {
            if (BossStage == 2)
            {
                float orbitSign = Mathf.Sin(Time.time * .28f) < 0 ? -1f : 1f;
                Vector2 orbit = new Vector2(-toward.y, toward.x) * orbitSign;
                toward = (toward * .52f + orbit * .86f).normalized;
                motion *= distance > 6.4f ? 1.1f : distance < 3.4f ? -.48f : .38f;
            }
            else if (BossStage == 3) motion = 0;
            else motion *= distance > 4.2f ? 1 : .2f;
            BossAttacks(toward, dt);
            if (BossStage == 2 && echoRelocated) { position = transform.position; echoRelocated = false; }
            if (BossStage == 2 && echoWarn > 0) motion = 0;
            if (BossStage == 1 && chargeWarn > 0) motion = 0;
            if (BossStage == 1 && chargeTime > 0) { toward = chargeDirection; motion = 10.5f; chargeTime -= dt; }
        }
        else if (Kind == EnemyKind.Spitter || Kind == EnemyKind.EchoWisp || Kind == EnemyKind.SiegeOrb)
        {
            fireTimer -= dt;
            if (fireTimer <= 0 && distance < 13)
            {
                if (Kind == EnemyKind.Spitter) { fireTimer = 2.4f; Shoot(toward, 4.3f, Mathf.RoundToInt(10 * difficultyFactor), .23f); }
                else if (Kind == EnemyKind.EchoWisp)
                {
                    fireTimer = 2.2f; Shoot(Quaternion.Euler(0, 0, -12) * toward, 3.7f, Mathf.RoundToInt(12 * difficultyFactor), .2f); Shoot(Quaternion.Euler(0, 0, 12) * toward, 3.7f, Mathf.RoundToInt(12 * difficultyFactor), .2f);
                }
                else
                {
                    fireTimer = 3.2f; int count = 5;
                    for (int i = 0; i < count; i++) Shoot(Quaternion.Euler(0, 0, (i - 2) * 15f) * toward, 3.1f, Mathf.RoundToInt(17 * difficultyFactor), .27f);
                    Effects.Telegraph(transform.position, (Vector2)transform.position + toward * 8f, .45f);
                }
                Effects.Ring(transform.position, tint, .9f, .22f);
            }
        }
        else if (Kind == EnemyKind.NullMaw)
        {
            ringTimer -= dt;
            if (ringTimer <= 0)
            {
                ringTimer = 4.3f; int count = 10;
                for (int i = 0; i < count; i++) { float a = (i * 360f / count + Time.time * 27f) * Mathf.Deg2Rad; Shoot(new Vector2(Mathf.Cos(a), Mathf.Sin(a)), 2.7f, Mathf.RoundToInt(18 * difficultyFactor), .28f); }
                Effects.Ring(transform.position, tint, 2.2f, .48f);
            }
        }
        else if (Kind == EnemyKind.RiftBrute)
        {
            ringTimer -= dt;
            if (ringTimer <= 0 && distance < 4.6f)
            {
                ringTimer = 4.4f; int count = 9;
                Effects.Telegraph(transform.position, player, .52f);
                for (int i = 0; i < count; i++)
                {
                    float a = (i * 360f / count + Time.time * 11f) * Mathf.Deg2Rad;
                    Shoot(new Vector2(Mathf.Cos(a), Mathf.Sin(a)), 2.8f, Mathf.RoundToInt(15 * difficultyFactor), .22f);
                }
                Effects.Ring(transform.position, tint, 2.6f, .5f);
            }
        }
        if (slowUntil > Time.time) motion *= slowMultiplier;
        else { slowMultiplier = 1f; slowDamage = 0; slowDamageTimer = 0; }
        position += toward * motion * dt + recoil * dt + (Kind == EnemyKind.Boss ? Vector2.zero : game.Separation(this) * dt); recoil = Vector2.Lerp(recoil, Vector2.zero, dt * 12);
        transform.position = GameRules.Clamp(position);
        visual.transform.localPosition = IsAnchored ? Vector3.zero : new Vector3(0, Mathf.Sin(Time.time * (Kind == EnemyKind.Runner || Kind == EnemyKind.NovaReaver ? 15 : 5) + GetInstanceID()) * .04f, 0);
        visual.flipX = toward.x < 0; flashTimer -= dt;
        visual.color = flashTimer > 0 ? new Color(1, .65f, .65f) : chargeWarn > 0 || eliteWarn > 0 ? Color.Lerp(tint, GameRules.Pink, Mathf.PingPong(Time.time * 8, 1)) : IsChilled ? Color.Lerp(tint, new Color(.52f, .84f, 1f), .72f) : tint;
        contactTimer -= dt;
        if (distance < Radius + .36f && contactTimer <= 0) { contactTimer = .7f; game.Player.TakeDamage(touchDamage); }
    }

    private void BossAttacks(Vector2 toward, float dt)
    {
        var game = GameBootstrap.Game;
        float intensity = GameRules.BossAttackMultiplier(BossStage);
        if (PhaseTwo && !announced) { announced = true; Effects.Ring(transform.position, tint, 4 + BossStage * .35f, .7f); game.ShakeCamera(.2f, .24f); }
        fireTimer -= dt; ringTimer -= dt; chargeTimer -= dt;
        if (BossStage == 2)
        {
            echoTimer -= dt;
            if (echoWarn > 0)
            {
                echoWarn -= dt;
                if (echoWarn <= 0)
                {
                    Vector3 origin = transform.position;
                    Effects.Ring(origin, GameRules.Gold, 2.5f, .32f);
                    transform.position = echoDestination;
                    echoRelocated = true;
                    Effects.Ring(transform.position, tint, 3.4f, .55f);
                    Effects.Burst(transform.position, tint, 16, .11f);
                    game.ShakeCamera(.12f, .2f);
                }
            }
            else if (echoTimer <= 0)
            {
                echoTimer = PhaseTwo ? 5.5f : 8.0f;
                Vector2 away = ((Vector2)transform.position - (Vector2)game.Player.transform.position).normalized;
                if (away.sqrMagnitude < .1f) away = Vector2.up;
                Vector2 side = new Vector2(-away.y, away.x) * (Mathf.Sin(Time.time) < 0 ? -1f : 1f);
                echoDestination = GameRules.Clamp((Vector2)game.Player.transform.position + (away * .82f + side * .58f).normalized * 7.2f);
                echoWarn = .72f;
                Effects.Telegraph(transform.position, echoDestination, echoWarn);
                Effects.Ring(transform.position, GameRules.Gold, 3.7f, echoWarn);
            }
        }
        if (fireTimer <= 0)
        {
            if (BossStage == 1)
            {
                fireTimer = (PhaseTwo ? .86f : 1.55f) / intensity;
                int count = PhaseTwo ? 7 : 5;
                for (int i = 0; i < count; i++)
                {
                    float spread = (i - (count - 1) * .5f) * 15f;
                    Shoot(Quaternion.Euler(0, 0, spread) * toward, PhaseTwo ? 5.5f : 4.4f, Mathf.RoundToInt(13 * intensity), .24f);
                }
            }
            else if (BossStage == 2)
            {
                fireTimer = (PhaseTwo ? .82f : 1.35f) / intensity;
                if ((attackPattern++ % 2) == 0)
                {
                    int count = PhaseTwo ? 7 : 5;
                    for (int i = 0; i < count; i++)
                    {
                        float spread = (i - (count - 1) * .5f) * 16f;
                        Shoot(Quaternion.Euler(0, 0, spread) * toward, 4.2f, Mathf.RoundToInt(14 * intensity), .24f);
                    }
                }
                else
                {
                    Vector2 perpendicular = new Vector2(-toward.y, toward.x);
                    Shoot(toward, 5.2f, Mathf.RoundToInt(16 * intensity), .27f);
                    Shoot(Quaternion.Euler(0, 0, 10) * toward, 4.4f, Mathf.RoundToInt(13 * intensity), .22f);
                    Shoot(Quaternion.Euler(0, 0, -10) * toward, 4.4f, Mathf.RoundToInt(13 * intensity), .22f);
                    if (PhaseTwo) { Shoot(perpendicular, 3.8f, Mathf.RoundToInt(12 * intensity), .2f); Shoot(-perpendicular, 3.8f, Mathf.RoundToInt(12 * intensity), .2f); }
                }
            }
            else
            {
                fireTimer = (PhaseTwo ? .92f : 1.55f) / intensity;
                int count = PhaseTwo ? 9 : 5;
                float trackingOffset = Mathf.Sin(Time.time * 1.6f) * 12f;
                for (int i = 0; i < count; i++)
                {
                    float spread = trackingOffset + (i - (count - 1) * .5f) * (PhaseTwo ? 10f : 13f);
                    Shoot(Quaternion.Euler(0, 0, spread) * toward, PhaseTwo ? 6.2f : 5.4f, Mathf.RoundToInt(17 * intensity), .28f);
                }
                if (PhaseTwo)
                {
                    Vector2 perpendicular = new Vector2(-toward.y, toward.x);
                    Shoot(perpendicular, 4.2f, Mathf.RoundToInt(12 * intensity), .22f);
                    Shoot(-perpendicular, 4.2f, Mathf.RoundToInt(12 * intensity), .22f);
                }
            }
        }
        if (ringTimer <= 0)
        {
            if (BossStage == 1)
            {
                ringTimer = PhaseTwo ? 3.1f : 4.8f; int count = PhaseTwo ? 16 : 11;
                for (int i = 0; i < count; i++) { float a = i * 360f / count * Mathf.Deg2Rad; Shoot(new Vector2(Mathf.Cos(a), Mathf.Sin(a)), 3.1f, Mathf.RoundToInt(12 * intensity), .22f); }
            }
            else if (BossStage == 2)
            {
                ringTimer = PhaseTwo ? 3.2f : 4.8f; int count = PhaseTwo ? 22 : 16;
                float spin = PhaseTwo ? -Time.time * 38f : Time.time * 22f;
                for (int i = 0; i < count; i++) { float a = (i * 360f / count + spin) * Mathf.Deg2Rad; Shoot(new Vector2(Mathf.Cos(a), Mathf.Sin(a)), 3.35f, Mathf.RoundToInt(13 * intensity), .25f); }
                game.SpawnBossAdds(transform.position, PhaseTwo ? 3 : 2);
            }
            else
            {
                ringTimer = PhaseTwo ? 2.65f : 4.5f; int count = PhaseTwo ? 34 : 26;
                for (int i = 0; i < count; i++) { float a = (i * 360f / count + Time.time * (PhaseTwo ? 16f : -9f)) * Mathf.Deg2Rad; Shoot(new Vector2(Mathf.Cos(a), Mathf.Sin(a)), 3.8f, Mathf.RoundToInt(17 * intensity), .29f); }
                if (PhaseTwo)
                {
                    int innerCount = 14;
                    for (int i = 0; i < innerCount; i++)
                    {
                        float a = (i * 360f / innerCount + 180f / innerCount - Time.time * 24f) * Mathf.Deg2Rad;
                        Shoot(new Vector2(Mathf.Cos(a), Mathf.Sin(a)), 2.9f, Mathf.RoundToInt(9 * intensity), .2f);
                    }
                }
                Effects.Ring(transform.position, new Color(.8f, .65f, 1f), Radius * 2.7f, .7f);
            }
            if (BossStage != 3) Effects.Ring(transform.position, tint, 2.8f + BossStage * .35f, .42f);
        }
        if (BossStage == 3 && PhaseTwo)
        {
            // Spiral stream adds constant pressure during the final phase.
            if (chargeTimer > 1000f) chargeTimer = .7f;
            if (chargeTimer <= 0f)
            {
                chargeTimer = .42f;
                float spin = Time.time * 240f;
                for (int k = 0; k < 3; k++)
                {
                    float a = (spin + k * 120f) * Mathf.Deg2Rad;
                    Shoot(new Vector2(Mathf.Cos(a), Mathf.Sin(a)), 4.8f, Mathf.RoundToInt(10 * intensity), .22f);
                }
            }
        }
        if (BossStage == 1 && PhaseTwo && chargeTimer <= 0 && chargeTime <= 0 && chargeWarn <= 0)
        {
            chargeTimer = Mathf.Max(4.2f, 7.4f / intensity); chargeWarn = .98f; chargeDirection = toward;
            Effects.Telegraph(transform.position, (Vector2)transform.position + toward * 12, chargeWarn);
        }
        if (chargeWarn > 0)
        {
            chargeWarn -= dt;
            if (chargeWarn <= 0) { chargeTime = .85f; SoundBank.Play(SoundCue.Dash, .25f); }
        }
    }

    private bool UpdateEliteLunge(Vector2 toward, float distance, float dt, ref float motion, ref Vector2 direction)
    {
        if (Kind != EnemyKind.PhaseLancer && Kind != EnemyKind.NovaReaver) return false;
        eliteAttackTimer -= dt;
        if (eliteWarn > 0)
        {
            eliteWarn -= dt; motion = 0; direction = eliteDirection;
            if (eliteWarn <= 0) eliteDash = Kind == EnemyKind.NovaReaver ? .48f : .4f;
            return true;
        }
        if (eliteDash > 0)
        {
            eliteDash -= dt; direction = eliteDirection; motion = Kind == EnemyKind.NovaReaver ? 8.2f : 7.1f;
            return true;
        }
        if (eliteAttackTimer <= 0 && distance < 10f)
        {
            eliteAttackTimer = Kind == EnemyKind.NovaReaver ? 3.4f : 4.1f;
            eliteWarn = Kind == EnemyKind.NovaReaver ? .42f : .58f;
            eliteDirection = toward; motion = 0;
            Effects.Telegraph(transform.position, (Vector2)transform.position + toward * 7.8f, eliteWarn);
            Effects.Ring(transform.position, tint, Radius * 2.4f, eliteWarn);
            return true;
        }
        return false;
    }

    private void Shoot(Vector2 direction, float velocity, int damage, float size)
    { Projectile.Create((Vector2)transform.position + direction * (Radius + .12f), direction, velocity, damage, 1, 7, tint, false, true, size); }

    public void ApplySlow(float multiplier, float duration, int damagePerTick = 0, float damageTickInterval = .25f, bool evolvedVisual = false)
    {
        if (dead || duration <= 0) return;
        slowMultiplier = Mathf.Min(slowMultiplier, Mathf.Clamp(multiplier, .15f, 1f));
        slowUntil = Mathf.Max(slowUntil, Time.time + duration);
        if (damagePerTick > 0)
        {
            if (slowDamage <= 0) slowDamageTimer = Mathf.Max(.05f, damageTickInterval);
            slowDamage = Mathf.Max(slowDamage, damagePerTick);
            slowDamageInterval = Mathf.Max(.05f, damageTickInterval);
            slowDamageUsesEvolutionVisual |= evolvedVisual;
        }
        if (evolvedVisual) Effects.EvolvedCryoSlow(transform.position);
        else Effects.Ring(transform.position, new Color(.47f, .82f, 1f), Radius * 1.35f, .24f);
    }

    public void Hit(int amount, bool critical, Vector2 knockback, bool evolvedImpact = false)
    {
        if (dead || spawnDelay > 0) return;
        Health -= amount; flashTimer = .09f; recoil += knockback * (Kind == EnemyKind.Boss ? 0 : 12);
        Effects.FloatingNumber(transform.position + Vector3.up * .65f, amount.ToString(), critical ? GameRules.Gold : Color.white, critical);
        if (!evolvedImpact) Effects.Burst(transform.position, critical ? GameRules.Gold : GameRules.Cyan, 3, .05f);
        if (Health > 0) return;
        dead = true;
        if (Kind == EnemyKind.Boss) Effects.BossDefeat(transform.position, BossStage);
        else Effects.Burst(transform.position, GameRules.Gold, 8, .12f);
        if (xp > 0) XpPickup.Create(transform.position, xp, false);
        if (Kind != EnemyKind.Boss && Random.value < .055f) XpPickup.Create(transform.position + Vector3.right * .4f, 10, true);
        GameBootstrap.Game.RegisterKill(this);
        Destroy(gameObject);
    }
}

public sealed class Projectile : MonoBehaviour
{
    private static readonly HashSet<Projectile> active = new HashSet<Projectile>();
    private static readonly Color HostileShotColor = new Color(1f, .18f, .2f, 1f);
    private Vector2 velocity;
    private int damage, remaining;
    private float lifetime, radius;
    private bool hostile, critical;
    private string projectileShape;
    private int weaponId = -1;
    private float slowMultiplier, slowDuration, slowDamageInterval;
    private int slowDamage;
    private readonly HashSet<int> hit = new HashSet<int>();

    public static void Create(Vector2 position, Vector2 direction, float speed, int damage, int pierce, float life, Color tint, bool critical, bool hostile, float size, float slowMultiplier = 0, float slowDuration = 0, float lengthScale = 2.2f, int slowDamage = 0, float slowDamageInterval = .25f, string projectileShape = null, int weaponId = -1)
    {
        var obj = new GameObject(hostile ? "Hostile bolt" : "Energy bolt"); obj.transform.position = position;
        obj.transform.localScale = new Vector3(size, hostile ? size : size * lengthScale, 1); obj.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90);
        var renderer = obj.AddComponent<SpriteRenderer>(); renderer.sprite = GameBootstrap.Game.Shape(hostile ? "circle" : (projectileShape ?? "diamond")); renderer.color = hostile ? HostileShotColor : tint; renderer.sortingOrder = 20;
        if (!hostile && (projectileShape == "cryo_lance" || projectileShape == "cryo_lance_evolved"))
        {
            var glow = new GameObject(projectileShape == "cryo_lance_evolved" ? "Evolved glacial lance halo" : "Prismatic lance glow"); glow.transform.SetParent(obj.transform, false); glow.transform.localScale = projectileShape == "cryo_lance_evolved" ? new Vector3(2.25f, 1.22f, 1) : new Vector3(1.8f, 1.02f, 1); var glowRenderer = glow.AddComponent<SpriteRenderer>(); glowRenderer.sprite = renderer.sprite; glowRenderer.color = projectileShape == "cryo_lance_evolved" ? new Color(.64f, .98f, 1f, .3f) : new Color(.45f, .95f, 1f, .24f); glowRenderer.sortingOrder = 19;
        }
        var bullet = obj.AddComponent<Projectile>(); bullet.velocity = direction.normalized * speed; bullet.damage = damage; bullet.remaining = pierce; bullet.lifetime = life; bullet.hostile = hostile; bullet.critical = critical; bullet.radius = size * .55f; bullet.slowMultiplier = slowMultiplier; bullet.slowDuration = slowDuration; bullet.slowDamage = slowDamage; bullet.slowDamageInterval = Mathf.Max(.05f, slowDamageInterval); bullet.projectileShape = projectileShape; bullet.weaponId = weaponId; active.Add(bullet);
    }

    public static int DispelHostileNear(Vector2 center, float bladeRadius)
    {
        List<Projectile> dispelled = null;
        foreach (var projectile in active)
        {
            if (projectile == null || !projectile.hostile || Vector2.Distance(center, projectile.transform.position) > bladeRadius + projectile.radius) continue;
            if (dispelled == null) dispelled = new List<Projectile>();
            dispelled.Add(projectile);
        }
        if (dispelled == null) return 0;
        for (int i = 0; i < dispelled.Count; i++)
        {
            active.Remove(dispelled[i]);
            if (dispelled[i] != null) Destroy(dispelled[i].gameObject);
        }
        return dispelled.Count;
    }

    private void Update()
    {
        var game = GameBootstrap.Game; if (game.State != RunState.Playing) return;
        Vector2 previous = transform.position; Vector2 next = previous + velocity * Time.deltaTime;
        transform.position = next; lifetime -= Time.deltaTime;
        if (lifetime <= 0 || Mathf.Abs(next.x) > GameRules.HalfWidth + 3 || Mathf.Abs(next.y) > GameRules.HalfHeight + 3) { Destroy(gameObject); return; }
        if (hostile)
        {
            if (game.Player != null && SegmentDistance(game.Player.transform.position, previous, next) < radius + .31f)
            { game.Player.TakeDamage(damage); Destroy(gameObject); }
            return;
        }
        float swept = (next - previous).magnitude;
        var nearby = game.Nearby((previous + next) * .5f, swept * .5f + 1.9f);
        foreach (var enemy in nearby)
        {
            if (enemy == null || hit.Contains(enemy.GetInstanceID()) || SegmentDistance(enemy.transform.position, previous, next) > enemy.Radius + radius) continue;
            bool evolvedImpact = !hostile && !string.IsNullOrEmpty(projectileShape);
            hit.Add(enemy.GetInstanceID());
            // Apply the slow before the hit so a lethal cryo shot still leaves the status applied (and reads correctly in tests).
            if (slowMultiplier > 0) enemy.ApplySlow(slowMultiplier, slowDuration, slowDamage, slowDamageInterval, projectileShape == "cryo_lance_evolved");
            int dealt = Mathf.Min(damage, Mathf.Max(0, enemy.Health));
            enemy.Hit(damage, critical, velocity.normalized * .2f, evolvedImpact);
            game.TrackWeaponDamage(weaponId, dealt);
            if (evolvedImpact) Effects.EvolvedProjectileImpact(transform.position, projectileShape);
            if (--remaining <= 0) { Destroy(gameObject); break; }
        }
    }
    private static float SegmentDistance(Vector2 point, Vector2 a, Vector2 b)
    { Vector2 ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / Mathf.Max(.00001f, ab.sqrMagnitude)); return Vector2.Distance(point, a + ab * t); }
    private void OnDestroy() { active.Remove(this); }
}

public sealed class XpPickup : MonoBehaviour
{
    private int value;
    private bool healing, attracted;
    private Vector3 original;
    private float phase;
    public static void Create(Vector3 position, int value, bool healing)
    {
        var obj = new GameObject(healing ? "Medkit" : "Stardust"); obj.transform.position = position; obj.transform.localScale = Vector3.one * (healing ? .34f : .18f + Mathf.Min(5, value) * .02f);
        var renderer = obj.AddComponent<SpriteRenderer>(); renderer.sprite = GameBootstrap.Game.Shape(healing ? "square" : "diamond"); renderer.color = healing ? GameRules.Pink : GameRules.Cyan; renderer.sortingOrder = 2;
        var pickup = obj.AddComponent<XpPickup>(); pickup.value = value; pickup.healing = healing; pickup.original = position; pickup.phase = Random.value * 6;
    }
    private void Update()
    {
        var game = GameBootstrap.Game;
        if (game.State != RunState.Playing || game.Player == null) return;
        Vector2 player = game.Player.transform.position; float distance = Vector2.Distance(player, transform.position);
        if (distance < (healing ? 1.6f : game.Stats.Magnet)) attracted = true;
        if (attracted) transform.position = Vector2.MoveTowards(transform.position, player, (10f + distance * 2) * Time.deltaTime);
        else transform.position = original + Vector3.up * Mathf.Sin(Time.time * 3 + phase) * .06f;
        if (distance < .48f)
        {
            if (healing) game.Player.Heal(value); else game.AddXp(value);
            SoundBank.Play(SoundCue.Pickup, .05f); Destroy(gameObject);
        }
    }
}

public sealed class SupplyChest : MonoBehaviour
{
    private void Awake()
    {
        Effects.Sprite("Cache shadow", transform, GameBootstrap.Game.Shape("circle"), new Color(0, 0, 0, .4f), new Vector3(0, -.18f, .1f), new Vector3(1, .4f, 1), 0);
        Effects.Sprite("Cache", transform, GameBootstrap.Game.Shape("square"), GameRules.Gold, Vector3.zero, new Vector3(.6f, .46f, 1), 4);
        Effects.Sprite("Cache band", transform, GameBootstrap.Game.Shape("square"), new Color(.16f, .2f, .24f), Vector3.zero, new Vector3(.13f, .49f, 1), 5);
        Effects.Ring(transform.position, GameRules.Gold, 1.4f, .7f);
    }
    private void Update()
    {
        var game = GameBootstrap.Game;
        if (game.State != RunState.Playing || game.Player == null) return;
        transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 2) * 4);
        if (Vector2.Distance(transform.position, game.Player.transform.position) < .85f) { game.OpenChest(); Effects.Burst(transform.position, GameRules.Gold, 16, .12f); Destroy(gameObject); }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

public enum RunState { Menu, CharacterSelect, Playing, Upgrade, Paused, Victory, Defeat, CharacterSheet }
public enum EnemyKind { Grunt, Runner, Brute, Spitter, PhaseLancer, EchoWisp, RiftBrute, NovaReaver, SiegeOrb, NullMaw, Boss }
public enum UpgradeFlow { None, Normal, Weapon, Boss }
public enum WeaponKind { Pulse, Scatter, Blades, Lightning, Cryo }
public enum CharacterKind { Rei, Rikka, Rin, Mei, Yuna }

public sealed class CharacterProfile
{
    public readonly CharacterKind Id;
    public readonly string Name, CallSign, Gender, Weapon, Passive, Story, Portrait;
    public readonly int WeaponId;
    public readonly Color Accent;
    public CharacterProfile(CharacterKind id, string name, string callSign, string gender, string weapon, string passive, string story, string portrait, int weaponId, Color accent)
    { Id = id; Name = name; CallSign = callSign; Gender = gender; Weapon = weapon; Passive = passive; Story = story; Portrait = portrait; WeaponId = weaponId; Accent = accent; }
}

public static class CharacterRoster
{
    public static readonly CharacterProfile[] All = {
        new CharacterProfile(CharacterKind.Rei, "苍真 零", "零号观测员 / REI", "男 · 24岁", "星芒脉冲", "临界瞄准：暴击率 +12%，拾取范围 +0.8 米。", "曾是方舟舰队的领航员。他在坠毁的遗迹中听见了星核的心跳，决定为幸存者打开最后一条航路。", "portrait_rei", 0, new Color(.31f, .93f, .94f)),
        new CharacterProfile(CharacterKind.Rikka, "星野 莉花", "赤焰枪姬 / RIKKA", "女 · 19岁", "余烬散射", "热装填：全武器攻击速度 +16%。", "出身于轨道炮兵团的王牌。莉花把每一次危险都当成烟火表演，嘴上说着不在乎，撤离名单却总被她偷偷加长。", "portrait_rikka", 1, new Color(1f, .48f, .31f)),
        new CharacterProfile(CharacterKind.Rin, "白濑 凛", "绝对零度 / RIN", "女 · 20岁", "冰晶长枪", "零域共鸣：霜缓持续时间 +1 秒，子弹穿透 +1。", "前寒区研究所的理论物理学家。她将失控的冷凝核心改造成武器，相信所有失序的事物都能被重新计算。", "portrait_rin", 4, new Color(.47f, .82f, 1f)),
        new CharacterProfile(CharacterKind.Mei, "月城 芽衣", "月环舞者 / MEI", "女 · 18岁", "环轨光刃", "轨道校准：光刃环绕半径 +0.5 米，转速 +20%。", "机械神社最后的守卫，和她的四枚微型刃无人机一起长大。她记得每一台机器的名字，也从不让同伴独自返航。", "portrait_mei", 2, new Color(.74f, .55f, 1f)),
        new CharacterProfile(CharacterKind.Yuna, "雨宫 由奈", "雷光信使 / YUNA", "女 · 22岁", "连锁电弧", "过载回路：电弧伤害 +25%。", "在城市断电的那一夜，她用自制线圈给整座避难站供能。如今她带着相同的线圈穿越荒原，追逐一则来自星核的求救信号。", "portrait_yuna", 3, new Color(1f, .48f, .72f))
    };
    public static CharacterProfile Get(CharacterKind id) => All[Mathf.Clamp((int)id, 0, All.Length - 1)];
    public static void ApplyLoadout(CharacterKind id, RunStats stats)
    {
        stats.Weapons[Get(id).WeaponId] = 1;
        switch (id)
        {
            case CharacterKind.Rei: stats.Crit += .12f; stats.Magnet += .8f; break;
            case CharacterKind.Rikka: stats.TalentHasteMultiplier *= 1.16f; break;
            case CharacterKind.Rin: stats.ChillDuration += 1f; stats.Pierce += 1; break;
            case CharacterKind.Mei: stats.BladeRadiusBonus += .5f; stats.BladeSpeedMultiplier *= 1.2f; break;
            case CharacterKind.Yuna: stats.LightningDamage *= 1.25f; break;
        }
        stats.DashCooldownMultiplier = DashCooldownMultiplier(Stat(id, 1));
        int survival = Stat(id, 0);
        stats.MaxHealth = survival >= 4 ? 120 : survival <= 2 ? 90 : 100;
        stats.SignatureWeapon = Get(id).WeaponId;
    }

    public static readonly int[,] StatTable = { { 3, 4, 3 }, { 3, 3, 4 }, { 3, 2, 4 }, { 4, 3, 2 }, { 2, 3, 4 } };
    public static int Stat(CharacterKind id, int category) => StatTable[Mathf.Clamp((int)id, 0, 4), Mathf.Clamp(category, 0, 2)];
    public static float DashCooldownMultiplier(int mobility) => mobility <= 2 ? 1.2f : mobility >= 4 ? .85f : 1f;
}

[Serializable]
public sealed class RunStats
{
    public const float BaseSpeed = 5.2f;
    public int MaxHealth = 100;
    public float DamageBonus;
    public float HasteBonus;
    public float SpeedBonus;
    public float TalentDamageMultiplier = 1f;
    public float TalentHasteMultiplier = 1f;
    public float TalentSpeedMultiplier = 1f;
    public float CoreDamageMultiplier = 1f;
    public float CoreHasteMultiplier = 1f;
    public float CoreSpeedMultiplier = 1f;
    public float BoostDamageMultiplier = 1f;
    public float BoostHasteMultiplier = 1f;
    public float Magnet = 2.8f;
    public readonly float[] WeaponDamage = new float[5];
    public float Crit = .08f;
    public float CritDamage = 2f;
    public float Armor;
    public float DamageImmunityChance;
    public float Regeneration;
    public float ChillDuration = 1.1f;
    public float BladeRadiusBonus;
    public float BladeSpeedMultiplier = 1f;
    public float DashCooldownMultiplier = 1f;
    public int SignatureWeapon = -1;
    public float LightningDamage = 1f;
    public int Pierce;
    public bool ResonanceOverflow;
    public readonly int[] Weapons = new int[5];
    public readonly bool[] EvolvedWeapons = new bool[5];
    public readonly int[] Ranks = new int[15];

    // 属性强化是加算区：先累加，再乘天赋、Boss 核心、武装补强三个独立乘区。
    public float Damage => (1f + DamageBonus) * TalentDamageMultiplier * CoreDamageMultiplier * BoostDamageMultiplier;
    public float Haste => (1f + HasteBonus) * TalentHasteMultiplier * CoreHasteMultiplier * BoostHasteMultiplier;
    public float Speed => BaseSpeed * (1f + SpeedBonus) * TalentSpeedMultiplier * CoreSpeedMultiplier;

    public int RollDamage(float baseDamage, out bool critical)
    {
        critical = Crit >= 1f || UnityEngine.Random.value < Mathf.Clamp01(Crit);
        float overCritDamage = ResonanceOverflow ? Mathf.Max(0, Crit - 1f) * 2f : 0f;
        return Mathf.RoundToInt(baseDamage * Damage * (critical ? CritDamage + overCritDamage : 1f));
    }
}

public sealed class UpgradeOption
{
    public readonly int Id;
    public readonly string Title;
    public readonly string Description;
    public readonly string Icon;
    public readonly Color Color;
    public readonly bool IsWeapon;
    public readonly bool IsEvolution;
    public readonly int RewardId;
    public readonly UpgradeFlow Flow;
    public UpgradeOption(int id, string title, string description, string icon, Color color, bool weapon = false, int rewardId = -1, UpgradeFlow flow = UpgradeFlow.Normal, bool evolution = false)
    { Id = id; Title = title; Description = description; Icon = icon; Color = color; IsWeapon = weapon; RewardId = rewardId; Flow = flow; IsEvolution = evolution; }
}

public static class GameRules
{
    public const float BossTime = 180f;
    public const int BossCount = 3;
    public const float StageTwoIntermission = 120f;
    public const float StageThreeIntermission = 150f;
    public const int WeaponChoiceInterval = 4;
    public const int MaxWeaponRank = 5;
    public const int MaxUpgradeRank = 7;
    public const int MaxPierceRank = 4;
    public const float OrbitHitInterval = .38f;
    public const float ArcFirstTargetBonus = 1.3f;
    public const float OrbitHitIntervalEvolved = .32f;
    public const float AutoTargetRange = 12f;

    // 十类普通强化的逐级数值。数据与《数值调整方案.xlsm》Sheet3 一致。
    private static readonly float[] DamageUpgradeValues = { .25f, .30f, .35f, .40f, .45f, .50f, .60f };
    private static readonly float[] HasteUpgradeValues = { .15f, .18f, .21f, .24f, .28f, .32f, .36f };
    private static readonly float[] SpeedUpgradeValues = { .10f, .10f, .10f, .10f, .10f, .10f, .10f };
    private static readonly float[] CritDamageUpgradeValues = { .20f, .20f, .25f, .25f, .30f, .30f, .40f };
    private static readonly float[] MagnetUpgradeValues = { 1.5f, 2f, 2.5f };
    private static readonly float[] CritUpgradeValues = { .10f, .12f, .14f, .16f, .18f, .21f, .25f };
    private static readonly int[] PierceUpgradeValues = { 1, 1, 1, 2 };
    private static readonly float[] RegenerationUpgradeValues = { 1f, 1.2f, 1.4f, 1.6f, 1.8f, 2f, 2.2f };
    private static readonly float[] ArmorUpgradeValues = { .08f, .08f, .08f, .08f, .08f, .08f, .08f };
    private static readonly int[] MaxHealthUpgradeValues = { 20, 24, 28, 32, 36, 40, 44 };
    private static readonly int[] MaxHealthHealValues = { 40, 48, 56, 64, 72, 80, 88 };
    public const float PhoenixDamageImmunity = .30f;
    public const int EmergencyHeal = 30;
    public const float HalfWidth = 17f;
    public const float HalfHeight = 8.2f;
    public static readonly Color Cyan = new Color(.28f, .94f, .86f);
    public static readonly Color Gold = new Color(1f, .76f, .32f);
    public static readonly Color Pink = new Color(1f, .3f, .51f);
    public static readonly Color Violet = new Color(.69f, .5f, 1f);
    public static readonly Color ArcColor = new Color(.38f, 1f, .45f);

    public static Color WeaponColor(int id)
    {
        switch (id)
        {
            case 0: return Cyan;
            case 1: return Gold;
            case 2: return Violet;
            case 3: return ArcColor;
            case 4: return new Color(.47f, .82f, 1f);
            default: return Color.white;
        }
    }
    public static int XpNeeded(int level) { return 36 + level * 14 + Mathf.Max(0, level - 10) * 5; }
    public static string BossName(int stage)
    {
        switch (stage) { case 1: return "THE DREAD WARDEN  /  01  ·  噬影守望者"; case 2: return "THE ECHO HERALD  /  02  ·  回响先驱"; default: return "THE STAR EATER  /  03  ·  星蚀巨兽"; }
    }
    public static string StageName(int stage)
    {
        switch (stage) { case 1: return "第一阶段  /  灰烬之门"; case 2: return "第二阶段  /  回响裂隙"; default: return "第三阶段  /  星蚀核心"; }
    }
    public static string BossVisual(int stage) => stage == 1 ? "starfall_boss_star_eater" : stage == 2 ? "starfall_boss_echo_herald" : "starfall_boss_warden";
    public static Color BossColor(int stage) => stage == 1 ? Pink : stage == 2 ? Gold : Violet;
    public static int EnemyTierForBossesDefeated(int defeatedBosses) => Mathf.Clamp(defeatedBosses + 1, 1, BossCount);
    public static float BossIntermissionSeconds(int defeatedBosses)
    { return defeatedBosses == 1 ? StageTwoIntermission : defeatedBosses == 2 ? StageThreeIntermission : 0f; }
    public static EnemyKind SelectEnemyKind(int defeatedBosses, float elapsed, float roll)
    {
        int tier = EnemyTierForBossesDefeated(defeatedBosses);
        if (tier == 2)
        {
            if (roll < .15f) return EnemyKind.PhaseLancer;
            if (roll < .28f) return EnemyKind.EchoWisp;
            if (roll < .40f) return EnemyKind.RiftBrute;
            roll = (roll - .40f) / .60f;
        }
        else if (tier == 3)
        {
            if (roll < .12f) return EnemyKind.NovaReaver;
            if (roll < .24f) return EnemyKind.SiegeOrb;
            if (roll < .36f) return EnemyKind.NullMaw;
            if (roll < .56f) return EnemyKind.PhaseLancer;
            if (roll < .68f) return EnemyKind.EchoWisp;
            if (roll < .80f) return EnemyKind.RiftBrute;
            roll = (roll - .80f) / .20f;
        }
        if (elapsed > 25 && roll < .25f) return EnemyKind.Runner;
        if (elapsed > 48 && roll > .76f) return EnemyKind.Brute;
        if (elapsed > 76 && roll > .57f && roll <= .76f) return EnemyKind.Spitter;
        return EnemyKind.Grunt;
    }
    public static string EnemyVisual(EnemyKind kind)
    {
        switch (kind)
        {
            case EnemyKind.Runner: return "starfall_runner";
            case EnemyKind.Brute: return "starfall_brute";
            case EnemyKind.Spitter: return "starfall_grunt";
            case EnemyKind.PhaseLancer: return "starfall_phase_lancer";
            case EnemyKind.EchoWisp: return "starfall_echo_wisp";
            case EnemyKind.RiftBrute: return "starfall_rift_brute";
            case EnemyKind.NovaReaver: return "starfall_nova_reaver";
            case EnemyKind.SiegeOrb: return "starfall_siege_orb";
            case EnemyKind.NullMaw: return "starfall_null_maw";
            default: return "starfall_grunt";
        }
    }
    public static float EnemyStageAttackMultiplier(int defeatedBosses) => defeatedBosses >= 2 ? .8f : 1f;
    public static float EnemyStageHealthMultiplier(int defeatedBosses) => defeatedBosses >= 2 ? 1.95f : 1f + Mathf.Clamp(defeatedBosses, 0, BossCount - 1) * .2f;
    public static float BossEnemyMultiplier(int defeatedBosses)
    { return 1f + Mathf.Clamp(defeatedBosses, 0, BossCount - 1) * .12f; }
    public static float EnemySpawnInterval(float elapsed, int defeatedBosses)
    {
        float timeFactor = Mathf.Lerp(1f, .6f, Mathf.Clamp01(elapsed / (BossTime * 1.5f)));
        float stageFactor = 1f + Mathf.Clamp(defeatedBosses, 0, BossCount - 1) * .18f;
        return Mathf.Max(.4f, .92f * timeFactor / stageFactor);
    }
    public static float EnemyTimeMultiplier(float elapsed)
    { return 1f + Mathf.Clamp01(elapsed / (BossTime * 1.5f)) * .35f; }
    public static bool RollDamageImmunity(float chance) => chance >= 1f || UnityEngine.Random.value < Mathf.Clamp01(chance);
    public static float BossHealthMultiplier(int stage)
    { return 1f + Mathf.Clamp(stage - 1, 0, BossCount - 1) * .85f; }
    public static float BossAttackMultiplier(int stage)
    { return stage >= 3 ? 1.4f : 1f + Mathf.Clamp(stage - 1, 0, BossCount - 1) * .32f; }
    public static int MaxRank(int id) => id == 11 ? MaxPierceRank : id == 9 ? 3 : id == 13 ? 5 : MaxUpgradeRank;
    private static float ValueAt(float[] values, int rank) => values[Mathf.Clamp(rank, 0, values.Length - 1)];
    private static int ValueAt(int[] values, int rank) => values[Mathf.Clamp(rank, 0, values.Length - 1)];
    public static float DamageUpgradeAtRank(int rank) => ValueAt(DamageUpgradeValues, rank);
    public static float HasteUpgradeAtRank(int rank) => ValueAt(HasteUpgradeValues, rank);
    public static float SpeedUpgradeAtRank(int rank) => ValueAt(SpeedUpgradeValues, rank);
    public static float CritDamageUpgradeAtRank(int rank) => ValueAt(CritDamageUpgradeValues, rank);
    public static float MagnetUpgradeAtRank(int rank) => ValueAt(MagnetUpgradeValues, rank);
    public static float CritUpgradeAtRank(int rank) => ValueAt(CritUpgradeValues, rank);
    public static int PierceUpgradeAtRank(int rank) => ValueAt(PierceUpgradeValues, rank);
    public static float RegenerationUpgradeAtRank(int rank) => ValueAt(RegenerationUpgradeValues, rank);
    public static float ArmorUpgradeAtRank(int rank) => ValueAt(ArmorUpgradeValues, rank);
    public static int MaxHealthUpgradeAtRank(int rank) => ValueAt(MaxHealthUpgradeValues, rank);
    public static int MaxHealthHealAtRank(int rank) => ValueAt(MaxHealthHealValues, rank);
    public static float EvolutionDamageMultiplier(int weaponId, RunStats stats)
    { return stats != null && weaponId >= 0 && weaponId < stats.EvolvedWeapons.Length && stats.EvolvedWeapons[weaponId] ? 1.5f : 1f; }
    public static float PulseInterval(int level, float haste) => Mathf.Max(.22f, (.55f - (level - 1) * .02f) / Mathf.Max(.1f, haste));
    private static int WeaponRankDamage(int baseDamage, int rankGain, int level)
    {
        int rank = Mathf.Clamp(level, 1, MaxWeaponRank);
        float damage = baseDamage;
        for (int previousRank = 1; previousRank < rank; previousRank++)
            damage += rankGain * (1f + (previousRank - 1) * .03f);
        return Mathf.RoundToInt(damage);
    }
    private static readonly int[] PulseDamageValues = { 18, 23, 29, 36, 45 };
    public static int PulseDamage(int level) => ValueAt(PulseDamageValues, level - 1);
    public const int PulsePierce = 1;
    public static int PulseEchoDamage(int level) => WeaponRankDamage(8, 3, level);
    public static float ScatterInterval(int level, float haste) => Mathf.Max(.55f, (1.55f - level * .10f - level * Mathf.Max(0, level - 1) * .010f) / haste);
    public const float ScatterSpread = 9f;
    private static readonly int[] ScatterPelletValues = { 3, 3, 4, 4, 5 };
    public static int ScatterPellets(int level) => ValueAt(ScatterPelletValues, level - 1);
    private static readonly int[] ScatterDamageValues = { 16, 18, 20, 22, 24 };
    public static int ScatterDamage(int level) => ValueAt(ScatterDamageValues, level - 1);
    public static int OrbitBlades(int level) => 3 + Mathf.Max(0, (level - 1) / 2);
    public static int OrbitDamage(int level) => WeaponRankDamage(20, 5, level);
    public static float OrbitSpeed(int level) => 140 + level * 3 + level * Mathf.Max(0, level - 1) * .5f;
    public static float OrbitDpsBudget(int level) => OrbitDamage(level) * Mathf.Min(1f / OrbitHitInterval, OrbitBlades(level) * OrbitSpeed(level) / 360f);
    public static float ArcInterval(int level, float haste) => Mathf.Max(.62f, (2.15f - level * .15f - level * Mathf.Max(0, level - 1) * .012f) / haste);
    public static int ArcTargets(int level) => 2 + level;
    private static readonly int[] ArcDamageValues = { 20, 25, 31, 38, 46 };
    public static int ArcDamage(int level) => ValueAt(ArcDamageValues, level - 1);
    private static readonly float[] CryoIntervalValues = { 1.4f, 1.3f, 1.19f, 1.02f, .9f };
    public static float CryoInterval(int level, float haste) => ValueAt(CryoIntervalValues, level - 1) / Mathf.Max(.1f, haste);
    public static float CryoProjectileSpeed(int level) => 18f + Mathf.Clamp(level - 1, 0, MaxWeaponRank - 1) * 1.1f;
    public const int CryoPierce = 4;
    public static float CryoProjectileLength(int level) => 4.6f + Mathf.Clamp(level - 1, 0, MaxWeaponRank - 1) * .12f;
    private static readonly int[] CryoDamageValues = { 35, 45, 57, 65, 80 };
    public static int CryoDamage(int level) => ValueAt(CryoDamageValues, level - 1);
    public static int CryoEchoDamage(int level) => WeaponRankDamage(15, 4, level);
    public static float CryoSlowDuration(int level, float baseDuration) => baseDuration + Mathf.Clamp(level - 1, 0, MaxWeaponRank - 1) * .22f;
    public static int CryoEvolutionTickDamage(int level) => ValueAt(new[] { 3, 3, 4, 4, 5 }, level - 1);
    public const float CryoEvolutionTickInterval = .25f;
    public static Vector2 Clamp(Vector2 position)
    { return new Vector2(Mathf.Clamp(position.x, -HalfWidth + .5f, HalfWidth - .5f), Mathf.Clamp(position.y, -HalfHeight + .5f, HalfHeight - .5f)); }
    public static string TimeLabel(float seconds)
    { return string.Format("{0:00}:{1:00}", (int)Mathf.Max(0, seconds) / 60, (int)Mathf.Max(0, seconds) % 60); }

    public static string IconAsset(int id)
    {
        switch (id)
        {
            case 0: return "weapon_pulse";
            case 1: return "weapon_scatter";
            case 2: return "weapon_orbit";
            case 3: return "weapon_arc";
            case 4: return "weapon_cryo";
            case 5: return "stat_damage";
            case 6: return "stat_haste";
            case 7: return "stat_speed";
            case 8: return "stat_crit_damage";
            case 9: return "stat_magnet";
            case 10: return "stat_crit";
            case 11: return "stat_pierce";
            case 12: return "stat_regen";
            case 13: return "stat_armor";
            case 14: return "stat_health";
            case -4: return "boss_overload";
            case -5: return "boss_phoenix";
            case -6: return "boss_resonance";
            case -1: return "stat_damage";
            case -2: return "stat_haste";
            case -3: return "stat_crit";
            default: return "emergency_repair";
        }
    }

    public static List<UpgradeOption> Choices(RunStats stats, ICollection<int> avoid = null, int playerLevel = 99)
    {
        var result = new List<UpgradeOption>();
        var pool = new List<int>();
        for (int i = 0; i < 5; i++) if (stats.Weapons[i] > 0 && stats.Weapons[i] < MaxWeaponRank) pool.Add(i);
        for (int i = 5; i < 15; i++) if (stats.Ranks[i] < MaxRank(i)) pool.Add(i);
        if (avoid != null && avoid.Count > 0)
        {
            var filtered = new List<int>();
            foreach (int id in pool) if (!avoid.Contains(id)) filtered.Add(id);
            if (filtered.Count > 0) pool = filtered;
        }
        int categoryOf(int id)
        {
            if (id == 7 || id == 9) return 2; // mobility and pickup utility
            if (id == 12 || id == 13 || id == 14) return 1; // sustain and defense
            return 0; // damage, criticals, pierce and owned weapon skills
        }
        var usedCategories = new List<int>();
        for (int slot = 0; slot < 3; slot++)
        {
            var available = new List<int>();
            foreach (int id in pool) if (!usedCategories.Contains(categoryOf(id))) available.Add(id);
            if (available.Count == 0) available.AddRange(pool);
            if (available.Count == 0)
            {
                string[] recoveryNames = { "纳米修复", "现场医疗", "急救协议" };
                int recovery = slot % recoveryNames.Length;
                result.Add(new UpgradeOption(15 + recovery, recoveryNames[recovery], "恢复 " + EmergencyHeal + " 点生命。", "+", Cyan));
                continue;
            }
            var weighted = new List<int>(available);
            if (playerLevel < 10)
            {
                // Slightly favour offensive modules early; no UI text changes.
                foreach (int id in available)
                    if ((id >= 0 && id < 5) || id == 5 || id == 6 || id == 10) weighted.Add(id);
            }
            if (stats.SignatureWeapon >= 0 && available.Contains(stats.SignatureWeapon)) weighted.Add(stats.SignatureWeapon);
            int chosen = weighted[UnityEngine.Random.Range(0, weighted.Count)];
            result.Add(Describe(chosen, stats, UpgradeFlow.Normal)); pool.Remove(chosen);
            usedCategories.Add(categoryOf(chosen));
        }
        return result;
    }

    public static List<UpgradeOption> WeaponChoices(RunStats stats, ICollection<int> avoid = null)
    {
        var result = new List<UpgradeOption>();
        var newWeapons = new List<UpgradeOption>();
        for (int i = 0; i < 5; i++)
        {
            if (stats.Weapons[i] == 0) newWeapons.Add(Describe(i, stats, UpgradeFlow.Weapon));
            else if (!stats.EvolvedWeapons[i]) newWeapons.Add(DescribeEvolution(i));
        }
        var allCandidates = new List<UpgradeOption>(newWeapons);
        if (avoid != null && avoid.Count > 0) newWeapons.RemoveAll(option => avoid.Contains(option.Id));
        if (newWeapons.Count == 0 && allCandidates.Count > 0)
        {
            // When every remaining weapon/evolution was already shown, a reroll
            // can only reshuffle them. Rotate the previous order so the cards move.
            newWeapons.AddRange(allCandidates);
            if (allCandidates.Count > 1 && avoid != null && avoid.Count == allCandidates.Count)
            {
                var ordered = new List<UpgradeOption>();
                foreach (int id in avoid)
                {
                    for (int i = 0; i < allCandidates.Count; i++)
                    {
                        if (allCandidates[i].Id != id) continue;
                        ordered.Add(allCandidates[i]);
                        break;
                    }
                }
                if (ordered.Count == allCandidates.Count)
                {
                    UpgradeOption first = ordered[0];
                    ordered.RemoveAt(0);
                    ordered.Add(first);
                    newWeapons.Clear();
                    newWeapons.AddRange(ordered);
                }
            }
            result.AddRange(newWeapons);
            return result;
        }
        while (newWeapons.Count > 0 && result.Count < 3)
        {
            int index = UnityEngine.Random.Range(0, newWeapons.Count);
            result.Add(newWeapons[index]); newWeapons.RemoveAt(index);
        }
        if (result.Count == 0)
            for (int i = 0; i < 3; i++)
                result.Add(new UpgradeOption(-1 - i, new[] { "武装超载", "急速装填", "超频瞄准" }[i], new[] { "全部武器伤害 +10%。", "攻击速度 +8%。", "暴击率 +6%。" }[i], "✦", Gold, false, -1, UpgradeFlow.Weapon));
        return result;
    }

    public static UpgradeOption DescribeEvolution(int id)
    {
        string[] names = { "星芒脉冲 · 星链协议", "余烬散射 · 双生齐射", "环轨光刃 · 六曜环阵", "连锁电弧 · 超导网络", "冰晶长枪 · 霜蚀棱镜" };
        string[] effects = {
            "三向齐射：每发 75% 伤害\n独立加成",
            "每发伤害 ×1.3，弹体 +2\n散布 8° → 11° · 独立加成",
            "光刃 +2，命中间隔 0.38 → 0.32 秒\n单次总伤害 ×1.5 · 独立加成",
            "连锁目标 +2，单次总伤害 ×1.5\n电弧可继续跳跃 · 独立加成",
            "伤害 ×1.4，弹体加长 8%\n霜蚀：每 0.25 秒 3–5 点（随等级）· 独立加成"
        };
        Color color = WeaponColor(id);
        return new UpgradeOption(id, names[Mathf.Clamp(id, 0, names.Length - 1)], effects[Mathf.Clamp(id, 0, effects.Length - 1)], "✦", color, true, -1, UpgradeFlow.Weapon, true);
    }

    public static List<UpgradeOption> BossChoices(int stage, bool[] claimedRewards = null)
    {
        var all = new[] {
            new UpgradeOption(-4, "超载核心", "最终伤害 +20%，最终攻速 +15%，暴击率 +20%。", "✦", Gold, false, 0, UpgradeFlow.Boss),
            new UpgradeOption(-5, "凤凰协议", "生命上限 +60，护甲 +15%，移速 +18%，每秒回复 +1；受击时 30% 概率免疫。", "✧", Pink, false, 1, UpgradeFlow.Boss),
            new UpgradeOption(-6, "星核共鸣", "暴击率 +30%，暴击伤害 +50%；暴击率超过 100% 时，溢出部分每 1% 额外增加 2% 暴击伤害。", "◈", Violet, false, 2, UpgradeFlow.Boss)
        };
        var result = new List<UpgradeOption>();
        for (int i = 0; i < all.Length; i++) if (claimedRewards == null || i >= claimedRewards.Length || !claimedRewards[i]) result.Add(all[i]);
        return result;
    }

    private static string WeaponIntro(int id)
    {
        switch (id)
        {
            case 0: return "自动锁定最近目标，发射星弧脉冲。\n伤害 " + PulseDamage(1) + " · 间隔 " + PulseInterval(1, 1f).ToString("0.00") + " 秒 · 穿透 " + PulsePierce;
            case 1: return "扇形投射 " + ScatterPellets(1) + " 枚弹体，适合清理近身敌潮。\n每发伤害 " + ScatterDamage(1) + " · 间隔 " + ScatterInterval(1, 1f).ToString("0.00") + " 秒";
            case 2: return OrbitBlades(1) + "枚光刃环绕身体旋转，命中造成伤害并消除弹幕。\n单次命中 " + OrbitDamage(1) + " · 命中间隔 " + OrbitHitInterval.ToString("0.00") + " 秒";
            case 3: return "电弧在敌群间跳跃，首个目标额外承受 30% 伤害。\n伤害 " + ArcDamage(1) + " · 连锁 " + ArcTargets(1) + " 个 · 间隔 " + ArcInterval(1, 1f).ToString("0.00") + " 秒";
            default: return "狙击式冰晶长枪，贯穿并施加霜缓。\n伤害 " + CryoDamage(1) + " · 穿透 " + CryoPierce + " · 霜缓 " + CryoSlowDuration(1, 1.1f).ToString("0.0") + " 秒";
        }
    }

    private static string WeaponUpgradeText(int id, RunStats stats)
    {
        int level = Mathf.Clamp(stats.Weapons[id], 1, MaxWeaponRank - 1);
        int next = level + 1;
        float haste = Mathf.Max(.1f, stats.Haste);
        switch (id)
        {
            case 0:
            {
                // Pulse levels raise damage and slightly shorten the interval; no extra bolts.
                string text = "伤害 " + PulseDamage(level) + " → " + PulseDamage(next);
                text += " · 间隔 " + PulseInterval(level, haste).ToString("0.00") + " → " + PulseInterval(next, haste).ToString("0.00") + " 秒";
                return text;
            }
            case 1:
            {
                string text = "每发伤害 " + ScatterDamage(level) + " → " + ScatterDamage(next);
                if (ScatterPellets(next) != ScatterPellets(level)) text += " · 弹体 " + ScatterPellets(level) + " → " + ScatterPellets(next);
                text += "\n间隔 " + ScatterInterval(level, haste).ToString("0.00") + " → " + ScatterInterval(next, haste).ToString("0.00") + " 秒";
                return text;
            }
            case 2:
            {
                string text = "单次命中 " + OrbitDamage(level) + " → " + OrbitDamage(next);
                if (OrbitBlades(next) != OrbitBlades(level)) text += " · 光刃 " + OrbitBlades(level) + " → " + OrbitBlades(next);
                text += "\n转速 " + OrbitSpeed(level).ToString("0") + " → " + OrbitSpeed(next).ToString("0") + " °/秒";
                return text;
            }
            case 3:
            {
                string text = "伤害 " + ArcDamage(level) + " → " + ArcDamage(next);
                if (ArcTargets(next) != ArcTargets(level)) text += " · 目标 " + ArcTargets(level) + " → " + ArcTargets(next);
                text += "\n间隔 " + ArcInterval(level, haste).ToString("0.00") + " → " + ArcInterval(next, haste).ToString("0.00") + " 秒";
                return text;
            }
            default:
            {
                string text = "伤害 " + CryoDamage(level) + " → " + CryoDamage(next) + " · 霜缓 +0.14 秒";
                text += "\n弹速 " + CryoProjectileSpeed(level).ToString("0.0") + " → " + CryoProjectileSpeed(next).ToString("0.0") + " · 间隔 " + CryoInterval(level, haste).ToString("0.00") + " → " + CryoInterval(next, haste).ToString("0.00");
                return text;
            }
        }
    }

    public static UpgradeOption Describe(int id, RunStats stats, UpgradeFlow flow = UpgradeFlow.Normal)
    {
        string next = id < 5 ? (stats.Weapons[id] == 0 ? "新武器解锁" : "武器升至 " + (stats.Weapons[id] + 1) + " 级") : "强化 · " + (stats.Ranks[id] + 1) + "/" + MaxRank(id);
        switch (id)
        {
            case 0: return new UpgradeOption(id, "星芒脉冲", next + "\n" + (stats.Weapons[0] == 0 ? WeaponIntro(0) : WeaponUpgradeText(0, stats)), "P", Cyan, true, -1, flow);
            case 1: return new UpgradeOption(id, "余烬散射", next + "\n" + (stats.Weapons[1] == 0 ? WeaponIntro(1) : WeaponUpgradeText(1, stats)), "S", Gold, true, -1, flow);
            case 2: return new UpgradeOption(id, "环轨光刃", next + "\n" + (stats.Weapons[2] == 0 ? WeaponIntro(2) : WeaponUpgradeText(2, stats)), "O", Violet, true, -1, flow);
            case 3: return new UpgradeOption(id, "连锁电弧", next + "\n" + (stats.Weapons[3] == 0 ? WeaponIntro(3) : WeaponUpgradeText(3, stats)), "Z", ArcColor, true, -1, flow);
            case 4: return new UpgradeOption(id, "冰晶长枪", next + "\n" + (stats.Weapons[4] == 0 ? WeaponIntro(4) : WeaponUpgradeText(4, stats)), "F", WeaponColor(4), true, -1, flow);
            case 5: return new UpgradeOption(id, "反应堆增幅", next + "\n全部武器伤害 +" + Mathf.RoundToInt(DamageUpgradeAtRank(stats.Ranks[id]) * 100) + "%。", ">", Gold);
            case 6: return new UpgradeOption(id, "超频回路", next + "\n全部武器攻击速度 +" + Mathf.RoundToInt(HasteUpgradeAtRank(stats.Ranks[id]) * 100) + "%。", ">>", Cyan);
            case 7: return new UpgradeOption(id, "轻量推进", next + "\n移动速度 +" + Mathf.RoundToInt(SpeedUpgradeAtRank(stats.Ranks[id]) * 100) + "%。", "~", new Color(.42f, .78f, 1f));
            case 8: return new UpgradeOption(id, "暴击增幅", next + "\n暴击伤害 +" + Mathf.RoundToInt(CritDamageUpgradeAtRank(stats.Ranks[id]) * 100) + "%。", "✦", Pink);
            case 9: return new UpgradeOption(id, "星尘引力", next + "\n经验拾取范围 +" + MagnetUpgradeAtRank(stats.Ranks[id]).ToString("0.0") + " 米。", "*", new Color(.72f, .92f, .35f));
            case 10: return new UpgradeOption(id, "精准校准", next + "\n暴击率 +" + Mathf.RoundToInt(CritUpgradeAtRank(stats.Ranks[id]) * 100) + "%。", "!", new Color(1f, .62f, .28f));
            case 11: return new UpgradeOption(id, "穿透晶核", next + "\n子弹穿透数 +" + PierceUpgradeAtRank(stats.Ranks[id]) + " 个目标。", "//", Violet);
            case 12: return new UpgradeOption(id, "自愈纳米机", next + "\n每秒恢复 " + RegenerationUpgradeAtRank(stats.Ranks[id]).ToString("0.00") + " 点生命。", "+", new Color(.35f, .88f, .66f));
            case 13: return new UpgradeOption(id, "偏转护甲", next + "\n受到的伤害降低 " + Mathf.RoundToInt(ArmorUpgradeAtRank(stats.Ranks[id]) * 100) + "%（上限 50%）。", "[]", new Color(.62f, .62f, .82f));
            case 14: return new UpgradeOption(id, "生命扩容", next + "\n生命上限 +" + MaxHealthUpgradeAtRank(stats.Ranks[id]) + "，并恢复 " + MaxHealthHealAtRank(stats.Ranks[id]) + " 点生命。", "+", new Color(1f, .42f, .42f));
            default: return new UpgradeOption(id, "应急修复", $"恢复 {EmergencyHeal} 点生命。", "+", Cyan);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public static class Localization
{
    public static int Language { get; private set; }
    public static event Action Changed;

    private static readonly Dictionary<string, string> Exact = new Dictionary<string, string>
    {
        // Menu / shell
        { "收集星尘，组合武器，穿过不断增强的敌潮，击败苏醒的遗迹守望者。", "Collect stardust, combine weapons, survive the growing swarm, and defeat the awakened ruins wardens." },
        { "选择驾驶员  /  SELECT PILOT", "SELECT PILOT" },
        { "选择驾驶员", "SELECT PILOT" },
        { "左侧拖动  移动     右下按钮  冲刺     武器自动攻击", "Drag left to move     Dash button right     Auto attack" },
        { "WASD / 方向键  移动     空格  冲刺     武器自动攻击", "WASD / Arrows  Move     Space  Dash     Auto attack" },
        { "WASD / 方向键  移动     空格  冲刺     自动攻击     C  角色     ESC  暂停", "WASD / Arrows  Move     Space  Dash     Auto attack     C  Pilot     ESC  Pause" },
        { "设置 / SETTINGS", "SETTINGS" },
        { "选择你的共鸣者", "CHOOSE YOUR RESONATOR" },
        { "五名驾驶员 · 五种本命武器 · 一套专属被动", "Five pilots · Five signature weapons · One unique passive each" },
        { "以当前角色出击", "DEPLOY PILOT" },
        { "返回标题", "BACK TO TITLE" },
        { "生存", "Survival" }, { "机动", "Mobility" }, { "输出", "Output" },
        { "本命武器", "SIGNATURE WEAPON" }, { "专属被动", "SIGNATURE TRAIT" },
        { "SIGNATURE WEAPON / 本命武器", "SIGNATURE WEAPON" }, { "SIGNATURE TRAIT / 专属被动", "SIGNATURE TRAIT" },
        { "WEAPON DAMAGE / 本局武器伤害", "WEAPON DAMAGE" }, { "RUN STATUS / 作战状态", "RUN STATUS" },
        { "WEAPONS / 武器", "WEAPONS" }, { "BONUSES / 当前加成", "BONUSES" },
        { "本局武器伤害", "WEAPON DAMAGE" }, { "星链协议", "Star Chain Protocol" }, { "双生齐射", "Twin Volley" }, { "六曜环阵", "Six-Star Array" }, { "超导网络", "Superconducting Network" }, { "霜蚀棱镜", "Frost Prism" },
        { "角色", "PILOT" }, { "设置", "SETTINGS" },

        // Characters
        { "苍真 零", "Rei Soma" }, { "星野 莉花", "Rikka Hoshino" }, { "白濑 凛", "Rin Shirae" }, { "月城 芽衣", "Mei Tsukishiro" }, { "雨宫 由奈", "Yuna Amamiya" },
        { "零号观测员 / REI", "Zero Observer / REI" }, { "赤焰枪姬 / RIKKA", "Crimson Gunner / RIKKA" }, { "绝对零度 / RIN", "Absolute Zero / RIN" }, { "月环舞者 / MEI", "Lunar Dancer / MEI" }, { "雷光信使 / YUNA", "Lightning Courier / YUNA" },
        { "零号观测员", "Zero Observer" }, { "赤焰枪姬", "Crimson Gunner" }, { "绝对零度", "Absolute Zero" }, { "月环舞者", "Lunar Dancer" }, { "雷光信使", "Lightning Courier" },
        { "男 · 24岁", "Male · 24" }, { "女 · 19岁", "Female · 19" }, { "女 · 20岁", "Female · 20" }, { "女 · 18岁", "Female · 18" }, { "女 · 22岁", "Female · 22" },
        { "星芒脉冲", "Pulse" }, { "余烬散射", "Scatter" }, { "环轨光刃", "Orbit Blade" }, { "连锁电弧", "Chain Arc" }, { "冰晶长枪", "Cryo Lance" },
        { "临界瞄准：暴击率 +12%，拾取范围 +0.8 米。", "Critical Aim: +12% crit chance, +0.8m pickup range." },
        { "热装填：全武器攻击速度 +16%。", "Hot Reload: +16% attack speed for all weapons." },
        { "零域共鸣：霜缓持续时间 +1 秒，子弹穿透 +1。", "Zero Field Resonance: +1s chill duration, +1 bullet pierce." },
        { "轨道校准：光刃环绕半径 +0.5 米，转速 +20%。", "Orbit Calibration: +0.5m blade radius, +20% spin speed." },
        { "过载回路：电弧伤害 +25%。", "Overload Circuit: +25% arc damage." },
        { "曾是方舟舰队的领航员。他在坠毁的遗迹中听见了星核的心跳，决定为幸存者打开最后一条航路。", "Once a navigator of the Ark Fleet. In the wreckage he heard the starcore's heartbeat, and chose to open one last route for the survivors." },
        { "出身于轨道炮兵团的王牌。莉花把每一次危险都当成烟火表演，嘴上说着不在乎，撤离名单却总被她偷偷加长。", "An ace from the orbital artillery corps. Rikka treats every danger like a fireworks show, claiming not to care while quietly adding names to the evacuation list." },
        { "前寒区研究所的理论物理学家。她将失控的冷凝核心改造成武器，相信所有失序的事物都能被重新计算。", "A theoretical physicist from the former Cryo Institute. She turned a runaway condenser core into a weapon, believing every disorder can be recalculated." },
        { "机械神社最后的守卫，和她的四枚微型刃无人机一起长大。她记得每一台机器的名字，也从不让同伴独自返航。", "The last guardian of the Machine Shrine, raised with four micro-blade drones. She remembers every machine's name and never lets a companion return alone." },
        { "在城市断电的那一夜，她用自制线圈给整座避难站供能。如今她带着相同的线圈穿越荒原，追逐一则来自星核的求救信号。", "On the night the city lost power, she kept an entire shelter alive with a homemade coil. Now she crosses the wasteland with that same coil, chasing a distress signal from the starcore." },
        { "临界瞄准", "Critical Aim" }, { "热装填", "Hot Reload" }, { "零域共鸣", "Zero Field Resonance" }, { "轨道校准", "Orbit Calibration" }, { "过载回路", "Overload Circuit" },
        { "\"我会找到下一段航线。\"", "\"I will find the next route.\"" },
        { "\"让每一发炮火，都成为回家的信号。\"", "\"Let every shot be a signal home.\"" },
        { "\"失序只是尚未被计算的寒流。\"", "\"Disorder is only a cold current not yet calculated.\"" },
        { "\"轨道锁定，刃阵开始同步。\"", "\"Orbit locked. Blade array synchronizing.\"" },
        { "\"听见了吗？星核还在呼吸。\"", "\"Do you hear it? The starcore is still breathing.\"" },

        // Pause / results
        { "三位守望者尽数陨落 · 航道已经打开", "All three wardens have fallen · The route is open" },
        { "灰烬之门吞没了又一名跑者", "The Ashen Gate has swallowed another runner" },
        { "深呼吸，跑者", "Breathe, runner" },
        { "用时", "TIME" }, { "击破", "KILLS" }, { "等级", "LEVEL" }, { "首领", "BOSSES" },
        { "伤害", "DMG" }, { "攻速", "ATK SPD" }, { "暴击", "CRIT" }, { "暴伤", "CRIT DMG" }, { "护甲", "ARMOR" }, { "生命", "HP" },
        { "武器", "WEAPONS" }, { "进化", "Evolved" }, { "下次会走得更远", "You will go farther next time" },
        { "继续探索", "RESUME" }, { "再次探索", "RUN AGAIN" }, { "返回主菜单", "BACK TO TITLE" },
        { "完成", "CLEARED" },

        // Pilot sheet
        { "作战状态", "RUN STATUS" }, { "C / ESC  关闭", "C / ESC  CLOSE" },
        { "攻击速度", "ATK SPD" }, { "移动速度", "MOVE SPD" }, { "暴击率", "CRIT" }, { "暴击伤害", "CRIT DMG" }, { "每秒回复", "REGEN/S" }, { "拾取范围", "PICKUP" }, { "穿透", "PIERCE" },
        { "被动", "PASSIVE" }, { "当前加成", "CURRENT BONUSES" }, { "角色被动", "PASSIVE" },
        { "武装补强", "ARMORY BOOST" }, { "属性加成", "STAT BONUS" }, { "已进化", "EVOLVED" }, { "未解锁", "LOCKED" },
        { "滚轮 / 拖动查看全部加成", "Scroll / drag to view all bonuses" },

        // Boss / stage
        { "第一阶段  /  灰烬之门", "STAGE 1  /  ASHEN GATE" }, { "第二阶段  /  回响裂隙", "STAGE 2  /  ECHO RIFT" }, { "第三阶段  /  星蚀核心", "STAGE 3  /  ECLIPSE CORE" },
        { "第1阶段/灰烬之门", "STAGE 1/ASHEN GATE" }, { "第2阶段/回响裂隙", "STAGE 2/ECHO RIFT" }, { "第3阶段/星蚀核心", "STAGE 3/ECLIPSE CORE" },
        { "THE DREAD WARDEN  /  01  ·  噬影守望者", "THE DREAD WARDEN  /  01  ·  SHADOW WARDEN" },
        { "THE ECHO HERALD  /  02  ·  回响先驱", "THE ECHO HERALD  /  02  ·  ECHO HERALD" },
        { "THE STAR EATER  /  03  ·  星蚀巨兽", "THE STAR EATER  /  03  ·  STAR EATER" },
        { "噬影守望者", "SHADOW WARDEN" }, { "回响先驱", "ECHO HERALD" }, { "星蚀巨兽", "STAR EATER" },
        { "强敌接近", "BOSS INCOMING" }, { "核心暴露", "CORE EXPOSED" },
        { "新武器 · 武器进化", "NEW WEAPONS · EVOLUTIONS" }, { "战地补给", "FIELD SUPPLY" }, { "属性模块与武器技能", "STAT MODULES + WEAPON SKILLS" },
        { "强敌接近  /  BOSS INCOMING", "BOSS INCOMING" },

        // HUD / upgrades
        { "冲刺", "DASH" }, { "就绪", "Ready" }, { "免疫", "IMMUNE" },
        { "左侧任意位置拖动移动     右下按钮冲刺     武器自动攻击", "Drag anywhere left to move     Dash right     Auto attack" },
        { "点击卡牌选择强化", "Tap a card to choose an upgrade" },
        { "点击卡牌，或按 1 / 2 / 3 选择", "Click a card, or press 1 / 2 / 3" },
        { "选择属性与已有武器技能", "CHOOSE AN ATTRIBUTE OR WEAPON SKILL" },
        { "选择核心共鸣", "CHOOSE A CORE RESONANCE" },
        { "选择新武器或进化武器", "CHOOSE A NEW WEAPON OR EVOLUTION" },
        { "选择战地补给", "CHOOSE A FIELD SUPPLY" },
        { "刷新选项  ·  剩余 2", "REROLL  ·  2 LEFT" },
        { "刷新选项", "REROLL" }, { "剩余", "LEFT" },
        { "唯一核心  ·  每种限选一次", "UNIQUE CORE  ·  ONCE PER TYPE" },
        { "武器进阶", "WEAPON EVOLUTION" }, { "新武器解锁", "NEW WEAPON" },
        { "武器升至", "Weapon Lv." }, { "级", "" },
        { "强化", "RANK" },
        { "自动锁定最近目标，发射星弧脉冲。", "Auto-locks the nearest target and fires stable star-arc pulses." },
        { "自动锁定最近目标，发射稳定的星弧能量脉冲。", "Auto-locks the nearest target and fires stable star-arc pulses." },
        { "扇形投射多枚弹体，适合清理近身敌潮。", "Fires a fan of hot projectiles, ideal for clearing close swarms." },
        { "扇形投射多枚高热弹体，适合清理近距离敌潮。", "Fires a fan of hot projectiles, ideal for clearing close swarms." },
        { "轨道刃围绕驾驶员旋转，接触敌人并可消除弹幕。", "Orbiting blades circle the pilot, damaging enemies and clearing projectiles." },
        { "枚光刃环绕身体旋转，命中造成伤害并消除弹幕。", " blades orbit the pilot, damaging enemies and clearing projectiles." },
        { "电弧在敌群间跳跃，首个目标额外承受 30% 伤害。", "Arcs jump between enemies; the first target takes 30% extra damage." },
        { "电弧在敌群之间跳跃，连续打击多个目标，首个目标额外承受 30% 伤害。", "Arcs jump between enemies and hit multiple targets; the first takes 30% extra damage." },
        { "狙击式冰晶长枪，贯穿并施加霜缓。", "A sniper-style cryo lance that pierces and chills." },
        { "狙击式冰晶长枪，长弹体高速贯穿并施加霜缓。", "A sniper-style cryo lance whose long projectile pierces at speed and chills." },
        { "发射三颗子弹，火力覆盖更密集", "Fires three bullets for denser coverage" },
        { "增加子弹数和伤害，弹幕覆盖更广", "More bullets and damage for wider coverage" },
        { "增加环刃速度和数量，运转更迅捷", "More blades and speed for a faster orbit" },
        { "增加电弧伤害和扩散数，连锁跳跃更远", "More arc damage and jumps for longer chains" },
        { "增加伤害和减速，持续造成霜蚀伤害", "More damage and slow, with continuous frost damage" },
        { "超载核心", "OVERLOAD CORE" }, { "凤凰协议", "PHOENIX PROTOCOL" }, { "星核共鸣", "STARCORE RESONANCE" },
        { "最终伤害 +30%，最终攻速 +20%，暴击率 +20%。", "Final damage +30%, final attack speed +20%, crit +20%." },
        { "生命上限 +30，护甲 +10%，每秒回复 +2；受击时 30% 概率免疫。", "Max HP +30, armor +10%, regen +2/s; 30% chance to negate damage." },
        { "暴击率 +30%，暴击伤害 +50%；暴击率超过 100% 时，溢出部分每 1% 额外增加 2% 暴击伤害。", "Crit +30%, crit damage +50%. Above 100% crit, each 1% overflow adds 2% crit damage." },

        // Upgrade names / descriptions
        { "反应堆增幅", "Reactor Boost" }, { "超频回路", "Overclock Circuit" }, { "轻量推进", "Light Thrusters" }, { "暴击增幅", "Crit Amplifier" },
        { "星尘引力", "Stardust Gravity" }, { "精准校准", "Precision Calibration" }, { "穿透晶核", "Pierce Core" }, { "自愈纳米机", "Self-Heal Nanites" },
        { "偏转护甲", "Deflection Armor" }, { "生命扩容", "Health Expansion" }, { "应急修复", "Emergency Repair" },
        { "武装超载", "Armory Overload" }, { "急速装填", "Rapid Reload" }, { "超频瞄准", "Overclocked Aim" },
        { "纳米修复", "Nano Repair" }, { "现场医疗", "Field Medic" }, { "急救协议", "First Aid Protocol" },
        { "恢复", "Restore" }, { "点生命", "HP" }, { "全部武器伤害", "All weapon damage" },
        { "经验拾取范围", "XP pickup range" }, { "子弹穿透数", "Bullet pierce" }, { "每秒恢复", "Regen per second" },
        { "受到的伤害降低", "Damage taken -" }, { "上限", "cap" }, { "生命上限", "Max HP" }, { "并恢复", "and restore" },
        { "武器性能提升", "Weapon performance increased" },

        // Settings
        { "音频", "AUDIO" }, { "画面", "VIDEO" }, { "通用", "GENERAL" }, { "数据", "DATA" },
        { "主音量", "Master" }, { "音乐", "Music" }, { "音效", "SFX" },
        { "画质", "Quality" }, { "帧率", "Frame rate" }, { "伤害数字", "Damage numbers" }, { "屏幕震动", "Screen shake" }, { "粒子密度", "Particle density" },
        { "低", "Low" }, { "中", "Med" }, { "高", "High" }, { "关", "Off" }, { "开", "On" }, { "标准", "Standard" },
        { "语言", "Language" }, { "简体中文", "Chinese" }, { "失焦自动暂停", "Auto-pause on focus loss" }, { "退出", "Exit" }, { "退出游戏", "EXIT GAME" },
        { "存档导入与导出", "SAVE IMPORT & EXPORT" }, { "未开放", "NOT AVAILABLE" },
        { "将在后续版本接入，当前不可使用。", "Coming in a future version; currently unavailable." },
        { "导出存档", "EXPORT SAVE" }, { "导入存档", "IMPORT SAVE" },
        { "功能暂未开放", "Feature not available yet" },
        { "修改即时保存并生效", "" }, { "ESC  关闭设置", "" },
        { "唯一核心", "UNIQUE CORE" },
        { "BEST KILLS", "BEST KILLS" }, { "CLEARS", "CLEARS" },
    };

    private static readonly string[] TokenKeys =
    {
        "苍真 零", "星野 莉花", "白濑 凛", "月城 芽衣", "雨宫 由奈",
        "零号观测员 / REI", "赤焰枪姬 / RIKKA", "绝对零度 / RIN", "月环舞者 / MEI", "雷光信使 / YUNA",
        "零号观测员", "赤焰枪姬", "绝对零度", "月环舞者", "雷光信使",
        "男 · 24岁", "女 · 19岁", "女 · 20岁", "女 · 18岁", "女 · 22岁",
        "本命武器", "专属被动", "本局武器伤害", "星链协议", "双生齐射", "六曜环阵", "超导网络", "霜蚀棱镜",
        "唯一核心奖励", "作战状态", "超载核心", "凤凰协议", "星核共鸣",
        "生存", "导出存档", "导入存档",
        "临界瞄准", "热装填", "零域共鸣", "轨道校准", "过载回路",
        "星芒脉冲", "余烬散射", "环轨光刃", "连锁电弧", "冰晶长枪",
        "第一阶段  /  灰烬之门", "第二阶段  /  回响裂隙", "第三阶段  /  星蚀核心",
        "第1阶段/灰烬之门", "第2阶段/回响裂隙", "第3阶段/星蚀核心",
        "受到的伤害降低", "子弹穿透数", "全部武器伤害", "并", "上限",
        "自动锁定最近目标，发射星弧脉冲。", "扇形投射", "枚弹体，适合清理近身敌潮。",
        "光刃环绕身体旋转，命中造成伤害并消除弹幕。", "轨道刃围绕驾驶员旋转，接触敌人并可消除弹幕。",
        "电弧在敌群间跳跃，首个目标额外承受 30% 伤害。", "狙击式冰晶长枪，贯穿并施加霜缓。",
        "全部武器攻击速度", "最终攻速", "最终伤害", "攻击速度", "伤害", "攻速", "暴击伤害", "暴击率", "暴击", "护甲", "生命上限", "生命",
        "移动速度", "每秒回复", "每秒恢复", "拾取范围", "经验拾取范围", "穿透", "霜缓", "间隔", "弹体", "单次命中", "每发", "命中间隔", "连锁", "目标", "子弹", "枚", "个",
        "恢复", "点生命", "秒", "米", "级", "强化", "武器升至", "武器进阶", "新武器解锁", "已进化", "未解锁", "剩余", "唯一核心", "还可选择", "种",
        "时间", "等级", "击杀", "武器", "角色被动", "属性加成", "武装补强", "件", "已进化", "功能暂未开放",
        "刷新选项",
        "新级别敌潮已部署", "共鸣强化已载入", "获得新武器", "武器进阶", "下次会走得更远", "核心暴露", "属性模块与武器技能", "战地补给"
    };

    private static readonly Dictionary<string, string> Tokens = new Dictionary<string, string>
    {
        { "苍真 零", "Rei Soma" }, { "星野 莉花", "Rikka Hoshino" }, { "白濑 凛", "Rin Shirae" }, { "月城 芽衣", "Mei Tsukishiro" }, { "雨宫 由奈", "Yuna Amamiya" },
        { "零号观测员 / REI", "Zero Observer / REI" }, { "赤焰枪姬 / RIKKA", "Crimson Gunner / RIKKA" }, { "绝对零度 / RIN", "Absolute Zero / RIN" }, { "月环舞者 / MEI", "Lunar Dancer / MEI" }, { "雷光信使 / YUNA", "Lightning Courier / YUNA" },
        { "零号观测员", "Zero Observer" }, { "赤焰枪姬", "Crimson Gunner" }, { "绝对零度", "Absolute Zero" }, { "月环舞者", "Lunar Dancer" }, { "雷光信使", "Lightning Courier" },
        { "男 · 24岁", "Male · 24" }, { "女 · 19岁", "Female · 19" }, { "女 · 20岁", "Female · 20" }, { "女 · 18岁", "Female · 18" }, { "女 · 22岁", "Female · 22" },
        { "本命武器", "SIGNATURE WEAPON" }, { "专属被动", "SIGNATURE TRAIT" }, { "本局武器伤害", "WEAPON DAMAGE" },
        { "唯一核心奖励", "UNIQUE CORE REWARD" }, { "作战状态", "RUN STATUS" },
        { "超载核心", "OVERLOAD CORE" }, { "凤凰协议", "PHOENIX PROTOCOL" }, { "星核共鸣", "STARCORE RESONANCE" },
        { "生存", "SURVIVE" }, { "导出存档", "EXPORT SAVE" }, { "导入存档", "IMPORT SAVE" },
        { "星链协议", "Star Chain Protocol" }, { "双生齐射", "Twin Volley" }, { "六曜环阵", "Six-Star Array" }, { "超导网络", "Superconducting Network" }, { "霜蚀棱镜", "Frost Prism" },
        { "临界瞄准", "Critical Aim" }, { "热装填", "Hot Reload" }, { "零域共鸣", "Zero Field Resonance" }, { "轨道校准", "Orbit Calibration" }, { "过载回路", "Overload Circuit" },
        { "星芒脉冲", "Pulse" }, { "余烬散射", "Scatter" }, { "环轨光刃", "Orbit Blade" }, { "连锁电弧", "Chain Arc" }, { "冰晶长枪", "Cryo Lance" },
        { "第一阶段  /  灰烬之门", "STAGE 1  /  ASHEN GATE" }, { "第二阶段  /  回响裂隙", "STAGE 2  /  ECHO RIFT" }, { "第三阶段  /  星蚀核心", "STAGE 3  /  ECLIPSE CORE" },
        { "第1阶段/灰烬之门", "STAGE 1/ASHEN GATE" }, { "第2阶段/回响裂隙", "STAGE 2/ECHO RIFT" }, { "第3阶段/星蚀核心", "STAGE 3/ECLIPSE CORE" },
        { "受到的伤害降低", "Damage taken -" }, { "子弹穿透数", "Bullet pierce " },
        { "全部武器伤害", "All weapon damage " }, { "并", " and " }, { "上限", " cap " },
        { "自动锁定最近目标，发射星弧脉冲。", "Auto-locks the nearest target and fires star-arc pulses." },
        { "扇形投射", "Fires a fan of " }, { "枚弹体，适合清理近身敌潮。", "projectiles, ideal for clearing close swarms." },
        { "光刃环绕身体旋转，命中造成伤害并消除弹幕。", " blades orbit the pilot, damaging enemies and clearing projectiles." },
        { "轨道刃围绕驾驶员旋转，接触敌人并可消除弹幕。", "Orbiting blades circle the pilot, damaging enemies and clearing projectiles." },
        { "电弧在敌群间跳跃，首个目标额外承受 30% 伤害。", "Arcs jump between enemies; the first target takes 30% extra damage." },
        { "狙击式冰晶长枪，贯穿并施加霜缓。", "A sniper-style cryo lance that pierces and chills." },
        { "全部武器攻击速度", "All weapon attack speed " }, { "最终攻速", "Final attack speed " }, { "最终伤害", "Final damage " },
        { "攻击速度", "Attack speed " }, { "伤害", "Damage " }, { "攻速", "ATK SPD " }, { "暴击伤害", "Crit damage " }, { "暴击率", "Crit " }, { "暴击", "Crit " },
        { "护甲", "Armor " }, { "生命上限", "Max HP " }, { "生命", "HP " }, { "移动速度", "Move speed " }, { "每秒回复", "Regen/s " }, { "每秒恢复", "Regen/s " },
        { "拾取范围", "Pickup range " }, { "经验拾取范围", "XP pickup range " }, { "穿透", "Pierce " }, { "霜缓", "Chill " },
        { "间隔", "Interval " }, { "弹体", "projectiles " }, { "单次命中", "Hit " }, { "每发", "Per shot " }, { "命中间隔", "Hit interval " },
        { "连锁", "Chain " }, { "目标", "targets " }, { "子弹", "bullets " }, { "枚", "" }, { "个", "" },
        { "恢复", "Restore " }, { "点生命", " HP" }, { "秒", "s" }, { "米", "m" }, { "级", "" },
        { "强化", "Rank " }, { "武器升至", "Weapon Lv." }, { "武器进阶", "WEAPON EVOLUTION" }, { "新武器解锁", "NEW WEAPON" },
        { "已进化", "Evolved " }, { "未解锁", "LOCKED" }, { "剩余", "LEFT " }, { "唯一核心", "UNIQUE CORE" }, { "还可选择", "" }, { "种", " options" },
        { "时间", "Time " }, { "等级", "Level " }, { "击杀", "Kills " }, { "武器", "Weapons " }, { "角色被动", "Passive: " },
        { "刷新选项", "REROLL" },
        { "属性加成", "Stat bonus " }, { "武装补强", "Armory boost " }, { "件", "" }, { "任务", "Mission " }, { "状态", "Status " },
        { "功能暂未开放", "Feature not available yet" }, { "新级别敌潮已部署", "NEW ENEMY WAVE DEPLOYED" },
        { "共鸣强化已载入", "RESONANCE UPGRADE LOADED" }, { "获得新武器", "NEW WEAPON ACQUIRED" }, { "下次会走得更远", "You will go farther next time" },
        { "核心暴露", "CORE EXPOSED" }, { "属性模块与武器技能", "STAT MODULES + WEAPON SKILLS" }, { "战地补给", "FIELD SUPPLY" }
    };

    private static readonly Dictionary<string, string> Chinese = new Dictionary<string, string>
    {
        { "BEST KILLS", "最佳击杀" }, { "CLEARS", "通关" },
        { "PILOT ARCHIVE / ASHEN GATE EXPEDITION", "驾驶员档案 / 灰烬门远征" }, { "ACTIVE FILE", "当前档案" },
        { "SYSTEM CONFIG", "系统设置" }, { "FIELD SIGNAL", "战场信号" },
        { "RUN COMPLETE / SECTOR CLEARED", "远征完成 / 区域清除" }, { "RUN OVER / SIGNAL LOST", "远征结束 / 信号丢失" },
        { "THE NIGHT BREAKS", "夜幕破晓" }, { "SIGNAL LOST", "信号丢失" }, { "PAUSED", "已暂停" },
        { "WEAPON EVOLUTION", "武器进化" }, { "NEW WEAPON", "新武器" }, { "ARSENAL BOOST", "武装补强" },
        { "ARSENAL SUPPORT", "武装支援" }, { "STAT MODULE", "属性模块" }, { "WEAPON SKILL", "武器技能" },
        { "FIELD SUPPLY", "战地补给" }, { "UNIQUE CORE", "唯一核心" }, { "BOSS INCOMING", "强敌接近" },
        { "SECTOR CLEARED", "区域清除" }, { "STARFALL STUDIO / MOBILE", "星陨工作室 / 移动端" },
        { "SURVIVOR FILE 001 / ASHEN GATE", "幸存者档案 001 / 灰烬之门" },
        { "ARMORY UPLINK / WEAPON GROWTH", "武器上行 / 武器成长" }
    };

    public static void SetLanguage(int language)
    {
        language = language == 1 ? 1 : 0;
        if (Language == language) return;
        Language = language;
        Changed?.Invoke();
    }

    public static string Translate(string source)
    {
        if (string.IsNullOrEmpty(source)) return source;
        if (Language == 0)
        {
            string plain = Regex.Replace(source.Replace("\u2009", string.Empty), " {2,}", " ").Trim();
            return Chinese.TryGetValue(plain, out string chinese) ? chinese : source;
        }
        bool tracked = source.Contains("\u2009");
        string cleaned = tracked ? source.Replace("\u2009", string.Empty) : source;
        string working = Regex.Replace(cleaned, " {2,}", " ").Trim();
        if (Exact.TryGetValue(working, out string exact)) return tracked ? UiKit.Track(exact) : exact;

        string result = working;
        bool translated = false;
        result = Regex.Replace(result, @"武器升至\s*(\d+)\s*级", "Weapon Lv.$1");
        result = Regex.Replace(result, @"强化\s*·\s*(\d+)/(\d+)", "Rank $1/$2");
        result = Regex.Replace(result, @"恢复\s*(\d+)\s*点生命", "Restore $1 HP");
        result = Regex.Replace(result, @"生命\s*(\d+)\s*/\s*(\d+)", "HP $1 / $2");
        result = Regex.Replace(result, @"武器\s*(\d+)\s*/\s*5\s*·\s*进化\s*(\d+)\s*/\s*5", "Weapons $1/5 · Evolved $2/5");
        result = Regex.Replace(result, @"武器\s*(\d+)\s*/\s*5\s*·\s*已进化\s*(\d+)\s*件", "Weapons $1/5 · Evolved $2");
        result = Regex.Replace(result, @"剩余\s*(\d+)", "Left $1");
        result = Regex.Replace(result, @"还可选择\s*(\d+)\s*种", "$1 options left");
        translated = result != working;

        // Replace longest phrases first so partial shorter terms cannot corrupt them.
        var orderedKeys = new List<string>(TokenKeys);
        orderedKeys.Sort((a, b) => b.Length.CompareTo(a.Length));
        foreach (string key in orderedKeys)
            if (result.Contains(key) && Tokens.TryGetValue(key, out string replacement))
            {
                result = result.Replace(key, replacement);
                translated = true;
            }
        result = result.Replace("。", ".").Replace("，", ", ").Replace("；", "; ").Replace("：", ": ").Replace("！", "!").Replace("？", "?").Replace("（", " (").Replace("）", ")");
        if (!translated) return source;
        if (tracked) result = UiKit.Track(result);
        return result;
    }
}

public sealed class LocalizedText : MonoBehaviour
{
    private Text target;
    private string source;
    private string shown;

    public void Bind(Text text)
    {
        target = text;
        source = text.text;
        Apply();
    }

    private void OnEnable()
    {
        Localization.Changed += Refresh;
        if (target != null)
        {
            if (target.text != shown) source = target.text;
            Apply();
        }
    }

    private void OnDisable()
    {
        Localization.Changed -= Refresh;
    }

    private void LateUpdate()
    {
        if (target == null) return;
        if (target.text != shown)
        {
            source = target.text;
            Apply();
        }
    }

    private void Refresh()
    {
        if (target != null) Apply();
    }

    private void Apply()
    {
        if (target == null) return;
        if (source == null) source = target.text;
        shown = Localization.Translate(source);
        target.text = shown;
    }
}

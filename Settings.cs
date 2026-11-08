using System;
using UnityEngine;
using UnityModManagerNet;

namespace ADOFAIPlus
{
    public enum AlternativeIndicatorStyle
    {
        SignPrefix = 0,   // 正负符号前缀: -宽判！(太快) / +标判！(太慢) [默认]
        LetterPrefix = 1  // 字符名称前缀: L太快！/ N太慢！
    }

    public enum ModLanguage
    {
        Auto = 0,               // 跟随游戏 (Follow Game)
        ChineseSimplified = 1,  // 简体中文
        ChineseTraditional = 2, // 繁體中文
        English = 3,            // English
        Korean = 4,             // 한국어
        Japanese = 5,           // 日本語
        Spanish = 6,            // Español
        Portuguese = 7,         // Português
        French = 8,             // Français
        German = 9,             // Deutsch
        Polish = 10,            // Polski
        Russian = 11,           // Русский
        Vietnamese = 12,        // Tiếng Việt
        Czech = 13,             // Čeština
        Romanian = 14           // Română
    }

    /// <summary>
    /// 更好的判定 Lite (Better Judgment Lite) 纯净绿色版设置：
    /// 不含任何判定窗口篡改、不死模式、自动播放或自动播放+，仅提供原生 PC 判定下的微观判定指示器与实用显示增强
    /// </summary>
    public class Settings : UnityModManager.ModSettings
    {
        /// <summary>
        /// 模组语言设置（默认跟随游戏当前语言）
        /// </summary>
        public ModLanguage Language = ModLanguage.Auto;

        /// <summary>
        /// 更好的日语本地化（当语言为日语时，将原版未翻译的英文判定词完整本地化为日语；默认开启）
        /// </summary>
        public bool BetterJapaneseLocalization = true;

        /// <summary>
        /// 日语使用中文字体（当语言为日语且启用更好的日语本地化时，使用游戏内的中文字体替代原版日语字体；默认开启）
        /// </summary>
        public bool JapaneseUseChineseFont = true;

        public bool MigratedJapaneseFontDefault = false;

        /// <summary>
        /// 判定文字边框染色（对浮动判定文字的边框材质同步染灰/绿色；关闭则保留原版白金光晕基底）
        /// </summary>
        public bool ColorHitTextBorder = true;

        /// <summary>
        /// 判定指示器：打出 Counted 时重测，标判合格施加灰色描边，宽判合格施加绿色描边
        /// </summary>
        public bool EnableHitIndicator = true;

        /// <summary>
        /// 判定指示器+：将 Counted 文字分别替换为「标判！」和「宽判！」（精简模式显示 NOR / LEN）
        /// </summary>
        public bool EnableHitIndicatorPlus = true;

        /// <summary>
        /// 为「太快！！」与「太慢！！」启用判定指示器：即使在空敲或失误情况下也重测显示「宽判！！」与「标判！！」
        /// </summary>
        public bool EnableDoubleHitIndicator = false;

        /// <summary>
        /// 轨道染色：当打出「标判！」与「宽判！」时对应将轨道染为灰色与绿色
        /// </summary>
        public bool EnableTrackColoring = false;

        /// <summary>
        /// 另类判定指示器+：默认为启用，为指示器文字提供实际早晚方向指示（太快/太慢）
        /// </summary>
        public bool EnableAlternativeIndicatorPlus = true;

        /// <summary>
        /// 另类指示器工作方式（默认：正负符号前缀 -宽判！太快 / +标判！太慢）
        /// </summary>
        public AlternativeIndicatorStyle AltStyle = AlternativeIndicatorStyle.SignPrefix;

        /// <summary>
        /// 强制启用判定选择（即使在低 BPM 或官方关卡也强制开启原版严/标/宽判定选择栏）
        /// </summary>
        public bool ForceDifficultySelector = false;

        /// <summary>
        /// 杂项：隐藏星球判定圈（虚线圆圈）
        /// </summary>
        public bool HidePlanetRing = false;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }
}

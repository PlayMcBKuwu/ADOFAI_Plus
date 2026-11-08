using System;
using System.Collections.Generic;
using UnityEngine;

namespace BetterJudgmentLite
{
    public static class Loc
    {
        private static readonly Dictionary<SystemLanguage, Dictionary<string, string>> Translations =
            new Dictionary<SystemLanguage, Dictionary<string, string>>();

        static Loc()
        {
            InitChineseSimplified();
            InitChineseTraditional();
            InitEnglish();
            InitKorean();
            InitJapanese();
            InitSpanish();
            InitPortuguese();
            InitFrench();
            InitGerman();
            InitPolish();
            InitRussian();
            InitVietnamese();
            InitCzech();
            InitRomanian();
        }

        public static SystemLanguage CurrentLanguage
        {
            get
            {
                if (Main.Settings != null && Main.Settings.Language != ModLanguage.Auto)
                {
                    switch (Main.Settings.Language)
                    {
                        case ModLanguage.ChineseSimplified: return SystemLanguage.ChineseSimplified;
                        case ModLanguage.ChineseTraditional: return SystemLanguage.ChineseTraditional;
                        case ModLanguage.English: return SystemLanguage.English;
                        case ModLanguage.Korean: return SystemLanguage.Korean;
                        case ModLanguage.Japanese: return SystemLanguage.Japanese;
                        case ModLanguage.Spanish: return SystemLanguage.Spanish;
                        case ModLanguage.Portuguese: return SystemLanguage.Portuguese;
                        case ModLanguage.French: return SystemLanguage.French;
                        case ModLanguage.German: return SystemLanguage.German;
                        case ModLanguage.Polish: return SystemLanguage.Polish;
                        case ModLanguage.Russian: return SystemLanguage.Russian;
                        case ModLanguage.Vietnamese: return SystemLanguage.Vietnamese;
                        case ModLanguage.Czech: return SystemLanguage.Czech;
                        case ModLanguage.Romanian: return SystemLanguage.Romanian;
                    }
                }

                // 自动跟随 ADOFAI 原生当前语言
                SystemLanguage lang = RDString.language;
                if (lang == SystemLanguage.Chinese) lang = SystemLanguage.ChineseSimplified;

                if (Translations.ContainsKey(lang))
                {
                    return lang;
                }

                return SystemLanguage.English;
            }
        }

        public static bool IsCJK()
        {
            SystemLanguage lang = CurrentLanguage;
            if (lang == SystemLanguage.Japanese)
            {
                return Main.Settings != null && Main.Settings.BetterJapaneseLocalization;
            }
            return lang == SystemLanguage.ChineseSimplified ||
                   lang == SystemLanguage.ChineseTraditional ||
                   lang == SystemLanguage.Chinese ||
                   lang == SystemLanguage.Korean;
        }

        public static string GetSingleExcl()
        {
            return IsCJK() ? "！" : "!";
        }

        public static string GetDoubleExcl()
        {
            return IsCJK() ? "！！" : "!!";
        }

        public static bool TryGetBetterJapaneseRDString(string key, out string result)
        {
            result = null;
            if (string.IsNullOrEmpty(key)) return false;

            switch (key)
            {
                // 浮动判定词 (Default) —— 与韩语（조금 빠름 / 조금 느림）、中文（稍快！ / 稍慢！）一致，采用自然形容词（少し早い！ / 少し遅い！）
                case "HitMargin.TooEarly": result = "早すぎ！！"; return true;
                case "HitMargin.VeryEarly": result = "早い！"; return true;
                case "HitMargin.EarlyPerfect": result = "少し早い！"; return true;
                case "HitMargin.PerfectMinus": result = "-完璧！"; return true;
                case "HitMargin.XPerfect": result = "X完璧！"; return true;
                case "HitMargin.Perfect": result = "完璧！"; return true;
                case "HitMargin.PerfectPlus": result = "+完璧！"; return true;
                case "HitMargin.LatePerfect": result = "少し遅い！"; return true;
                case "HitMargin.VeryLate": result = "遅い！"; return true;
                case "HitMargin.TooLate": result = "遅すぎ！！"; return true;
                case "HitMargin.Multipress": result = "同時押し！！"; return true;
                case "HitMargin.OverPress": result = "押しすぎ！！"; return true;
                case "HitMargin.FailMiss": result = "ミス！！"; return true;
                case "HitMargin.FailOverload": result = "連打しすぎ！！"; return true;
                case "HitMargin.Auto": result = "オート！"; return true;

                // 倒计时 Ready / Go 本地化（保留原版日语的三种判定名称 優しい / 通常 / 厳しい 不做改动）
                case "status.getReady": result = "準備！"; return true;
                case "status.go": result = "スタート！"; return true;

                // 详细结算统计词 —— 与韩语（조금 빠름 / 조금 느림）、中文（稍快 / 稍慢）一致
                case "status.results.perfectM": result = "-完璧"; return true;
                case "status.results.xPerfect": result = "X完璧"; return true;
                case "status.results.perfectP": result = "+完璧"; return true;
                case "status.results.ePerfect": result = "少し早い"; return true;
                case "status.results.perfect": result = "完璧"; return true;
                case "status.results.lPerfect": result = "少し遅い"; return true;
                case "status.results.tooEarly": result = "早すぎ"; return true;
                case "status.results.early": result = "早い"; return true;
                case "status.results.late": result = "遅い"; return true;
                case "status.results.missFails": result = "ミス"; return true;
                case "status.results.overloadFails": result = "連打ミス"; return true;
                case "status.results.multipress": result = "同時押し"; return true;
                case "status.results.overpress": result = "押しすぎ"; return true;
                default:
                    return false;
            }
        }

        public static string Get(string key)
        {
            SystemLanguage lang = CurrentLanguage;

            // 原版日语中游戏复用英文判定词；仅当开启「更好的日语本地化」时判定词与结算词使用日语
            if (lang == SystemLanguage.Japanese && (Main.Settings == null || !Main.Settings.BetterJapaneseLocalization))
            {
                if (key == "indicator.normal" || key == "results.normal" ||
                    key == "indicator.lenient" || key == "results.lenient" ||
                    key == "indicator.tooEarly" || key == "indicator.tooLate")
                {
                    lang = SystemLanguage.English;
                }
            }

            Dictionary<string, string> dict;
            if (Translations.TryGetValue(lang, out dict))
            {
                string val;
                if (dict.TryGetValue(key, out val))
                {
                    return val;
                }
            }

            // 备用兜底回英语
            if (Translations.TryGetValue(SystemLanguage.English, out dict))
            {
                string val;
                if (dict.TryGetValue(key, out val))
                {
                    return val;
                }
            }

            return key;
        }

        private static void Add(SystemLanguage lang, string key, string value)
        {
            Dictionary<string, string> dict;
            if (!Translations.TryGetValue(lang, out dict))
            {
                dict = new Dictionary<string, string>();
                Translations[lang] = dict;
            }
            dict[key] = value;
        }

        private static void InitChineseSimplified()
        {
            SystemLanguage l = SystemLanguage.ChineseSimplified;
            Add(l, "master.warning_title", "[!] 免责声明与安全警告");
            Add(l, "master.warning_body", "严禁在多人游戏中使用该模组！作者不为使用该模组导致的任何后果（如账户封禁、联机断开等）承担任何责任。");
            Add(l, "master.agree_btn", "我已知晓并同意免责声明，开启模组总开关");
            Add(l, "master.status_on", "模组总开关已开启 (Master Switch: ON)");
            Add(l, "master.disable_btn", "关闭总开关");
            Add(l, "misc.section", "3. 杂项功能 (Miscellaneous)");
            Add(l, "misc.editor_toggle", "始终允许使用关卡编辑器功能 (官谱或特殊情况亦可开启)");
            Add(l, "misc.ring_toggle", "隐藏星球判定圈 (隐藏围绕旋转星球的虚线圈)");
            Add(l, "misc.autoplay_plus_toggle", "启用 自动播放+ (内部零延迟判定 + 拟人双通道按键点亮 KV，自动阻断局内按键干扰)");
            Add(l, "misc.autoplay_keys_label", "自动播放+ 16 键位面板 (左键点击按钮后按键盘绑定，右键清空，Esc 取消):");
            Add(l, "misc.autoplay_keys_hint", "上方 8 键与下方 8 键各分为 [左手 4 键 | 右手 4 键]，智能匹配单指平点、左右食指单双交替、四指内旋轮指与八指顺滑流");
            Add(l, "misc.autoplay_preset_btn", "恢复预设 16 键");
            Add(l, "misc.sync_jipper_btn", "从 JipperResourcePack 一键同步键位");
            Add(l, "misc.sync_jipper_ok", "✓ 已成功从 JipperResourcePack 同步键位！");
            Add(l, "misc.sync_jipper_fail", "× 未找到 JipperResourcePack 键位配置");
            Add(l, "misc.key_unset", "未设");
            Add(l, "misc.key_waiting", "请按键...");
            Add(l, "misc.hand_left", "左手区 (L)");
            Add(l, "misc.hand_right", "右手区 (R)");
            Add(l, "misc.autoplay_acc_label", "拟人目标 X 精准度: {0:0.##}% (闭环控制在合法及格边界内注入生理节奏抖动，绝不死)");
            Add(l, "hud.autoplay_plus", "自动播放+");
            
            Add(l, "mod.title", "更好的判定");
            Add(l, "stamp.mobile", "移动判定通关");
            Add(l, "stamp.custom", "自定判定通关");
            Add(l, "ui.custom.unit_label", "自定义判定模式 (Unit Mode):");
            Add(l, "ui.custom.unit_time", "时间模式 (毫秒 ms)");
            Add(l, "ui.custom.unit_angle", "角度模式 (度数 °)");
            Add(l, "ui.custom.counted_angle", "及格 Counted 角度 ({0}°): ");
            Add(l, "ui.custom.perfect_angle", "早晚完美 Perfect 角度 ({0}°): ");
            Add(l, "ui.custom.pure_angle", "偏偏完美 Pure 角度 ({0}°): ");
            Add(l, "ui.custom.mult_title", "一键按预设百分比乘算:");
            Add(l, "ui.custom.mult_apply", "应用百分比");
            Add(l, "ui.custom.preset_strict", "严格预设");
            Add(l, "ui.custom.preset_normal", "标准预设");
            Add(l, "ui.custom.preset_lenient", "宽松预设");
            Add(l, "ui.custom.preset_mobile", "移动端预设");
            Add(l, "ui.custom.profiles_title", "自定判定快速档案槽位 (Profiles):");
            Add(l, "ui.custom.save_slot", "保存至槽位 {0}");
            Add(l, "ui.custom.load_slot", "载入槽位 {0}");
            Add(l, "ui.custom.free_input_hint", "（可在右侧文本框自由键入任意数值突破滑块范围）");
            Add(l, "hud.mobile", "移动判定");
            Add(l, "hud.custom", "自定判定");
            Add(l, "diff.mobile", "移动");
            Add(l, "diff.custom", "自定义");
            Add(l, "indicator.normal", "标判");
            Add(l, "indicator.lenient", "宽判");
            Add(l, "indicator.tooEarly", "太快");
            Add(l, "indicator.tooLate", "太慢");
            Add(l, "results.normal", "标判");
            Add(l, "results.lenient", "宽判");

            Add(l, "ui.language_label", "语言设置 (Language)");
            Add(l, "ui.lang.auto", "跟随游戏 (Auto)");
            Add(l, "ui.japanese_better_loc", "启用「更好的日语本地化」：将原版日语下未翻译的英文判定词完整本地化为日语");
            Add(l, "ui.japanese_use_chinese_font", "日语使用中文字体：当处于日语时使用游戏内中文字体渲染（替换原版日语字体，推荐开启）");
            Add(l, "ui.border_color.toggle", "启用「判定文字边框染色」：为「标判！」与「宽判！」浮动文字边框染上对应灰/绿色（关闭则保留原版高亮白金光晕）");
            Add(l, "ui.track_color.toggle", "启用「轨道染色」：打出「标判！」与「宽判！」时，对应将脚下轨道砖块染为灰色与绿色");

            Add(l, "mode.vanilla", "原生 PC 模式 (Vanilla)");
            Add(l, "mode.mobile", "移动端判定模式 (Mobile)");
            Add(l, "mode.custom", "自定义判定模式 (Custom)");

            Add(l, "ui.section1", "1. 判定模式选择 (Judgment Mode Selection)");
            Add(l, "ui.mobile.desc_title", "[移动端判定模式已激活]");
            Add(l, "ui.mobile.desc_counted", "及格 (Counted) 下限: 90 ms（PC 原生标准为 65 ms / 严格 40 ms）");
            Add(l, "ui.mobile.desc_perfect", "早晚完美 (Perfect) 下限: 70 ms（PC 原生为 30 ms）");
            Add(l, "ui.mobile.desc_pure", "稍偏完美 (Pure) 下限: 50 ms（PC 原生为 25 ms）");
            Add(l, "ui.mobile.desc_speed", "倍速试炼特性已合并：高倍速试炼下判定下限不会缩减（还原手机手感）");
            Add(l, "ui.mobile.desc_hud", "界面指示：局内左上角自动播放位置常驻显示「移动判定」（真实自动播放时保留）");
            Add(l, "ui.mobile.desc_diff", "关卡难度栏：所有关卡强制常驻显示，原「宽松」重定向并显示为「移动」，锁定不可调");

            Add(l, "ui.custom.desc_title", "[自定义判定模式设置]");
            Add(l, "ui.custom.desc_hud", "界面指示：左上角常驻显示「自定判定」，难度栏重定向显示为「自定义」");
            Add(l, "ui.custom.counted", "及格 Counted 下限 ({0} ms): ");
            Add(l, "ui.custom.perfect", "早晚完美 Perfect 下限 ({0} ms): ");
            Add(l, "ui.custom.pure", "偏偏完美 Pure 下限 ({0} ms): ");
            Add(l, "ui.custom.scale_speed", "倍速试炼时按倍率缩小判定下限 (Scale with speed trial)");
            Add(l, "ui.custom.reset_mobile", "重置为官方移动端数值 (90 / 70 / 50 ms)");

            Add(l, "ui.section2", "2. PC 判定模式专属功能 (PC Mode Features)");
            Add(l, "ui.pc_only_hint", "(以下判定指示器仅在「原生 PC 模式」下生效)");
            Add(l, "ui.indicator.toggle", "启用「判定指示器」：打出及格 (Counted) 时重测，标判合格施加灰色描边，宽判合格施加绿色描边");
            Add(l, "ui.indicator.desc_normal", "标判成立 (<= 标判下限)：施加灰色描边");
            Add(l, "ui.indicator.desc_lenient", "宽判成立 (<= 宽判下限)：施加绿色描边");
            Add(l, "ui.indicator.desc_meter", "误击量计（判定条）对应打点同步加上灰/绿描边");
            Add(l, "ui.indicator_plus.toggle", "启用「判定指示器+」：将文字替换为「标判！」与「宽判！」（精简模式: NOR / LEN）");

            Add(l, "ui.alt_indicator.toggle", "启用「另类判定指示器+」：为判定文字追加早晚击打指示（太快/太慢）");
            Add(l, "ui.alt_indicator.style_label", "另类指示器工作方式 (Alternative Indicator Style):");
            Add(l, "ui.alt_style_sign", "正负符号前缀 (默认): -宽判！(太快) / +标判！(太慢)");
            Add(l, "ui.alt_style_letter", "字符名称前缀: L太快！/ N太慢！");

            Add(l, "ui.double_indicator.toggle", "为「太快！！」与「太慢！！」启用判定指示器：即使在空敲或失误断连情况下也进行重测，符合条件时显示「标判！！」与「宽判！！」");

            Add(l, "ui.guide.title", "判定指示器与原生判定显示格式对照说明：");
            Add(l, "ui.guide.default", "默认文字模式 (Default)：");
            Add(l, "ui.guide.default_plus", "判定指示器+：显示为「标判！」与「宽判！」");
            Add(l, "ui.guide.default_alt_sign", "另类指示器+ (符号前缀[默认])：显示为「-宽判！」(太快) 与「+标判！」(太慢)");
            Add(l, "ui.guide.default_alt_letter", "另类指示器+ (字符前缀)：显示为「N太快！/N太慢！」与「L太快！/L太慢！」");
            Add(l, "ui.guide.default_double", "空敲/失误双感叹号：若启用，显示「标判！！」/「宽判！！」或「-宽判！！」等");
            Add(l, "ui.guide.shortened", "精简判定模式 (Shortened)：");
            Add(l, "ui.guide.shortened_plus", "判定指示器+：显示为「NOR」(Normal) 与「LEN」(Lenient)");
            Add(l, "ui.guide.shortened_alt_sign", "另类指示器+ (符号)：显示为「-NOR/-LEN」(太快) 与「+NOR/+LEN」(太慢)");
            Add(l, "ui.guide.shortened_alt_letter", "另类指示器+ (字符)：显示为「NE/NL」(Normal Early/Late) 与「LE/LL」(Lenient Early/Late)");
            Add(l, "ui.guide.ms", "毫秒误差模式 (ms Difference)：");
            Add(l, "ui.guide.ms_desc", "在误差后追加等级后缀：如「-60msN」(标判及格) 与「+60msL」(宽判及格)");
            Add(l, "ui.guide.results", "结算详细结果 (Detailed Results)：");
            Add(l, "ui.guide.results_desc", "太快/太慢保持原生总计数，下方显示：标判: X     宽判: Y");

            Add(l, "ui.force_diff.toggle", "强制启用判定选择栏：在低 BPM（<220）及官方关卡中也强制解锁难度切换（可自由选择 宽松/标准/严格）");
            Add(l, "ui.section3", "3. 全局高级辅助功能 (Universal Cheats)");
            Add(l, "ui.nofail.toggle", "开启「不死模式」(No Fail) —— 官谱、BOSS关等任何情况均免死 (快捷键: Alt + N)");
            Add(l, "ui.auto.toggle", "开启「自动播放」(Autoplay) —— 官谱、任何关卡均开启自动纯完美 (快捷键: Alt + A)");
        }

        private static void InitChineseTraditional()
        {
            SystemLanguage l = SystemLanguage.ChineseTraditional;
            Add(l, "master.warning_title", "[!] 免責聲明與安全警告");
            Add(l, "master.warning_body", "嚴禁在多人遊戲中使用該模組！作者不為使用該模組導致的任何後果（如帳戶封禁、連線斷開等）承擔任何責任。");
            Add(l, "master.agree_btn", "我已知曉並同意免責聲明，開啟模組總開關");
            Add(l, "master.status_on", "模組總開關已開啟 (Master Switch: ON)");
            Add(l, "master.disable_btn", "關閉總開關");
            Add(l, "misc.section", "3. 雜項功能 (Miscellaneous)");
            Add(l, "misc.editor_toggle", "始終允許使用關卡編輯器功能 (官譜或特殊情況亦可開啟)");
            Add(l, "misc.ring_toggle", "隱藏星球判定圈 (隱藏圍繞旋轉星球的虛線圈)");
            Add(l, "misc.autoplay_plus_toggle", "啟用 自動播放+ (內部零延遲判定 + 擬人雙通道按鍵點亮 KV，自動阻斷局內按鍵干擾)");
            Add(l, "misc.autoplay_keys_label", "自動播放+ 16 鍵位面板 (左鍵點擊按鈕後按鍵盤綁定，右鍵清空，Esc 取消):");
            Add(l, "misc.autoplay_keys_hint", "上方 8 鍵與下方 8 鍵各分為 [左手 4 鍵 | 右手 4 鍵]，智慧匹配單指平點、左右食指單雙交替、四指內旋輪指與八指順滑流");
            Add(l, "misc.autoplay_preset_btn", "恢復預設 16 鍵");
            Add(l, "misc.sync_jipper_btn", "從 JipperResourcePack 一鍵同步鍵位");
            Add(l, "misc.sync_jipper_ok", "✓ 已成功從 JipperResourcePack 同步鍵位！");
            Add(l, "misc.sync_jipper_fail", "× 未找到 JipperResourcePack 鍵位配置");
            Add(l, "misc.key_unset", "未設");
            Add(l, "misc.key_waiting", "請按鍵...");
            Add(l, "misc.hand_left", "左手區 (L)");
            Add(l, "misc.hand_right", "右手區 (R)");
            Add(l, "misc.autoplay_acc_label", "擬人目標 X 精準度: {0:0.##}% (閉環控制在合法及格邊界內注入生理節奏抖動，絕不死)");
            Add(l, "hud.autoplay_plus", "自動播放+");
            
            Add(l, "mod.title", "更好的判定");
            Add(l, "stamp.mobile", "移動判定通關");
            Add(l, "stamp.custom", "自定判定通關");
            Add(l, "ui.custom.unit_label", "自定義判定模式 (Unit Mode):");
            Add(l, "ui.custom.unit_time", "時間模式 (毫秒 ms)");
            Add(l, "ui.custom.unit_angle", "角度模式 (度數 °)");
            Add(l, "ui.custom.counted_angle", "及格 Counted 角度 ({0}°): ");
            Add(l, "ui.custom.perfect_angle", "早晚完美 Perfect 角度 ({0}°): ");
            Add(l, "ui.custom.pure_angle", "偏偏完美 Pure 角度 ({0}°): ");
            Add(l, "ui.custom.mult_title", "一鍵按預設百分比乘算:");
            Add(l, "ui.custom.mult_apply", "應用百分比");
            Add(l, "ui.custom.preset_strict", "嚴格預設");
            Add(l, "ui.custom.preset_normal", "標準預設");
            Add(l, "ui.custom.preset_lenient", "寬鬆預設");
            Add(l, "ui.custom.preset_mobile", "移動端預設");
            Add(l, "ui.custom.profiles_title", "自定判定快速檔案槽位 (Profiles):");
            Add(l, "ui.custom.save_slot", "保存至槽位 {0}");
            Add(l, "ui.custom.load_slot", "載入槽位 {0}");
            Add(l, "ui.custom.free_input_hint", "（可在右側文本框自由鍵入任意數值突破滑塊範圍）");
            Add(l, "hud.mobile", "移動判定");
            Add(l, "hud.custom", "自訂判定");
            Add(l, "diff.mobile", "移動");
            Add(l, "diff.custom", "自訂");
            Add(l, "indicator.normal", "標判");
            Add(l, "indicator.lenient", "寬判");
            Add(l, "indicator.tooEarly", "太快");
            Add(l, "indicator.tooLate", "太慢");
            Add(l, "results.normal", "標判");
            Add(l, "results.lenient", "寬判");

            Add(l, "ui.language_label", "語言設定 (Language)");
            Add(l, "ui.lang.auto", "跟隨遊戲 (Auto)");
            Add(l, "ui.japanese_better_loc", "啟用「更好的日語本地化」：將原版日語下未翻譯的英文判定詞完整本地化為日語");
            Add(l, "ui.japanese_use_chinese_font", "日語使用中文字體：當處於日語時使用遊戲內中文字體渲染（替換原版日語字體，推薦開啟）");
            Add(l, "ui.border_color.toggle", "啟用「判定文字邊框染色」：為「標判！」與「寬判！」浮動文字邊框染上對應灰/綠色（關閉則保留原版高亮白金光暈）");
            Add(l, "ui.track_color.toggle", "啟用「軌道染色」：打出「標判！」與「寬判！」時，對應將腳下軌道磚塊染為灰色與綠色");

            Add(l, "mode.vanilla", "原生 PC 模式 (Vanilla)");
            Add(l, "mode.mobile", "移動端判定模式 (Mobile)");
            Add(l, "mode.custom", "自訂判定模式 (Custom)");

            Add(l, "ui.section1", "1. 判定模式選擇 (Judgment Mode Selection)");
            Add(l, "ui.mobile.desc_title", "[移動端判定模式已啟用]");
            Add(l, "ui.mobile.desc_counted", "及格 (Counted) 下限: 90 ms（PC 原生標準為 65 ms / 嚴格 40 ms）");
            Add(l, "ui.mobile.desc_perfect", "早晚完美 (Perfect) 下限: 70 ms（PC 原生為 30 ms）");
            Add(l, "ui.mobile.desc_pure", "稍偏完美 (Pure) 下限: 50 ms（PC 原生為 25 ms）");
            Add(l, "ui.mobile.desc_speed", "倍速試煉特性已合併：高倍速試煉下判定下限不會縮減（還原手機手感）");
            Add(l, "ui.mobile.desc_hud", "介面指示：局內左上角自動播放位置常駐顯示「移動判定」（真實自動播放時保留）");
            Add(l, "ui.mobile.desc_diff", "關卡難度欄：所有關卡強制常駐顯示，原「寬鬆」重定向並顯示為「移動」，鎖定不可調");

            Add(l, "ui.custom.desc_title", "[自訂判定模式設定]");
            Add(l, "ui.custom.desc_hud", "介面指示：左上角常駐顯示「自訂判定」，難度欄重定向顯示為「自訂」");
            Add(l, "ui.custom.counted", "及格 Counted 下限 ({0} ms): ");
            Add(l, "ui.custom.perfect", "早晚完美 Perfect 下限 ({0} ms): ");
            Add(l, "ui.custom.pure", "偏偏完美 Pure 下限 ({0} ms): ");
            Add(l, "ui.custom.scale_speed", "倍速試煉時按倍率縮小判定下限 (Scale with speed trial)");
            Add(l, "ui.custom.reset_mobile", "重設為官方移動端數值 (90 / 70 / 50 ms)");

            Add(l, "ui.section2", "2. PC 判定模式專屬功能 (PC Mode Features)");
            Add(l, "ui.pc_only_hint", "(以下判定指示器僅在「原生 PC 模式」下生效)");
            Add(l, "ui.indicator.toggle", "啟用「判定指示器」：打出及格 (Counted) 時重測，標判合格施加灰色描邊，寬判合格施加綠色描邊");
            Add(l, "ui.indicator.desc_normal", "標判成立 (<= 標判下限)：施加灰色描邊");
            Add(l, "ui.indicator.desc_lenient", "寬判成立 (<= 寬判下限)：施加綠色描邊");
            Add(l, "ui.indicator.desc_meter", "誤擊量計（判定條）對應打點同步加上灰/綠描邊");
            Add(l, "ui.indicator_plus.toggle", "啟用「判定指示器+」：將文字替換為「標判！」與「寬判！」（精簡模式: NOR / LEN）");

            Add(l, "ui.alt_indicator.toggle", "啟用「另類判定指示器+」：為判定文字追加早晚擊打指示（太快/太慢）");
            Add(l, "ui.alt_indicator.style_label", "另類指示器工作方式 (Alternative Indicator Style):");
            Add(l, "ui.alt_style_sign", "正負符號前綴 (預設): -寬判！(太快) / +標判！(太慢)");
            Add(l, "ui.alt_style_letter", "字符名稱前綴: L太快！/ N太慢！");

            Add(l, "ui.double_indicator.toggle", "為「太快！！」與「太慢！！」啟用判定指示器：即使在空敲或失誤斷連情況下也進行重測，符合條件時顯示「標判！！」與「寬判！！」");

            Add(l, "ui.guide.title", "判定指示器與原生判定顯示格式對照說明：");
            Add(l, "ui.guide.default", "預設文字模式 (Default)：");
            Add(l, "ui.guide.default_plus", "判定指示器+：顯示為「標判！」與「寬判！」");
            Add(l, "ui.guide.default_alt_sign", "另類指示器+ (符號前綴[預設])：顯示為「-寬判！」(太快) 與「+標判！」(太慢)");
            Add(l, "ui.guide.default_alt_letter", "另类指示器+ (字符前綴)：顯示為「N太快！/N太慢！」與「L太快！/L太慢！」");
            Add(l, "ui.guide.default_double", "空敲/失誤雙驚嘆號：若啟用，顯示「標判！！」/「寬判！！」或「-寬判！！」等");
            Add(l, "ui.guide.shortened", "精簡判定模式 (Shortened)：");
            Add(l, "ui.guide.shortened_plus", "判定指示器+：顯示為「NOR」(Normal) 與「LEN」(Lenient)");
            Add(l, "ui.guide.shortened_alt_sign", "另類指示器+ (符號)：顯示為「-NOR/-LEN」(太快) 與「+NOR/+LEN」(太慢)");
            Add(l, "ui.guide.shortened_alt_letter", "另類指示器+ (字符)：顯示為「NE/NL」(Normal Early/Late) 與「LE/LL」(Lenient Early/Late)");
            Add(l, "ui.guide.ms", "毫秒誤差模式 (ms Difference)：");
            Add(l, "ui.guide.ms_desc", "在誤差後追加等級字尾：如「-60msN」(標判及格) 與「+60msL」(寬判及格)");
            Add(l, "ui.guide.results", "結算詳細結果 (Detailed Results)：");
            Add(l, "ui.guide.results_desc", "太快/太慢保持原生總計數，下方顯示：標判: X     寬判: Y");

            Add(l, "ui.force_diff.toggle", "強制啟用判定選擇欄：在低 BPM（<220）及官方關卡中也強制解鎖難度切換（可自由選擇 寬鬆/標準/嚴格）");
            Add(l, "ui.section3", "3. 全域進階輔助功能 (Universal Cheats)");
            Add(l, "ui.nofail.toggle", "開啟「不死模式」(No Fail) —— 官譜、BOSS關等任何情況均免死 (快捷鍵: Alt + N)");
            Add(l, "ui.auto.toggle", "開啟「自動播放」(Autoplay) —— 官譜、任何關卡均開啟自動純完美 (快捷鍵: Alt + A)");
        }

        private static void InitEnglish()
        {
            SystemLanguage l = SystemLanguage.English;
            Add(l, "master.warning_title", "[!] Safety Warning & Disclaimer");
            Add(l, "master.warning_body", "Using this mod in multiplayer games is strictly prohibited! The author takes no responsibility for any consequences (such as account bans or penalties).");
            Add(l, "master.agree_btn", "I understand and agree to the disclaimer. Enable Master Switch");
            Add(l, "master.status_on", "Mod Master Switch is ON");
            Add(l, "master.disable_btn", "Disable Master Switch");
            Add(l, "misc.section", "3. Miscellaneous");
            Add(l, "misc.editor_toggle", "Always Allow Level Editor (Accessible even in official levels or special scenes)");
            Add(l, "misc.ring_toggle", "Hide Planet Judgment Ring (Hides the dotted orbit circle)");
            Add(l, "misc.autoplay_plus_toggle", "Enable Autoplay+ (Zero-latency internal hits + dual-channel human keystrokes for KV)");
            Add(l, "misc.autoplay_keys_label", "Autoplay+ 16-Key Bindings (Left-click slot & press key to bind, Right-click to clear, Esc to cancel):");
            Add(l, "misc.autoplay_keys_hint", "Each row of 8 keys is split into [Left Hand 4 | Right Hand 4], matching human index tapping, L/R alternation, 4-key & 8-key rolls");
            Add(l, "misc.autoplay_preset_btn", "Reset Default 16 Keys");
            Add(l, "misc.sync_jipper_btn", "Sync Keys from JipperResourcePack");
            Add(l, "misc.sync_jipper_ok", "✓ Successfully synced keys from JipperResourcePack!");
            Add(l, "misc.sync_jipper_fail", "× Could not find JipperResourcePack key config");
            Add(l, "misc.key_unset", "None");
            Add(l, "misc.key_waiting", "Press key...");
            Add(l, "misc.hand_left", "Left Hand (L)");
            Add(l, "misc.hand_right", "Right Hand (R)");
            Add(l, "misc.autoplay_acc_label", "Human Target X-Accuracy: {0:0.##}% (Closed-loop humanized variance within safe Counted bounds)");
            Add(l, "hud.autoplay_plus", "Autoplay+");
            
            Add(l, "mod.title", "Better Judgment");
            Add(l, "stamp.mobile", "Cleared with Mobile Judgment");
            Add(l, "stamp.custom", "Cleared with Custom Judgment");
            Add(l, "ui.custom.unit_label", "Custom Judgment Mode (Unit):");
            Add(l, "ui.custom.unit_time", "Time Mode (ms)");
            Add(l, "ui.custom.unit_angle", "Angle Mode (deg °)");
            Add(l, "ui.custom.counted_angle", "Counted Angle ({0}°): ");
            Add(l, "ui.custom.perfect_angle", "Perfect Angle ({0}°): ");
            Add(l, "ui.custom.pure_angle", "Pure Angle ({0}°): ");
            Add(l, "ui.custom.mult_title", "Preset Percentage Multiplier:");
            Add(l, "ui.custom.mult_apply", "Apply %");
            Add(l, "ui.custom.preset_strict", "Strict Preset");
            Add(l, "ui.custom.preset_normal", "Normal Preset");
            Add(l, "ui.custom.preset_lenient", "Lenient Preset");
            Add(l, "ui.custom.preset_mobile", "Mobile Preset");
            Add(l, "ui.custom.profiles_title", "Custom Judgment Profiles:");
            Add(l, "ui.custom.save_slot", "Save to Slot {0}");
            Add(l, "ui.custom.load_slot", "Load Slot {0}");
            Add(l, "ui.custom.free_input_hint", "(Type any number in text field to exceed slider bounds)");
            Add(l, "hud.mobile", "Mobile Judgment");
            Add(l, "hud.custom", "Custom Judgment");
            Add(l, "diff.mobile", "Mobile");
            Add(l, "diff.custom", "Custom");
            Add(l, "indicator.normal", "Normal");
            Add(l, "indicator.lenient", "Lenient");
            Add(l, "indicator.tooEarly", "Too Early");
            Add(l, "indicator.tooLate", "Too Late");
            Add(l, "results.normal", "Normal");
            Add(l, "results.lenient", "Lenient");

            Add(l, "ui.language_label", "Language Settings");
            Add(l, "ui.lang.auto", "Follow Game (Auto)");
            Add(l, "ui.japanese_better_loc", "Enable \"Better Japanese Localization\": Localize all untranslated English judgment texts into Japanese");
            Add(l, "ui.japanese_use_chinese_font", "Use Chinese Font in Japanese: Render Japanese text using the in-game Chinese font instead of default Japanese font");
            Add(l, "ui.border_color.toggle", "Enable \"Hit Text Border Coloring\": Color floating hit text border gray/green (disable for vanilla white/gold highlight)");
            Add(l, "ui.track_color.toggle", "Enable \"Track Coloring\": Color the stepped track tile gray on \"Normal!\" and green on \"Lenient!\"");

            Add(l, "mode.vanilla", "Vanilla PC Mode");
            Add(l, "mode.mobile", "Mobile Judgment Mode");
            Add(l, "mode.custom", "Custom Judgment Mode");

            Add(l, "ui.section1", "1. Judgment Mode Selection");
            Add(l, "ui.mobile.desc_title", "[Mobile Judgment Mode Active]");
            Add(l, "ui.mobile.desc_counted", "Counted Margin: 90 ms (Vanilla PC is 65 ms / Strict 40 ms)");
            Add(l, "ui.mobile.desc_perfect", "Perfect Margin: 70 ms (Vanilla PC is 30 ms)");
            Add(l, "ui.mobile.desc_pure", "Pure Margin: 50 ms (Vanilla PC is 25 ms)");
            Add(l, "ui.mobile.desc_speed", "Speed trial scaling disabled: limits do not shrink at high speeds (matches mobile)");
            Add(l, "ui.mobile.desc_hud", "HUD: Top-left displays \"Mobile Judgment\" (retained during real autoplay)");
            Add(l, "ui.mobile.desc_diff", "Difficulty UI: Forced visible, \"Lenient\" redirected to \"Mobile\" and locked");

            Add(l, "ui.custom.desc_title", "[Custom Judgment Mode Settings]");
            Add(l, "ui.custom.desc_hud", "HUD: Top-left displays \"Custom Judgment\", difficulty redirected to \"Custom\"");
            Add(l, "ui.custom.counted", "Counted Margin ({0} ms): ");
            Add(l, "ui.custom.perfect", "Perfect Margin ({0} ms): ");
            Add(l, "ui.custom.pure", "Pure Margin ({0} ms): ");
            Add(l, "ui.custom.scale_speed", "Scale margin limits with speed trial");
            Add(l, "ui.custom.reset_mobile", "Reset to Official Mobile Values (90 / 70 / 50 ms)");

            Add(l, "ui.section2", "2. PC Mode Features");
            Add(l, "ui.pc_only_hint", "(The hit indicators below only take effect in \"Vanilla PC Mode\")");
            Add(l, "ui.indicator.toggle", "Enable \"Hit Indicator\": Re-evaluates Counted hits; applies gray border for Normal, green for Lenient");
            Add(l, "ui.indicator.desc_normal", "Normal Pass (<= Normal limit): Gray border applied");
            Add(l, "ui.indicator.desc_lenient", "Lenient Pass (<= Lenient limit): Green border applied");
            Add(l, "ui.indicator.desc_meter", "Error meter ticks synchronously colored gray/green");
            Add(l, "ui.indicator_plus.toggle", "Enable \"Hit Indicator+\": Replaces hit text with \"Normal!\" and \"Lenient!\" (Shortened: NOR / LEN)");

            Add(l, "ui.alt_indicator.toggle", "Enable \"Alternative Indicator+\": Appends Early/Late direction to indicator text");
            Add(l, "ui.alt_indicator.style_label", "Alternative Indicator Style:");
            Add(l, "ui.alt_style_sign", "Sign Prefix (Default): -Lenient! (Early) / +Normal! (Late)");
            Add(l, "ui.alt_style_letter", "Letter Prefix: L-Early! / N-Late!");

            Add(l, "ui.double_indicator.toggle", "Enable for Double Hits (Misses/Overpress): Re-evaluates on misses; displays \"Normal!!\" & \"Lenient!!\" if eligible");

            Add(l, "ui.guide.title", "Hit Indicator & Display Presets Guide:");
            Add(l, "ui.guide.default", "Default Text Mode:");
            Add(l, "ui.guide.default_plus", "Indicator+: Displays \"Normal!\" and \"Lenient!\"");
            Add(l, "ui.guide.default_alt_sign", "Alternative+ (Sign Prefix [Default]): Displays \"-Lenient!\" (Early) & \"+Normal!\" (Late)");
            Add(l, "ui.guide.default_alt_letter", "Alternative+ (Letter Prefix): Displays \"N-Early!/N-Late!\" & \"L-Early!/L-Late!\"");
            Add(l, "ui.guide.default_double", "Double Hits (Overpress/Miss): If enabled, displays \"Normal!!\", \"Lenient!!\", \"-Lenient!!\", etc.");
            Add(l, "ui.guide.shortened", "Shortened Preset:");
            Add(l, "ui.guide.shortened_plus", "Indicator+: Displays \"NOR\" (Normal) & \"LEN\" (Lenient)");
            Add(l, "ui.guide.shortened_alt_sign", "Alternative+ (Sign): Displays \"-NOR/-LEN\" (Early) & \"+NOR/+LEN\" (Late)");
            Add(l, "ui.guide.shortened_alt_letter", "Alternative+ (Letter): Displays \"NE/NL\" (Normal Early/Late) & \"LE/LL\" (Lenient Early/Late)");
            Add(l, "ui.guide.ms", "ms Difference Preset:");
            Add(l, "ui.guide.ms_desc", "Suffixes timing with level: e.g. \"-60msN\" (Normal) & \"+60msL\" (Lenient)");
            Add(l, "ui.guide.results", "Detailed Results:");
            Add(l, "ui.guide.results_desc", "Early/Late retain total Counted hits; displays \"Normal: X     Lenient: Y\" below");

            Add(l, "ui.force_diff.toggle", "Force Difficulty Selector: Unlock difficulty selection in low BPM (<220) and official levels");
            Add(l, "ui.section3", "3. Universal Cheats & Assists");
            Add(l, "ui.nofail.toggle", "Enable \"No Fail\": Invulnerable in official levels & boss stages (Hotkey: Alt + N)");
            Add(l, "ui.auto.toggle", "Enable \"Autoplay\": Auto pure perfect on all levels (Hotkey: Alt + A)");
        }

        private static void InitKorean()
        {
            SystemLanguage l = SystemLanguage.Korean;
            Add(l, "master.warning_title", "[!] 안전 경고 및 면책 조항");
            Add(l, "master.warning_body", "멀티플레이어 게임에서 이 모드를 사용하는 것은 엄격히 금지됩니다! 모드 사용으로 인한 계정 정지 등 어떠한 결과에 대해서도 책임을 지지 않습니다.");
            Add(l, "master.agree_btn", "면책 조항에 동의하며 마스터 스위치 켜기");
            Add(l, "master.status_on", "모드 마스터 스위치 활성화됨");
            Add(l, "master.disable_btn", "마스터 스위치 끄기");
            Add(l, "misc.section", "3. 기타 기능 (Miscellaneous)");
            Add(l, "misc.editor_toggle", "항상 레벨 에디터 허용 (공식 레벨에서도 에디터 사용 가능)");
            Add(l, "misc.ring_toggle", "행성 판정 링 숨기기 (점선 원 숨김)");
            Add(l, "misc.autoplay_plus_toggle", "자동 재생+ 활성화 (KeyViewer 연동 키 시뮬레이션 및 플레이어 입력 차단)");
            Add(l, "misc.autoplay_keys_label", "허용된 키 (최대 16개, 공백 구분):");
            Add(l, "misc.autoplay_preset_btn", "기본 키 재설정");
            Add(l, "misc.autoplay_acc_label", "인간 목표 정확도: {0:0}% (현실적인 타이밍 편차 적용)");
            Add(l, "hud.autoplay_plus", "자동 재생+");
            
            Add(l, "mod.title", "더 나은 판정");
            Add(l, "stamp.mobile", "모바일 판정 클리어");
            Add(l, "stamp.custom", "커스텀 판정 클리어");
            Add(l, "ui.custom.unit_label", "판정 단위 모드:");
            Add(l, "ui.custom.unit_time", "시간 모드 (ms)");
            Add(l, "ui.custom.unit_angle", "각도 모드 (deg °)");
            Add(l, "ui.custom.counted_angle", "체크 각도 ({0}°): ");
            Add(l, "ui.custom.perfect_angle", "완벽 각도 ({0}°): ");
            Add(l, "ui.custom.pure_angle", "순정 각도 ({0}°): ");
            Add(l, "ui.custom.mult_title", "프리셋 비율 곱셈:");
            Add(l, "ui.custom.mult_apply", "비율 적용");
            Add(l, "ui.custom.preset_strict", "엄격 프리셋");
            Add(l, "ui.custom.preset_normal", "보통 프리셋");
            Add(l, "ui.custom.preset_lenient", "느슨 프리셋");
            Add(l, "ui.custom.preset_mobile", "모바일 프리셋");
            Add(l, "ui.custom.profiles_title", "커스텀 판정 프로필 슬롯:");
            Add(l, "ui.custom.save_slot", "슬롯 {0}에 저장");
            Add(l, "ui.custom.load_slot", "슬롯 {0} 불러오기");
            Add(l, "ui.custom.free_input_hint", "(슬라이더 한계를 넘어 임의의 수치를 텍스트 필드에 직접 입력 가능)");
            Add(l, "hud.mobile", "모바일 판정");
            Add(l, "hud.custom", "커스텀 판정");
            Add(l, "diff.mobile", "모바일");
            Add(l, "diff.custom", "커스텀");
            Add(l, "indicator.normal", "보통");
            Add(l, "indicator.lenient", "완화");
            Add(l, "indicator.tooEarly", "너무 빠름");
            Add(l, "indicator.tooLate", "너무 느림");
            Add(l, "results.normal", "보통");
            Add(l, "results.lenient", "완화");

            Add(l, "ui.language_label", "언어 설정 (Language)");
            Add(l, "ui.lang.auto", "게임 설정 따름 (Auto)");
            Add(l, "ui.japanese_better_loc", "「더 나은 일본어 현지화」 활성화: 원작 일본어에서 영어로 표시되는 모든 판정 텍스트를 일본어로 번역");
            Add(l, "ui.japanese_use_chinese_font", "일본어에서 중국어 폰트 사용: 일본어일 때 게임 내 중국어 폰트로 렌더링 (기본 일본어 폰트 대체)");
            Add(l, "ui.border_color.toggle", "「판정 텍스트 테두리 염색」 활성화: 판정 텍스트 테두리를 회색/초록색으로 염색 (끄면 원작 하이라이트 유지)");
            Add(l, "ui.track_color.toggle", "「트랙 염색」 활성화: 「보통！」 및 「완화！」 타격 시 트랙 타일을 각각 회색과 초록색으로 염색");

            Add(l, "mode.vanilla", "원작 PC 모드 (Vanilla)");
            Add(l, "mode.mobile", "모바일 판정 모드 (Mobile)");
            Add(l, "mode.custom", "커스텀 판정 모드 (Custom)");

            Add(l, "ui.section1", "1. 판정 모드 선택 (Judgment Mode Selection)");
            Add(l, "ui.mobile.desc_title", "[모바일 판정 모드 활성화됨]");
            Add(l, "ui.mobile.desc_counted", "체크 (Counted) 기준: 90 ms (PC 원작은 보통 65 ms / 엄격 40 ms)");
            Add(l, "ui.mobile.desc_perfect", "완벽 (Perfect) 기준: 70 ms (PC 원작은 30 ms)");
            Add(l, "ui.mobile.desc_pure", "정확 (Pure) 기준: 50 ms (PC 원작은 25 ms)");
            Add(l, "ui.mobile.desc_speed", "배속 시련 특성 통합: 고배속에서도 판정 범위가 축소되지 않음 (모바일 손맛 재현)");
            Add(l, "ui.mobile.desc_hud", "HUD 표시: 좌측 상단에 「모바일 판정」 상시 표시");
            Add(l, "ui.mobile.desc_diff", "난이도 선택창: 항상 표시되며 「완화」가 「모바일」로 리다이렉트되어 고정됨");

            Add(l, "ui.custom.desc_title", "[커스텀 판정 모드 설정]");
            Add(l, "ui.custom.desc_hud", "HUD: 좌측 상단 「커스텀 판정」 표시, 난이도 「커스텀」 고정");
            Add(l, "ui.custom.counted", "체크 Counted 기준 ({0} ms): ");
            Add(l, "ui.custom.perfect", "완벽 Perfect 기준 ({0} ms): ");
            Add(l, "ui.custom.pure", "정확 Pure 기준 ({0} ms): ");
            Add(l, "ui.custom.scale_speed", "배속 시련 시 판정 범위 축소");
            Add(l, "ui.custom.reset_mobile", "공식 모바일 수치로 재설정 (90 / 70 / 50 ms)");

            Add(l, "ui.section2", "2. PC 모드 전용 기능 (PC Mode Features)");
            Add(l, "ui.pc_only_hint", "(아래 인디케이터는 「원작 PC 모드」에서만 동작합니다)");
            Add(l, "ui.indicator.toggle", "「판정 인디케이터」 활성화: Counted 타격 시 재측정하여 보통 합격은 회색, 완화 합격은 초록색 테두리 적용");
            Add(l, "ui.indicator.desc_normal", "보통 판정 합격: 회색 테두리");
            Add(l, "ui.indicator.desc_lenient", "완화 판정 합격: 초록색 테두리");
            Add(l, "ui.indicator.desc_meter", "오차 측정 게이지(판정바)의 눈금에도 동일 색상 테두리 적용");
            Add(l, "ui.indicator_plus.toggle", "「판정 인디케이터+」 활성화: 타격 텍스트를 「보통！」 및 「완화！」로 변경 (단축: NOR / LEN)");

            Add(l, "ui.alt_indicator.toggle", "「대체 인디케이터+」 활성화: 판정 텍스트에 빠름/느림 방향 표시 추가");
            Add(l, "ui.alt_indicator.style_label", "대체 인디케이터 작동 방식:");
            Add(l, "ui.alt_style_sign", "부호 접두사 (기본값): -완화！(빠름) / +보통！(느림)");
            Add(l, "ui.alt_style_letter", "문자 접두사: L빠름！ / N느림！");

            Add(l, "ui.double_indicator.toggle", "미스 및 연타 오류에 인디케이터 활성화: 탈락 시에도 재측정하여 「보통！！」, 「완화！！」 표시");

            Add(l, "ui.guide.title", "판정 인디케이터 및 표시 형식 안내:");
            Add(l, "ui.guide.default", "기본 텍스트 모드:");
            Add(l, "ui.guide.default_plus", "인디케이터+: 「보통！」 및 「완화！」 표시");
            Add(l, "ui.guide.default_alt_sign", "대체+ (부호 접두사[기본값]): 「-완화！」(빠름) 및 「+보통！」(느림)");
            Add(l, "ui.guide.default_alt_letter", "대체+ (문자 접두사): 「N빠름！/N느림！」 및 「L빠름！/L느림！」");
            Add(l, "ui.guide.default_double", "미스 더블 느낌표: 활성화 시 「보통！！」, 「완화！！」, 「-완화！！」 등 표시");
            Add(l, "ui.guide.shortened", "단축 모드 (Shortened):");
            Add(l, "ui.guide.shortened_plus", "인디케이터+: 「NOR」(보통) 및 「LEN」(완화)");
            Add(l, "ui.guide.shortened_alt_sign", "대체+ (부호): 「-NOR/-LEN」(빠름) 및 「+NOR/+LEN」(느림)");
            Add(l, "ui.guide.shortened_alt_letter", "대체+ (문자): 「NE/NL」 및 「LE/LL」");
            Add(l, "ui.guide.ms", "밀리초 오차 모드 (ms Difference):");
            Add(l, "ui.guide.ms_desc", "오차 뒤에 등급 접미사 추가: 예) 「-60msN」(보통) 및 「+60msL」(완화)");
            Add(l, "ui.guide.results", "결과 상세 통계 (Detailed Results):");
            Add(l, "ui.guide.results_desc", "원작 빠름/느림 총합 유지, 아래에 보통: X     완화: Y 별도 표시");

            Add(l, "ui.force_diff.toggle", "난이도 선택창 강제 표시: 저 BPM 및 공식 레벨에서도 난이도 선택 잠금 해제");
            Add(l, "ui.section3", "3. 범용 보조 및 치트 기능 (Universal Cheats)");
            Add(l, "ui.nofail.toggle", "「노페일 모드」(No Fail) 활성화: 공식 레벨, 보스전 등에서 사망 방지 (단축키: Alt + N)");
            Add(l, "ui.auto.toggle", "「자동 재생」(Autoplay) 활성화: 모든 레벨에서 자동 순수 완벽 연주 (단축키: Alt + A)");
        }

        private static void InitJapanese()
        {
            SystemLanguage l = SystemLanguage.Japanese;
            Add(l, "master.warning_title", "[!] 安全警告と免責事項");
            Add(l, "master.warning_body", "マルチプレイヤーでの本MODの使用は固く禁止されています！アカウント停止等のいかなる結果についても作者は責任を負いません。");
            Add(l, "master.agree_btn", "免責事項に同意してマスタースイッチを有効化");
            Add(l, "master.status_on", "MODマスタースイッチは有効です");
            Add(l, "master.disable_btn", "マスタースイッチを無効化");
            Add(l, "misc.section", "3. その他機能 (Miscellaneous)");
            Add(l, "misc.editor_toggle", "常にレベルエディタの使用を許可 (公式ステージでも使用可能)");
            Add(l, "misc.ring_toggle", "惑星の判定リングを非表示 (点線の円を非表示)");
            Add(l, "misc.autoplay_plus_toggle", "オートプレイ+を有効化 (KeyViewer対応のキーストローク模擬 & プレイヤー入力遮断)");
            Add(l, "misc.autoplay_keys_label", "使用可能なキー (最大16個、空白区切り):");
            Add(l, "misc.autoplay_preset_btn", "プリセットキーに戻す");
            Add(l, "misc.autoplay_acc_label", "目標精度: {0:0}% (人間らしい反応ジッターを付与)");
            Add(l, "hud.autoplay_plus", "オートプレイ+");
            
            Add(l, "mod.title", "より良い判定");
            Add(l, "stamp.mobile", "モバイル判定クリア");
            Add(l, "stamp.custom", "カスタム判定クリア");
            Add(l, "ui.custom.unit_label", "判定単位モード:");
            Add(l, "ui.custom.unit_time", "時間モード (ms)");
            Add(l, "ui.custom.unit_angle", "角度モード (deg °)");
            Add(l, "ui.custom.counted_angle", "Counted 角度 ({0}°): ");
            Add(l, "ui.custom.perfect_angle", "Perfect 角度 ({0}°): ");
            Add(l, "ui.custom.pure_angle", "Pure 角度 ({0}°): ");
            Add(l, "ui.custom.mult_title", "プリセット倍率一括乗算:");
            Add(l, "ui.custom.mult_apply", "倍率適用");
            Add(l, "ui.custom.preset_strict", "厳しいプリセット");
            Add(l, "ui.custom.preset_normal", "通常プリセット");
            Add(l, "ui.custom.preset_lenient", "優しいプリセット");
            Add(l, "ui.custom.preset_mobile", "モバイルプリセット");
            Add(l, "ui.custom.profiles_title", "カスタムプロファイルスロット:");
            Add(l, "ui.custom.save_slot", "スロット {0} に保存");
            Add(l, "ui.custom.load_slot", "スロット {0} を読込");
            Add(l, "ui.custom.free_input_hint", "(テキスト入力でスライダー範囲を超えた自由な数値を設定可能)");
            Add(l, "hud.mobile", "モバイル判定");
            Add(l, "hud.custom", "カスタム判定");
            Add(l, "diff.mobile", "モバイル");
            Add(l, "diff.custom", "カスタム");
            Add(l, "indicator.normal", "通常");
            Add(l, "indicator.lenient", "優しい");
            Add(l, "indicator.tooEarly", "早すぎ");
            Add(l, "indicator.tooLate", "遅すぎ");
            Add(l, "results.normal", "通常");
            Add(l, "results.lenient", "優しい");

            Add(l, "ui.language_label", "言語設定 (Language)");
            Add(l, "ui.lang.auto", "ゲームに合わせる (Auto)");
            Add(l, "ui.japanese_better_loc", "「より良い日本語ローカライズ」を有効化: バニラでは英語のままの判定表示やReady/Goを自然な日本語に翻訳");
            Add(l, "ui.japanese_use_chinese_font", "日本語で中国語フォントを使用: 日本語表示時にゲーム内の中国語フォントを使用（標準の日本語フォントを置換・推奨）");
            Add(l, "ui.border_color.toggle", "「判定文字の枠線着色」を有効化: 判定文字の枠線をグレー/緑に着色（オフでバニラの白金ハイライト枠）");
            Add(l, "ui.track_color.toggle", "「トラック着色」を有効化: 通常合格時にタイルをグレー、優しい合格時に緑色に着色");

            Add(l, "mode.vanilla", "バニラ PC モード (Vanilla)");
            Add(l, "mode.mobile", "モバイル判定モード (Mobile)");
            Add(l, "mode.custom", "カスタム判定モード (Custom)");

            Add(l, "ui.section1", "1. 判定モード選択 (Judgment Mode Selection)");
            Add(l, "ui.mobile.desc_title", "[モバイル判定モード有効]");
            Add(l, "ui.mobile.desc_counted", "合格 (Counted) 範囲: 90 ms (PC通常は65ms / 厳しい40ms)");
            Add(l, "ui.mobile.desc_perfect", "早遅パーフェクト (Perfect) 範囲: 70 ms (PC通常は30ms)");
            Add(l, "ui.mobile.desc_pure", "真パーフェクト (Pure) 範囲: 50 ms (PC通常は25ms)");
            Add(l, "ui.mobile.desc_speed", "倍速試練特性統合: 高倍速でも判定幅が縮小されません（スマホ版の感覚）");
            Add(l, "ui.mobile.desc_hud", "HUD表示: 左上に「モバイル判定」が常時表示されます");
            Add(l, "ui.mobile.desc_diff", "難易度バー: 常時表示され、「優しい」が「モバイル」に固定されます");

            Add(l, "ui.custom.desc_title", "[カスタム判定モード設定]");
            Add(l, "ui.custom.desc_hud", "HUD: 左上に「カスタム判定」、難易度バーに「カスタム」表示");
            Add(l, "ui.custom.counted", "合格 Counted 範囲 ({0} ms): ");
            Add(l, "ui.custom.perfect", "早遅パーフェクト Perfect 範囲 ({0} ms): ");
            Add(l, "ui.custom.pure", "真パーフェクト Pure 範囲 ({0} ms): ");
            Add(l, "ui.custom.scale_speed", "倍速試練時に判定範囲を縮小する");
            Add(l, "ui.custom.reset_mobile", "公式モバイル数値にリセット (90 / 70 / 50 ms)");

            Add(l, "ui.section2", "2. PC モード専用機能 (PC Mode Features)");
            Add(l, "ui.pc_only_hint", "(以下のインジケーターは「バニラ PC モード」でのみ動作します)");
            Add(l, "ui.indicator.toggle", "「判定インジケーター」を有効化: Counted 打鍵時に再判定し、通常合格にグレー枠、優しい合格に緑枠を表示");
            Add(l, "ui.indicator.desc_normal", "通常判定合格: グレーの枠線");
            Add(l, "ui.indicator.desc_lenient", "優しい判定合格: 緑色の枠線");
            Add(l, "ui.indicator.desc_meter", "判定バーの目盛りにも対応する色の枠線を表示");
            Add(l, "ui.indicator_plus.toggle", "「判定インジケーター+」を有効化: テキストを「通常！」と「優しい！」に変更 (短縮: NOR / LEN)");

            Add(l, "ui.alt_indicator.toggle", "「オルタナティブインジケーター+」を有効化: 判定文字に進み/遅れ方向を表示");
            Add(l, "ui.alt_indicator.style_label", "オルタナティブインジケーターの動作形式:");
            Add(l, "ui.alt_style_sign", "符号プレフィックス (デフォルト): -優しい！(早すぎ) / +通常！(遅すぎ)");
            Add(l, "ui.alt_style_letter", "文字プレフィックス: L早すぎ！ / N遅すぎ！");

            Add(l, "ui.double_indicator.toggle", "ミス・空打ち判定インジケーター: ミス時にも再判定し「通常！！」「優しい！！」を表示");

            Add(l, "ui.guide.title", "判定インジケーター表示形式ガイド:");
            Add(l, "ui.guide.default", "デフォルト文字モード:");
            Add(l, "ui.guide.default_plus", "インジケーター+: 「通常！」および「優しい！」表示");
            Add(l, "ui.guide.default_alt_sign", "オルタナティブ+ (符号[デフォルト]): 「-優しい！」(早すぎ) および 「+通常！」(遅すぎ)");
            Add(l, "ui.guide.default_alt_letter", "オルタナティブ+ (文字): 「N早すぎ！/N遅すぎ！」および「L早すぎ！/L遅すぎ！」");
            Add(l, "ui.guide.default_double", "ミスダブル感嘆符: 有効時、「通常！！」「優しい！！」「-優しい！！」等を表示");
            Add(l, "ui.guide.shortened", "短縮モード (Shortened):");
            Add(l, "ui.guide.shortened_plus", "インジケーター+: 「NOR」(通常) および 「LEN」(優しい)");
            Add(l, "ui.guide.shortened_alt_sign", "オルタナティブ+ (符号): 「-NOR/-LEN」(早) および 「+NOR/+LEN」(遅)");
            Add(l, "ui.guide.shortened_alt_letter", "オルタナティブ+ (文字): 「NE/NL」および「LE/LL」");
            Add(l, "ui.guide.ms", "ミリ秒誤差モード (ms Difference):");
            Add(l, "ui.guide.ms_desc", "誤差に判定接尾辞を追加: 例「-60msN」(通常) および「+60msL」(優しい)");
            Add(l, "ui.guide.results", "詳細リザルト統計 (Detailed Results):");
            Add(l, "ui.guide.results_desc", "早すぎ/遅すぎの総数を維持し、下に「通常: X     優しい: Y」を追加表示");

            Add(l, "ui.force_diff.toggle", "難易度選択バーを強制表示: 低BPMや公式ステージでも難易度変更をロック解除");
            Add(l, "ui.section3", "3. ユニバーサルアシスト機能 (Universal Cheats)");
            Add(l, "ui.nofail.toggle", "「ノーフェイル」(No Fail) を有効化: 公式面やボス面でも無敵 (ショートカット: Alt + N)");
            Add(l, "ui.auto.toggle", "「オートプレイ」(Autoplay) を有効化: 全ステージで自動ピュアパーフェクト (ショートカット: Alt + A)");
        }

        private static void InitSpanish()
        {
            SystemLanguage l = SystemLanguage.Spanish;
            Add(l, "master.warning_title", "[!] Advertencia de seguridad y descargo de responsabilidad");
            Add(l, "master.warning_body", "¡Está estrictamente prohibido usar este mod en multijugador! El autor no se hace responsable de consecuencias como bloqueos de cuenta.");
            Add(l, "master.agree_btn", "Acepto el descargo. Activar interruptor maestro");
            Add(l, "master.status_on", "Interruptor maestro activado");
            Add(l, "master.disable_btn", "Desactivar interruptor maestro");
            Add(l, "misc.section", "3. Miscelánea");
            Add(l, "misc.editor_toggle", "Permitir siempre el editor de niveles (incluso en niveles oficiales)");
            Add(l, "misc.ring_toggle", "Ocultar anillo de juicio del planeta (oculta el círculo punteado)");
            Add(l, "misc.autoplay_plus_toggle", "Activar Reproducción Automática+ (simula pulsaciones de teclas para KeyViewer)");
            Add(l, "misc.autoplay_keys_label", "Teclas permitidas (máx 16, separadas por espacio):");
            Add(l, "misc.autoplay_preset_btn", "Restablecer teclas predeterminadas");
            Add(l, "misc.autoplay_acc_label", "Precisión humana objetivo: {0:0}% (agrega variación de reacción)");
            Add(l, "hud.autoplay_plus", "Auto+");
            
            Add(l, "mod.title", "Mejor Juicio");
            Add(l, "stamp.mobile", "Completado con Juicio Móvil");
            Add(l, "stamp.custom", "Completado con Juicio Personalizado");
            Add(l, "ui.custom.unit_label", "Modo de Unidad de Juicio:");
            Add(l, "ui.custom.unit_time", "Modo Tiempo (ms)");
            Add(l, "ui.custom.unit_angle", "Modo Ángulo (grados °)");
            Add(l, "ui.custom.counted_angle", "Ángulo Contado ({0}°): ");
            Add(l, "ui.custom.perfect_angle", "Ángulo Perfecto ({0}°): ");
            Add(l, "ui.custom.pure_angle", "Ángulo Puro ({0}°): ");
            Add(l, "ui.custom.mult_title", "Multiplicador de Porcentaje:");
            Add(l, "ui.custom.mult_apply", "Aplicar %");
            Add(l, "ui.custom.preset_strict", "Preset Estricto");
            Add(l, "ui.custom.preset_normal", "Preset Normal");
            Add(l, "ui.custom.preset_lenient", "Preset Indulgente");
            Add(l, "ui.custom.preset_mobile", "Preset Móvil");
            Add(l, "ui.custom.profiles_title", "Ranuras de Perfil:");
            Add(l, "ui.custom.save_slot", "Guardar en Ranura {0}");
            Add(l, "ui.custom.load_slot", "Cargar Ranura {0}");
            Add(l, "ui.custom.free_input_hint", "(Escriba cualquier número en el campo de texto)");
            Add(l, "hud.mobile", "Juicio Móvil");
            Add(l, "hud.custom", "Juicio Personalizado");
            Add(l, "diff.mobile", "Móvil");
            Add(l, "diff.custom", "Personalizado");
            Add(l, "indicator.normal", "Normal");
            Add(l, "indicator.lenient", "Tolerante");
            Add(l, "indicator.tooEarly", "Muy Rápido");
            Add(l, "indicator.tooLate", "Muy Lento");
            Add(l, "results.normal", "Normal");
            Add(l, "results.lenient", "Tolerante");

            Add(l, "ui.language_label", "Configuración de Idioma");
            Add(l, "ui.lang.auto", "Seguir Juego (Auto)");

            Add(l, "mode.vanilla", "Modo PC Original (Vanilla)");
            Add(l, "mode.mobile", "Modo de Juicio Móvil");
            Add(l, "mode.custom", "Modo de Juicio Personalizado");

            Add(l, "ui.section1", "1. Selección del Modo de Juicio");
            Add(l, "ui.mobile.desc_title", "[Modo de Juicio Móvil Activado]");
            Add(l, "ui.mobile.desc_counted", "Límite Counted: 90 ms (PC Original es 65 ms / Estricto 40 ms)");
            Add(l, "ui.mobile.desc_perfect", "Límite Perfect: 70 ms (PC Original es 30 ms)");
            Add(l, "ui.mobile.desc_pure", "Límite Pure: 50 ms (PC Original es 25 ms)");
            Add(l, "ui.mobile.desc_speed", "Escala de prueba de velocidad desactivada (igual que en móvil)");
            Add(l, "ui.mobile.desc_hud", "HUD: Arriba a la izquierda muestra \"Juicio Móvil\"");
            Add(l, "ui.mobile.desc_diff", "Selector de dificultad: Fijo en \"Móvil\"");

            Add(l, "ui.custom.desc_title", "[Ajustes de Juicio Personalizado]");
            Add(l, "ui.custom.desc_hud", "HUD: Muestra \"Juicio Personalizado\"");
            Add(l, "ui.custom.counted", "Límite Counted ({0} ms): ");
            Add(l, "ui.custom.perfect", "Límite Perfect ({0} ms): ");
            Add(l, "ui.custom.pure", "Límite Pure ({0} ms): ");
            Add(l, "ui.custom.scale_speed", "Reducir límites en pruebas de velocidad");
            Add(l, "ui.custom.reset_mobile", "Restablecer a valores móviles oficiales (90 / 70 / 50 ms)");

            Add(l, "ui.section2", "2. Funciones del Modo PC");
            Add(l, "ui.pc_only_hint", "(Los siguientes indicadores solo funcionan en el \"Modo PC Original\")");
            Add(l, "ui.indicator.toggle", "Activar \"Indicador de Impacto\": Reevalúa los aciertos; borde gris para Normal, verde para Tolerante");
            Add(l, "ui.indicator.desc_normal", "Pasa Normal: Borde gris");
            Add(l, "ui.indicator.desc_lenient", "Pasa Tolerante: Borde verde");
            Add(l, "ui.indicator.desc_meter", "Marcas del medidor de error sincronizadas en gris/verde");
            Add(l, "ui.indicator_plus.toggle", "Activar \"Indicador de Impacto+\": Cambia el texto a \"Normal!\" y \"Tolerante!\" (Abrev: NOR / LEN)");

            Add(l, "ui.alt_indicator.toggle", "Activar \"Indicador Alternativo+\": Añade dirección rápido/lento");
            Add(l, "ui.alt_indicator.style_label", "Estilo de Indicador Alternativo:");
            Add(l, "ui.alt_style_sign", "Prefijo de Signo (Por defecto): -Tolerante! (Rápido) / +Normal! (Lento)");
            Add(l, "ui.alt_style_letter", "Prefijo de Letra: L-Rápido! / N-Lento!");

            Add(l, "ui.double_indicator.toggle", "Activar para Dobles Golpes/Fallos: Muestra \"Normal!!\" y \"Tolerante!!\"");

            Add(l, "ui.guide.title", "Guía de formatos del Indicador:");
            Add(l, "ui.guide.default", "Modo Texto Estándar:");
            Add(l, "ui.guide.default_plus", "Indicador+: Muestra \"Normal!\" y \"Tolerante!\"");
            Add(l, "ui.guide.default_alt_sign", "Alternativo+ (Signo [Por defecto]): \"-Tolerante!\" y \"+Normal!\"");
            Add(l, "ui.guide.default_alt_letter", "Alternativo+ (Letra): \"N-Rápido!/N-Lento!\" y \"L-Rápido!/L-Lento!\"");
            Add(l, "ui.guide.default_double", "Doble Exclamación: Muestra \"Normal!!\", \"Tolerante!!\", etc.");
            Add(l, "ui.guide.shortened", "Modo Abreviado (Shortened):");
            Add(l, "ui.guide.shortened_plus", "Indicador+: \"NOR\" (Normal) y \"LEN\" (Tolerante)");
            Add(l, "ui.guide.shortened_alt_sign", "Alternativo+ (Signo): \"-NOR/-LEN\" y \"+NOR/+LEN\"");
            Add(l, "ui.guide.shortened_alt_letter", "Alternativo+ (Letra): \"NE/NL\" y \"LE/LL\"");
            Add(l, "ui.guide.ms", "Modo Diferencia ms:");
            Add(l, "ui.guide.ms_desc", "Sufijo con nivel: ej. \"-60msN\" (Normal) y \"+60msL\" (Tolerante)");
            Add(l, "ui.guide.results", "Resultados Detallados:");
            Add(l, "ui.guide.results_desc", "Mantiene total de aciertos; añade abajo Normal: X     Tolerante: Y");

            Add(l, "ui.force_diff.toggle", "Forzar selector de dificultad en bajo BPM y niveles oficiales");
            Add(l, "ui.section3", "3. Modificadores y Asistencias Universales");
            Add(l, "ui.nofail.toggle", "Activar \"No Fail\" (Modo Inmortal) (Atajo: Alt + N)");
            Add(l, "ui.auto.toggle", "Activar \"Autoplay\" (Reproducción Automática) (Atajo: Alt + A)");
        }

        private static void InitRussian()
        {
            SystemLanguage l = SystemLanguage.Russian;
            Add(l, "master.warning_title", "[!] Предупреждение о безопасности");
            Add(l, "master.warning_body", "Использование этого мода в сетевой игре строго запрещено! Автор не несет ответственности за возможные блокировки аккаунта.");
            Add(l, "master.agree_btn", "Я принимаю условия. Включить мастер-переключатель");
            Add(l, "master.status_on", "Мастер-переключатель включен");
            Add(l, "master.disable_btn", "Выключить мастер-переключатель");
            Add(l, "misc.section", "3. Разное");
            Add(l, "misc.editor_toggle", "Всегда разрешать редактор уровней (даже на официальных уровнях)");
            Add(l, "misc.ring_toggle", "Скрыть кольцо суждения планет (пунктирный круг)");
            Add(l, "misc.autoplay_plus_toggle", "Включить Автовоспроизведение+ (эмуляция клавиш для KeyViewer)");
            Add(l, "misc.autoplay_keys_label", "Разрешенные клавиши (макс. 16, через пробел):");
            Add(l, "misc.autoplay_preset_btn", "Сбросить клавиши по умолчанию");
            Add(l, "misc.autoplay_acc_label", "Человеческая точность: {0:0}% (добавляет погрешность)");
            Add(l, "hud.autoplay_plus", "Авто+");
            
            Add(l, "mod.title", "Улучшенное судейство");
            Add(l, "stamp.mobile", "Пройдено с мобильным судейством");
            Add(l, "stamp.custom", "Пройдено с кастомным судейством");
            Add(l, "ui.custom.unit_label", "Режим Единиц Судейства:");
            Add(l, "ui.custom.unit_time", "Режим Времени (мс)");
            Add(l, "ui.custom.unit_angle", "Режим Угла (градусы °)");
            Add(l, "ui.custom.counted_angle", "Угол Зачета ({0}°): ");
            Add(l, "ui.custom.perfect_angle", "Угол Идеально ({0}°): ");
            Add(l, "ui.custom.pure_angle", "Угол Чисто ({0}°): ");
            Add(l, "ui.custom.mult_title", "Процентный Умножитель:");
            Add(l, "ui.custom.mult_apply", "Применить %");
            Add(l, "ui.custom.preset_strict", "Строгий Пресет");
            Add(l, "ui.custom.preset_normal", "Обычный Пресет");
            Add(l, "ui.custom.preset_lenient", "Мягкий Пресет");
            Add(l, "ui.custom.preset_mobile", "Мобильный Пресет");
            Add(l, "ui.custom.profiles_title", "Слоты Профилей:");
            Add(l, "ui.custom.save_slot", "Сохранить в Слот {0}");
            Add(l, "ui.custom.load_slot", "Загрузить Слот {0}");
            Add(l, "ui.custom.free_input_hint", "(Введите любое число в текстовое поле)");
            Add(l, "hud.mobile", "Мобильное судейство");
            Add(l, "hud.custom", "Особое судейство");
            Add(l, "diff.mobile", "Мобильный");
            Add(l, "diff.custom", "Особый");
            Add(l, "indicator.normal", "Норма");
            Add(l, "indicator.lenient", "Мягкий");
            Add(l, "indicator.tooEarly", "Рано");
            Add(l, "indicator.tooLate", "Поздно");
            Add(l, "results.normal", "Норма");
            Add(l, "results.lenient", "Мягкий");

            Add(l, "ui.language_label", "Настройки языка");
            Add(l, "ui.lang.auto", "Как в игре (Auto)");

            Add(l, "mode.vanilla", "Оригинальный режим ПК (Vanilla)");
            Add(l, "mode.mobile", "Мобильный режим судейства");
            Add(l, "mode.custom", "Пользовательский режим судейства");

            Add(l, "ui.section1", "1. Выбор режима судейства");
            Add(l, "ui.mobile.desc_title", "[Мобильный режим судейства активен]");
            Add(l, "ui.mobile.desc_counted", "Окно зачёта: 90 мс (ПК норма: 65 мс / строго: 40 мс)");
            Add(l, "ui.mobile.desc_perfect", "Окно идеально: 70 мс (ПК: 30 мс)");
            Add(l, "ui.mobile.desc_pure", "Окно точно: 50 мс (ПК: 25 мс)");
            Add(l, "ui.mobile.desc_speed", "Масштабирование скорости отключено (как на мобильных устройствах)");
            Add(l, "ui.mobile.desc_hud", "HUD: Слева вверху отображается «Мобильное судейство»");
            Add(l, "ui.mobile.desc_diff", "Панель сложности: Зафиксирована на «Мобильный»");

            Add(l, "ui.custom.desc_title", "[Настройки пользовательского судейства]");
            Add(l, "ui.custom.desc_hud", "HUD: Отображает «Особое судейство»");
            Add(l, "ui.custom.counted", "Окно зачёта ({0} мс): ");
            Add(l, "ui.custom.perfect", "Окно идеально ({0} мс): ");
            Add(l, "ui.custom.pure", "Окно точно ({0} мс): ");
            Add(l, "ui.custom.scale_speed", "Уменьшать окна при ускорении");
            Add(l, "ui.custom.reset_mobile", "Сброс к официальным мобильным значениям (90 / 70 / 50 мс)");

            Add(l, "ui.section2", "2. Функции режима ПК");
            Add(l, "ui.pc_only_hint", "(Индикаторы ниже работают только в оригинальном режиме ПК)");
            Add(l, "ui.indicator.toggle", "Включить «Индикатор судейства»: серая обводка для нормы, зелёная для мягкого");
            Add(l, "ui.indicator.desc_normal", "Норма: серая обводка");
            Add(l, "ui.indicator.desc_lenient", "Мягкий: зелёная обводка");
            Add(l, "ui.indicator.desc_meter", "Синхронизация шкалы ошибок в серый/зелёный цвет");
            Add(l, "ui.indicator_plus.toggle", "Включить «Индикатор судейства+»: текст заменяется на «Норма!» и «Мягкий!» (Кратко: NOR / LEN)");

            Add(l, "ui.alt_indicator.toggle", "Включить «Альтернативный индикатор+»: направление рано/поздно");
            Add(l, "ui.alt_indicator.style_label", "Стиль альтернативного индикатора:");
            Add(l, "ui.alt_style_sign", "Префикс знака (по умолч.): -Мягкий! (Рано) / +Норма! (Поздно)");
            Add(l, "ui.alt_style_letter", "Буквенный префикс: L-Рано! / N-Поздно!");

            Add(l, "ui.double_indicator.toggle", "Индикатор промахов: пересчитывает ошибки и показывает «Норма!!» / «Мягкий!!»");

            Add(l, "ui.guide.title", "Справка по форматам индикатора:");
            Add(l, "ui.guide.default", "Обычный текстовый режим:");
            Add(l, "ui.guide.default_plus", "Индикатор+: «Норма!» и «Мягкий!»");
            Add(l, "ui.guide.default_alt_sign", "Альтернативный+ (знак): «-Мягкий!» (рано) и «+Норма!» (поздно)");
            Add(l, "ui.guide.default_alt_letter", "Альтернативный+ (буквы): «N-Рано!/N-Поздно!» и «L-Рано!/L-Поздно!»");
            Add(l, "ui.guide.default_double", "Двойной восклицательный знак: «Норма!!», «Мягкий!!» и т.д.");
            Add(l, "ui.guide.shortened", "Краткий режим (Shortened):");
            Add(l, "ui.guide.shortened_plus", "Индикатор+: «NOR» и «LEN»");
            Add(l, "ui.guide.shortened_alt_sign", "Альтернативный+ (знак): «-NOR/-LEN» и «+NOR/+LEN»");
            Add(l, "ui.guide.shortened_alt_letter", "Альтернативный+ (буквы): «NE/NL» и «LE/LL»");
            Add(l, "ui.guide.ms", "Режим миллисекунд (ms):");
            Add(l, "ui.guide.ms_desc", "Суффикс уровня: напр. «-60msN» (Норма) и «+60msL» (Мягкий)");
            Add(l, "ui.guide.results", "Подробные результаты:");
            Add(l, "ui.guide.results_desc", "Сохраняет общий счёт; ниже выводит Норма: X     Мягкий: Y");

            Add(l, "ui.force_diff.toggle", "Принудительный выбор сложности на низком BPM и в официальных уровнях");
            Add(l, "ui.section3", "3. Универсальные читы и помощники");
            Add(l, "ui.nofail.toggle", "Включить «Бессмертие» (No Fail) (Горячая клавиша: Alt + N)");
            Add(l, "ui.auto.toggle", "Включить «Автоигру» (Autoplay) (Горячая клавиша: Alt + A)");
        }

        private static void InitGerman()
        {
            SystemLanguage l = SystemLanguage.German;
            Add(l, "master.warning_title", "[!] Sicherheitswarnung & Haftungsausschluss");
            Add(l, "master.warning_body", "Die Nutzung dieser Mod im Mehrspielermodus ist strengstens untersagt! Der Autor übernimmt keine Haftung für Kontosperren.");
            Add(l, "master.agree_btn", "Haftungsausschluss akzeptieren & Hauptschalter aktivieren");
            Add(l, "master.status_on", "Hauptschalter ist AKTIV");
            Add(l, "master.disable_btn", "Hauptschalter deaktivieren");
            Add(l, "misc.section", "3. Sonstiges");
            Add(l, "misc.editor_toggle", "Leveleditor immer erlauben (auch in offiziellen Leveln)");
            Add(l, "misc.ring_toggle", "Planeten-Urteilsring ausblenden (gestrichelte Linie)");
            Add(l, "misc.autoplay_plus_toggle", "Autoplay+ aktivieren (Tastensimulation für KeyViewer)");
            Add(l, "misc.autoplay_keys_label", "Erlaubte Tasten (max 16, getrennt durch Leerzeichen):");
            Add(l, "misc.autoplay_preset_btn", "Tasten zurücksetzen");
            Add(l, "misc.autoplay_acc_label", "Menschliche Zielgenauigkeit: {0:0}% (Reaktionsjitter)");
            Add(l, "hud.autoplay_plus", "Autoplay+");
            
            Add(l, "mod.title", "Bessere Beurteilung");
            Add(l, "stamp.mobile", "Mit Mobiler Beurteilung abgeschlossen");
            Add(l, "stamp.custom", "Mit benutzerdefinierter Beurteilung abgeschlossen");
            Add(l, "ui.custom.unit_label", "Beurteilungs-Einheitsmodus:");
            Add(l, "ui.custom.unit_time", "Zeitmodus (ms)");
            Add(l, "ui.custom.unit_angle", "Winkelmodus (Grad °)");
            Add(l, "ui.custom.counted_angle", "Gezählter Winkel ({0}°): ");
            Add(l, "ui.custom.perfect_angle", "Perfekter Winkel ({0}°): ");
            Add(l, "ui.custom.pure_angle", "Reiner Winkel ({0}°): ");
            Add(l, "ui.custom.mult_title", "Prozent-Multiplikator:");
            Add(l, "ui.custom.mult_apply", "% Anwenden");
            Add(l, "ui.custom.preset_strict", "Streng-Vorlage");
            Add(l, "ui.custom.preset_normal", "Normal-Vorlage");
            Add(l, "ui.custom.preset_lenient", "Tolerant-Vorlage");
            Add(l, "ui.custom.preset_mobile", "Mobil-Vorlage");
            Add(l, "ui.custom.profiles_title", "Benutzerdefinierte Profile:");
            Add(l, "ui.custom.save_slot", "Speichern auf Slot {0}");
            Add(l, "ui.custom.load_slot", "Laden von Slot {0}");
            Add(l, "ui.custom.free_input_hint", "(Beliebigen Wert in das Textfeld eingeben)");
            Add(l, "hud.mobile", "Mobiles Urteil");
            Add(l, "hud.custom", "Benutzerdefiniertes Urteil");
            Add(l, "diff.mobile", "Mobil");
            Add(l, "diff.custom", "Benutzerdefiniert");
            Add(l, "indicator.normal", "Normal");
            Add(l, "indicator.lenient", "Nachsichtig");
            Add(l, "indicator.tooEarly", "Zu Früh");
            Add(l, "indicator.tooLate", "Zu Spät");
            Add(l, "results.normal", "Normal");
            Add(l, "results.lenient", "Nachsichtig");
            Add(l, "ui.language_label", "Spracheinstellungen");
            Add(l, "ui.lang.auto", "Dem Spiel folgen (Auto)");
            Add(l, "mode.vanilla", "Vanilla PC-Modus");
            Add(l, "mode.mobile", "Mobiles Urteilsmodus");
            Add(l, "mode.custom", "Benutzerdefinierter Urteilsmodus");
            Add(l, "ui.section1", "1. Auswahl des Urteilsmodus");
            Add(l, "ui.section2", "2. PC-Modus-Funktionen");
            Add(l, "ui.section3", "3. Universelle Hilfen & Cheats");
            Add(l, "ui.indicator.toggle", "\"Trefferindikator\" aktivieren: grauer Rand für Normal, grüner für Nachsichtig");
            Add(l, "ui.indicator_plus.toggle", "\"Trefferindikator+\" aktivieren: Text ersetzen durch Normal! / Nachsichtig!");
            Add(l, "ui.alt_indicator.toggle", "\"Alternativer Indikator+\" aktivieren: Früh/Spät-Richtung anzeigen");
            Add(l, "ui.alt_style_sign", "Vorzeichen-Präfix (Standard): -Nachsichtig! (Früh) / +Normal! (Spät)");
            Add(l, "ui.alt_style_letter", "Buchstaben-Präfix: L-Früh! / N-Spät!");
            Add(l, "ui.double_indicator.toggle", "Indikator für Fehlschläge aktivieren (Normal!! / Nachsichtig!!)");
            Add(l, "ui.force_diff.toggle", "Schwierigkeitsauswahl immer erzwingen");
            Add(l, "ui.nofail.toggle", "\"Kein Fehlschlag\" (No Fail) aktivieren (Hotkey: Alt + N)");
            Add(l, "ui.auto.toggle", "\"Autoplay\" aktivieren (Hotkey: Alt + A)");
        }

        private static void InitFrench()
        {
            SystemLanguage l = SystemLanguage.French;
            Add(l, "master.warning_title", "[!] Avertissement de sécurité et clause de non-responsabilité");
            Add(l, "master.warning_body", "L'utilisation de ce mod en multijoueur est strictement interdite! L'auteur décline toute responsabilité pour tout bannissement de compte.");
            Add(l, "master.agree_btn", "J'accepte la clause de non-responsabilité. Activer l'interrupteur principal");
            Add(l, "master.status_on", "Interrupteur principal activé");
            Add(l, "master.disable_btn", "Désactiver l'interrupteur principal");
            Add(l, "misc.section", "3. Divers");
            Add(l, "misc.editor_toggle", "Toujours autoriser l'éditeur de niveaux (même dans les niveaux officiels)");
            Add(l, "misc.ring_toggle", "Masquer l'anneau de jugement des planètes (cercle pointillé)");
            Add(l, "misc.autoplay_plus_toggle", "Activer Autoplay+ (simulation de touches pour KeyViewer)");
            Add(l, "misc.autoplay_keys_label", "Touches autorisées (max 16, séparées par un espace):");
            Add(l, "misc.autoplay_preset_btn", "Réinitialiser les touches par défaut");
            Add(l, "misc.autoplay_acc_label", "Précision humaine ciblée: {0:0}% (ajoute une variance de timing)");
            Add(l, "hud.autoplay_plus", "Autoplay+");
            
            Add(l, "mod.title", "Meilleur Jugement");
            Add(l, "stamp.mobile", "Réussi avec Jugement Mobile");
            Add(l, "stamp.custom", "Réussi avec Jugement Personnalisé");
            Add(l, "ui.custom.unit_label", "Mode d'Unité de Jugement:");
            Add(l, "ui.custom.unit_time", "Mode Temps (ms)");
            Add(l, "ui.custom.unit_angle", "Mode Angle (degrés °)");
            Add(l, "ui.custom.counted_angle", "Angle Compté ({0}°): ");
            Add(l, "ui.custom.perfect_angle", "Angle Parfait ({0}°): ");
            Add(l, "ui.custom.pure_angle", "Angle Pur ({0}°): ");
            Add(l, "ui.custom.mult_title", "Multiplicateur de Pourcentage:");
            Add(l, "ui.custom.mult_apply", "Appliquer %");
            Add(l, "ui.custom.preset_strict", "Préréglage Strict");
            Add(l, "ui.custom.preset_normal", "Préréglage Normal");
            Add(l, "ui.custom.preset_lenient", "Préréglage Tolérant");
            Add(l, "ui.custom.preset_mobile", "Préréglage Mobile");
            Add(l, "ui.custom.profiles_title", "Emplacements de Profil:");
            Add(l, "ui.custom.save_slot", "Enregistrer dans Slot {0}");
            Add(l, "ui.custom.load_slot", "Charger Slot {0}");
            Add(l, "ui.custom.free_input_hint", "(Saisissez n'importe quel nombre pour dépasser le curseur)");
            Add(l, "hud.mobile", "Jugement Mobile");
            Add(l, "hud.custom", "Jugement Personnalisé");
            Add(l, "diff.mobile", "Mobile");
            Add(l, "diff.custom", "Personnalisé");
            Add(l, "indicator.normal", "Normal");
            Add(l, "indicator.lenient", "Tolérant");
            Add(l, "indicator.tooEarly", "Trop Tôt");
            Add(l, "indicator.tooLate", "Trop Tard");
            Add(l, "results.normal", "Normal");
            Add(l, "results.lenient", "Tolérant");
            Add(l, "ui.language_label", "Paramètres de Langue");
            Add(l, "ui.lang.auto", "Suivre le jeu (Auto)");
            Add(l, "mode.vanilla", "Mode PC Vanilla");
            Add(l, "mode.mobile", "Mode Jugement Mobile");
            Add(l, "mode.custom", "Mode Jugement Personnalisé");
            Add(l, "ui.section1", "1. Sélection du Mode de Jugement");
            Add(l, "ui.section2", "2. Fonctionnalités du Mode PC");
            Add(l, "ui.section3", "3. Aides et Triche Universelles");
            Add(l, "ui.indicator.toggle", "Activer \"Indicateur de Frappe\": bordure grise pour Normal, verte pour Tolérant");
            Add(l, "ui.indicator_plus.toggle", "Activer \"Indicateur de Frappe+\": remplace le texte par Normal! / Tolérant!");
            Add(l, "ui.alt_indicator.toggle", "Activer \"Indicateur Alternatif+\": ajoute la direction Tôt/Tard");
            Add(l, "ui.alt_style_sign", "Préfixe de Signe (Défaut): -Tolérant! (Tôt) / +Normal! (Tard)");
            Add(l, "ui.alt_style_letter", "Préfixe de Lettre: L-Tôt! / N-Tard!");
            Add(l, "ui.double_indicator.toggle", "Indicateur pour les échecs (Normal!! / Tolérant!!)");
            Add(l, "ui.force_diff.toggle", "Forcer le sélecteur de difficulté");
            Add(l, "ui.nofail.toggle", "Activer \"No Fail\" (Invincible) (Raccourci: Alt + N)");
            Add(l, "ui.auto.toggle", "Activer \"Autoplay\" (Automatique) (Raccourci: Alt + A)");
        }

        private static void InitPortuguese()
        {
            SystemLanguage l = SystemLanguage.Portuguese;
            Add(l, "master.warning_title", "[!] Aviso de segurança e isenção de responsabilidade");
            Add(l, "master.warning_body", "É estritamente proibido usar este mod no modo multijogador! O autor não se responsabiliza por banimentos de conta.");
            Add(l, "master.agree_btn", "Aceito a isenção de responsabilidade. Ativar interruptor principal");
            Add(l, "master.status_on", "Interruptor principal ativado");
            Add(l, "master.disable_btn", "Desativar interruptor principal");
            Add(l, "misc.section", "3. Diversos");
            Add(l, "misc.editor_toggle", "Sempre permitir editor de níveis (mesmo em níveis oficiais)");
            Add(l, "misc.ring_toggle", "Ocultar anel de julgamento do planeta (círculo pontilhado)");
            Add(l, "misc.autoplay_plus_toggle", "Ativar Autoplay+ (simulação de teclas para KeyViewer)");
            Add(l, "misc.autoplay_keys_label", "Teclas permitidas (máx 16, separadas por espaço):");
            Add(l, "misc.autoplay_preset_btn", "Redefinir teclas padrão");
            Add(l, "misc.autoplay_acc_label", "Precisão humana alvo: {0:0}% (adiciona variação de reação)");
            Add(l, "hud.autoplay_plus", "Autoplay+");
            
            Add(l, "mod.title", "Melhor Julgamento");
            Add(l, "stamp.mobile", "Concluído com Julgamento Móvel");
            Add(l, "stamp.custom", "Concluído com Julgamento Personalizado");
            Add(l, "ui.custom.unit_label", "Modo de Unidade de Julgamento:");
            Add(l, "ui.custom.unit_time", "Modo Tempo (ms)");
            Add(l, "ui.custom.unit_angle", "Modo Ângulo (graus °)");
            Add(l, "ui.custom.counted_angle", "Ângulo Contado ({0}°): ");
            Add(l, "ui.custom.perfect_angle", "Ângulo Perfeito ({0}°): ");
            Add(l, "ui.custom.pure_angle", "Ângulo Puro ({0}°): ");
            Add(l, "ui.custom.mult_title", "Multiplicador de Porcentagem:");
            Add(l, "ui.custom.mult_apply", "Aplicar %");
            Add(l, "ui.custom.preset_strict", "Predefinição Estrita");
            Add(l, "ui.custom.preset_normal", "Predefinição Normal");
            Add(l, "ui.custom.preset_lenient", "Predefinição Indulgente");
            Add(l, "ui.custom.preset_mobile", "Predefinição Móvel");
            Add(l, "ui.custom.profiles_title", "Perfis Personalizados:");
            Add(l, "ui.custom.save_slot", "Salvar no Slot {0}");
            Add(l, "ui.custom.load_slot", "Carregar Slot {0}");
            Add(l, "ui.custom.free_input_hint", "(Digite qualquer valor na caixa de texto)");
            Add(l, "hud.mobile", "Julgamento Móvel");
            Add(l, "hud.custom", "Julgamento Personalizado");
            Add(l, "diff.mobile", "Móvel");
            Add(l, "diff.custom", "Personalizado");
            Add(l, "indicator.normal", "Normal");
            Add(l, "indicator.lenient", "Tolerante");
            Add(l, "indicator.tooEarly", "Muito Cedo");
            Add(l, "indicator.tooLate", "Muito Tarde");
            Add(l, "results.normal", "Normal");
            Add(l, "results.lenient", "Tolerante");
            Add(l, "ui.language_label", "Configuração de Idioma");
            Add(l, "ui.lang.auto", "Seguir Jogo (Auto)");
            Add(l, "mode.vanilla", "Modo PC Original (Vanilla)");
            Add(l, "mode.mobile", "Modo de Julgamento Móvel");
            Add(l, "mode.custom", "Modo de Julgamento Personalizado");
            Add(l, "ui.section1", "1. Seleção do Modo de Julgamento");
            Add(l, "ui.section2", "2. Recursos do Modo PC");
            Add(l, "ui.section3", "3. Assistências e Modificadores Universais");
            Add(l, "ui.indicator.toggle", "Ativar \"Indicador de Batida\": borda cinza para Normal, verde para Tolerante");
            Add(l, "ui.indicator_plus.toggle", "Ativar \"Indicador de Batida+\": texto Normal! / Tolerante!");
            Add(l, "ui.alt_indicator.toggle", "Ativar \"Indicador Alternativo+\": direção Cedo/Tarde");
            Add(l, "ui.alt_style_sign", "Prefixo de Sinal (Padrão): -Tolerante! (Cedo) / +Normal! (Tarde)");
            Add(l, "ui.alt_style_letter", "Prefixo de Letra: L-Cedo! / N-Tarde!");
            Add(l, "ui.double_indicator.toggle", "Indicador para erros/batidas duplas (Normal!! / Tolerante!!)");
            Add(l, "ui.force_diff.toggle", "Forçar seletor de dificuldade em qualquer nível");
            Add(l, "ui.nofail.toggle", "Ativar \"No Fail\" (Modo Imortal) (Atalho: Alt + N)");
            Add(l, "ui.auto.toggle", "Ativar \"Autoplay\" (Reprodução Automática) (Atalho: Alt + A)");
        }

        private static void InitPolish()
        {
            SystemLanguage l = SystemLanguage.Polish;
            Add(l, "master.warning_title", "[!] Ostrzeżenie dotyczące bezpieczeństwa");
            Add(l, "master.warning_body", "Używanie tego moda w trybie wieloosobowym jest surowo zabronione! Autor nie ponosi odpowiedzialności za blokady kont.");
            Add(l, "master.agree_btn", "Akceptuję wyłączenie odpowiedzialności. Włącz główny przełącznik");
            Add(l, "master.status_on", "Główny przełącznik włączony");
            Add(l, "master.disable_btn", "Wyłącz główny przełącznik");
            Add(l, "misc.section", "3. Różne");
            Add(l, "misc.editor_toggle", "Zawsze zezwalaj na edytor poziomów (również na poziomach oficjalnych)");
            Add(l, "misc.ring_toggle", "Ukryj pierścień oceny planet (przerywany okrąg)");
            Add(l, "misc.autoplay_plus_toggle", "Włącz Autoplay+ (symulacja klawiszy dla KeyViewer)");
            Add(l, "misc.autoplay_keys_label", "Dozwolone klawisze (maks. 16, oddzielone spacją):");
            Add(l, "misc.autoplay_preset_btn", "Zresetuj klawisze domyślne");
            Add(l, "misc.autoplay_acc_label", "Docelowa ludzka dokładność: {0:0}% (wariancja reakcji)");
            Add(l, "hud.autoplay_plus", "Autoplay+");
            
            Add(l, "mod.title", "Lepszy Sąd");
            Add(l, "stamp.mobile", "Ukończono w trybie mobilnym");
            Add(l, "stamp.custom", "Ukończono w trybie własnym");
            Add(l, "ui.custom.unit_label", "Tryb Jednostki Oceny:");
            Add(l, "ui.custom.unit_time", "Tryb Czasu (ms)");
            Add(l, "ui.custom.unit_angle", "Tryb Kąta (stopnie °)");
            Add(l, "ui.custom.counted_angle", "Kąt Zaliczony ({0}°): ");
            Add(l, "ui.custom.perfect_angle", "Kąt Idealny ({0}°): ");
            Add(l, "ui.custom.pure_angle", "Kąt Czysty ({0}°): ");
            Add(l, "ui.custom.mult_title", "Mnożnik Procentowy:");
            Add(l, "ui.custom.mult_apply", "Zastosuj %");
            Add(l, "ui.custom.preset_strict", "Profil Rygorystyczny");
            Add(l, "ui.custom.preset_normal", "Profil Normalny");
            Add(l, "ui.custom.preset_lenient", "Profil Łagodny");
            Add(l, "ui.custom.preset_mobile", "Profil Mobilny");
            Add(l, "ui.custom.profiles_title", "Profile Własne:");
            Add(l, "ui.custom.save_slot", "Zapisz w Slocie {0}");
            Add(l, "ui.custom.load_slot", "Wczytaj Slot {0}");
            Add(l, "ui.custom.free_input_hint", "(Wpisz dowolną wartość w polu tekstowym)");
            Add(l, "hud.mobile", "Ocena Mobilna");
            Add(l, "hud.custom", "Własna Ocena");
            Add(l, "diff.mobile", "Mobilny");
            Add(l, "diff.custom", "Własny");
            Add(l, "indicator.normal", "Normalny");
            Add(l, "indicator.lenient", "Łagodny");
            Add(l, "indicator.tooEarly", "Za Wcześnie");
            Add(l, "indicator.tooLate", "Za Późno");
            Add(l, "results.normal", "Normalny");
            Add(l, "results.lenient", "Łagodny");
            Add(l, "ui.language_label", "Ustawienia Języka");
            Add(l, "ui.lang.auto", "Zgodnie z grą (Auto)");
            Add(l, "mode.vanilla", "Oryginalny tryb PC (Vanilla)");
            Add(l, "mode.mobile", "Tryb Oceny Mobilnej");
            Add(l, "mode.custom", "Tryb Własnej Oceny");
            Add(l, "ui.section1", "1. Wybór Trybu Oceny");
            Add(l, "ui.section2", "2. Funkcje Trybu PC");
            Add(l, "ui.section3", "3. Globalne Modyfikatory i Ułatwienia");
            Add(l, "ui.indicator.toggle", "Włącz \"Wskaźnik Oceny\": szara ramka dla Normalny, zielona dla Łagodny");
            Add(l, "ui.indicator_plus.toggle", "Włącz \"Wskaźnik Oceny+\": zamienia na Normalny! / Łagodny!");
            Add(l, "ui.alt_indicator.toggle", "Włącz \"Alternatywny Wskaźnik+\": kierunek Wcześnie/Późno");
            Add(l, "ui.alt_style_sign", "Prefiks znaku (Domyślny): -Łagodny! (Wcześnie) / +Normalny! (Późno)");
            Add(l, "ui.alt_style_letter", "Prefiks litery: L-Wcześnie! / N-Późno!");
            Add(l, "ui.double_indicator.toggle", "Wskaźnik dla błędów (Normalny!! / Łagodny!!)");
            Add(l, "ui.force_diff.toggle", "Wymuś selektor trudności na każdym poziomie");
            Add(l, "ui.nofail.toggle", "Włącz \"No Fail\" (Nieśmiertelność) (Skrót: Alt + N)");
            Add(l, "ui.auto.toggle", "Włącz \"Autoplay\" (Automatyczna gra) (Skrót: Alt + A)");
        }

        private static void InitVietnamese()
        {
            SystemLanguage l = SystemLanguage.Vietnamese;
            Add(l, "master.warning_title", "[!] Cảnh báo an toàn và miễn trừ trách nhiệm");
            Add(l, "master.warning_body", "Nghiêm cấm sử dụng bản mod này trong chế độ nhiều người chơi! Tác giả không chịu trách nhiệm về bất kỳ hình phất nào đối với tài khoản.");
            Add(l, "master.agree_btn", "Tôi đồng ý với tuyên bố miễn trừ. Bật công tắc chính");
            Add(l, "master.status_on", "Công tắc chính đã bật");
            Add(l, "master.disable_btn", "Tắt công tắc chính");
            Add(l, "misc.section", "3. Khác");
            Add(l, "misc.editor_toggle", "Luôn cho phép trình chỉnh sửa màn chơi (ngay cả trong màn chơi chính thức)");
            Add(l, "misc.ring_toggle", "Ẩn vòng phán đoán hành tinh (vòng chấm tròn)");
            Add(l, "misc.autoplay_plus_toggle", "Bật Tự động chơi+ (mô phỏng phím cho KeyViewer)");
            Add(l, "misc.autoplay_keys_label", "Các phím được phép (tối đa 16, cách nhau bằng dấu cách):");
            Add(l, "misc.autoplay_preset_btn", "Đặt lại phím mặc định");
            Add(l, "misc.autoplay_acc_label", "Độ chính xác mô phỏng: {0:0}% (dao động thời gian thực tế)");
            Add(l, "hud.autoplay_plus", "Tự động+");
            
            Add(l, "mod.title", "Phán Đoán Tốt Hơn");
            Add(l, "stamp.mobile", "Vượt qua với Phán đoán Di động");
            Add(l, "stamp.custom", "Vượt qua với Phán đoán Tùy chỉnh");
            Add(l, "ui.custom.unit_label", "Chế độ Đơn vị Phán đoán:");
            Add(l, "ui.custom.unit_time", "Chế độ Thời gian (ms)");
            Add(l, "ui.custom.unit_angle", "Chế độ Góc (độ °)");
            Add(l, "ui.custom.counted_angle", "Góc Counted ({0}°): ");
            Add(l, "ui.custom.perfect_angle", "Góc Perfect ({0}°): ");
            Add(l, "ui.custom.pure_angle", "Góc Pure ({0}°): ");
            Add(l, "ui.custom.mult_title", "Hệ số Phần trăm Cài đặt sẵn:");
            Add(l, "ui.custom.mult_apply", "Áp dụng %");
            Add(l, "ui.custom.preset_strict", "Cài đặt Khắt khe");
            Add(l, "ui.custom.preset_normal", "Cài đặt Tiêu chuẩn");
            Add(l, "ui.custom.preset_lenient", "Cài đặt Khoan dung");
            Add(l, "ui.custom.preset_mobile", "Cài đặt Di động");
            Add(l, "ui.custom.profiles_title", "Hồ sơ Tùy chỉnh:");
            Add(l, "ui.custom.save_slot", "Lưu vào Ô {0}");
            Add(l, "ui.custom.load_slot", "Tải Ô {0}");
            Add(l, "ui.custom.free_input_hint", "(Nhập số bất kỳ vào ô nhập liệu để vượt giới hạn)");
            Add(l, "hud.mobile", "Phán đoán Di động");
            Add(l, "hud.custom", "Phán đoán Tùy chỉnh");
            Add(l, "diff.mobile", "Di động");
            Add(l, "diff.custom", "Tùy chỉnh");
            Add(l, "indicator.normal", "Tiêu chuẩn");
            Add(l, "indicator.lenient", "Nới lỏng");
            Add(l, "indicator.tooEarly", "Quá Sớm");
            Add(l, "indicator.tooLate", "Quá Trễ");
            Add(l, "results.normal", "Tiêu chuẩn");
            Add(l, "results.lenient", "Nới lỏng");
            Add(l, "ui.language_label", "Cài đặt Ngôn ngữ");
            Add(l, "ui.lang.auto", "Theo trò chơi (Auto)");
            Add(l, "mode.vanilla", "Chế độ PC Gốc (Vanilla)");
            Add(l, "mode.mobile", "Chế độ Phán đoán Di động");
            Add(l, "mode.custom", "Chế độ Phán đoán Tùy chỉnh");
            Add(l, "ui.section1", "1. Chọn Chế độ Phán đoán");
            Add(l, "ui.section2", "2. Tính năng Chế độ PC");
            Add(l, "ui.section3", "3. Hỗ trợ & Gian lận Toàn cục");
            Add(l, "ui.indicator.toggle", "Bật \"Chỉ báo Phán đoán\": viền xám cho Tiêu chuẩn, xanh lá cho Nới lỏng");
            Add(l, "ui.indicator_plus.toggle", "Bật \"Chỉ báo Phán đoán+\": đổi chữ thành Tiêu chuẩn! / Nới lỏng!");
            Add(l, "ui.alt_indicator.toggle", "Bật \"Chỉ báo Thay thế+\": thêm hướng Sớm/Trễ");
            Add(l, "ui.alt_style_sign", "Tiền tố Dấu (Mặc định): -Nới lỏng! (Sớm) / +Tiêu chuẩn! (Trễ)");
            Add(l, "ui.alt_style_letter", "Tiền tố Chữ cái: L-Sớm! / N-Trễ!");
            Add(l, "ui.double_indicator.toggle", "Chỉ báo cho lỗi/gõ trượt (Tiêu chuẩn!! / Nới lỏng!!)");
            Add(l, "ui.force_diff.toggle", "Bắt buộc mở thanh chọn độ khó ở mọi màn");
            Add(l, "ui.nofail.toggle", "Bật \"Không chết\" (No Fail) (Phím tắt: Alt + N)");
            Add(l, "ui.auto.toggle", "Bật \"Tự chơi\" (Autoplay) (Phím tắt: Alt + A)");
        }

        private static void InitCzech()
        {
            SystemLanguage l = SystemLanguage.Czech;
            Add(l, "master.warning_title", "[!] Bezpečnostní varování a vyloučení odpovědnosti");
            Add(l, "master.warning_body", "Používání tohoto módu ve hře pro více hráčů je přísně zakázáno! Autor nenese odpovědnost za blokování účtu.");
            Add(l, "master.agree_btn", "Přijímám podmínky. Zapnout hlavní vypínač");
            Add(l, "master.status_on", "Hlavní vypínač je zapnut");
            Add(l, "master.disable_btn", "Vypnout hlavní vypínač");
            Add(l, "misc.section", "3. Různé");
            Add(l, "misc.editor_toggle", "Vždy povolit editor úrovní (i v oficiálních úrovních)");
            Add(l, "misc.ring_toggle", "Skrýt kroužek hodnocení planety (tečkovaný kruh)");
            Add(l, "misc.autoplay_plus_toggle", "Povolit Automatické přehrávání+ (simulace kláves pro KeyViewer)");
            Add(l, "misc.autoplay_keys_label", "Povolené klávesy (max. 16, oddělené mezerou):");
            Add(l, "misc.autoplay_preset_btn", "Obnovit výchozí klávesy");
            Add(l, "misc.autoplay_acc_label", "Cílová lidská přesnost: {0:0}% (variace odezvy)");
            Add(l, "hud.autoplay_plus", "Auto+");
            
            Add(l, "mod.title", "Lepší Posouzení");
            Add(l, "stamp.mobile", "Dokončeno s mobilním posouzením");
            Add(l, "stamp.custom", "Dokončeno s vlastním posouzením");
            Add(l, "ui.custom.unit_label", "Režim Jednotky Posouzení:");
            Add(l, "ui.custom.unit_time", "Režim Času (ms)");
            Add(l, "ui.custom.unit_angle", "Režim Úhlu (stupně °)");
            Add(l, "ui.custom.counted_angle", "Započtený Úhel ({0}°): ");
            Add(l, "ui.custom.perfect_angle", "Perfektní Úhel ({0}°): ");
            Add(l, "ui.custom.pure_angle", "Čistý Úhel ({0}°): ");
            Add(l, "ui.custom.mult_title", "Procentuální Násobič:");
            Add(l, "ui.custom.mult_apply", "Použít %");
            Add(l, "ui.custom.preset_strict", "Přísná Předvolba");
            Add(l, "ui.custom.preset_normal", "Normální Předvolba");
            Add(l, "ui.custom.preset_lenient", "Mírná Předvolba");
            Add(l, "ui.custom.preset_mobile", "Mobilní Předvolba");
            Add(l, "ui.custom.profiles_title", "Vlastní Profily:");
            Add(l, "ui.custom.save_slot", "Uložit do Slotu {0}");
            Add(l, "ui.custom.load_slot", "Načíst Slot {0}");
            Add(l, "ui.custom.free_input_hint", "(Zadejte libovolnou hodnotu do textového pole)");
            Add(l, "hud.mobile", "Mobilní posuzování");
            Add(l, "hud.custom", "Vlastní posuzování");
            Add(l, "diff.mobile", "Mobilní");
            Add(l, "diff.custom", "Vlastní");
            Add(l, "indicator.normal", "Normální");
            Add(l, "indicator.lenient", "Mírný");
            Add(l, "indicator.tooEarly", "Příliš Brzy");
            Add(l, "indicator.tooLate", "Příliš Pozdě");
            Add(l, "results.normal", "Normální");
            Add(l, "results.lenient", "Mírný");
            Add(l, "ui.language_label", "Nastavení Jazyka");
            Add(l, "ui.lang.auto", "Podle hry (Auto)");
            Add(l, "mode.vanilla", "Původní režim PC (Vanilla)");
            Add(l, "mode.mobile", "Režim Mobilního Posuzování");
            Add(l, "mode.custom", "Vlastní Režim Posuzování");
            Add(l, "ui.section1", "1. Výběr Režimu Posuzování");
            Add(l, "ui.section2", "2. Funkce Režimu PC");
            Add(l, "ui.section3", "3. Univerzální Pomocníci a Cheaty");
            Add(l, "ui.indicator.toggle", "Povolit \"Indikátor Zásahu\": šedý rámeček pro Normální, zelený pro Mírný");
            Add(l, "ui.indicator_plus.toggle", "Povolit \"Indikátor Zásahu+\": změní text na Normální! / Mírný!");
            Add(l, "ui.alt_indicator.toggle", "Povolit \"Alternativní Indikátor+\": směr Brzy/Pozdě");
            Add(l, "ui.alt_style_sign", "Předpona Znaménka (Výchozí): -Mírný! (Brzy) / +Normální! (Pozdě)");
            Add(l, "ui.alt_style_letter", "Předpona Písmene: L-Brzy! / N-Pozdě!");
            Add(l, "ui.double_indicator.toggle", "Indikátor pro chyby/dvojité stisknutí (Normální!! / Mírný!!)");
            Add(l, "ui.force_diff.toggle", "Vynutit volič obtížnosti na všech úrovních");
            Add(l, "ui.nofail.toggle", "Povolit \"No Fail\" (Nesmrtelnost) (Zkratka: Alt + N)");
            Add(l, "ui.auto.toggle", "Povolit \"Autoplay\" (Automatická hra) (Zkratka: Alt + A)");
        }

        private static void InitRomanian()
        {
            SystemLanguage l = SystemLanguage.Romanian;
            Add(l, "master.warning_title", "[!] Avertisment de siguranță și declinare a responsabilității");
            Add(l, "master.warning_body", "Utilizarea acestui mod în multiplayer este strict interzisă! Autorul nu este responsabil pentru blocarea contului.");
            Add(l, "master.agree_btn", "Accept declinarea. Activează comutatorul principal");
            Add(l, "master.status_on", "Comutatorul principal este activat");
            Add(l, "master.disable_btn", "Dezactivează comutatorul principal");
            Add(l, "misc.section", "3. Diverse");
            Add(l, "misc.editor_toggle", "Permite întotdeauna editorul de niveluri (chiar și în niveluri oficiale)");
            Add(l, "misc.ring_toggle", "Ascunde inelul de judecată al planetelor (cercul punctat)");
            Add(l, "misc.autoplay_plus_toggle", "Activează Redare automată+ (simulare taste pentru KeyViewer)");
            Add(l, "misc.autoplay_keys_label", "Taste permise (max 16, separate prin spațiu):");
            Add(l, "misc.autoplay_preset_btn", "Resetează tastele implicite");
            Add(l, "misc.autoplay_acc_label", "Precizie umană țintă: {0:0}% (variație de reacție)");
            Add(l, "hud.autoplay_plus", "Auto+");
            
            Add(l, "mod.title", "Judecată Mai Bună");
            Add(l, "stamp.mobile", "Finalizat cu Judecată Mobilă");
            Add(l, "stamp.custom", "Finalizat cu Judecată Personalizată");
            Add(l, "ui.custom.unit_label", "Mod Unitate de Judecată:");
            Add(l, "ui.custom.unit_time", "Mod Timp (ms)");
            Add(l, "ui.custom.unit_angle", "Mod Unghi (grade °)");
            Add(l, "ui.custom.counted_angle", "Unghi Contorizat ({0}°): ");
            Add(l, "ui.custom.perfect_angle", "Unghi Perfect ({0}°): ");
            Add(l, "ui.custom.pure_angle", "Unghi Pur ({0}°): ");
            Add(l, "ui.custom.mult_title", "Multiplicator Procentual:");
            Add(l, "ui.custom.mult_apply", "Aplică %");
            Add(l, "ui.custom.preset_strict", "Preset Strict");
            Add(l, "ui.custom.preset_normal", "Preset Normal");
            Add(l, "ui.custom.preset_lenient", "Preset Indulgent");
            Add(l, "ui.custom.preset_mobile", "Preset Mobil");
            Add(l, "ui.custom.profiles_title", "Profiluri Personalizate:");
            Add(l, "ui.custom.save_slot", "Salvează în Slotul {0}");
            Add(l, "ui.custom.load_slot", "Încarcă Slotul {0}");
            Add(l, "ui.custom.free_input_hint", "(Introduceți orice număr în câmpul de text)");
            Add(l, "hud.mobile", "Judecată Mobilă");
            Add(l, "hud.custom", "Judecată Personalizată");
            Add(l, "diff.mobile", "Mobil");
            Add(l, "diff.custom", "Personalizat");
            Add(l, "indicator.normal", "Normal");
            Add(l, "indicator.lenient", "Indulgent");
            Add(l, "indicator.tooEarly", "Prea Devreme");
            Add(l, "indicator.tooLate", "Prea Târziu");
            Add(l, "results.normal", "Normal");
            Add(l, "results.lenient", "Indulgent");
            Add(l, "ui.language_label", "Setări Limbă");
            Add(l, "ui.lang.auto", "Urmează jocul (Auto)");
            Add(l, "mode.vanilla", "Mod PC Original (Vanilla)");
            Add(l, "mode.mobile", "Mod de Judecată Mobilă");
            Add(l, "mode.custom", "Mod de Judecată Personalizată");
            Add(l, "ui.section1", "1. Selectarea Modului de Judecată");
            Add(l, "ui.section2", "2. Caracteristici Mod PC");
            Add(l, "ui.section3", "3. Modificatori și Asistență Universală");
            Add(l, "ui.indicator.toggle", "Activează \"Indicator de Lovire\": contur gri pentru Normal, verde pentru Indulgent");
            Add(l, "ui.indicator_plus.toggle", "Activează \"Indicator de Lovire+\": schimbă textul în Normal! / Indulgent!");
            Add(l, "ui.alt_indicator.toggle", "Activează \"Indicator Alternativ+\": direcție Devreme/Târziu");
            Add(l, "ui.alt_style_sign", "Prefix de Semn (Implicit): -Indulgent! (Devreme) / +Normal! (Târziu)");
            Add(l, "ui.alt_style_letter", "Prefix de Literă: L-Devreme! / N-Târziu!");
            Add(l, "ui.double_indicator.toggle", "Indicator pentru ratări (Normal!! / Indulgent!!)");
            Add(l, "ui.force_diff.toggle", "Forțează selectorul de dificultate pe orice nivel");
            Add(l, "ui.nofail.toggle", "Activează \"No Fail\" (Nemurire) (Comandă rapidă: Alt + N)");
            Add(l, "ui.auto.toggle", "Activează \"Autoplay\" (Joc automat) (Comandă rapidă: Alt + A)");
        }
    }
}

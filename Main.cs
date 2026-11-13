using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityModManagerNet;

namespace ADOFAIPlus
{
    public static class Main
    {
        public static UnityModManager.ModEntry ModEntry { get; private set; }

        public static UnityModManager.ModEntry.ModLogger Logger
        {
            get
            {
                return ModEntry != null ? ModEntry.Logger : null;
            }
        }

        public static Settings Settings { get; private set; }
        public static bool Enabled { get; private set; }

        private static Harmony _harmony;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModEntry = modEntry;
            Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            if (!Settings.MigratedJapaneseFontDefault)
            {
                Settings.BetterJapaneseLocalization = true;
                Settings.JapaneseUseChineseFont = true;
                Settings.MigratedJapaneseFontDefault = true;
                Settings.Save(modEntry);
            }

            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;

            _harmony = new Harmony(modEntry.Info.Id);
            SceneManager.sceneLoaded += Patches.OnSceneLoaded;

            return true;
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            Enabled = value;

            if (value)
            {
                PatchAllSafely();
                try
                {
                    Patches.ApplyJapaneseFontPreference();
                    Patches.ApplyClassicFontPreference();
                    if (Settings.HidePlanetRing) Patches.ApplyHidePlanetRing(true);
                }
                catch (Exception ex)
                {
                    if (Logger != null) Logger.Error("ApplyStateChange failed: " + ex);
                }
                if (Logger != null)
                {
                    Logger.Log("BetterJudgmentLite enabled.");
                }
            }
            else
            {
                try
                {
                    Patches.RestoreOriginalState();
                }
                catch (Exception ex)
                {
                    if (Logger != null) Logger.Error("RestoreOriginalState failed: " + ex);
                }
                _harmony.UnpatchAll(modEntry.Info.Id);
                if (Logger != null)
                {
                    Logger.Log("BetterJudgmentLite disabled.");
                }
            }

            return true;
        }

        private static void PatchAllSafely()
        {
            int ok = 0, failed = 0;
            foreach (Type type in AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly()))
            {
                if (type.GetCustomAttributes(typeof(HarmonyPatch), false).Length == 0)
                    continue;

                try
                {
                    _harmony.CreateClassProcessor(type).Patch();
                    ok++;
                }
                catch (Exception ex)
                {
                    failed++;
                    if (Logger != null) Logger.Error("Patch failed: " + type.Name + " -> " + ex.Message);
                }
            }
            if (Logger != null) Logger.Log(string.Format("BetterJudgmentLite patches applied: {0} ok, {1} failed.", ok, failed));
        }

        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            // 0. 语言设置 (Language Settings)
            GUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();
            GUILayout.Label("<b>" + Loc.Get("ui.language_label") + "</b>", GUILayout.Width(220));
            GUILayout.Label("<i>(" + Loc.CurrentLanguage.ToString() + ")</i>");
            GUILayout.EndHorizontal();
            GUILayout.Space(3);

            string[] langNames = new string[]
            {
                Loc.Get("ui.lang.auto"),
                "简体中文",
                "繁體中文",
                "English",
                "한국어",
                "日本語",
                "Español",
                "Português",
                "Français",
                "Deutsch",
                "Polski",
                "Русский",
                "Tiếng Việt",
                "Čeština",
                "Română"
            };

            int curLangIdx = (int)Settings.Language;
            int newLangIdx = GUILayout.SelectionGrid(curLangIdx, langNames, 5);
            if (newLangIdx != curLangIdx)
            {
                Settings.Language = (ModLanguage)newLangIdx;
                Patches.ApplyJapaneseFontPreference();
                Patches.ApplyClassicFontPreference();
            }

            if (Loc.CurrentLanguage == SystemLanguage.Japanese || RDString.language == SystemLanguage.Japanese)
            {
                GUILayout.Space(4);
                bool oldJpLoc = Settings.BetterJapaneseLocalization;
                bool newJpLoc = GUILayout.Toggle(
                    oldJpLoc,
                    " " + Loc.Get("ui.japanese_better_loc"));
                if (newJpLoc != oldJpLoc)
                {
                    Settings.BetterJapaneseLocalization = newJpLoc;
                    Patches.ApplyJapaneseFontPreference();
                }

                if (Settings.BetterJapaneseLocalization)
                {
                    bool oldJpFont = Settings.JapaneseUseChineseFont;
                    bool newJpFont = GUILayout.Toggle(
                        oldJpFont,
                        "     " + Loc.Get("ui.japanese_use_chinese_font"));
                    if (newJpFont != oldJpFont)
                    {
                        Settings.JapaneseUseChineseFont = newJpFont;
                        Patches.ApplyJapaneseFontPreference();
                    }
                }
            }

            if (Loc.CurrentLanguage == SystemLanguage.ChineseSimplified || RDString.language == SystemLanguage.ChineseSimplified)
            {
                GUILayout.Space(4);
                bool oldClassic = Settings.ClassicMode;
                bool newClassic = GUILayout.Toggle(
                    oldClassic,
                    " " + Loc.Get("ui.classic_mode"));
                if (newClassic != oldClassic)
                {
                    Settings.ClassicMode = newClassic;
                    Patches.ApplyClassicFontPreference();
                }
                GUILayout.Label("    <color=#888888>" + Loc.Get("ui.classic_mode_desc") + "</color>");
            }
            GUILayout.EndVertical();

            GUILayout.Space(5);

            // 1. 判定指示器全家桶与难度栏常驻 (Hit Indicator Features)
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>" + Loc.Get("ui.section2") + "</b>");
            GUILayout.Space(5);

            Settings.EnableHitIndicator = GUILayout.Toggle(
                Settings.EnableHitIndicator,
                " " + Loc.Get("ui.indicator.toggle"));

            if (Settings.EnableHitIndicator)
            {
                GUILayout.Space(2);
                GUILayout.Label("    • " + Loc.Get("ui.indicator.desc_normal"));
                GUILayout.Label("    • " + Loc.Get("ui.indicator.desc_lenient"));
                GUILayout.Label("    • " + Loc.Get("ui.indicator.desc_meter"));

                GUILayout.Space(5);
                Settings.EnableHitIndicatorPlus = GUILayout.Toggle(
                    Settings.EnableHitIndicatorPlus,
                    " " + Loc.Get("ui.indicator_plus.toggle"));

                if (Settings.EnableHitIndicatorPlus)
                {
                    Settings.EnableAlternativeIndicatorPlus = GUILayout.Toggle(
                        Settings.EnableAlternativeIndicatorPlus,
                        " " + Loc.Get("ui.alt_indicator.toggle"));

                    if (Settings.EnableAlternativeIndicatorPlus)
                    {
                        GUILayout.BeginVertical("box");
                        GUILayout.Label("<b>" + Loc.Get("ui.alt_indicator.style_label") + "</b>");

                        bool curSign = Settings.AltStyle == AlternativeIndicatorStyle.SignPrefix;
                        bool curLetter = Settings.AltStyle == AlternativeIndicatorStyle.LetterPrefix;

                        bool newSign = GUILayout.Toggle(curSign, " " + Loc.Get("ui.alt_style_sign"));
                        bool newLetter = GUILayout.Toggle(curLetter, " " + Loc.Get("ui.alt_style_letter"));

                        if (newSign && !curSign)
                        {
                            Settings.AltStyle = AlternativeIndicatorStyle.SignPrefix;
                        }
                        else if (newLetter && !curLetter)
                        {
                            Settings.AltStyle = AlternativeIndicatorStyle.LetterPrefix;
                        }
                        GUILayout.EndVertical();
                    }
                }

                GUILayout.Space(5);
                Settings.EnableDoubleHitIndicator = GUILayout.Toggle(
                    Settings.EnableDoubleHitIndicator,
                    " " + Loc.Get("ui.double_indicator.toggle"));

                GUILayout.Space(5);
                Settings.ColorHitTextBorder = GUILayout.Toggle(
                    Settings.ColorHitTextBorder,
                    " " + Loc.Get("ui.border_color.toggle"));

                GUILayout.Space(5);
                Settings.EnableTrackColoring = GUILayout.Toggle(
                    Settings.EnableTrackColoring,
                    " " + Loc.Get("ui.track_color.toggle"));

                GUILayout.Space(6);
                GUILayout.BeginVertical("box");
                GUILayout.Label("<b>" + Loc.Get("ui.guide.title") + "</b>");
                GUILayout.Label("• <b>" + Loc.Get("ui.guide.default") + "</b>");
                GUILayout.Label("    - " + Loc.Get("ui.guide.default_plus"));
                GUILayout.Label("    - " + Loc.Get("ui.guide.default_alt_sign"));
                GUILayout.Label("    - " + Loc.Get("ui.guide.default_alt_letter"));
                GUILayout.Label("    - " + Loc.Get("ui.guide.default_double"));
                GUILayout.Label("• <b>" + Loc.Get("ui.guide.shortened") + "</b>");
                GUILayout.Label("    - " + Loc.Get("ui.guide.shortened_plus"));
                GUILayout.Label("    - " + Loc.Get("ui.guide.shortened_alt_sign"));
                GUILayout.Label("    - " + Loc.Get("ui.guide.shortened_alt_letter"));
                GUILayout.Label("• <b>" + Loc.Get("ui.guide.ms") + "</b>");
                GUILayout.Label("    - " + Loc.Get("ui.guide.ms_desc"));
                GUILayout.Label("• <b>" + Loc.Get("ui.guide.results") + "</b>");
                GUILayout.Label("    - " + Loc.Get("ui.guide.results_desc"));
                GUILayout.EndVertical();
            }

            GUILayout.EndVertical();

            GUILayout.Space(5);

            // 2. 杂项功能 (Miscellaneous)
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>" + Loc.Get("misc.section") + "</b>");
            GUILayout.Space(5);

            bool oldRing = Settings.HidePlanetRing;
            bool newRing = GUILayout.Toggle(oldRing, " " + Loc.Get("misc.ring_toggle"));
            if (newRing != oldRing)
            {
                Settings.HidePlanetRing = newRing;
                Patches.ApplyHidePlanetRing(newRing);
            }
            GUILayout.EndVertical();
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            Settings.Save(modEntry);
        }
    }
}

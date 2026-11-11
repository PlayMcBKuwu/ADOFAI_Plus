using System;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using HarmonyLib;
using TMPro;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace ADOFAIPlus
{
    public class IndicatorData
    {
        public Color OutlineColor;
        public string DisplayText;
    }

    public static class HitIndicatorStats
    {
        public static int NormalCount = 0;
        public static int LenientCount = 0;

        public static void Reset()
        {
            NormalCount = 0;
            LenientCount = 0;
            Patches.ClearColoredFloors();
        }
    }

    public static class Patches
    {
        public const string NativeHitTextToken = "<NATIVE_HIT_TEXT>";

        public static IndicatorData CurrentIndicator;

        private static readonly FieldInfo RDStringFontDataField = AccessTools.Field(typeof(RDString), "<fontData>k__BackingField");
        private static readonly Dictionary<scrFloor, Color> ColoredFloors = new Dictionary<scrFloor, Color>();

        public static void ClearColoredFloors()
        {
            ColoredFloors.Clear();
        }

        public static void ColorHitFloor(scrFloor floor, Color targetColor)
        {
            if (floor == null) return;
            ColoredFloors[floor] = targetColor;
            ApplyFloorColorInternal(floor, targetColor);
        }

        public static void ApplyFloorColorInternal(scrFloor floor, Color targetColor)
        {
            if (floor == null) return;
            try
            {
                floor.specialColorType = TrackColorType.Single;
                floor.specialColor1 = targetColor;
                floor.specialColor2 = targetColor;

                if (floor.floorRenderer != null)
                {
                    float alpha = floor.floorRenderer.color.a;
                    Color fc = new Color(targetColor.r, targetColor.g, targetColor.b, alpha);
                    floor.floorRenderer.color = fc;
                    floor.floorRenderer.deselectedColor = fc;
                }

                if (floor.topGlow != null)
                {
                    float topA = floor.topGlow.color.a;
                    if (topA <= 0.05f)
                    {
                        float baseGlow = floor.glowMultiplier > 0f ? floor.glowMultiplier : 1f;
                        float floorA = floor.floorRenderer != null ? floor.floorRenderer.color.a : 1f;
                        topA = Mathf.Min(baseGlow, floorA);
                    }
                    floor.topGlow.color = new Color(targetColor.r, targetColor.g, targetColor.b, topA);
                }

                if (floor.bottomGlow != null)
                {
                    float botA = floor.bottomGlow.color.a;
                    if (botA <= 0.05f)
                    {
                        botA = floor.topGlow != null ? floor.topGlow.color.a : 1f;
                    }
                    floor.bottomGlow.color = new Color(targetColor.r, targetColor.g, targetColor.b, botA);
                }
            }
            catch {}
        }

        public static bool IsCjkStart(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '<')
                {
                    int close = s.IndexOf('>', i);
                    if (close > i)
                    {
                        i = close;
                        continue;
                    }
                }
                if (char.IsWhiteSpace(c) || c == '-' || c == '+')
                    continue;
                return c > 0x2E80;
            }
            return false;
        }

        // 缓存最近一次原生判定调用的未截断精确参数（避免 MoveToNextFloor 改变砖块 BPM 或 msDiff 整数截断造成重测失真）
        private static bool _isReEvaluating = false;
        private static bool _hasCachedHitParams = false;
        private static bool _cachedUsedSec = false;
        private static double _cachedTimeDiff = 0.0;
        private static float _cachedHitAngle = 0f;
        private static float _cachedRefAngle = 0f;
        private static bool _cachedIsCW = true;
        private static float _cachedFloorBpm = 100f;
        private static float _cachedConductorPitch = 1f;
        private static double _cachedMarginScale = 1.0;

        public static bool IsInMainMenuOrLobby()
        {
            if (ADOBase.isLevelSelect || ADOBase.isCLS || ADOBase.isFreeroamScene)
                return true;
            if (scrController.instance != null && (!scrController.instance.gameworld || scrController.instance.isPuzzleRoom))
                return true;
            return false;
        }

        public static bool IsInPlayableLevel()
        {
            if (IsInMainMenuOrLobby())
                return false;
            return ADOBase.isScnGame || ADOBase.isLevelEditor || ADOBase.isOfficialLevel || ADOBase.isInternalLevel ||
                   (scrController.instance != null && scrController.instance.gameworld);
        }

        public static bool IsInLevelScene()
        {
            return IsInPlayableLevel();
        }

        public static float GetLevelBPM()
        {
            float baseBpm = 0f;
            if (scrConductor.instance != null && scrConductor.instance.bpm > 0)
            {
                baseBpm = (float)scrConductor.instance.bpm;
            }
            else if (ADOBase.conductor != null && ADOBase.conductor.bpm > 0)
            {
                baseBpm = (float)ADOBase.conductor.bpm;
            }
            else if (ADOBase.editor != null && ADOBase.editor.levelData != null && ADOBase.editor.levelData.bpm > 0f)
            {
                baseBpm = ADOBase.editor.levelData.bpm;
            }
            else if (ADOBase.customLevel != null && ADOBase.customLevel.levelData != null && ADOBase.customLevel.levelData.bpm > 0f)
            {
                baseBpm = ADOBase.customLevel.levelData.bpm;
            }

            float maxFloorSpeed = 0f;
            scrLevelMaker lm = scrLevelMaker.instance != null ? scrLevelMaker.instance : ADOBase.lm;
            if (lm != null && lm.listFloors != null && lm.listFloors.Count > 0)
            {
                for (int i = 0; i < lm.listFloors.Count; i++)
                {
                    scrFloor f = lm.listFloors[i];
                    if (f != null && f.speed > maxFloorSpeed)
                    {
                        maxFloorSpeed = (float)f.speed;
                    }
                }
            }

            float bpm = 0f;
            if (baseBpm > 0f && maxFloorSpeed > 0f)
            {
                bpm = baseBpm * maxFloorSpeed;
            }
            else if (lm != null && lm.highestBPM > 0f)
            {
                bpm = lm.highestBPM;
            }
            else if (ADOBase.customLevel != null && ADOBase.customLevel.highestBPM > 0f)
            {
                bpm = ADOBase.customLevel.highestBPM;
            }
            else if (ADOBase.editor != null && ADOBase.editor.customLevel != null && ADOBase.editor.customLevel.highestBPM > 0f)
            {
                bpm = ADOBase.editor.customLevel.highestBPM;
            }
            else if (baseBpm > 0f)
            {
                bpm = baseBpm;
            }

            float pitch = 1f;
            if (scrConductor.instance != null && scrConductor.instance.song != null && scrConductor.instance.song.pitch > 0f)
            {
                pitch = scrConductor.instance.song.pitch;
            }
            else if (GCS.currentSpeedTrial > 0f)
            {
                pitch = GCS.currentSpeedTrial;
            }

            if (bpm > 0f && pitch > 0f)
            {
                bpm *= pitch;
            }

            return bpm;
        }

        public static void RestoreOriginalState()
        {
            ApplyHidePlanetRing(false);
        }

        public static void ApplyJapaneseFontPreference()
        {
            try
            {
                if (RDConstants.data == null)
                    return;
                if (Loc.CurrentLanguage != SystemLanguage.Japanese)
                    return;

                FontData fd = RDString.GetFontDataForLanguage(SystemLanguage.Japanese);
                if (RDStringFontDataField != null)
                {
                    RDStringFontDataField.SetValue(null, fd);
                }

                if (fd.font != null)
                {
                    Text[] texts = UnityEngine.Object.FindObjectsOfType<Text>(true);
                    if (texts != null)
                    {
                        for (int i = 0; i < texts.Length; i++)
                        {
                            if (texts[i] != null)
                            {
                                texts[i].font = fd.font;
                            }
                        }
                    }
                }

                if (fd.fontTMP != null)
                {
                    Material borderMatAsset = (Main.Settings != null && Main.Settings.BetterJapaneseLocalization && Main.Settings.JapaneseUseChineseFont)
                        ? RDConstants.data.chineseFontTMProXPerfectBorderMaterial
                        : RDConstants.data.japaneseFontTMProXPerfectBorderMaterial;

                    scrHitTextMesh[] hitMeshes = UnityEngine.Object.FindObjectsOfType<scrHitTextMesh>(true);
                    if (hitMeshes != null)
                    {
                        for (int i = 0; i < hitMeshes.Length; i++)
                        {
                            scrHitTextMesh m = hitMeshes[i];
                            if (m != null)
                            {
                                if (m.text != null)
                                {
                                    m.text.font = fd.fontTMP;
                                    if (fd.fontTMP.material != null)
                                    {
                                        m.text.fontSharedMaterial = fd.fontTMP.material;
                                    }
                                }
                                if (m.xPerfectBorder != null)
                                {
                                    m.xPerfectBorder.font = fd.fontTMP;
                                    if (borderMatAsset != null)
                                    {
                                        m.xPerfectBorder.fontMaterial = borderMatAsset;
                                    }
                                    else if (fd.fontTMP.material != null)
                                    {
                                        m.xPerfectBorder.fontSharedMaterial = fd.fontTMP.material;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (Main.Logger != null) Main.Logger.Error("ApplyJapaneseFontPreference failed: " + ex);
            }
        }

        private static TMP_FontAsset cachedClassicFontTMP = null;
        private static Font cachedClassicBaseFont = null;
        private static bool fontLoadAttempted = false;

        public static bool IsLevelEditor()
        {
            try
            {
                if (ADOBase.isLevelEditor) return true;
                if (ADOBase.isEditingLevel) return true;
                if (ADOBase.editor != null) return true;
                if (scnEditor.instance != null) return true;
                if (!string.IsNullOrEmpty(ADOBase.sceneName) && ADOBase.sceneName.IndexOf("Editor", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            catch {}
            return false;
        }

        public static bool IsEditorObject(GameObject go)
        {
            if (go == null) return false;
            if (IsLevelEditor()) return true;
            try
            {
                Transform curr = go.transform;
                while (curr != null)
                {
                    string name = curr.name;
                    if (name != null && (name.IndexOf("editor", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         name.IndexOf("scnEditor", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        return true;
                    }
                    curr = curr.parent;
                }
            }
            catch {}
            return false;
        }

        private static string GetModFontPath()
        {
            if (Main.ModEntry != null && !string.IsNullOrEmpty(Main.ModEntry.Path))
            {
                string p1 = Path.Combine(Main.ModEntry.Path, "Fonts", "站酷快乐体 开源版.ttf");
                if (File.Exists(p1)) return p1;

                string p2 = Path.Combine(Main.ModEntry.Path, "站酷快乐体 开源版.ttf");
                if (File.Exists(p2)) return p2;
            }

            try
            {
                string gameDir = Directory.GetCurrentDirectory();
                if (!string.IsNullOrEmpty(gameDir))
                {
                    string p3 = Path.Combine(gameDir, "Mods", "ADOFAIPlusPlus", "Fonts", "站酷快乐体 开源版.ttf");
                    if (File.Exists(p3)) return p3;

                    string p4 = Path.Combine(gameDir, "Mods", "ADOFAIPlus", "Fonts", "站酷快乐体 开源版.ttf");
                    if (File.Exists(p4)) return p4;
                }
            }
            catch {}

            return null;
        }

        public static TMP_FontAsset GetOrCreateClassicFontTMP()
        {
            if (cachedClassicFontTMP != null) return cachedClassicFontTMP;
            if (fontLoadAttempted && cachedClassicFontTMP == null) return null;

            try
            {
                string fontPath = GetModFontPath();
                if (string.IsNullOrEmpty(fontPath))
                {
                    if (Main.Logger != null) Main.Logger.Log("[ClassicFont] Font file not found in mod directory.");
                    fontLoadAttempted = true;
                    return null;
                }

                try
                {
                    cachedClassicFontTMP = TMP_FontAsset.CreateFontAsset(
                        fontPath,
                        0,
                        90,
                        9,
                        GlyphRenderMode.SDFAA,
                        1024,
                        1024);
                }
                catch (Exception ex)
                {
                    if (Main.Logger != null) Main.Logger.Log("[ClassicFont] CreateFontAsset from path failed, trying fallback from Font: " + ex.Message);
                }

                if (cachedClassicFontTMP == null)
                {
                    cachedClassicBaseFont = new Font(fontPath);
                    cachedClassicBaseFont.name = "站酷快乐体 开源版";
                    UnityEngine.Object.DontDestroyOnLoad(cachedClassicBaseFont);

                    cachedClassicFontTMP = TMP_FontAsset.CreateFontAsset(
                        cachedClassicBaseFont,
                        90,
                        9,
                        GlyphRenderMode.SDFAA,
                        1024,
                        1024,
                        AtlasPopulationMode.Dynamic,
                        true);
                }

                if (cachedClassicFontTMP != null)
                {
                    cachedClassicFontTMP.name = "站酷快乐体 开源版";
                    UnityEngine.Object.DontDestroyOnLoad(cachedClassicFontTMP);

                    if (RDConstants.data != null && RDConstants.data.chineseFontTMPro != null)
                    {
                        if (RDConstants.data.chineseFontTMPro.material != null && RDConstants.data.chineseFontTMPro.material.shader != null)
                        {
                            cachedClassicFontTMP.material.shader = RDConstants.data.chineseFontTMPro.material.shader;
                        }
                    }

                    if (cachedClassicFontTMP.fallbackFontAssetTable == null)
                    {
                        cachedClassicFontTMP.fallbackFontAssetTable = new List<TMP_FontAsset>();
                    }
                    if (RDConstants.data != null)
                    {
                        if (RDConstants.data.chineseFontTMPro != null && !cachedClassicFontTMP.fallbackFontAssetTable.Contains(RDConstants.data.chineseFontTMPro))
                        {
                            cachedClassicFontTMP.fallbackFontAssetTable.Add(RDConstants.data.chineseFontTMPro);
                        }
                        if (RDConstants.data.latinFontTMPro != null && !cachedClassicFontTMP.fallbackFontAssetTable.Contains(RDConstants.data.latinFontTMPro))
                        {
                            cachedClassicFontTMP.fallbackFontAssetTable.Add(RDConstants.data.latinFontTMPro);
                        }
                    }
                    if (Main.Logger != null) Main.Logger.Log("[ClassicFont] Successfully created dynamic TMP_FontAsset for 站酷快乐体 开源版");
                }
            }
            catch (Exception ex)
            {
                if (Main.Logger != null) Main.Logger.Error("[ClassicFont] Failed to create TMP_FontAsset: " + ex);
            }
            finally
            {
                fontLoadAttempted = true;
            }
            return cachedClassicFontTMP;
        }

        public static void ApplyClassicFontPreference()
        {
            try
            {
                if (RDConstants.data == null)
                    return;
                if (Loc.CurrentLanguage != SystemLanguage.ChineseSimplified && RDString.language != SystemLanguage.ChineseSimplified)
                    return;

                bool useClassic = Main.Enabled && Main.Settings != null && Main.Settings.ClassicMode && !IsLevelEditor();

                TMP_FontAsset targetFontTMP = useClassic ? GetOrCreateClassicFontTMP() : RDConstants.data.chineseFontTMPro;
                if (targetFontTMP == null) targetFontTMP = RDConstants.data.chineseFontTMPro;

                FontData fd = RDString.GetFontDataForLanguage(SystemLanguage.ChineseSimplified);
                if (targetFontTMP != null)
                {
                    fd.fontTMP = targetFontTMP;
                }
                if (RDStringFontDataField != null)
                {
                    RDStringFontDataField.SetValue(null, fd);
                }

                scrHitTextMesh[] hitMeshes = UnityEngine.Object.FindObjectsOfType<scrHitTextMesh>(true);
                if (hitMeshes != null && targetFontTMP != null)
                {
                    for (int i = 0; i < hitMeshes.Length; i++)
                    {
                        scrHitTextMesh m = hitMeshes[i];
                        if (m != null && !IsEditorObject(m.gameObject))
                        {
                            if (m.text != null)
                            {
                                m.text.font = targetFontTMP;
                            }
                            if (m.xPerfectBorder != null)
                            {
                                m.xPerfectBorder.font = targetFontTMP;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (Main.Logger != null) Main.Logger.Error("ApplyClassicFontPreference failed: " + ex);
            }
        }

        public static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            try
            {
                if (Main.Enabled && Main.Settings != null)
                {
                    if (IsLevelEditor())
                    {
                        if (RDConstants.data != null)
                        {
                            FontData fd = RDString.GetFontDataForLanguage(RDString.language);
                            if (RDStringFontDataField != null)
                            {
                                RDStringFontDataField.SetValue(null, fd);
                            }
                        }
                    }
                    else
                    {
                        if (Loc.CurrentLanguage == SystemLanguage.ChineseSimplified || RDString.language == SystemLanguage.ChineseSimplified)
                        {
                            ApplyClassicFontPreference();
                        }
                        if (Loc.CurrentLanguage == SystemLanguage.Japanese || RDString.language == SystemLanguage.Japanese)
                        {
                            ApplyJapaneseFontPreference();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (Main.Logger != null) Main.Logger.Error("OnSceneLoaded font apply error: " + ex);
            }
        }

        public static void ApplyHidePlanetRing(bool hide)
        {
            scrRing[] rings = UnityEngine.Object.FindObjectsOfType<scrRing>();
            if (rings != null)
            {
                for (int i = 0; i < rings.Length; i++)
                {
                    if (rings[i] != null && rings[i].line != null)
                    {
                        rings[i].line.enabled = !hide;
                    }
                }
            }
        }

        public static string GetHardcodedTimingText(bool isEarly, bool isDouble)
        {
            SystemLanguage lang = Loc.CurrentLanguage;
            if (lang == SystemLanguage.Chinese || lang == SystemLanguage.ChineseSimplified || lang == SystemLanguage.ChineseTraditional)
            {
                if (isEarly) return isDouble ? "太快！！" : "太快！";
                return isDouble ? "太慢！！" : "太慢！";
            }
            if (lang == SystemLanguage.Korean)
            {
                if (isEarly) return isDouble ? "빠름!!" : "빠름!";
                return isDouble ? "느림!!" : "느림!";
            }
            if (lang == SystemLanguage.Japanese)
            {
                if (Main.Settings != null && Main.Settings.BetterJapaneseLocalization)
                {
                    if (isEarly) return isDouble ? "早すぎ！！" : "早すぎ！";
                    return isDouble ? "遅すぎ！！" : "遅すぎ！";
                }
                if (isEarly) return isDouble ? "Too Early!!" : "Early!";
                return isDouble ? "Late!!" : "Late!";
            }
            if (lang == SystemLanguage.English)
            {
                if (isEarly) return isDouble ? "Too Early!!" : "Early!";
                return isDouble ? "Late!!" : "Late!";
            }

            string word = Loc.Get(isEarly ? "indicator.tooEarly" : "indicator.tooLate");
            string excl = isDouble ? Loc.GetDoubleExcl() : Loc.GetSingleExcl();
            return word + excl;
        }

        public static string GetIndicatorDisplayText(bool isLenient, bool isEarly, bool isDouble, int rawMs)
        {
            HitMarginTextPreset preset = Persistence.hitMarginText;
            string suffix = isLenient ? "L" : "N";

            // 1. 毫秒判定模式 (MsDifference / MsDifferenceColorSign)
            if (preset == HitMarginTextPreset.MsDifference || preset == HitMarginTextPreset.MsDifferenceColorSign)
            {
                string sign = (rawMs < 0 || (rawMs == 0 && isEarly)) ? "-" : "+";
                if (isDouble) sign = sign + sign; // -- 或 ++
                int absMs = Math.Abs(rawMs);
                string excl = isDouble ? Loc.GetDoubleExcl() : string.Empty;
                return string.Format("{0}{1}ms{2}{3}", sign, absMs, suffix, excl);
            }

            // 2. 精简判定模式 (Shortened)
            if (preset == HitMarginTextPreset.Shortened)
            {
                if (Main.Settings.EnableHitIndicatorPlus && Main.Settings.EnableAlternativeIndicatorPlus)
                {
                    if (Main.Settings.AltStyle == AlternativeIndicatorStyle.SignPrefix)
                    {
                        string word = isLenient ? "LEN" : "NOR";
                        string excl = isDouble ? "!!" : string.Empty;
                        if (isEarly)
                        {
                            return "-" + word + excl;
                        }
                        else
                        {
                            return word + excl + "+";
                        }
                    }
                    else
                    {
                        string dir = isEarly ? (isDouble ? "TE" : "E") : (isDouble ? "TL" : "L");
                        return suffix + dir;
                    }
                }
                if (Main.Settings.EnableHitIndicatorPlus)
                {
                    return (isLenient ? "LEN" : "NOR") + (isDouble ? "!!" : string.Empty);
                }
                return null;
            }

            // 3. 默认文字模式 (Default / EarlyOrLate 等)
            if (!Main.Settings.EnableHitIndicatorPlus)
            {
                return null;
            }

            string exclText = isDouble ? Loc.GetDoubleExcl() : Loc.GetSingleExcl();

            if (Main.Settings.EnableAlternativeIndicatorPlus)
            {
                if (Main.Settings.AltStyle == AlternativeIndicatorStyle.SignPrefix)
                {
                    // 另类指示器+（早-在前 / 晚+在后）：-标判！/ -宽判！！ (太快)；标判！+ / 宽判！！+ (太慢)
                    string word = isLenient ? Loc.Get("indicator.lenient") : Loc.Get("indicator.normal");
                    if (isEarly)
                    {
                        return "-" + word + exclText;
                    }
                    else
                    {
                        return word + exclText + "+";
                    }
                }
                else
                {
                    // 另类指示器+（字符前缀）：完全硬编码对应语言词汇 (如 L-太快！/ N-太慢！/ L-Early! / N-Late!)
                    string timingText = GetHardcodedTimingText(isEarly, isDouble);
                    return suffix + "-" + timingText;
                }
            }

            // 标准判定指示器+：纯正硬编码文字「宽判！」/「标判！」(双感叹号时: 宽判！！/ 标判！！)
            string baseWord = isLenient ? Loc.Get("indicator.lenient") : Loc.Get("indicator.normal");
            return baseWord + exclText;
        }

        [HarmonyPatch(typeof(scrMisc), "GetHitMarginInSec")]
        public static class Patch_ScrMisc_GetHitMarginInSec
        {
            public static void Postfix(double timeDiff, float floorBpm, float conductorPitch, double marginScale)
            {
                if (_isReEvaluating) return;
                _hasCachedHitParams = true;
                _cachedUsedSec = true;
                _cachedTimeDiff = timeDiff;
                _cachedFloorBpm = floorBpm;
                _cachedConductorPitch = conductorPitch;
                _cachedMarginScale = marginScale;
            }
        }

        [HarmonyPatch(typeof(scrMisc), "GetHitMarginInDeg")]
        public static class Patch_ScrMisc_GetHitMarginInDeg
        {
            public static void Postfix(float hitAngle, float refAngle, bool clockwise, float floorBpm, float conductorPitch, double marginScale)
            {
                if (_isReEvaluating) return;
                _hasCachedHitParams = true;
                _cachedUsedSec = false;
                _cachedHitAngle = hitAngle;
                _cachedRefAngle = refAngle;
                _cachedIsCW = clockwise;
                _cachedFloorBpm = floorBpm;
                _cachedConductorPitch = conductorPitch;
                _cachedMarginScale = marginScale;
            }
        }

        private static HitMargin EvaluateDifficultyMargin(Difficulty diff, double fallbackTimeDiffSec, float fallbackBpm, float fallbackPitch, double fallbackMarginScale)
        {
            _isReEvaluating = true;
            try
            {
                if (_hasCachedHitParams)
                {
                    if (_cachedUsedSec)
                    {
                        return scrMisc.GetHitMarginInSec(diff, _cachedTimeDiff, _cachedFloorBpm, _cachedConductorPitch, _cachedMarginScale);
                    }
                    else
                    {
                        return scrMisc.GetHitMarginInDeg(diff, _cachedHitAngle, _cachedRefAngle, _cachedIsCW, _cachedFloorBpm, _cachedConductorPitch, _cachedMarginScale);
                    }
                }
                return scrMisc.GetHitMarginInSec(diff, fallbackTimeDiffSec, fallbackBpm, fallbackPitch, fallbackMarginScale);
            }
            finally
            {
                _isReEvaluating = false;
            }
        }

        [HarmonyPatch(typeof(scrHitTextManager), "ShowHitText")]
        public static class Patch_ShowHitText
        {
            public static void Prefix(HitMargin hitMargin, scrPlanet planet, float missAngle, int? msDiffNullable)
            {
                CurrentIndicator = null;

                if (!Main.Enabled || !Main.Settings.EnableHitIndicator)
                    return;

                bool isCounted = (hitMargin == HitMargin.VeryEarly || hitMargin == HitMargin.VeryLate);
                bool isDoubleFail = (hitMargin == HitMargin.TooEarly || hitMargin == HitMargin.TooLate ||
                                     hitMargin == HitMargin.OverPress || hitMargin == HitMargin.FailMiss ||
                                     hitMargin == HitMargin.FailOverload);

                if (!isCounted && !(Main.Settings.EnableDoubleHitIndicator && isDoubleFail))
                    return;

                // 核心守卫 1：若当前已是「严格判定 (Strict)」，任何及格命中 (VeryEarly / VeryLate) 本身就已通过严判，绝不可能是标判或宽判保命！
                if (isCounted && GCS.difficulty == Difficulty.Strict)
                    return;

                scrPlanet effectivePlanet = planet;
                if (effectivePlanet == null && scrController.instance != null)
                {
                    effectivePlanet = scrController.instance.chosenPlanet;
                }

                float floorBpmVal = _hasCachedHitParams ? _cachedFloorBpm : 100f;
                float pitchVal = _hasCachedHitParams ? _cachedConductorPitch : 1f;
                double marginScale = _hasCachedHitParams ? _cachedMarginScale : 1.0;

                if (!_hasCachedHitParams)
                {
                    if (effectivePlanet != null && effectivePlanet.conductor != null)
                    {
                        floorBpmVal = (float)(effectivePlanet.conductor.bpm * effectivePlanet.planetarySystem.speed);
                        if (effectivePlanet.conductor.song != null)
                            pitchVal = effectivePlanet.conductor.song.pitch;
                        if (effectivePlanet.currfloor != null)
                            marginScale = effectivePlanet.currfloor.marginScale;
                    }
                    else if (scrConductor.instance != null)
                    {
                        floorBpmVal = (float)scrConductor.instance.bpm;
                        if (scrConductor.instance.song != null)
                            pitchVal = scrConductor.instance.song.pitch;
                    }
                }

                if (floorBpmVal <= 0f) floorBpmVal = 100f;
                if (pitchVal <= 0f) pitchVal = 1f;

                bool isEarly = false;
                if (msDiffNullable.HasValue)
                {
                    isEarly = msDiffNullable.Value < 0;
                }
                else if (hitMargin == HitMargin.VeryEarly || hitMargin == HitMargin.TooEarly || hitMargin == HitMargin.OverPress)
                {
                    isEarly = true;
                }
                else if (hitMargin == HitMargin.VeryLate || hitMargin == HitMargin.TooLate || hitMargin == HitMargin.FailMiss || hitMargin == HitMargin.FailOverload)
                {
                    isEarly = false;
                }
                else
                {
                    isEarly = missAngle < 0f;
                }

                double timeDiffSec;
                int rawMs;
                if (_hasCachedHitParams && _cachedUsedSec)
                {
                    timeDiffSec = _cachedTimeDiff;
                    rawMs = msDiffNullable.HasValue ? msDiffNullable.Value : (int)Math.Round(timeDiffSec * 1000.0);
                }
                else if (msDiffNullable.HasValue)
                {
                    rawMs = msDiffNullable.Value;
                    timeDiffSec = (double)rawMs / 1000.0;
                }
                else
                {
                    timeDiffSec = scrMisc.AngleToTime((double)missAngle, (double)floorBpmVal) / (double)pitchVal;
                    rawMs = (int)Math.Round(timeDiffSec * 1000.0);
                }

                // 依次用游戏原生精确参数测算 Strict / Normal / Lenient
                HitMargin strictRes = EvaluateDifficultyMargin(Difficulty.Strict, timeDiffSec, floorBpmVal, pitchVal, marginScale);
                bool isStrictPass = HitMarginHelper.IsCounted(strictRes, HitMarginLimit.None);

                // 核心守卫 2：只要严判成立（无论处于何种难度或练习模式倍速），均不触发标判/宽判指示器！
                if (isStrictPass)
                {
                    return;
                }

                HitMargin normalRes = EvaluateDifficultyMargin(Difficulty.Normal, timeDiffSec, floorBpmVal, pitchVal, marginScale);
                HitMargin lenientRes = EvaluateDifficultyMargin(Difficulty.Lenient, timeDiffSec, floorBpmVal, pitchVal, marginScale);

                bool isNormalTier = HitMarginHelper.IsCounted(normalRes, HitMarginLimit.None);
                bool isLenientTier = !isNormalTier && HitMarginHelper.IsCounted(lenientRes, HitMarginLimit.None);

                // 核心守卫 3：若当前处于「标准判定 (Normal)」且本次为及格命中 (isCounted)，它必然已通过标判，绝不可能降级为宽判
                if (isCounted && GCS.difficulty == Difficulty.Normal)
                {
                    isNormalTier = true;
                    isLenientTier = false;
                }

                if (!isNormalTier && !isLenientTier)
                {
                    return;
                }

                if (isCounted)
                {
                    if (isNormalTier)
                    {
                        HitIndicatorStats.NormalCount++;
                    }
                    else if (isLenientTier)
                    {
                        HitIndicatorStats.LenientCount++;
                    }

                    // 轨道染色：当打出及格的「标判！」和「宽判！」时对应给脚下轨道砖块染灰色和绿色
                    if (Main.Settings.EnableTrackColoring && effectivePlanet != null && effectivePlanet.currfloor != null)
                    {
                        Color trackColor = isNormalTier
                            ? new Color(0.55f, 0.56f, 0.58f, 1f)
                            : new Color(0.15f, 0.92f, 0.32f, 1f);
                        ColorHitFloor(effectivePlanet.currfloor, trackColor);
                    }
                }

                CurrentIndicator = new IndicatorData();
                CurrentIndicator.OutlineColor = isNormalTier
                    ? new Color(0.60f, 0.62f, 0.66f, 1f)
                    : new Color(0.15f, 0.95f, 0.35f, 1f);

                CurrentIndicator.DisplayText = GetIndicatorDisplayText(isLenientTier, isEarly, !isCounted, rawMs);
            }

            public static void Postfix()
            {
                _hasCachedHitParams = false;
            }
        }



        [HarmonyPatch(typeof(scrHitTextMesh), "Show")]
        public static class Patch_ScrHitTextMesh_Show
        {
            [HarmonyPriority(Priority.Last)]
            public static void Postfix(scrHitTextMesh __instance, float fadeSpeed, string customText)
            {
                if (!string.IsNullOrEmpty(customText))
                    return;

                if (CurrentIndicator == null || !__instance.gameObject.activeSelf)
                    return;

                if (!string.IsNullOrEmpty(CurrentIndicator.DisplayText) && __instance.text != null)
                {
                    __instance.text.text = CurrentIndicator.DisplayText;
                }

                if (__instance.xPerfectBorder != null)
                {
                    Color borderCol = CurrentIndicator.OutlineColor;
                    bool colorShaderBorder = (Main.Settings == null || Main.Settings.ColorHitTextBorder);

                    // 确保 xPerfectBorder 使用与主文本一致的字体 asset 与 material，彻底避免 CJK 缺字漏显
                    if (__instance.text != null && __instance.xPerfectBorder.font != __instance.text.font)
                    {
                        __instance.xPerfectBorder.font = __instance.text.font;
                        __instance.xPerfectBorder.fontSharedMaterial = __instance.text.fontSharedMaterial;
                    }

                    __instance.xPerfectBorder.gameObject.SetActive(true);
                    if (__instance.text != null)
                    {
                        __instance.xPerfectBorder.text = __instance.text.text;
                    }
                    __instance.xPerfectBorder.color = borderCol;

                    if (colorShaderBorder)
                    {
                        __instance.xPerfectBorder.faceColor = borderCol;
                        __instance.xPerfectBorder.outlineColor = borderCol;
                    }
                    else
                    {
                        __instance.xPerfectBorder.faceColor = Color.white;
                        __instance.xPerfectBorder.outlineColor = Color.white;
                    }

                    try
                    {
                        Material borderMat = __instance.xPerfectBorder.fontMaterial;
                        if (borderMat != null)
                        {
                            Color matCol = colorShaderBorder ? borderCol : Color.white;
                            if (borderMat.HasProperty(ShaderUtilities.ID_OutlineColor))
                                borderMat.SetColor(ShaderUtilities.ID_OutlineColor, matCol);
                            if (borderMat.HasProperty(ShaderUtilities.ID_FaceColor))
                                borderMat.SetColor(ShaderUtilities.ID_FaceColor, Color.white);
                            if (borderMat.HasProperty(ShaderUtilities.ID_UnderlayColor))
                                borderMat.SetColor(ShaderUtilities.ID_UnderlayColor, matCol);
                            if (borderMat.HasProperty(ShaderUtilities.ID_GlowColor))
                                borderMat.SetColor(ShaderUtilities.ID_GlowColor, matCol);
                        }
                    }
                    catch {}

                    __instance.xPerfectBorder.DOKill();
                    __instance.xPerfectBorder.DOFade(0f, 0.7f / fadeSpeed)
                        .SetDelay(0.5f / fadeSpeed)
                        .SetEase(Ease.OutQuad);
                }
            }
        }

        [HarmonyPatch(typeof(scrFloor), "SetToRandomColor")]
        public static class Patch_ScrFloor_SetToRandomColor
        {
            public static void Postfix(scrFloor __instance)
            {
                if (!Main.Enabled || Main.Settings == null || !Main.Settings.EnableHitIndicator || !Main.Settings.EnableTrackColoring)
                    return;

                Color targetColor;
                if (__instance != null && ColoredFloors.Count > 0 && ColoredFloors.TryGetValue(__instance, out targetColor))
                {
                    ApplyFloorColorInternal(__instance, targetColor);
                }
            }
        }

        [HarmonyPatch(typeof(scrFloor), "ColorFloor")]
        public static class Patch_ScrFloor_ColorFloor
        {
            public static void Postfix(scrFloor __instance)
            {
                if (!Main.Enabled || Main.Settings == null || !Main.Settings.EnableHitIndicator || !Main.Settings.EnableTrackColoring)
                    return;

                Color targetColor;
                if (__instance != null && ColoredFloors.Count > 0 && ColoredFloors.TryGetValue(__instance, out targetColor))
                {
                    ApplyFloorColorInternal(__instance, targetColor);
                }
            }
        }

        [HarmonyPatch(typeof(RDString), "GetWithCheck")]
        public static class Patch_RDString_GetWithCheck
        {
            public static void Postfix(string key, ref bool exists, ref string __result)
            {
                if (!Main.Enabled || Main.Settings == null || !Main.Settings.BetterJapaneseLocalization)
                    return;

                if (Loc.CurrentLanguage != SystemLanguage.Japanese)
                    return;

                string jpVal;
                if (Loc.TryGetBetterJapaneseRDString(key, out jpVal))
                {
                    exists = true;
                    __result = jpVal;
                }
            }
        }

        [HarmonyPatch(typeof(RDString), "fontData", MethodType.Getter)]
        public static class Patch_RDString_fontData_Getter
        {
            public static void Postfix(ref FontData __result)
            {
                if (Main.Enabled &&
                    Main.Settings != null &&
                    Main.Settings.ClassicMode &&
                    (Loc.CurrentLanguage == SystemLanguage.ChineseSimplified || RDString.language == SystemLanguage.ChineseSimplified))
                {
                    if (IsLevelEditor())
                    {
                        if (RDConstants.data != null && RDConstants.data.chineseFontTMPro != null)
                        {
                            __result.fontTMP = RDConstants.data.chineseFontTMPro;
                        }
                    }
                    else
                    {
                        TMP_FontAsset cft = GetOrCreateClassicFontTMP();
                        if (cft != null) __result.fontTMP = cft;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(RDString), "GetFontDataForLanguage")]
        public static class Patch_RDString_GetFontDataForLanguage
        {
            public static void Prefix(ref SystemLanguage language)
            {
                if (Main.Enabled &&
                    Main.Settings != null &&
                    Main.Settings.BetterJapaneseLocalization &&
                    Main.Settings.JapaneseUseChineseFont &&
                    language == SystemLanguage.Japanese)
                {
                    language = SystemLanguage.ChineseSimplified;
                }
            }

            public static void Postfix(SystemLanguage language, ref FontData __result)
            {
                if (Main.Enabled &&
                    Main.Settings != null &&
                    Main.Settings.ClassicMode &&
                    language == SystemLanguage.ChineseSimplified &&
                    !IsLevelEditor())
                {
                    TMP_FontAsset cft = GetOrCreateClassicFontTMP();
                    if (cft != null) __result.fontTMP = cft;
                }
            }
        }

        [HarmonyPatch(typeof(RDString), "GetAppropiateFontForString")]
        public static class Patch_RDString_GetAppropiateFontForString
        {
            public static void Postfix(ref Font __result)
            {
                if (Main.Enabled &&
                    Main.Settings != null &&
                    Main.Settings.BetterJapaneseLocalization &&
                    Main.Settings.JapaneseUseChineseFont &&
                    Loc.CurrentLanguage == SystemLanguage.Japanese &&
                    RDConstants.data != null &&
                    RDConstants.data.chineseFont != null &&
                    __result == RDConstants.data.japaneseFont)
                {
                    __result = RDConstants.data.chineseFont;
                }
            }
        }

        [HarmonyPatch(typeof(RDString), "GetInlinedFontForString")]
        public static class Patch_RDString_GetInlinedFontForString
        {
            public static void Postfix(ref string __result)
            {
                if (Main.Enabled &&
                    Main.Settings != null &&
                    Main.Settings.BetterJapaneseLocalization &&
                    Main.Settings.JapaneseUseChineseFont &&
                    Loc.CurrentLanguage == SystemLanguage.Japanese &&
                    !string.IsNullOrEmpty(__result) &&
                    RDConstants.data != null &&
                    RDConstants.data.japaneseFont != null &&
                    RDConstants.data.chineseFont != null)
                {
                    string jpName = RDConstants.data.japaneseFont.name;
                    string cnName = RDConstants.data.chineseFont.name;
                    if (!string.IsNullOrEmpty(jpName) && !string.IsNullOrEmpty(cnName) && __result.IndexOf(jpName) >= 0)
                    {
                        __result = __result.Replace("<font=\"" + jpName + "\">", "<font=\"" + cnName + "\">");
                    }
                }

                if (Main.Enabled &&
                    Main.Settings != null &&
                    Main.Settings.ClassicMode &&
                    (Loc.CurrentLanguage == SystemLanguage.ChineseSimplified || RDString.language == SystemLanguage.ChineseSimplified) &&
                    !IsLevelEditor() &&
                    !string.IsNullOrEmpty(__result) &&
                    RDConstants.data != null)
                {
                    TMP_FontAsset cft = GetOrCreateClassicFontTMP();
                    if (cft != null)
                    {
                        string cftName = cft.name;
                        if (RDConstants.data.chineseFont != null)
                        {
                            string cnName = RDConstants.data.chineseFont.name;
                            if (!string.IsNullOrEmpty(cnName) && __result.IndexOf(cnName) >= 0)
                            {
                                __result = __result.Replace("<font=\"" + cnName + "\">", "<font=\"" + cftName + "\">");
                            }
                        }
                        if (RDConstants.data.chineseFontTMPro != null)
                        {
                            string cnTmpName = RDConstants.data.chineseFontTMPro.name;
                            if (!string.IsNullOrEmpty(cnTmpName) && __result.IndexOf(cnTmpName) >= 0)
                            {
                                __result = __result.Replace("<font=\"" + cnTmpName + "\">", "<font=\"" + cftName + "\">");
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(scnEditor), "Awake")]
        public static class Patch_ScnEditor_Awake
        {
            public static void Postfix()
            {
                if (Main.Enabled && Main.Settings != null && Main.Settings.ClassicMode)
                {
                    try
                    {
                        if (RDConstants.data != null)
                        {
                            FontData fd = RDString.GetFontDataForLanguage(RDString.language);
                            if (RDStringFontDataField != null)
                            {
                                RDStringFontDataField.SetValue(null, fd);
                            }
                        }
                    }
                    catch {}
                }
            }
        }

        [HarmonyPatch(typeof(ADOFAI.ErrorMeterTick), "Show")]
        public static class Patch_ErrorMeterTick_Show
        {
            public static void Postfix(ADOFAI.ErrorMeterTick __instance, string tweenId, float tweenDuration)
            {
                if (CurrentIndicator == null)
                    return;

                if (__instance.tickOutline != null)
                {
                    string outlineTweenId = tweenId + "o";
                    DOTween.Kill(outlineTweenId);
                    __instance.tickOutline.color = CurrentIndicator.OutlineColor;
                    __instance.tickOutline.DOColor(Color.clear, tweenDuration)
                        .SetEase(Ease.InQuad)
                        .SetId(outlineTweenId);
                }
            }
        }

        [HarmonyPatch(typeof(DetailedResults), "GenerateResults")]
        public static class Patch_DetailedResults_GenerateResults
        {
            public static void Postfix(scrMarginTracker marginTracker, ref string __result)
            {
                if (!Main.Enabled || !Main.Settings.EnableHitIndicator)
                    return;

                if (HitIndicatorStats.NormalCount > 0 || HitIndicatorStats.LenientCount > 0)
                {
                    string normalLabel = Loc.Get("results.normal");
                    string lenientLabel = Loc.Get("results.lenient");

                    string indicatorLine = string.Format(
                        "<color=#aaaaaa>{0}: {1}</color>     <color=#26ff59>{2}: {3}</color>\n",
                        normalLabel, HitIndicatorStats.NormalCount,
                        lenientLabel, HitIndicatorStats.LenientCount);

                    string lateKey = RDString.Get("status.results.late", null) + ": ";
                    int lateIdx = __result.IndexOf(lateKey);
                    if (lateIdx >= 0)
                    {
                        int newlineIdx = __result.IndexOf('\n', lateIdx);
                        if (newlineIdx >= 0)
                        {
                            __result = __result.Insert(newlineIdx + 1, indicatorLine);
                        }
                        else
                        {
                            __result += indicatorLine;
                        }
                    }
                    else
                    {
                        string earlyKey = RDString.Get("status.results.early", null) + ": ";
                        int earlyIdx = __result.IndexOf(earlyKey);
                        if (earlyIdx >= 0)
                        {
                            int newlineIdx = __result.IndexOf('\n', earlyIdx);
                            if (newlineIdx >= 0)
                            {
                                __result = __result.Insert(newlineIdx + 1, indicatorLine);
                            }
                            else
                            {
                                __result += indicatorLine;
                            }
                        }
                        else
                        {
                            __result += indicatorLine;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(scrMarginTracker), "Reset")]
        public static class Patch_ScrMarginTracker_Reset
        {
            public static void Postfix()
            {
                HitIndicatorStats.Reset();
            }
        }

        [HarmonyPatch(typeof(scrController), "Start")]
        public static class Patch_ScrController_Start
        {
            public static void Prefix()
            {
                HitIndicatorStats.Reset();
            }


        }

        [HarmonyPatch(typeof(scrRing), "Update")]
        public static class Patch_ScrRing_Update
        {
            public static void Postfix(scrRing __instance)
            {
                if (Main.Enabled && Main.Settings.HidePlanetRing)
                {
                    if (__instance.line != null && __instance.line.enabled)
                    {
                        __instance.line.enabled = false;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(scrRing), "Switch")]
        public static class Patch_ScrRing_Switch
        {
            public static void Postfix(scrRing __instance)
            {
                if (Main.Enabled && Main.Settings.HidePlanetRing)
                {
                    if (__instance.line != null && __instance.line.enabled)
                    {
                        __instance.line.enabled = false;
                    }
                }
            }
        }
    }
}

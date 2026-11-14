using System;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using HarmonyLib;
using TMPro;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore;
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
            ApplyClassicFontPreference();
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
        private static TMP_FontAsset cachedClassicFallbackFontTMP = null;
        private static Font cachedClassicBaseFont = null;
        private static Font cachedClassicUIFont = null;
        private static Material cachedClassicBorderMaterial = null;
        private static bool fontLoadAttempted = false;
        private static bool fallbackFontLoadAttempted = false;
        private static bool uiFontLoadAttempted = false;
        private static bool origConstantsCaptured = false;
        private static bool classicConstantsSwapped = false;

        private static Font origChineseFont = null;
        private static TMP_FontAsset origChineseFontTMP = null;
        private static TMP_FontAsset origChineseFontTMPDuplicate = null;
        private static Material origChineseBorderMaterial = null;

        private static readonly Dictionary<int, Font> OriginalUIFonts = new Dictionary<int, Font>();
        private static readonly Dictionary<int, Font> OriginalTextMeshFonts = new Dictionary<int, Font>();
        private static readonly Dictionary<int, TMP_FontAsset> OriginalTMPFonts = new Dictionary<int, TMP_FontAsset>();
        private static readonly Dictionary<int, Material> OriginalTMPMaterials = new Dictionary<int, Material>();
        private static readonly Dictionary<int, Material> ClassicMaterialCache = new Dictionary<int, Material>();
        private static readonly HashSet<int> ClassicMaterialIds = new HashSet<int>();
        private static readonly Dictionary<Assembly, bool> SafeGameAssemblies = new Dictionary<Assembly, bool>();

        public static bool IsClassicFontActive()
        {
            return Main.Enabled &&
                   Main.Settings != null &&
                   Main.Settings.ClassicMode &&
                   (Loc.CurrentLanguage == SystemLanguage.ChineseSimplified || RDString.language == SystemLanguage.ChineseSimplified);
        }


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

        private static void CaptureOriginalChineseConstants()
        {
            if (origConstantsCaptured || RDConstants.data == null) return;
            origConstantsCaptured = true;
            origChineseFont = RDConstants.data.chineseFont;
            origChineseFontTMP = RDConstants.data.chineseFontTMPro;
            origChineseFontTMPDuplicate = RDConstants.data.chineseFontTMProDuplicate;
            origChineseBorderMaterial = RDConstants.data.chineseFontTMProXPerfectBorderMaterial;
        }

        private static string FindClassicFontFile(params string[] fileNames)
        {
            List<string> baseDirs = new List<string>();
            if (Main.ModEntry != null && !string.IsNullOrEmpty(Main.ModEntry.Path))
            {
                baseDirs.Add(Path.Combine(Main.ModEntry.Path, "Fonts"));
                baseDirs.Add(Main.ModEntry.Path);
            }
            try
            {
                string gameDir = Directory.GetCurrentDirectory();
                if (!string.IsNullOrEmpty(gameDir))
                {
                    baseDirs.Add(Path.Combine(gameDir, "Mods", "ADOFAIPlus", "Fonts"));
                    baseDirs.Add(Path.Combine(gameDir, "Mods", "ADOFAIPlusPlus", "Fonts"));
                }
            }
            catch {}

            for (int d = 0; d < baseDirs.Count; d++)
            {
                string dir = baseDirs[d];
                if (string.IsNullOrEmpty(dir)) continue;
                for (int f = 0; f < fileNames.Length; f++)
                {
                    string candidate = Path.Combine(dir, fileNames[f]);
                    if (File.Exists(candidate)) return candidate;
                }
            }
            return null;
        }

        private static string FindClassicFontPath()
        {
            return FindClassicFontFile("zhankuhappy_normalized.ttf", "ClassicFont.ttf", "站酷快乐体 开源版.ttf");
        }

        private static string FindClassicFallbackFontPath()
        {
            return FindClassicFontFile("站酷快乐体 开源版.ttf");
        }

        public static Material GetOrCreateClassicBorderMaterial()
        {
            if (cachedClassicBorderMaterial != null) return cachedClassicBorderMaterial;
            CaptureOriginalChineseConstants();
            try
            {
                TMP_FontAsset cft = GetOrCreateClassicFontTMP();
                Material srcBorder = origChineseBorderMaterial != null
                    ? origChineseBorderMaterial
                    : (RDConstants.data != null ? RDConstants.data.chineseFontTMProXPerfectBorderMaterial : null);
                if (cft != null && srcBorder != null)
                {
                    cachedClassicBorderMaterial = new Material(srcBorder);
                    cachedClassicBorderMaterial.name = "ClassicFontTMProXPerfectBorderMaterial";
                    if (cft.atlasTexture != null)
                    {
                        cachedClassicBorderMaterial.SetTexture(ShaderUtilities.ID_MainTex, cft.atlasTexture);
                    }
                    if (cachedClassicBorderMaterial.HasProperty(ShaderUtilities.ID_TextureWidth))
                    {
                        cachedClassicBorderMaterial.SetFloat(ShaderUtilities.ID_TextureWidth, (float)cft.atlasWidth);
                    }
                    if (cachedClassicBorderMaterial.HasProperty(ShaderUtilities.ID_TextureHeight))
                    {
                        cachedClassicBorderMaterial.SetFloat(ShaderUtilities.ID_TextureHeight, (float)cft.atlasHeight);
                    }
                    float gradScale = (cft.material != null && cft.material.HasProperty(ShaderUtilities.ID_GradientScale))
                        ? cft.material.GetFloat(ShaderUtilities.ID_GradientScale)
                        : (float)(cft.atlasPadding + 1);
                    if (cachedClassicBorderMaterial.HasProperty(ShaderUtilities.ID_GradientScale))
                    {
                        cachedClassicBorderMaterial.SetFloat(ShaderUtilities.ID_GradientScale, gradScale);
                    }
                    ShaderUtilities.UpdateShaderRatios(cachedClassicBorderMaterial);
                    UnityEngine.Object.DontDestroyOnLoad(cachedClassicBorderMaterial);
                    ClassicMaterialIds.Add(cachedClassicBorderMaterial.GetInstanceID());
                }
            }
            catch (Exception ex)
            {
                if (Main.Logger != null) Main.Logger.Error("[ClassicFont] Failed to create border material: " + ex);
            }
            return cachedClassicBorderMaterial;
        }

        public static Font GetOrCreateClassicUIFont()
        {
            if (cachedClassicUIFont != null) return cachedClassicUIFont;
            if (RDConstants.data == null) return null;
            if (uiFontLoadAttempted && cachedClassicUIFont == null) return null;

            CaptureOriginalChineseConstants();
            try
            {
                string fontPath = FindClassicFontPath();
                if (string.IsNullOrEmpty(fontPath))
                {
                    uiFontLoadAttempted = true;
                    return null;
                }

                if (cachedClassicBaseFont == null)
                {
                    cachedClassicBaseFont = new Font(fontPath);
                    cachedClassicBaseFont.name = "ClassicFont";
                    UnityEngine.Object.DontDestroyOnLoad(cachedClassicBaseFont);
                }

                cachedClassicUIFont = cachedClassicBaseFont;
                if (Main.Logger != null) Main.Logger.Log("[ClassicFont] UI Font initialized from asset file: " + fontPath);
            }
            catch (Exception ex)
            {
                if (Main.Logger != null) Main.Logger.Error("[ClassicFont] Failed to initialize UI Font: " + ex);
            }
            finally
            {
                uiFontLoadAttempted = true;
            }
            return cachedClassicUIFont;
        }

        private static void ConfigureClassicTMPMaterial(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null || fontAsset.material == null) return;
            CaptureOriginalChineseConstants();
            TMP_FontAsset refFont = origChineseFontTMP != null
                ? origChineseFontTMP
                : (RDConstants.data != null ? RDConstants.data.chineseFontTMPro : null);
            if (refFont != null && refFont.material != null && refFont.material.shader != null)
            {
                fontAsset.material.shader = refFont.material.shader;
            }
            if (fontAsset.atlasTexture != null)
            {
                fontAsset.material.SetTexture(ShaderUtilities.ID_MainTex, fontAsset.atlasTexture);
            }
            if (fontAsset.material.HasProperty(ShaderUtilities.ID_TextureWidth))
            {
                fontAsset.material.SetFloat(ShaderUtilities.ID_TextureWidth, (float)fontAsset.atlasWidth);
            }
            if (fontAsset.material.HasProperty(ShaderUtilities.ID_TextureHeight))
            {
                fontAsset.material.SetFloat(ShaderUtilities.ID_TextureHeight, (float)fontAsset.atlasHeight);
            }
            float gradScale = (float)(fontAsset.atlasPadding + 1);
            if (fontAsset.material.HasProperty(ShaderUtilities.ID_GradientScale))
            {
                fontAsset.material.SetFloat(ShaderUtilities.ID_GradientScale, gradScale);
            }
            ShaderUtilities.UpdateShaderRatios(fontAsset.material);
            ClassicMaterialIds.Add(fontAsset.material.GetInstanceID());
        }

        private static TMP_FontAsset CreateClassicFallbackOpenSourceTMP(UnityEngine.TextCore.FaceInfo mainFi)
        {
            if (cachedClassicFallbackFontTMP != null) return cachedClassicFallbackFontTMP;
            if (fallbackFontLoadAttempted) return null;
            fallbackFontLoadAttempted = true;

            try
            {
                string fbPath = FindClassicFallbackFontPath();
                string mainPath = FindClassicFontPath();
                if (string.IsNullOrEmpty(fbPath) || string.Equals(fbPath, mainPath, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                cachedClassicFallbackFontTMP = TMP_FontAsset.CreateFontAsset(
                    fbPath,
                    0,
                    48,
                    9,
                    GlyphRenderMode.SDFAA,
                    1024,
                    1024);

                if (cachedClassicFallbackFontTMP != null)
                {
                    cachedClassicFallbackFontTMP.name = "ClassicFontFallbackOpenSource";
                    cachedClassicFallbackFontTMP.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                    cachedClassicFallbackFontTMP.isMultiAtlasTexturesEnabled = true;
                    UnityEngine.Object.DontDestroyOnLoad(cachedClassicFallbackFontTMP);
                    ConfigureClassicTMPMaterial(cachedClassicFallbackFontTMP);

                    // 开源版 has slightly compressed OS/2 typo metrics; align vertical metrics to primary normalized font
                    UnityEngine.TextCore.FaceInfo fbFi = cachedClassicFallbackFontTMP.faceInfo;
                    fbFi.scale = 1.0f;
                    if (mainFi.pointSize > 0)
                    {
                        float ratio = (fbFi.pointSize > 0 ? (float)fbFi.pointSize : 48f) / (float)mainFi.pointSize;
                        if (mainFi.lineHeight > 0) fbFi.lineHeight = mainFi.lineHeight * ratio;
                        if (mainFi.ascentLine > 0) fbFi.ascentLine = mainFi.ascentLine * ratio;
                        if (mainFi.capLine > 0) fbFi.capLine = mainFi.capLine * ratio;
                        if (mainFi.meanLine > 0) fbFi.meanLine = mainFi.meanLine * ratio;
                        if (mainFi.descentLine != 0) fbFi.descentLine = mainFi.descentLine * ratio;
                    }
                    cachedClassicFallbackFontTMP.faceInfo = fbFi;
                    cachedClassicFallbackFontTMP.TryAddCharacters("()[]\\^_`|");
                }
            }
            catch (Exception ex)
            {
                if (Main.Logger != null) Main.Logger.Log("[ClassicFont] Optional fallback TMP creation skipped: " + ex.Message);
            }
            return cachedClassicFallbackFontTMP;
        }

        public static TMP_FontAsset GetOrCreateClassicFontTMP()
        {
            if (cachedClassicFontTMP != null) return cachedClassicFontTMP;
            if (RDConstants.data == null) return null;
            if (fontLoadAttempted && cachedClassicFontTMP == null) return null;

            CaptureOriginalChineseConstants();
            try
            {
                string fontPath = FindClassicFontPath();
                if (string.IsNullOrEmpty(fontPath))
                {
                    if (Main.Logger != null) Main.Logger.Log("[ClassicFont] Font file not found in mod directory.");
                    fontLoadAttempted = true;
                    return null;
                }

                // Prefer direct file-path SDF generation with padding=9 -> _GradientScale=10 matching XPerfectBorder
                try
                {
                    cachedClassicFontTMP = TMP_FontAsset.CreateFontAsset(
                        fontPath,
                        0,
                        48,
                        9,
                        GlyphRenderMode.SDFAA,
                        2048,
                        2048);
                }
                catch (Exception ex)
                {
                    if (Main.Logger != null) Main.Logger.Log("[ClassicFont] CreateFontAsset from path failed: " + ex.Message);
                }

                if (cachedClassicFontTMP == null)
                {
                    Font baseFont = GetOrCreateClassicUIFont();
                    if (baseFont != null)
                    {
                        try
                        {
                            cachedClassicFontTMP = TMP_FontAsset.CreateFontAsset(
                                baseFont,
                                48,
                                9,
                                GlyphRenderMode.SDFAA,
                                2048,
                                2048,
                                AtlasPopulationMode.Dynamic,
                                true);
                        }
                        catch (Exception ex)
                        {
                            if (Main.Logger != null) Main.Logger.Log("[ClassicFont] CreateFontAsset from Font failed: " + ex.Message);
                        }
                    }
                }

                if (cachedClassicFontTMP != null)
                {
                    cachedClassicFontTMP.name = "ClassicFontSDF";
                    cachedClassicFontTMP.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                    cachedClassicFontTMP.isMultiAtlasTexturesEnabled = true;
                    UnityEngine.Object.DontDestroyOnLoad(cachedClassicFontTMP);

                    ConfigureClassicTMPMaterial(cachedClassicFontTMP);

                    UnityEngine.TextCore.FaceInfo fi = cachedClassicFontTMP.faceInfo;
                    fi.scale = 1.0f;
                    cachedClassicFontTMP.faceInfo = fi;

                    string coreChars = " !\"#$%&'*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz{}~" +
                                       "，。！？：；“”‘’（）《》【】、—…·～＋－×÷＝％" +
                                       "经典模式回到最初的美好标判宽判严判太快太慢完美过早过晚极佳很好纯净练习自定移动判定按键速度关卡曲目艺术家设置返回主菜单退出开启关闭确定取消帮助说明关于暂停重试结算偏移校准";
                    bool prePop = cachedClassicFontTMP.TryAddCharacters(coreChars);
                    if (Main.Logger != null)
                    {
                        Main.Logger.Log("[ClassicFont] Pre-populate core characters: " + prePop +
                                        ", glyphCount=" + cachedClassicFontTMP.characterTable.Count +
                                        ", facePointSize=" + fi.pointSize + ", ascent=" + fi.ascentLine + ", descent=" + fi.descentLine);
                    }

                    try
                    {
                        MethodInfo addFontMethod = typeof(MaterialReferenceManager).GetMethod(
                            "AddFontAsset",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                            null,
                            new Type[] { typeof(TMP_FontAsset) },
                            null);
                        if (addFontMethod != null)
                        {
                            addFontMethod.Invoke(null, new object[] { cachedClassicFontTMP });
                        }
                    }
                    catch {}

                    if (cachedClassicFontTMP.fallbackFontAssetTable == null)
                    {
                        cachedClassicFontTMP.fallbackFontAssetTable = new List<TMP_FontAsset>();
                    }

                    // 1. Secondary fallback from 开源版 covers 9 ASCII symbols missing in zhankuhappy_normalized
                    TMP_FontAsset openSourceFb = CreateClassicFallbackOpenSourceTMP(fi);
                    if (openSourceFb != null && !cachedClassicFontTMP.fallbackFontAssetTable.Contains(openSourceFb))
                    {
                        cachedClassicFontTMP.fallbackFontAssetTable.Add(openSourceFb);
                    }

                    // 2. Game built-in CJK / Latin fallbacks for rare glyphs
                    TMP_FontAsset[] builtinFallbacks = new TMP_FontAsset[]
                    {
                        origChineseFontTMP,
                        RDConstants.data != null ? RDConstants.data.latinFontTMPro : null,
                        RDConstants.data != null ? RDConstants.data.japaneseFontTMPro : null,
                        RDConstants.data != null ? RDConstants.data.koreanFontTMPro : null
                    };
                    for (int i = 0; i < builtinFallbacks.Length; i++)
                    {
                        TMP_FontAsset fb = builtinFallbacks[i];
                        if (fb != null && fb != cachedClassicFontTMP && !cachedClassicFontTMP.fallbackFontAssetTable.Contains(fb))
                        {
                            cachedClassicFontTMP.fallbackFontAssetTable.Add(fb);
                        }
                    }

                    GetOrCreateClassicUIFont();
                    GetOrCreateClassicBorderMaterial();

                    if (Main.Logger != null) Main.Logger.Log("[ClassicFont] Successfully initialized dynamic TMP_FontAsset from " + fontPath);
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

        private static bool IsCustomDecorationFont(GameObject go, Font font)
        {
            if (go == null || font == null || RDConstants.data == null) return false;
            if (go.GetComponent<scrTextDecoration>() == null) return false;
            return font == RDConstants.data.arialFont ||
                   font == RDConstants.data.comicSansMSFont ||
                   font == RDConstants.data.courierNewFont ||
                   font == RDConstants.data.georgiaFont ||
                   font == RDConstants.data.impactFont ||
                   font == RDConstants.data.timesNewRomanFont;
        }

        private static bool IsSafeGameAssembly(Assembly asm)
        {
            if (asm == null) return false;
            bool safe;
            if (SafeGameAssemblies.TryGetValue(asm, out safe)) return safe;
            string name = asm.GetName().Name;
            safe = string.Equals(name, "Assembly-CSharp", StringComparison.Ordinal) ||
                   string.Equals(name, "Assembly-CSharp-firstpass", StringComparison.Ordinal) ||
                   string.Equals(name, "Unity.TextMeshPro", StringComparison.Ordinal) ||
                   string.Equals(name, "DOTween", StringComparison.Ordinal) ||
                   string.Equals(name, "DOTweenModules", StringComparison.Ordinal) ||
                   string.Equals(name, "RDTools", StringComparison.Ordinal) ||
                   name.StartsWith("UnityEngine", StringComparison.Ordinal) ||
                   name.StartsWith("Unity.", StringComparison.Ordinal);
            SafeGameAssemblies[asm] = safe;
            return safe;
        }

        private static bool IsAssemblyCSharp(Assembly asm)
        {
            if (asm == null) return false;
            string name = asm.GetName().Name;
            return string.Equals(name, "Assembly-CSharp", StringComparison.Ordinal) ||
                   string.Equals(name, "Assembly-CSharp-firstpass", StringComparison.Ordinal);
        }

        private static bool HasModNamePrefix(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return name.IndexOf("UnityModManager", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("ModManager", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("Overlayer", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("KeyViewer", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("AdofaiTweaks", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("PlanetTweaks", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("UnityExplorer", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("UniverseLib", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("BepInEx", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsThirdPartyModObject(GameObject go)
        {
            if (go == null) return true;
            bool inDontDestroyOnLoad = (go.scene.name == "DontDestroyOnLoad");
            bool foundGameMonoBehaviour = false;

            Transform cur = go.transform;
            int depth = 0;
            while (cur != null && depth < 32)
            {
                if (HasModNamePrefix(cur.name)) return true;

                Component[] comps = cur.GetComponents<Component>();
                if (comps != null)
                {
                    for (int i = 0; i < comps.Length; i++)
                    {
                        Component c = comps[i];
                        if (c == null) continue;
                        Assembly asm = c.GetType().Assembly;
                        if (!IsSafeGameAssembly(asm))
                            return true;
                        if (IsAssemblyCSharp(asm))
                            foundGameMonoBehaviour = true;
                    }
                }
                cur = cur.parent;
                depth++;
            }

            if (inDontDestroyOnLoad && !foundGameMonoBehaviour)
                return true;

            return false;
        }

        private static bool FontNameStartsWithAny(string name, params string[] prefixes)
        {
            if (string.IsNullOrEmpty(name)) return false;
            for (int i = 0; i < prefixes.Length; i++)
            {
                if (name.IndexOf(prefixes[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private static bool IsReplaceableUIFont(Font f, bool requireKnownGameFont)
        {
            if (f == null) return false;
            if (cachedClassicUIFont != null && f == cachedClassicUIFont) return false;
            if (cachedClassicBaseFont != null && f == cachedClassicBaseFont) return false;

            string fn = f.name;
            if (!string.IsNullOrEmpty(fn))
            {
                if (fn.IndexOf("Arial", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    fn.IndexOf("LegacyRuntime", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    fn.IndexOf("LiberationSans", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    fn.IndexOf("Microsoft YaHei", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    fn.IndexOf("SimHei", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return false;
                }
            }

            if (!requireKnownGameFont) return true;

            if (origChineseFont != null && f == origChineseFont) return true;
            if (RDConstants.data != null &&
                (f == RDConstants.data.chineseFont ||
                 f == RDConstants.data.latinFont ||
                 f == RDConstants.data.japaneseFont ||
                 f == RDConstants.data.koreanFont))
            {
                return true;
            }

            return FontNameStartsWithAny(fn, "SourceHanSans", "Chinese", "cjkFonts", "AvenirNext", "godoMaum", "MPLUS");
        }

        private static bool ShouldSkipTMPFontReplacement(TMP_FontAsset fontAsset, bool requireKnownGameFont)
        {
            if (fontAsset == null) return true;
            if (cachedClassicFontTMP != null && fontAsset == cachedClassicFontTMP) return true;
            if (cachedClassicFallbackFontTMP != null && fontAsset == cachedClassicFallbackFontTMP) return true;

            string name = fontAsset.name;
            if (!string.IsNullOrEmpty(name))
            {
                if (name.IndexOf("Icon", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Glyph", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Rewired", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Symbol", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Sprite", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Emoji", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("LiberationSans", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Arial", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Bangers", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Anton", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Oswald", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Roboto", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Electronic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Unity SDF", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            if (!requireKnownGameFont) return false;

            if (origChineseFontTMP != null && fontAsset == origChineseFontTMP) return false;
            if (origChineseFontTMPDuplicate != null && fontAsset == origChineseFontTMPDuplicate) return false;
            if (RDConstants.data != null &&
                (fontAsset == RDConstants.data.chineseFontTMPro ||
                 fontAsset == RDConstants.data.chineseFontTMProDuplicate ||
                 fontAsset == RDConstants.data.latinFontTMPro ||
                 fontAsset == RDConstants.data.japaneseFontTMPro ||
                 fontAsset == RDConstants.data.koreanFontTMPro))
            {
                return false;
            }

            if (FontNameStartsWithAny(name, "cjkFonts", "SourceHanSans", "ChineseSimplified", "ChineseTraditional", "godoMaum", "Latin-", "MPLUS"))
                return false;

            return true;
        }

        public static Material GetOrCloneClassicMaterial(Material srcMat, TMP_FontAsset cft)
        {
            if (cft == null) return srcMat;
            if (srcMat == null) return cft.material;
            if (cft.material != null && srcMat == cft.material) return cft.material;
            if (ClassicMaterialIds.Contains(srcMat.GetInstanceID()))
            {
                if (cft.atlasTexture != null && srcMat.mainTexture != cft.atlasTexture)
                {
                    srcMat.SetTexture(ShaderUtilities.ID_MainTex, cft.atlasTexture);
                }
                return srcMat;
            }

            if ((origChineseFontTMP != null && srcMat == origChineseFontTMP.material) ||
                (origChineseFontTMPDuplicate != null && srcMat == origChineseFontTMPDuplicate.material))
            {
                return cft.material;
            }

            int srcId = srcMat.GetInstanceID();
            Material cached;
            if (ClassicMaterialCache.TryGetValue(srcId, out cached) && cached != null)
            {
                if (cft.atlasTexture != null && cached.mainTexture != cft.atlasTexture)
                {
                    cached.SetTexture(ShaderUtilities.ID_MainTex, cft.atlasTexture);
                }
                return cached;
            }

            try
            {
                Material cloned = new Material(srcMat);
                cloned.name = (srcMat.name ?? "TMP") + " (Classic)";
                if (cloned.shader != null &&
                    cloned.shader.name.IndexOf("Bitmap", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    cft.material != null && cft.material.shader != null)
                {
                    cloned.shader = cft.material.shader;
                }

                if (cft.atlasTexture != null)
                {
                    cloned.SetTexture(ShaderUtilities.ID_MainTex, cft.atlasTexture);
                }
                if (cloned.HasProperty(ShaderUtilities.ID_TextureWidth))
                {
                    cloned.SetFloat(ShaderUtilities.ID_TextureWidth, (float)cft.atlasWidth);
                }
                if (cloned.HasProperty(ShaderUtilities.ID_TextureHeight))
                {
                    cloned.SetFloat(ShaderUtilities.ID_TextureHeight, (float)cft.atlasHeight);
                }
                float gradScale = (cft.material != null && cft.material.HasProperty(ShaderUtilities.ID_GradientScale))
                    ? cft.material.GetFloat(ShaderUtilities.ID_GradientScale)
                    : (float)(cft.atlasPadding + 1);
                if (cloned.HasProperty(ShaderUtilities.ID_GradientScale))
                {
                    cloned.SetFloat(ShaderUtilities.ID_GradientScale, gradScale);
                }
                ShaderUtilities.UpdateShaderRatios(cloned);
                UnityEngine.Object.DontDestroyOnLoad(cloned);

                ClassicMaterialCache[srcId] = cloned;
                ClassicMaterialIds.Add(cloned.GetInstanceID());
                return cloned;
            }
            catch (Exception ex)
            {
                if (Main.Logger != null) Main.Logger.Log("[ClassicFont] Material clone fallback: " + ex.Message);
                return cft.material;
            }
        }

        public static void ApplyClassicFontToText(Text t, bool useClassic, Font cf, bool requireKnownGameFont)
        {
            if (t == null || IsEditorObject(t.gameObject) || IsThirdPartyModObject(t.gameObject)) return;
            int id = t.GetInstanceID();
            if (useClassic)
            {
                if (cf == null || t.font == cf) return;
                if (IsCustomDecorationFont(t.gameObject, t.font)) return;
                if (!IsReplaceableUIFont(t.font, requireKnownGameFont)) return;
                if (!OriginalUIFonts.ContainsKey(id))
                {
                    OriginalUIFonts[id] = t.font;
                }
                t.font = cf;
            }
            else
            {
                Font orig;
                if (OriginalUIFonts.TryGetValue(id, out orig) && orig != null)
                {
                    if (t.font != orig) t.font = orig;
                }
                else if (cf != null && t.font == cf && (origChineseFont != null || (RDConstants.data != null && RDConstants.data.chineseFont != null)))
                {
                    t.font = origChineseFont != null ? origChineseFont : RDConstants.data.chineseFont;
                }
            }
        }

        public static void ApplyClassicFontToTextMesh(TextMesh tm, bool useClassic, Font cf, bool requireKnownGameFont)
        {
            if (tm == null || IsEditorObject(tm.gameObject) || IsThirdPartyModObject(tm.gameObject)) return;
            int id = tm.GetInstanceID();
            if (useClassic)
            {
                if (cf == null || tm.font == cf) return;
                if (IsCustomDecorationFont(tm.gameObject, tm.font)) return;
                if (!IsReplaceableUIFont(tm.font, requireKnownGameFont)) return;
                if (!OriginalTextMeshFonts.ContainsKey(id))
                {
                    OriginalTextMeshFonts[id] = tm.font;
                }
                tm.font = cf;
                Renderer r = tm.GetComponent<Renderer>();
                if (r != null && cf.material != null) r.sharedMaterial = cf.material;
            }
            else
            {
                Font orig;
                if (OriginalTextMeshFonts.TryGetValue(id, out orig) && orig != null)
                {
                    if (tm.font != orig)
                    {
                        tm.font = orig;
                        Renderer r = tm.GetComponent<Renderer>();
                        if (r != null && orig.material != null) r.sharedMaterial = orig.material;
                    }
                }
                else if (cf != null && tm.font == cf && (origChineseFont != null || (RDConstants.data != null && RDConstants.data.chineseFont != null)))
                {
                    Font restoreF = origChineseFont != null ? origChineseFont : RDConstants.data.chineseFont;
                    tm.font = restoreF;
                    Renderer r = tm.GetComponent<Renderer>();
                    if (r != null && restoreF.material != null) r.sharedMaterial = restoreF.material;
                }
            }
        }

        public static void ApplyClassicFontToTMP(TMP_Text tmp, bool useClassic, TMP_FontAsset cft, Material borderMat, bool requireKnownGameFont, Material overrideSrcMat)
        {
            if (tmp == null || IsEditorObject(tmp.gameObject) || IsThirdPartyModObject(tmp.gameObject)) return;
            int id = tmp.GetInstanceID();

            scrHitTextMesh hitParent = tmp.GetComponentInParent<scrHitTextMesh>();
            bool isXPerfectBorder = (hitParent != null && hitParent.xPerfectBorder == tmp) ||
                                    string.Equals(tmp.name, "xPerfectBorder", StringComparison.Ordinal);

            if (isXPerfectBorder)
            {
                if (useClassic)
                {
                    if (!OriginalTMPFonts.ContainsKey(id)) OriginalTMPFonts[id] = tmp.font;
                    if (!OriginalTMPMaterials.ContainsKey(id)) OriginalTMPMaterials[id] = tmp.fontSharedMaterial;
                    if (cft != null && tmp.font != cft) tmp.font = cft;
                    Material curM = tmp.fontSharedMaterial;
                    if (borderMat != null &&
                        (curM == null || (curM != borderMat && !ClassicMaterialIds.Contains(curM.GetInstanceID()))))
                    {
                        tmp.fontSharedMaterial = borderMat;
                    }
                }
                else
                {
                    TMP_FontAsset origF;
                    if (OriginalTMPFonts.TryGetValue(id, out origF) && origF != null && tmp.font != origF)
                    {
                        tmp.font = origF;
                    }
                    else if (cft != null && tmp.font == cft && origChineseFontTMP != null)
                    {
                        tmp.font = origChineseFontTMP;
                    }

                    Material origM;
                    if (OriginalTMPMaterials.TryGetValue(id, out origM) && origM != null && tmp.fontSharedMaterial != origM)
                    {
                        tmp.fontSharedMaterial = origM;
                    }
                    else if (origChineseBorderMaterial != null && tmp.font == origChineseFontTMP)
                    {
                        tmp.fontSharedMaterial = origChineseBorderMaterial;
                    }
                }
                return;
            }

            if (useClassic)
            {
                if (cft == null) return;
                bool alreadyClassic = (tmp.font == cft);
                if (!alreadyClassic && ShouldSkipTMPFontReplacement(tmp.font, requireKnownGameFont)) return;

                Material srcMat = overrideSrcMat != null ? overrideSrcMat : tmp.fontSharedMaterial;
                if (!alreadyClassic)
                {
                    if (!OriginalTMPFonts.ContainsKey(id)) OriginalTMPFonts[id] = tmp.font;
                    if (!OriginalTMPMaterials.ContainsKey(id)) OriginalTMPMaterials[id] = tmp.fontSharedMaterial;
                    tmp.font = cft;
                }

                Material targetMat = GetOrCloneClassicMaterial(srcMat, cft);
                if (targetMat != null && tmp.fontSharedMaterial != targetMat)
                {
                    tmp.fontSharedMaterial = targetMat;
                }
            }
            else
            {
                TMP_FontAsset origF;
                if (OriginalTMPFonts.TryGetValue(id, out origF) && origF != null)
                {
                    if (tmp.font != origF) tmp.font = origF;
                }
                else if (cft != null && tmp.font == cft && origChineseFontTMP != null)
                {
                    tmp.font = origChineseFontTMP;
                }

                Material origM;
                if (OriginalTMPMaterials.TryGetValue(id, out origM) && origM != null && tmp.fontSharedMaterial != origM)
                {
                    tmp.fontSharedMaterial = origM;
                }
            }
        }

        public static void ApplyClassicFontToHierarchy(GameObject root, bool useClassic, Font cf, TMP_FontAsset cft, Material borderMat, bool requireKnownGameFont)
        {
            if (root == null || IsEditorObject(root) || IsThirdPartyModObject(root)) return;
            Text[] texts = root.GetComponentsInChildren<Text>(true);
            if (texts != null)
            {
                for (int i = 0; i < texts.Length; i++)
                    ApplyClassicFontToText(texts[i], useClassic, cf, requireKnownGameFont);
            }
            TMP_Text[] tmps = root.GetComponentsInChildren<TMP_Text>(true);
            if (tmps != null)
            {
                for (int i = 0; i < tmps.Length; i++)
                    ApplyClassicFontToTMP(tmps[i], useClassic, cft, borderMat, requireKnownGameFont, null);
            }
            TextMesh[] meshes = root.GetComponentsInChildren<TextMesh>(true);
            if (meshes != null)
            {
                for (int i = 0; i < meshes.Length; i++)
                    ApplyClassicFontToTextMesh(meshes[i], useClassic, cf, requireKnownGameFont);
            }
        }

        private static void SyncFontFallbackTable(TMP_FontAsset hostFont, bool useClassic, TMP_FontAsset cft)
        {
            if (hostFont == null || cft == null || hostFont == cft) return;
            if (hostFont.fallbackFontAssetTable == null)
            {
                if (!useClassic) return;
                hostFont.fallbackFontAssetTable = new List<TMP_FontAsset>();
            }
            if (useClassic)
            {
                hostFont.fallbackFontAssetTable.Remove(cft);
                hostFont.fallbackFontAssetTable.Insert(0, cft);
            }
            else
            {
                hostFont.fallbackFontAssetTable.Remove(cft);
            }
        }

        public static void ApplyClassicFontPreference()
        {
            try
            {
                if (RDConstants.data == null)
                    return;

                CaptureOriginalChineseConstants();

                bool isSimplifiedChinese = (Loc.CurrentLanguage == SystemLanguage.ChineseSimplified || RDString.language == SystemLanguage.ChineseSimplified);
                bool useClassic = isSimplifiedChinese && IsClassicFontActive() && !IsLevelEditor();

                if (!isSimplifiedChinese && !useClassic)
                {
                    if (!classicConstantsSwapped && OriginalUIFonts.Count == 0 && OriginalTMPFonts.Count == 0 && OriginalTextMeshFonts.Count == 0)
                        return;
                }

                Font targetUIFont = useClassic ? GetOrCreateClassicUIFont() : origChineseFont;
                if (targetUIFont == null) targetUIFont = origChineseFont;

                TMP_FontAsset targetFontTMP = useClassic ? GetOrCreateClassicFontTMP() : origChineseFontTMP;
                if (targetFontTMP == null) targetFontTMP = origChineseFontTMP;

                Material targetBorderMat = useClassic ? GetOrCreateClassicBorderMaterial() : origChineseBorderMaterial;
                if (targetBorderMat == null) targetBorderMat = origChineseBorderMaterial;

                if (useClassic)
                {
                    if (targetUIFont != null) RDConstants.data.chineseFont = targetUIFont;
                    if (targetFontTMP != null)
                    {
                        RDConstants.data.chineseFontTMPro = targetFontTMP;
                        RDConstants.data.chineseFontTMProDuplicate = targetFontTMP;
                    }
                    if (targetBorderMat != null) RDConstants.data.chineseFontTMProXPerfectBorderMaterial = targetBorderMat;
                    classicConstantsSwapped = true;
                }
                else if (classicConstantsSwapped)
                {
                    if (origChineseFont != null) RDConstants.data.chineseFont = origChineseFont;
                    if (origChineseFontTMP != null) RDConstants.data.chineseFontTMPro = origChineseFontTMP;
                    if (origChineseFontTMPDuplicate != null) RDConstants.data.chineseFontTMProDuplicate = origChineseFontTMPDuplicate;
                    if (origChineseBorderMaterial != null) RDConstants.data.chineseFontTMProXPerfectBorderMaterial = origChineseBorderMaterial;
                    classicConstantsSwapped = false;
                }

                if (isSimplifiedChinese)
                {
                    FontData fd = RDString.GetFontDataForLanguage(SystemLanguage.ChineseSimplified);
                    if (targetUIFont != null)
                    {
                        fd.font = targetUIFont;
                        fd.lineSpacing = 1.0f;
                    }
                    if (targetFontTMP != null)
                    {
                        fd.fontTMP = targetFontTMP;
                        fd.fontScale = 1.0f;
                        fd.lineSpacingTMP = 1.0f;
                    }
                    if (RDStringFontDataField != null)
                    {
                        RDStringFontDataField.SetValue(null, fd);
                    }
                }

                if (cachedClassicFontTMP != null)
                {
                    SyncFontFallbackTable(RDConstants.data.latinFontTMPro, useClassic, cachedClassicFontTMP);
                    SyncFontFallbackTable(origChineseFontTMP, useClassic, cachedClassicFontTMP);
                    SyncFontFallbackTable(origChineseFontTMPDuplicate, useClassic, cachedClassicFontTMP);
                }

                Font cfForSweep = useClassic ? targetUIFont : cachedClassicUIFont;
                TMP_FontAsset cftForSweep = useClassic ? targetFontTMP : cachedClassicFontTMP;

                Text[] texts = UnityEngine.Object.FindObjectsOfType<Text>(true);
                if (texts != null)
                {
                    for (int i = 0; i < texts.Length; i++)
                        ApplyClassicFontToText(texts[i], useClassic, cfForSweep, true);
                }

                TMP_Text[] tmps = UnityEngine.Object.FindObjectsOfType<TMP_Text>(true);
                if (tmps != null)
                {
                    for (int i = 0; i < tmps.Length; i++)
                        ApplyClassicFontToTMP(tmps[i], useClassic, cftForSweep, targetBorderMat, true, null);
                }

                TextMesh[] meshes = UnityEngine.Object.FindObjectsOfType<TextMesh>(true);
                if (meshes != null)
                {
                    for (int i = 0; i < meshes.Length; i++)
                        ApplyClassicFontToTextMesh(meshes[i], useClassic, cfForSweep, true);
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
                        ApplyClassicFontPreference();
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



        [HarmonyPatch(typeof(scrHitTextMesh), "Init")]
        public static class Patch_ScrHitTextMesh_Init
        {
            public static void Postfix(scrHitTextMesh __instance)
            {
                if (!Main.Enabled || Main.Settings == null) return;
                if (!Main.Settings.ClassicMode || IsLevelEditor()) return;
                if (Loc.CurrentLanguage != SystemLanguage.ChineseSimplified && RDString.language != SystemLanguage.ChineseSimplified) return;

                TMP_FontAsset cft = GetOrCreateClassicFontTMP();
                if (cft != null)
                {
                    if (__instance.text != null)
                    {
                        __instance.text.font = cft;
                    }
                    if (__instance.xPerfectBorder != null)
                    {
                        __instance.xPerfectBorder.font = cft;
                        Material borderMat = GetOrCreateClassicBorderMaterial();
                        if (borderMat != null)
                        {
                            __instance.xPerfectBorder.fontMaterial = borderMat;
                        }
                    }
                }
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

                    bool useClassic = Main.Enabled && Main.Settings != null && Main.Settings.ClassicMode && !IsLevelEditor() &&
                        (Loc.CurrentLanguage == SystemLanguage.ChineseSimplified || RDString.language == SystemLanguage.ChineseSimplified);

                    TMP_FontAsset targetFont = useClassic ? GetOrCreateClassicFontTMP() : (__instance.text != null ? __instance.text.font : null);
                    if (targetFont != null && __instance.xPerfectBorder.font != targetFont)
                    {
                        __instance.xPerfectBorder.font = targetFont;
                    }

                    Material borderMatAsset = null;
                    if (useClassic)
                    {
                        borderMatAsset = GetOrCreateClassicBorderMaterial();
                    }
                    else if (RDConstants.data != null)
                    {
                        if (__instance.xPerfectBorder.font == RDConstants.data.chineseFontTMPro)
                            borderMatAsset = RDConstants.data.chineseFontTMProXPerfectBorderMaterial;
                        else if (__instance.xPerfectBorder.font == RDConstants.data.japaneseFontTMPro)
                            borderMatAsset = RDConstants.data.japaneseFontTMProXPerfectBorderMaterial;
                    }

                    if (borderMatAsset != null &&
                        (__instance.xPerfectBorder.fontSharedMaterial == null ||
                         __instance.xPerfectBorder.fontSharedMaterial.mainTexture != borderMatAsset.mainTexture ||
                         (useClassic && !ClassicMaterialIds.Contains(__instance.xPerfectBorder.fontSharedMaterial.GetInstanceID())) ||
                         (!useClassic && ClassicMaterialIds.Contains(__instance.xPerfectBorder.fontSharedMaterial.GetInstanceID()))))
                    {
                        __instance.xPerfectBorder.fontSharedMaterial = borderMatAsset;
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
                        Material curBorderMat = __instance.xPerfectBorder.fontMaterial;
                        if (curBorderMat != null)
                        {
                            Color matCol = colorShaderBorder ? borderCol : Color.white;
                            if (curBorderMat.HasProperty(ShaderUtilities.ID_OutlineColor))
                                curBorderMat.SetColor(ShaderUtilities.ID_OutlineColor, matCol);
                            if (curBorderMat.HasProperty(ShaderUtilities.ID_FaceColor))
                                curBorderMat.SetColor(ShaderUtilities.ID_FaceColor, Color.white);
                            if (curBorderMat.HasProperty(ShaderUtilities.ID_UnderlayColor))
                                curBorderMat.SetColor(ShaderUtilities.ID_UnderlayColor, matCol);
                            if (curBorderMat.HasProperty(ShaderUtilities.ID_GlowColor))
                                curBorderMat.SetColor(ShaderUtilities.ID_GlowColor, matCol);
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
                if (RDConstants.data == null) return;
                bool active = IsClassicFontActive();
                if (!active && cachedClassicUIFont == null && cachedClassicFontTMP == null) return;

                CaptureOriginalChineseConstants();
                Font cf = active ? GetOrCreateClassicUIFont() : cachedClassicUIFont;
                TMP_FontAsset cft = active ? GetOrCreateClassicFontTMP() : cachedClassicFontTMP;
                Material bm = active ? GetOrCreateClassicBorderMaterial() : cachedClassicBorderMaterial;
                if (active)
                {
                    if (IsLevelEditor())
                    {
                        if (origChineseFont != null) __result.font = origChineseFont;
                        else if (RDConstants.data.chineseFont != null) __result.font = RDConstants.data.chineseFont;
                        if (origChineseFontTMP != null) __result.fontTMP = origChineseFontTMP;
                        else if (RDConstants.data.chineseFontTMPro != null) __result.fontTMP = RDConstants.data.chineseFontTMPro;
                    }
                    else
                    {
                        if (cf != null) { __result.font = cf; __result.lineSpacing = 1.0f; }
                        if (cft != null) { __result.fontTMP = cft; __result.lineSpacingTMP = 1.0f; }
                        __result.fontScale = 1.0f;
                    }
                }
                else
                {
                    if (cf != null && __result.font == cf && origChineseFont != null)
                        __result.font = origChineseFont;
                    if (cft != null && __result.fontTMP == cft && origChineseFontTMP != null)
                        __result.fontTMP = origChineseFontTMP;
                }
                if (bm != null && active && !IsLevelEditor() && RDConstants.data.chineseFontTMProXPerfectBorderMaterial != bm)
                {
                    RDConstants.data.chineseFontTMProXPerfectBorderMaterial = bm;
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
                if (IsClassicFontActive() &&
                    language == SystemLanguage.ChineseSimplified &&
                    !IsLevelEditor())
                {
                    Font cf = GetOrCreateClassicUIFont();
                    if (cf != null) { __result.font = cf; __result.lineSpacing = 1.0f; }
                    TMP_FontAsset cft = GetOrCreateClassicFontTMP();
                    if (cft != null) { __result.fontTMP = cft; __result.lineSpacingTMP = 1.0f; }
                    __result.fontScale = 1.0f;
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
                    return;
                }

                if (IsClassicFontActive() &&
                    !IsLevelEditor() &&
                    RDConstants.data != null &&
                    (__result == RDConstants.data.chineseFont || __result == RDConstants.data.latinFont))
                {
                    Font cf = GetOrCreateClassicUIFont();
                    if (cf != null) __result = cf;
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

                if (IsClassicFontActive() &&
                    !IsLevelEditor() &&
                    !string.IsNullOrEmpty(__result) &&
                    RDConstants.data != null)
                {
                    TMP_FontAsset cft = GetOrCreateClassicFontTMP();
                    if (cft != null)
                    {
                        string cftName = !string.IsNullOrEmpty(cft.name) ? cft.name : "ClassicFontSDF";
                        string[] oldNames = new string[] {
                            origChineseFont != null ? origChineseFont.name : null,
                            origChineseFontTMP != null ? origChineseFontTMP.name : null,
                            RDConstants.data.chineseFont != null ? RDConstants.data.chineseFont.name : null,
                            RDConstants.data.chineseFontTMPro != null ? RDConstants.data.chineseFontTMPro.name : null,
                            RDConstants.data.latinFont != null ? RDConstants.data.latinFont.name : null,
                            RDConstants.data.latinFontTMPro != null ? RDConstants.data.latinFontTMPro.name : null
                        };
                        for (int i = 0; i < oldNames.Length; i++)
                        {
                            string oldN = oldNames[i];
                            if (!string.IsNullOrEmpty(oldN) && !string.Equals(oldN, cftName, StringComparison.Ordinal) && __result.IndexOf(oldN) >= 0)
                            {
                                __result = __result.Replace("<font=\"" + oldN + "\">", "<font=\"" + cftName + "\">");
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(RDString), "SetLocalizedFont", new Type[] { typeof(Text) })]
        public static class Patch_RDString_SetLocalizedFont_Text
        {
            public static void Postfix(Text text)
            {
                if (text == null || IsEditorObject(text.gameObject)) return;
                if (IsClassicFontActive())
                {
                    Font cf = GetOrCreateClassicUIFont();
                    if (cf != null) ApplyClassicFontToText(text, true, cf, false);
                }
            }
        }

        [HarmonyPatch(typeof(RDString), "SetLocalizedFont", new Type[] { typeof(TMP_Text) })]
        public static class Patch_RDString_SetLocalizedFont_TMP
        {
            public struct State
            {
                public TMP_FontAsset PrevFont;
                public Material PrevMat;
                public float PrevSize;
                public float PrevLineSpacing;
            }

            public static void Prefix(TMP_Text text, out State __state)
            {
                __state = new State();
                if (text != null)
                {
                    __state.PrevFont = text.font;
                    __state.PrevMat = text.fontSharedMaterial;
                    __state.PrevSize = text.fontSize;
                    __state.PrevLineSpacing = text.lineSpacing;
                }
            }

            public static void Postfix(TMP_Text text, State __state)
            {
                if (text == null || IsEditorObject(text.gameObject) || IsThirdPartyModObject(text.gameObject)) return;
                bool prevWasChinese = (__state.PrevFont != null) &&
                    (__state.PrevFont == origChineseFontTMP ||
                     __state.PrevFont == origChineseFontTMPDuplicate ||
                     (cachedClassicFontTMP != null && __state.PrevFont == cachedClassicFontTMP));

                if (IsClassicFontActive())
                {
                    if (prevWasChinese)
                    {
                        text.fontSize = __state.PrevSize;
                        text.lineSpacing = __state.PrevLineSpacing;
                    }
                    TMP_FontAsset cft = GetOrCreateClassicFontTMP();
                    Material bm = GetOrCreateClassicBorderMaterial();
                    if (cft != null) ApplyClassicFontToTMP(text, true, cft, bm, false, __state.PrevMat);
                }
                else if (cachedClassicFontTMP != null && __state.PrevFont == cachedClassicFontTMP)
                {
                    if (origChineseFontTMP != null && text.font == origChineseFontTMP)
                    {
                        text.fontSize = __state.PrevSize;
                        text.lineSpacing = __state.PrevLineSpacing;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(RDString), "SetLocalizedFont", new Type[] { typeof(TextMesh) })]
        public static class Patch_RDString_SetLocalizedFont_TextMesh
        {
            public static void Postfix(TextMesh text)
            {
                if (text == null || IsEditorObject(text.gameObject)) return;
                if (IsClassicFontActive())
                {
                    Font cf = GetOrCreateClassicUIFont();
                    if (cf != null) ApplyClassicFontToTextMesh(text, true, cf, false);
                }
            }
        }

        private static readonly FieldInfo DynamicallyOrderedSourceFontField =
            typeof(ADOFAI.DynamicallyOrderedFont).GetField("sourceFont", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        [HarmonyPatch(typeof(ADOFAI.DynamicallyOrderedFont), "ApplyForLanguage")]
        public static class Patch_DynamicallyOrderedFont_ApplyForLanguage
        {
            public static void Postfix(ADOFAI.DynamicallyOrderedFont __instance, SystemLanguage language)
            {
                if (DynamicallyOrderedSourceFontField == null) return;
                TMP_FontAsset sourceFont = DynamicallyOrderedSourceFontField.GetValue(__instance) as TMP_FontAsset;
                if (sourceFont == null || cachedClassicFontTMP == null) return;
                bool useClassic = IsClassicFontActive() && language == SystemLanguage.ChineseSimplified && !IsLevelEditor();
                SyncFontFallbackTable(sourceFont, useClassic, useClassic ? GetOrCreateClassicFontTMP() : cachedClassicFontTMP);
            }
        }

        [HarmonyPatch(typeof(RDStringToUIText), "Apply", new Type[] { typeof(Text), typeof(bool), typeof(bool), typeof(float) })]
        public static class Patch_RDStringToUIText_Apply_Text
        {
            public static void Postfix(Text text, bool changeFontForLanguage)
            {
                if (!changeFontForLanguage || text == null || IsEditorObject(text.gameObject)) return;
                if (IsClassicFontActive())
                {
                    Font cf = GetOrCreateClassicUIFont();
                    if (cf != null) ApplyClassicFontToText(text, true, cf, false);
                }
            }
        }

        [HarmonyPatch(typeof(RDStringToUIText), "Apply", new Type[] { typeof(TextMesh), typeof(bool), typeof(bool), typeof(float) })]
        public static class Patch_RDStringToUIText_Apply_TextMesh
        {
            public static void Postfix(TextMesh text, bool changeFontForLanguage)
            {
                if (!changeFontForLanguage || text == null || IsEditorObject(text.gameObject)) return;
                if (IsClassicFontActive())
                {
                    Font cf = GetOrCreateClassicUIFont();
                    if (cf != null) ApplyClassicFontToTextMesh(text, true, cf, false);
                }
            }
        }

        [HarmonyPatch(typeof(scrTextChanger), "Start")]
        public static class Patch_ScrTextChanger_Start
        {
            public static void Postfix(scrTextChanger __instance)
            {
                if (__instance == null) return;
                if (IsClassicFontActive() && !IsEditorObject(__instance.gameObject))
                {
                    ApplyClassicFontToHierarchy(__instance.gameObject, true, GetOrCreateClassicUIFont(), GetOrCreateClassicFontTMP(), GetOrCreateClassicBorderMaterial(), false);
                }
            }
        }

        [HarmonyPatch(typeof(scrTextChangerMultiline), "Start")]
        public static class Patch_ScrTextChangerMultiline_Start
        {
            public static void Postfix(scrTextChangerMultiline __instance)
            {
                if (__instance == null) return;
                if (IsClassicFontActive() && !IsEditorObject(__instance.gameObject))
                {
                    ApplyClassicFontToHierarchy(__instance.gameObject, true, GetOrCreateClassicUIFont(), GetOrCreateClassicFontTMP(), GetOrCreateClassicBorderMaterial(), false);
                }
            }
        }

        [HarmonyPatch(typeof(RDStringToUIText), "Start")]
        public static class Patch_RDStringToUIText_Start
        {
            public static void Postfix(RDStringToUIText __instance)
            {
                if (__instance == null) return;
                if (IsClassicFontActive() && !IsEditorObject(__instance.gameObject))
                {
                    ApplyClassicFontToHierarchy(__instance.gameObject, true, GetOrCreateClassicUIFont(), GetOrCreateClassicFontTMP(), GetOrCreateClassicBorderMaterial(), false);
                }
            }
        }

        [HarmonyPatch(typeof(scnCLS), "Awake")]
        public static class Patch_ScnCLS_Awake
        {
            public static void Postfix(scnCLS __instance)
            {
                if (__instance == null) return;
                if (IsClassicFontActive() && !IsLevelEditor())
                {
                    ApplyClassicFontToHierarchy(__instance.gameObject, true, GetOrCreateClassicUIFont(), GetOrCreateClassicFontTMP(), GetOrCreateClassicBorderMaterial(), true);
                }
            }
        }

        [HarmonyPatch(typeof(scnCLS), "DisplayLevel")]
        public static class Patch_ScnCLS_DisplayLevel
        {
            public static void Postfix(scnCLS __instance)
            {
                if (__instance == null) return;
                if (IsClassicFontActive() && !IsLevelEditor())
                {
                    ApplyClassicFontToHierarchy(__instance.gameObject, true, GetOrCreateClassicUIFont(), GetOrCreateClassicFontTMP(), GetOrCreateClassicBorderMaterial(), true);
                }
            }
        }

        [HarmonyPatch(typeof(PauseMenu), "ShowMainMenu")]
        public static class Patch_PauseMenu_ShowMainMenu
        {
            public static void Postfix(PauseMenu __instance)
            {
                if (__instance == null) return;
                if (IsClassicFontActive() && !IsLevelEditor())
                {
                    ApplyClassicFontToHierarchy(__instance.gameObject, true, GetOrCreateClassicUIFont(), GetOrCreateClassicFontTMP(), GetOrCreateClassicBorderMaterial(), true);
                }
            }
        }

        [HarmonyPatch(typeof(PauseMenu), "RefreshLayout")]
        public static class Patch_PauseMenu_RefreshLayout
        {
            public static void Postfix(PauseMenu __instance)
            {
                if (__instance == null) return;
                if (IsClassicFontActive() && !IsLevelEditor())
                {
                    ApplyClassicFontToHierarchy(__instance.gameObject, true, GetOrCreateClassicUIFont(), GetOrCreateClassicFontTMP(), GetOrCreateClassicBorderMaterial(), true);
                }
            }
        }

        [HarmonyPatch(typeof(SettingsMenu), "GenerateSettings")]
        public static class Patch_SettingsMenu_GenerateSettings
        {
            public static void Postfix(SettingsMenu __instance)
            {
                if (__instance == null) return;
                if (IsClassicFontActive() && !IsLevelEditor())
                {
                    ApplyClassicFontToHierarchy(__instance.gameObject, true, GetOrCreateClassicUIFont(), GetOrCreateClassicFontTMP(), GetOrCreateClassicBorderMaterial(), true);
                }
            }
        }

        [HarmonyPatch(typeof(scrHitTextMesh), "Init")]
        public static class Patch_ScrHitTextMesh_ClassicFont
        {
            [HarmonyPriority(Priority.Low)]
            public static void Postfix(scrHitTextMesh __instance)
            {
                if (__instance == null || IsEditorObject(__instance.gameObject)) return;
                if (IsClassicFontActive() && !IsLevelEditor())
                {
                    TMP_FontAsset cft = GetOrCreateClassicFontTMP();
                    Material bm = GetOrCreateClassicBorderMaterial();
                    if (__instance.text != null) ApplyClassicFontToTMP(__instance.text, true, cft, bm, false, null);
                    if (__instance.xPerfectBorder != null) ApplyClassicFontToTMP(__instance.xPerfectBorder, true, cft, bm, false, null);
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
                        ApplyClassicFontPreference();
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

using System;
using System.Collections.Generic;
using System.IO;
using TwentyFiveSlicer.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TwentyFiveSlicer.TFSEditor.Editor
{
    public static class TwentyFiveSliceJsonBridge
    {
        private const string ImportMenuPath = "Tools/Twenty Five Slicer Tools/Import Slice JSON For Selected Sprite";
        private const string ImportDesktopHandoffMenuPath = "Tools/Twenty Five Slicer Tools/Import Desktop Handoff Envelope For Selected Target";
        private const string ExportMenuPath = "Tools/Twenty Five Slicer Tools/Export Slice JSON For Selected Sprite";
        private const string ExportSpriteSidecarMenuPath = "Tools/Twenty Five Slicer Tools/Export Sprite Sidecar For Selected Sprite";
        private const string ExportDesktopHandoffMenuPath = "Tools/Twenty Five Slicer Tools/Export Desktop Handoff Envelope For Selected Target";

        [MenuItem(ImportMenuPath)]
        public static void ImportSliceJsonForSelectedSprite()
        {
            Sprite sprite = GetSelectedSprite();
            if (sprite == null)
            {
                ShowNoSpriteDialog();
                return;
            }

            string path = EditorUtility.OpenFilePanel("Import 25-slice JSON", Application.dataPath, "json");
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            try
            {
                SliceDataJson json = JsonUtility.FromJson<SliceDataJson>(File.ReadAllText(path));
                if (!IsValid(json))
                {
                    throw new InvalidDataException("The selected file must include verticalBorders and horizontalBorders arrays with four values each.");
                }

                var sliceData = new TwentyFiveSliceData
                {
                    verticalBorders = NormalizeAxis(json.verticalBorders),
                    horizontalBorders = NormalizeAxis(json.horizontalBorders)
                };

                SliceDataManager.Instance.SaveSliceData(sprite, sliceData);
                EditorUtility.DisplayDialog("Import Complete", $"Imported slice data for {sprite.name}.", "OK");
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog("Import Failed", exception.Message, "OK");
            }
        }

        [MenuItem(ImportMenuPath, true)]
        private static bool ValidateImportSliceJsonForSelectedSprite()
        {
            return GetSelectedSprite() != null;
        }

        [MenuItem(ImportDesktopHandoffMenuPath)]
        public static void ImportDesktopHandoffEnvelopeForSelectedTarget()
        {
            SelectionContext selection = GetSelectionContext();
            if (selection.Sprite == null)
            {
                ShowNoSpriteDialog();
                return;
            }

            string path = EditorUtility.OpenFilePanel("Import desktop handoff envelope", Application.dataPath, "json");
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            try
            {
                DesktopHandoffEnvelopeJson envelope = JsonUtility.FromJson<DesktopHandoffEnvelopeJson>(File.ReadAllText(path));
                if (!TryValidateEnvelopeForImport(envelope, selection, out string validationError))
                {
                    throw new InvalidDataException(validationError);
                }

                if (!TryConvertDesktopSliceDataToUnity(envelope.sliceData, out TwentyFiveSliceData sliceData, out string sliceError))
                {
                    throw new InvalidDataException(sliceError);
                }

                var warnings = new List<string>();
                if (envelope.spriteContext != null &&
                    !string.IsNullOrWhiteSpace(envelope.spriteContext.spriteName) &&
                    !string.Equals(envelope.spriteContext.spriteName, selection.Sprite.name, StringComparison.Ordinal))
                {
                    warnings.Add($"Envelope sprite '{envelope.spriteContext.spriteName}' was applied to the currently selected sprite '{selection.Sprite.name}'.");
                }

                SliceDataManager.Instance.SaveSliceData(selection.Sprite, sliceData);

                bool appliedRuntimeSettings = ApplyRuntimeSettingsFromEnvelope(selection, envelope, warnings);
                AssetDatabase.Refresh();

                string runtimeMessage = appliedRuntimeSettings
                    ? $"Applied runtime settings to {DescribeSelection(selection)}."
                    : "Slice data was applied, but no component runtime settings were updated for the current selection.";
                string warningText = warnings.Count == 0
                    ? string.Empty
                    : $"\n\nWarnings:\n- {string.Join("\n- ", warnings)}";

                EditorUtility.DisplayDialog(
                    "Import Complete",
                    $"Applied 25-slice data to {selection.Sprite.name}.\n{runtimeMessage}{warningText}",
                    "OK");
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog("Import Failed", exception.Message, "OK");
            }
        }

        [MenuItem(ImportDesktopHandoffMenuPath, true)]
        private static bool ValidateImportDesktopHandoffEnvelopeForSelectedTarget()
        {
            return GetSelectedSprite() != null;
        }

        [MenuItem(ExportMenuPath)]
        public static void ExportSliceJsonForSelectedSprite()
        {
            Sprite sprite = GetSelectedSprite();
            if (sprite == null)
            {
                ShowNoSpriteDialog();
                return;
            }

            if (!SliceDataManager.Instance.TryGetSliceData(sprite, out TwentyFiveSliceData sliceData))
            {
                EditorUtility.DisplayDialog("No Slice Data", $"No 25-slice data is saved for {sprite.name}.", "OK");
                return;
            }

            string path = EditorUtility.SaveFilePanel("Export 25-slice JSON", Application.dataPath, $"{sprite.name}.25slice.json", "json");
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            var json = new SliceDataJson
            {
                verticalBorders = NormalizeAxis(sliceData.verticalBorders),
                horizontalBorders = NormalizeAxis(sliceData.horizontalBorders)
            };

            File.WriteAllText(path, JsonUtility.ToJson(json, prettyPrint: true));
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Export Complete", $"Exported slice data for {sprite.name}.", "OK");
        }

        [MenuItem(ExportMenuPath, true)]
        private static bool ValidateExportSliceJsonForSelectedSprite()
        {
            return GetSelectedSprite() != null;
        }

        [MenuItem(ExportSpriteSidecarMenuPath)]
        public static void ExportSpriteSidecarForSelectedSprite()
        {
            Sprite sprite = GetSelectedSprite();
            if (sprite == null)
            {
                ShowNoSpriteDialog();
                return;
            }

            if (sprite.texture == null)
            {
                EditorUtility.DisplayDialog("No Texture", $"{sprite.name} does not have a readable source texture to export.", "OK");
                return;
            }

            string defaultDirectory = GetDefaultTextureDirectory(sprite);
            string defaultFileName = GetDefaultSidecarFileName(sprite);
            string path = EditorUtility.SaveFilePanel("Export sprite sidecar JSON", defaultDirectory, defaultFileName, "json");
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            SpriteSidecarJson json = CreateSpriteSidecarJson(sprite);

            File.WriteAllText(path, JsonUtility.ToJson(json, prettyPrint: true));
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Export Complete", $"Exported sprite sidecar for {sprite.name}.", "OK");
        }

        [MenuItem(ExportSpriteSidecarMenuPath, true)]
        private static bool ValidateExportSpriteSidecarForSelectedSprite()
        {
            return GetSelectedSprite() != null;
        }

        [MenuItem(ExportDesktopHandoffMenuPath)]
        public static void ExportDesktopHandoffEnvelopeForSelectedTarget()
        {
            SelectionContext selection = GetSelectionContext();
            if (selection.Sprite == null)
            {
                ShowNoSpriteDialog();
                return;
            }

            if (!SliceDataManager.Instance.TryGetSliceData(selection.Sprite, out TwentyFiveSliceData sliceData))
            {
                EditorUtility.DisplayDialog("No Slice Data", $"No 25-slice data is saved for {selection.Sprite.name}.", "OK");
                return;
            }

            string path = EditorUtility.SaveFilePanel(
                "Export desktop handoff envelope",
                GetDefaultTextureDirectory(selection.Sprite),
                GetDefaultDesktopHandoffFileName(selection),
                "json");
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            var envelope = new DesktopHandoffEnvelopeJson
            {
                schemaVersion = 1,
                targetKind = selection.TargetKind,
                targetName = string.IsNullOrWhiteSpace(selection.TargetName) ? selection.Sprite.name : selection.TargetName,
                sliceData = CreateDesktopSliceDataJson(sliceData),
                spriteContext = CreateSpriteSidecarJson(selection.Sprite),
                runtimeSettings = CreateRuntimeSettingsJson(selection)
            };

            File.WriteAllText(path, JsonUtility.ToJson(envelope, prettyPrint: true));
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Export Complete", $"Exported desktop handoff envelope for {envelope.targetName}.", "OK");
        }

        [MenuItem(ExportDesktopHandoffMenuPath, true)]
        private static bool ValidateExportDesktopHandoffEnvelopeForSelectedTarget()
        {
            return GetSelectedSprite() != null;
        }

        private static Sprite GetSelectedSprite()
        {
            return GetSelectionContext().Sprite;
        }

        private static SelectionContext GetSelectionContext()
        {
            if (Selection.activeObject is Sprite sprite)
            {
                return new SelectionContext
                {
                    Sprite = sprite,
                    TargetKind = "spriteAsset",
                    TargetName = sprite.name
                };
            }

            GameObject gameObject = Selection.activeGameObject;
            if (gameObject == null)
            {
                return new SelectionContext();
            }

            var image = gameObject.GetComponent<TwentyFiveSliceImage>();
            if (image != null && image.sprite != null)
            {
                return new SelectionContext
                {
                    Sprite = image.sprite,
                    Image = image,
                    TargetKind = "twentyFiveSliceImage",
                    TargetName = gameObject.name
                };
            }

            var twentyFiveRenderer = gameObject.GetComponent<TwentyFiveSliceSpriteRenderer>();
            if (twentyFiveRenderer != null && twentyFiveRenderer.Sprite != null)
            {
                return new SelectionContext
                {
                    Sprite = twentyFiveRenderer.Sprite,
                    TwentyFiveRenderer = twentyFiveRenderer,
                    TargetKind = "twentyFiveSliceSpriteRenderer",
                    TargetName = gameObject.name
                };
            }

            var spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                return new SelectionContext
                {
                    Sprite = spriteRenderer.sprite,
                    SpriteRenderer = spriteRenderer,
                    TargetKind = "spriteRenderer",
                    TargetName = gameObject.name
                };
            }

            return new SelectionContext();
        }

        private static bool IsValid(SliceDataJson json)
        {
            return json != null &&
                   json.verticalBorders != null &&
                   json.horizontalBorders != null &&
                   json.verticalBorders.Length == 4 &&
                   json.horizontalBorders.Length == 4;
        }

        private static float[] NormalizeAxis(float[] values)
        {
            var normalized = new float[4];
            for (int index = 0; index < normalized.Length; index++)
            {
                normalized[index] = Clamp01To100(values[index]);
            }

            Array.Sort(normalized);
            return normalized;
        }

        private static float Clamp01To100(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0f;
            }

            if (value < 0f)
            {
                return 0f;
            }

            return value > 100f ? 100f : value;
        }

        private static bool TryValidateEnvelopeForImport(DesktopHandoffEnvelopeJson envelope, SelectionContext selection, out string error)
        {
            error = string.Empty;
            if (envelope == null)
            {
                error = "The selected file does not contain a desktop handoff envelope.";
                return false;
            }

            if (envelope.sliceData == null)
            {
                error = "The handoff envelope is missing sliceData.";
                return false;
            }

            string targetKind = string.IsNullOrWhiteSpace(envelope.targetKind) ? "spriteAsset" : envelope.targetKind.Trim();
            switch (targetKind)
            {
                case "spriteAsset":
                    return true;
                case "twentyFiveSliceImage":
                    if (selection.Image != null)
                    {
                        return true;
                    }

                    error = "This handoff envelope targets a TwentyFiveSliceImage. Select a TwentyFiveSliceImage before importing it.";
                    return false;
                case "twentyFiveSliceSpriteRenderer":
                    if (selection.TwentyFiveRenderer != null)
                    {
                        return true;
                    }

                    error = "This handoff envelope targets a TwentyFiveSliceSpriteRenderer. Select a TwentyFiveSliceSpriteRenderer before importing it.";
                    return false;
                case "spriteRenderer":
                    if (selection.SpriteRenderer != null)
                    {
                        return true;
                    }

                    error = "This handoff envelope targets a SpriteRenderer. Select a SpriteRenderer before importing it.";
                    return false;
                default:
                    error = $"Unsupported handoff target kind '{targetKind}'.";
                    return false;
            }
        }

        private static bool TryConvertDesktopSliceDataToUnity(DesktopSliceDataJson json, out TwentyFiveSliceData sliceData, out string error)
        {
            sliceData = null;
            error = string.Empty;

            if (json == null || json.xGuidesPercent == null || json.yGuidesPercent == null)
            {
                error = "The handoff envelope is missing desktop slice guides.";
                return false;
            }

            if (json.xGuidesPercent.Length != 4 || json.yGuidesPercent.Length != 4)
            {
                error = "Unity currently supports importing only fixed 25-slice envelopes with exactly four X guides and four Y guides.";
                return false;
            }

            if (!HasSupportedSegmentPattern(json.xSegments, "X", out error) ||
                !HasSupportedSegmentPattern(json.ySegments, "Y", out error))
            {
                return false;
            }

            sliceData = new TwentyFiveSliceData
            {
                verticalBorders = NormalizeAxis(json.xGuidesPercent),
                horizontalBorders = NormalizeAxis(json.yGuidesPercent)
            };
            return true;
        }

        private static bool HasSupportedSegmentPattern(DesktopSliceSegmentJson[] segments, string axisName, out string error)
        {
            error = string.Empty;
            if (segments == null || segments.Length == 0)
            {
                return true;
            }

            string[] requiredPattern = { "fixed", "stretch", "fixed", "stretch", "fixed" };
            if (segments.Length != requiredPattern.Length)
            {
                error = $"Unity currently supports importing only fixed 25-slice {axisName} segment layouts with {requiredPattern.Length} entries.";
                return false;
            }

            for (int index = 0; index < requiredPattern.Length; index++)
            {
                string mode = segments[index] != null && !string.IsNullOrWhiteSpace(segments[index].mode)
                    ? segments[index].mode.Trim().ToLowerInvariant()
                    : requiredPattern[index];
                if (!string.Equals(mode, requiredPattern[index], StringComparison.Ordinal))
                {
                    error = $"Unity currently supports importing only the default 25-slice {axisName} segment pattern: fixed, stretch, fixed, stretch, fixed.";
                    return false;
                }
            }

            return true;
        }

        private static DesktopSliceDataJson CreateDesktopSliceDataJson(TwentyFiveSliceData sliceData)
        {
            float[] verticalBorders = NormalizeAxis(sliceData.verticalBorders);
            float[] horizontalBorders = NormalizeAxis(sliceData.horizontalBorders);
            return new DesktopSliceDataJson
            {
                schemaVersion = 2,
                xGuidesPercent = verticalBorders,
                yGuidesPercent = horizontalBorders,
                xSegments = CreateDefaultSegments(verticalBorders.Length + 1),
                ySegments = CreateDefaultSegments(horizontalBorders.Length + 1)
            };
        }

        private static DesktopSliceSegmentJson[] CreateDefaultSegments(int count)
        {
            var segments = new DesktopSliceSegmentJson[Mathf.Max(1, count)];
            for (int index = 0; index < segments.Length; index++)
            {
                segments[index] = new DesktopSliceSegmentJson
                {
                    mode = GetDefaultSegmentMode(index, segments.Length)
                };
            }

            return segments;
        }

        private static string GetDefaultSegmentMode(int index, int count)
        {
            if (index == 0 || index == count - 1)
            {
                return "fixed";
            }

            return index % 2 == 1 ? "stretch" : "fixed";
        }

        private static SpriteSidecarJson CreateSpriteSidecarJson(Sprite sprite)
        {
            return new SpriteSidecarJson
            {
                schemaVersion = 1,
                spriteName = sprite.name,
                texturePath = AssetDatabase.GetAssetPath(sprite),
                textureWidth = sprite.texture.width,
                textureHeight = sprite.texture.height,
                coordinateOrigin = "bottom-left",
                pixelsPerUnit = sprite.pixelsPerUnit,
                spriteRect = new SpriteRectJson
                {
                    x = Mathf.RoundToInt(sprite.rect.x),
                    y = Mathf.RoundToInt(sprite.rect.y),
                    width = Mathf.RoundToInt(sprite.rect.width),
                    height = Mathf.RoundToInt(sprite.rect.height)
                },
                pivotPixels = new SpritePointJson
                {
                    x = sprite.pivot.x,
                    y = sprite.pivot.y
                }
            };
        }

        private static RuntimeSettingsJson CreateRuntimeSettingsJson(SelectionContext selection)
        {
            if (selection.Image != null)
            {
                return CreateImageRuntimeSettingsJson(selection.Image, selection.Sprite);
            }

            if (selection.TwentyFiveRenderer != null)
            {
                return CreateTwentyFiveSpriteRendererRuntimeSettingsJson(selection.TwentyFiveRenderer, selection.Sprite);
            }

            if (selection.SpriteRenderer != null)
            {
                return CreateSpriteRendererRuntimeSettingsJson(selection.SpriteRenderer, selection.Sprite);
            }

            return new RuntimeSettingsJson
            {
                tintHex = "#FFFFFFFF",
                pixelsPerUnit = GetEffectivePixelsPerUnit(selection.Sprite, 0f),
                useSpritePivot = true,
                customPivotX = 0f,
                customPivotY = 0f,
                sortingLayerName = "Default",
                sortingOrder = 0,
                materialName = string.Empty,
                raycastTarget = false,
                raycastPaddingLeft = 0f,
                raycastPaddingBottom = 0f,
                raycastPaddingRight = 0f,
                raycastPaddingTop = 0f
            };
        }

        private static bool ApplyRuntimeSettingsFromEnvelope(SelectionContext selection, DesktopHandoffEnvelopeJson envelope, List<string> warnings)
        {
            if (selection == null || envelope == null)
            {
                return false;
            }

            RuntimeSettingsJson runtimeSettings = envelope.runtimeSettings ?? CreateRuntimeSettingsJson(selection);
            string targetKind = string.IsNullOrWhiteSpace(envelope.targetKind) ? "spriteAsset" : envelope.targetKind.Trim();
            if (string.Equals(targetKind, "spriteAsset", StringComparison.Ordinal))
            {
                warnings.Add("Envelope target kind is spriteAsset, so no component-specific runtime settings were applied.");
                return false;
            }

            if (!TryParseTintColor(runtimeSettings.tintHex, out Color tint))
            {
                tint = Color.white;
                warnings.Add($"Runtime tint '{runtimeSettings.tintHex}' was invalid and defaulted to white.");
            }

            switch (targetKind)
            {
                case "twentyFiveSliceImage":
                    ApplyToTwentyFiveSliceImage(selection.Image, runtimeSettings, tint, warnings);
                    return true;
                case "twentyFiveSliceSpriteRenderer":
                    ApplyToTwentyFiveSliceSpriteRenderer(selection.TwentyFiveRenderer, runtimeSettings, tint, warnings);
                    return true;
                case "spriteRenderer":
                    ApplyToSpriteRenderer(selection.SpriteRenderer, runtimeSettings, tint, warnings);
                    return true;
                default:
                    warnings.Add($"Unsupported runtime target kind '{targetKind}'.");
                    return false;
            }
        }

        private static void ApplyToTwentyFiveSliceImage(TwentyFiveSliceImage image, RuntimeSettingsJson runtimeSettings, Color tint, List<string> warnings)
        {
            if (image == null)
            {
                warnings.Add("No TwentyFiveSliceImage is selected, so image runtime settings were not applied.");
                return;
            }

            Undo.RecordObject(image, "Import Desktop Handoff Envelope");
            image.color = tint;
            image.raycastTarget = runtimeSettings.raycastTarget;
            image.raycastPadding = new Vector4(
                runtimeSettings.raycastPaddingLeft,
                runtimeSettings.raycastPaddingBottom,
                runtimeSettings.raycastPaddingRight,
                runtimeSettings.raycastPaddingTop);

            if (ShouldClearImageMaterial(runtimeSettings.materialName))
            {
                image.material = null;
            }
            else if (TryResolveMaterialByName(runtimeSettings.materialName, out Material material, out string materialWarning))
            {
                image.material = material;
            }
            else if (!string.IsNullOrWhiteSpace(materialWarning))
            {
                warnings.Add(materialWarning);
            }

            image.SetAllDirty();
            EditorUtility.SetDirty(image);
            MarkSceneDirty(image.gameObject);
        }

        private static void ApplyToTwentyFiveSliceSpriteRenderer(TwentyFiveSliceSpriteRenderer renderer, RuntimeSettingsJson runtimeSettings, Color tint, List<string> warnings)
        {
            if (renderer == null)
            {
                warnings.Add("No TwentyFiveSliceSpriteRenderer is selected, so renderer runtime settings were not applied.");
                return;
            }

            Undo.RecordObject(renderer, "Import Desktop Handoff Envelope");
            renderer.UseSpritePivot = runtimeSettings.useSpritePivot;
            renderer.CustomPivot = new Vector2(runtimeSettings.customPivotX, runtimeSettings.customPivotY);
            renderer.PixelsPerUnit = runtimeSettings.pixelsPerUnit;

            MeshRenderer meshRenderer = renderer.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                Undo.RecordObject(meshRenderer, "Import Desktop Handoff Envelope");
            }

            var serializedObject = new SerializedObject(renderer);
            SerializedProperty sortingLayerNameProperty = serializedObject.FindProperty("sortingLayerName");
            SerializedProperty sortingOrderProperty = serializedObject.FindProperty("sortingOrder");

            string sortingLayerName = NormalizeSortingLayerName(runtimeSettings.sortingLayerName, warnings);
            if (sortingLayerNameProperty != null)
            {
                sortingLayerNameProperty.stringValue = sortingLayerName;
            }

            if (sortingOrderProperty != null)
            {
                sortingOrderProperty.intValue = runtimeSettings.sortingOrder;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            if (meshRenderer != null)
            {
                meshRenderer.sortingLayerName = sortingLayerName;
                meshRenderer.sortingOrder = runtimeSettings.sortingOrder;

                if (TryResolveMaterialByName(runtimeSettings.materialName, out Material material, out string materialWarning))
                {
                    meshRenderer.sharedMaterial = material;
                }
                else if (!string.IsNullOrWhiteSpace(materialWarning))
                {
                    warnings.Add(materialWarning);
                }
            }

            renderer.Color = tint;
            EditorUtility.SetDirty(renderer);
            if (meshRenderer != null)
            {
                EditorUtility.SetDirty(meshRenderer);
            }

            MarkSceneDirty(renderer.gameObject);
        }

        private static void ApplyToSpriteRenderer(SpriteRenderer spriteRenderer, RuntimeSettingsJson runtimeSettings, Color tint, List<string> warnings)
        {
            if (spriteRenderer == null)
            {
                warnings.Add("No SpriteRenderer is selected, so renderer runtime settings were not applied.");
                return;
            }

            Undo.RecordObject(spriteRenderer, "Import Desktop Handoff Envelope");
            spriteRenderer.color = tint;
            spriteRenderer.sortingLayerName = NormalizeSortingLayerName(runtimeSettings.sortingLayerName, warnings);
            spriteRenderer.sortingOrder = runtimeSettings.sortingOrder;

            if (TryResolveMaterialByName(runtimeSettings.materialName, out Material material, out string materialWarning))
            {
                spriteRenderer.sharedMaterial = material;
            }
            else if (!string.IsNullOrWhiteSpace(materialWarning))
            {
                warnings.Add(materialWarning);
            }

            EditorUtility.SetDirty(spriteRenderer);
            MarkSceneDirty(spriteRenderer.gameObject);
        }

        private static bool TryResolveMaterialByName(string materialName, out Material material, out string warning)
        {
            material = null;
            warning = string.Empty;

            if (ShouldClearImageMaterial(materialName) || string.Equals(materialName?.Trim(), "Sprites/Default", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string query = materialName.Trim();
            string[] guids = AssetDatabase.FindAssets($"{query} t:Material");
            var exactMatches = new List<string>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.Equals(Path.GetFileNameWithoutExtension(path), query, StringComparison.OrdinalIgnoreCase))
                {
                    exactMatches.Add(path);
                }
            }

            if (exactMatches.Count == 1)
            {
                material = AssetDatabase.LoadAssetAtPath<Material>(exactMatches[0]);
                return material != null;
            }

            if (exactMatches.Count > 1)
            {
                warning = $"Material '{query}' matched multiple project assets, so the current material was left unchanged.";
                return false;
            }

            warning = $"Material '{query}' could not be resolved in the Unity project, so the current material was left unchanged.";
            return false;
        }

        private static bool ShouldClearImageMaterial(string materialName)
        {
            return string.IsNullOrWhiteSpace(materialName);
        }

        private static bool TryParseTintColor(string tintHex, out Color color)
        {
            if (!string.IsNullOrWhiteSpace(tintHex) && ColorUtility.TryParseHtmlString(tintHex, out color))
            {
                return true;
            }

            color = Color.white;
            return false;
        }

        private static string NormalizeSortingLayerName(string sortingLayerName, List<string> warnings)
        {
            string candidate = string.IsNullOrWhiteSpace(sortingLayerName) ? "Default" : sortingLayerName.Trim();
            foreach (SortingLayer layer in SortingLayer.layers)
            {
                if (string.Equals(layer.name, candidate, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            warnings.Add($"Sorting layer '{candidate}' does not exist in the current Unity project, so 'Default' was used instead.");
            return "Default";
        }

        private static string DescribeSelection(SelectionContext selection)
        {
            if (selection.Image != null)
            {
                return $"TwentyFiveSliceImage '{selection.TargetName}'";
            }

            if (selection.TwentyFiveRenderer != null)
            {
                return $"TwentyFiveSliceSpriteRenderer '{selection.TargetName}'";
            }

            if (selection.SpriteRenderer != null)
            {
                return $"SpriteRenderer '{selection.TargetName}'";
            }

            return $"sprite '{selection.Sprite.name}'";
        }

        private static void MarkSceneDirty(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            if (gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
        }

        private static RuntimeSettingsJson CreateImageRuntimeSettingsJson(TwentyFiveSliceImage image, Sprite sprite)
        {
            var serializedObject = new SerializedObject(image);
            SerializedProperty colorProperty = serializedObject.FindProperty("m_Color");
            SerializedProperty materialProperty = serializedObject.FindProperty("m_Material");
            SerializedProperty raycastTargetProperty = serializedObject.FindProperty("m_RaycastTarget");
            SerializedProperty raycastPaddingProperty = serializedObject.FindProperty("m_RaycastPadding");

            Color tint = colorProperty != null ? colorProperty.colorValue : Color.white;
            Material material = materialProperty != null ? materialProperty.objectReferenceValue as Material : null;
            bool raycastTarget = raycastTargetProperty == null || raycastTargetProperty.boolValue;
            Vector4 raycastPadding = raycastPaddingProperty != null ? raycastPaddingProperty.vector4Value : Vector4.zero;

            return new RuntimeSettingsJson
            {
                tintHex = ToTintHex(tint),
                pixelsPerUnit = GetEffectivePixelsPerUnit(sprite, 0f),
                useSpritePivot = true,
                customPivotX = 0f,
                customPivotY = 0f,
                sortingLayerName = "Default",
                sortingOrder = 0,
                materialName = material != null ? material.name : string.Empty,
                raycastTarget = raycastTarget,
                raycastPaddingLeft = raycastPadding.x,
                raycastPaddingBottom = raycastPadding.y,
                raycastPaddingRight = raycastPadding.z,
                raycastPaddingTop = raycastPadding.w
            };
        }

        private static RuntimeSettingsJson CreateTwentyFiveSpriteRendererRuntimeSettingsJson(TwentyFiveSliceSpriteRenderer renderer, Sprite sprite)
        {
            var serializedObject = new SerializedObject(renderer);
            SerializedProperty colorProperty = serializedObject.FindProperty("color");
            SerializedProperty useSpritePivotProperty = serializedObject.FindProperty("useSpritePivot");
            SerializedProperty customPivotProperty = serializedObject.FindProperty("customPivot");
            SerializedProperty pixelsPerUnitProperty = serializedObject.FindProperty("pixelsPerUnit");
            SerializedProperty sortingLayerNameProperty = serializedObject.FindProperty("sortingLayerName");
            SerializedProperty sortingOrderProperty = serializedObject.FindProperty("sortingOrder");

            Color tint = colorProperty != null ? colorProperty.colorValue : Color.white;
            Vector2 customPivot = customPivotProperty != null ? customPivotProperty.vector2Value : Vector2.zero;
            float pixelsPerUnitOverride = pixelsPerUnitProperty != null ? pixelsPerUnitProperty.floatValue : 0f;
            MeshRenderer meshRenderer = renderer.GetComponent<MeshRenderer>();

            return new RuntimeSettingsJson
            {
                tintHex = ToTintHex(tint),
                pixelsPerUnit = GetEffectivePixelsPerUnit(sprite, pixelsPerUnitOverride),
                useSpritePivot = useSpritePivotProperty == null || useSpritePivotProperty.boolValue,
                customPivotX = customPivot.x,
                customPivotY = customPivot.y,
                sortingLayerName = sortingLayerNameProperty != null ? sortingLayerNameProperty.stringValue : "Default",
                sortingOrder = sortingOrderProperty != null ? sortingOrderProperty.intValue : 0,
                materialName = meshRenderer != null && meshRenderer.sharedMaterial != null ? meshRenderer.sharedMaterial.name : string.Empty,
                raycastTarget = false,
                raycastPaddingLeft = 0f,
                raycastPaddingBottom = 0f,
                raycastPaddingRight = 0f,
                raycastPaddingTop = 0f
            };
        }

        private static RuntimeSettingsJson CreateSpriteRendererRuntimeSettingsJson(SpriteRenderer spriteRenderer, Sprite sprite)
        {
            return new RuntimeSettingsJson
            {
                tintHex = ToTintHex(spriteRenderer.color),
                pixelsPerUnit = GetEffectivePixelsPerUnit(sprite, 0f),
                useSpritePivot = true,
                customPivotX = 0f,
                customPivotY = 0f,
                sortingLayerName = spriteRenderer.sortingLayerName,
                sortingOrder = spriteRenderer.sortingOrder,
                materialName = spriteRenderer.sharedMaterial != null ? spriteRenderer.sharedMaterial.name : string.Empty,
                raycastTarget = false,
                raycastPaddingLeft = 0f,
                raycastPaddingBottom = 0f,
                raycastPaddingRight = 0f,
                raycastPaddingTop = 0f
            };
        }

        private static float GetEffectivePixelsPerUnit(Sprite sprite, float overridePixelsPerUnit)
        {
            if (overridePixelsPerUnit > 0f)
            {
                return overridePixelsPerUnit;
            }

            if (sprite != null && sprite.pixelsPerUnit > 0f)
            {
                return sprite.pixelsPerUnit;
            }

            return 100f;
        }

        private static string ToTintHex(Color color)
        {
            return $"#{ColorUtility.ToHtmlStringRGBA(color)}";
        }

        private static string GetDefaultTextureDirectory(Sprite sprite)
        {
            string absoluteTexturePath = GetAbsoluteTexturePath(sprite);
            return string.IsNullOrWhiteSpace(absoluteTexturePath)
                ? Application.dataPath
                : Path.GetDirectoryName(absoluteTexturePath);
        }

        private static string GetDefaultDesktopHandoffFileName(SelectionContext selection)
        {
            string targetName = string.IsNullOrWhiteSpace(selection.TargetName)
                ? (selection.Sprite != null ? selection.Sprite.name : "twenty-five-slice")
                : selection.TargetName;
            return $"{targetName}.25slice.handoff.json";
        }

        private static string GetDefaultSidecarFileName(Sprite sprite)
        {
            string textureAssetPath = AssetDatabase.GetAssetPath(sprite);
            string textureFileName = Path.GetFileName(textureAssetPath);
            return string.IsNullOrWhiteSpace(textureFileName)
                ? $"{sprite.name}.sprite.json"
                : $"{textureFileName}.sprite.json";
        }

        private static string GetAbsoluteTexturePath(Sprite sprite)
        {
            string textureAssetPath = AssetDatabase.GetAssetPath(sprite);
            if (string.IsNullOrWhiteSpace(textureAssetPath))
            {
                return null;
            }

            DirectoryInfo projectDirectory = Directory.GetParent(Application.dataPath);
            if (projectDirectory == null)
            {
                return null;
            }

            return Path.GetFullPath(Path.Combine(projectDirectory.FullName, textureAssetPath));
        }

        private static void ShowNoSpriteDialog()
        {
            EditorUtility.DisplayDialog(
                "No Sprite Selected",
                "Select a Sprite asset, a TwentyFiveSliceImage, a TwentyFiveSliceSpriteRenderer, or a SpriteRenderer before using this tool.",
                "OK");
        }

        [Serializable]
        private sealed class SelectionContext
        {
            public Sprite Sprite;
            public TwentyFiveSliceImage Image;
            public TwentyFiveSliceSpriteRenderer TwentyFiveRenderer;
            public SpriteRenderer SpriteRenderer;
            public string TargetKind;
            public string TargetName;
        }

        [Serializable]
        private sealed class DesktopHandoffEnvelopeJson
        {
            public int schemaVersion;
            public string targetKind;
            public string targetName;
            public DesktopSliceDataJson sliceData;
            public SpriteSidecarJson spriteContext;
            public RuntimeSettingsJson runtimeSettings;
        }

        [Serializable]
        private sealed class DesktopSliceDataJson
        {
            public int schemaVersion;
            public float[] xGuidesPercent;
            public float[] yGuidesPercent;
            public DesktopSliceSegmentJson[] xSegments;
            public DesktopSliceSegmentJson[] ySegments;
        }

        [Serializable]
        private sealed class DesktopSliceSegmentJson
        {
            public string mode;
        }

        [Serializable]
        private sealed class SliceDataJson
        {
            public float[] verticalBorders = new float[4];
            public float[] horizontalBorders = new float[4];
        }

        [Serializable]
        private sealed class SpriteSidecarJson
        {
            public int schemaVersion;
            public string spriteName;
            public string texturePath;
            public int textureWidth;
            public int textureHeight;
            public string coordinateOrigin;
            public float pixelsPerUnit;
            public SpriteRectJson spriteRect;
            public SpritePointJson pivotPixels;
        }

        [Serializable]
        private sealed class SpriteRectJson
        {
            public int x;
            public int y;
            public int width;
            public int height;
        }

        [Serializable]
        private sealed class SpritePointJson
        {
            public float x;
            public float y;
        }

        [Serializable]
        private sealed class RuntimeSettingsJson
        {
            public string tintHex;
            public float pixelsPerUnit;
            public bool useSpritePivot;
            public float customPivotX;
            public float customPivotY;
            public string sortingLayerName;
            public int sortingOrder;
            public string materialName;
            public bool raycastTarget;
            public float raycastPaddingLeft;
            public float raycastPaddingBottom;
            public float raycastPaddingRight;
            public float raycastPaddingTop;
        }
    }
}

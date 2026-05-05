using System;
using System.IO;
using TwentyFiveSlicer.Runtime;
using UnityEditor;
using UnityEngine;

namespace TwentyFiveSlicer.TFSEditor.Editor
{
    public static class TwentyFiveSliceJsonBridge
    {
        private const string ImportMenuPath = "Tools/Twenty Five Slicer Tools/Import Slice JSON For Selected Sprite";
        private const string ExportMenuPath = "Tools/Twenty Five Slicer Tools/Export Slice JSON For Selected Sprite";

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

        private static Sprite GetSelectedSprite()
        {
            if (Selection.activeObject is Sprite sprite)
            {
                return sprite;
            }

            GameObject gameObject = Selection.activeGameObject;
            if (gameObject == null)
            {
                return null;
            }

            var image = gameObject.GetComponent<TwentyFiveSliceImage>();
            if (image != null && image.sprite != null)
            {
                return image.sprite;
            }

            var twentyFiveRenderer = gameObject.GetComponent<TwentyFiveSliceSpriteRenderer>();
            if (twentyFiveRenderer != null && twentyFiveRenderer.Sprite != null)
            {
                return twentyFiveRenderer.Sprite;
            }

            var spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
            return spriteRenderer != null ? spriteRenderer.sprite : null;
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

        private static void ShowNoSpriteDialog()
        {
            EditorUtility.DisplayDialog(
                "No Sprite Selected",
                "Select a Sprite asset, a TwentyFiveSliceImage, a TwentyFiveSliceSpriteRenderer, or a SpriteRenderer before using this tool.",
                "OK");
        }

        [Serializable]
        private sealed class SliceDataJson
        {
            public float[] verticalBorders = new float[4];
            public float[] horizontalBorders = new float[4];
        }
    }
}

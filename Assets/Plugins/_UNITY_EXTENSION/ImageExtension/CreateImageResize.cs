#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.U2D.Sprites;
using System;

public class CreateImageResize : EditorWindow {
    Texture2D inputTexture;
    int newWidth = 512;
    int newHeight = 512;
    bool useOriginalPath = false;
    string assetPath;
    string unityRelativePath;

    [MenuItem("_UNITY_EXTENSION/Image Extension/Resize Texture")]
    static void ShowWindow() {
        GetWindow<CreateImageResize>("Resize Texture");
    }

    void OnGUI() {
        GUILayout.Label("Resize Texture", EditorStyles.boldLabel);
        inputTexture = (Texture2D)EditorGUILayout.ObjectField("Input Texture", inputTexture, typeof(Texture2D), false);
        newWidth = EditorGUILayout.IntField("New Width", newWidth);
        newHeight = EditorGUILayout.IntField("New Height", newHeight);
        useOriginalPath = EditorGUILayout.Toggle("Use Original Texture Path", useOriginalPath);

        if (GUILayout.Button("Resize and Save")) {
            ResizeAndSave();
        }
        if (GUILayout.Button("Copy Import Setting")) {
            CopyImportSetting();
        }
    }

    void ResizeAndSave() {
        if (inputTexture == null) {
            EditorUtility.DisplayDialog("Error", "Please assign a texture first.", "OK");
            return;
        }
        assetPath = AssetDatabase.GetAssetPath(inputTexture);
        string originalName = Path.GetFileNameWithoutExtension(assetPath);
        string newName = $"{originalName}_{newWidth}x{newHeight}.png";
        string savePath = "";
        unityRelativePath = "";
        if (useOriginalPath) {
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            string directory = Path.GetDirectoryName(fullPath);
            savePath = Path.Combine(directory, newName);
            unityRelativePath = assetPath.Replace(originalName, $"{originalName}_{newWidth}x{newHeight}");
        } else {
            string defaultFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
            savePath = EditorUtility.SaveFilePanel("Save Resized Texture", defaultFolder, newName, "png");
            if (string.IsNullOrEmpty(savePath))
                return;
        }
        Texture2D resized = ResizeTextureWithPivot(inputTexture, newWidth, newHeight);
        SaveTextureToFile(resized, savePath);
        // Apply original texture's platform-specific settings
        if (!string.IsNullOrEmpty(unityRelativePath)) {
            AssetDatabase.ImportAsset(unityRelativePath, ImportAssetOptions.ForceUpdate);
            ApplyOriginalPlatformSettings(assetPath, unityRelativePath);
        }
        Debug.Log("Texture saved to: " + savePath);
    }

    void ApplyOriginalPlatformSettings(string originalPath, string newPath) {
        TextureImporter originalImporter = (TextureImporter)TextureImporter.GetAtPath(originalPath);
        TextureImporter newImporter = (TextureImporter)TextureImporter.GetAtPath(newPath);
        if (originalImporter == null || newImporter == null) return;
        // Copy default settings
        newImporter.maxTextureSize = originalImporter.maxTextureSize;
        newImporter.textureCompression = originalImporter.textureCompression;
        // Copy platform-specific settings
        foreach (var platform in new string[] { "Standalone", "Android", "iPhone" }) {
            TextureImporterPlatformSettings platformSettings = originalImporter.GetPlatformTextureSettings(platform);
            if (platformSettings.overridden) {
                newImporter.SetPlatformTextureSettings(platformSettings);
            }
        }
        AssetDatabase.WriteImportSettingsIfDirty(newPath);
        AssetDatabase.ImportAsset(newPath, ImportAssetOptions.ForceUpdate);
    }

    void CopyImportSetting() {
        // Copy import settings from original texture
        TextureImporter originalImporter = (TextureImporter)TextureImporter.GetAtPath(assetPath);
        TextureImporter newImporter = (TextureImporter)TextureImporter.GetAtPath(unityRelativePath);
        if (originalImporter != null && newImporter != null) {
            // Copy các cài đặt chung
            newImporter.textureType = originalImporter.textureType;
            newImporter.textureShape = originalImporter.textureShape;
            newImporter.alphaSource = originalImporter.alphaSource;
            newImporter.alphaIsTransparency = originalImporter.alphaIsTransparency;
            newImporter.mipmapEnabled = originalImporter.mipmapEnabled;
            newImporter.wrapMode = originalImporter.wrapMode;
            newImporter.filterMode = originalImporter.filterMode;
            newImporter.textureCompression = originalImporter.textureCompression;
            newImporter.sRGBTexture = originalImporter.sRGBTexture;

            // Copy TextureImporterSettings (pivot, alignment, mesh, physics shape...)
            TextureImporterSettings originalSettings = new TextureImporterSettings();
            originalImporter.ReadTextureSettings(originalSettings);
            newImporter.SetTextureSettings(originalSettings);

            // Copy sprite meta nếu là Multiple
            if (originalImporter.spriteImportMode == SpriteImportMode.Multiple) {
                newImporter.spriteImportMode = SpriteImportMode.Multiple;
                CopyAndScaleSpriteData(originalImporter, newImporter, assetPath, unityRelativePath);
            } else
                newImporter.spriteImportMode = originalImporter.spriteImportMode;
            AssetDatabase.ImportAsset(unityRelativePath, ImportAssetOptions.ForceUpdate);
            Debug.Log("Texture Copy Import Setting Done");
        }
    }

    void CopyAndScaleSpriteData(TextureImporter originalImporter, TextureImporter newImporter, string originalPath, string newPath) {
        var factory = new SpriteDataProviderFactories();
        factory.Init();

        var originalProvider = factory.GetSpriteEditorDataProviderFromObject(AssetDatabase.LoadMainAssetAtPath(originalPath));
        var newProvider = factory.GetSpriteEditorDataProviderFromObject(AssetDatabase.LoadMainAssetAtPath(newPath));

        if (originalProvider != null && newProvider != null) {
            originalProvider.InitSpriteEditorDataProvider();
            newProvider.InitSpriteEditorDataProvider();

            var spriteRects = originalProvider.GetSpriteRects();
            var scaledSpriteRects = new SpriteRect[spriteRects.Length];

            // Get original texture dimensions from the source PNG file
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), originalPath);
            var (originalWidth, originalHeight) = GetPngDimensions(fullPath);
            if (originalWidth <= 0 || originalHeight <= 0) {
                Debug.LogError("Failed to read original texture dimensions.");
                return;
            }

            // Calculate scaling factors
            float widthRatio = (float)newWidth / originalWidth;
            float heightRatio = (float)newHeight / originalHeight;

            for (int i = 0; i < spriteRects.Length; i++) {
                var originalRect = spriteRects[i];
                var scaledRect = new SpriteRect {
                    name = originalRect.name,
                    alignment = originalRect.alignment,
                    border = originalRect.border,
                    spriteID = originalRect.spriteID
                };

                // Scale rectangle position and size
                scaledRect.rect = new Rect(
                    originalRect.rect.x * widthRatio,
                    originalRect.rect.y * heightRatio,
                    originalRect.rect.width * widthRatio,
                    originalRect.rect.height * heightRatio
                );

                // Scale pivot point (relative to the sprite's rectangle)
                scaledRect.pivot = new Vector2(
                    originalRect.pivot.x, // Pivot is normalized (0-1), so no scaling needed
                    originalRect.pivot.y
                );

                scaledSpriteRects[i] = scaledRect;
            }

            newProvider.SetSpriteRects(scaledSpriteRects);
            newProvider.Apply();
        }
    }

    (int width, int height) GetPngDimensions(string filePath) {
        try {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read)) {
                // Read PNG header (first 8 bytes are signature)
                byte[] header = new byte[8];
                stream.Read(header, 0, 8);

                // Verify PNG signature
                if (header[0] != 0x89 || header[1] != 0x50 || header[2] != 0x4E || header[3] != 0x47 ||
                    header[4] != 0x0D || header[5] != 0x0A || header[6] != 0x1A || header[7] != 0x0A) {
                    return (0, 0);
                }

                // Read IHDR chunk (next 8 bytes after signature: length and type)
                byte[] chunkHeader = new byte[8];
                stream.Read(chunkHeader, 0, 8);

                // Verify IHDR chunk type
                if (chunkHeader[4] != 0x49 || chunkHeader[5] != 0x48 || chunkHeader[6] != 0x44 || chunkHeader[7] != 0x52) {
                    return (0, 0);
                }

                // Read width and height (4 bytes each)
                byte[] dimensions = new byte[8];
                stream.Read(dimensions, 0, 8);

                // Convert to integers (big-endian)
                int width = (dimensions[0] << 24) | (dimensions[1] << 16) | (dimensions[2] << 8) | dimensions[3];
                int height = (dimensions[4] << 24) | (dimensions[5] << 16) | (dimensions[6] << 8) | dimensions[7];

                return (width, height);
            }
        } catch (Exception ex) {
            Debug.LogError($"Failed to read PNG dimensions: {ex.Message}");
            return (0, 0);
        }
    }

    Texture2D ResizeTextureWithPivot(Texture2D original, int newWidth, int newHeight) {
        Texture2D result = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
        Color32[] originalPixels = original.GetPixels32();
        Color32[] newPixels = new Color32[newWidth * newHeight];

        int offsetX = (newWidth - original.width) / 2;
        int offsetY = (newHeight - original.height) / 2;

        for (int y = 0; y < original.height; y++) {
            int newY = y + offsetY;
            if (newY < 0 || newY >= newHeight)
                continue;

            for (int x = 0; x < original.width; x++) {
                int newX = x + offsetX;
                if (newX < 0 || newX >= newWidth)
                    continue;

                newPixels[newY * newWidth + newX] = originalPixels[y * original.width + x];
            }
        }

        result.SetPixels32(newPixels);
        result.Apply();
        return result;
    }

    void SaveTextureToFile(Texture2D tex, string path) {
        byte[] pngData = tex.EncodeToPNG();
        if (pngData != null) {
            File.WriteAllBytes(path, pngData);
        }
    }
}
#endif
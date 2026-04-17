using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class CursorSamuraiEditor
{
    const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

    static CursorSamuraiEditor() => EditorApplication.delayCall += Setup;

    static void Setup()
    {
        var active = EditorSceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(active.path) && System.IO.File.Exists(SampleScenePath)) {
            EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
        }
        var scenes = EditorBuildSettings.scenes;
        bool has = false;
        foreach (var s in scenes) if (s.path == SampleScenePath) { has = true; break; }
        if (!has) {
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes);
            list.Add(new EditorBuildSettingsScene(SampleScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}

// Auto-configure sprite imports: 2D Sprite + high quality.
public class SpriteImportSettings : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.EndsWith(".png")) return;
        if (!assetPath.Contains("/Resources/Sprites/")) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
    }
}

// Auto-configure audio imports for the bundled WAVs.
public class AudioImportSettings : AssetPostprocessor
{
    void OnPreprocessAudio()
    {
        if (!assetPath.Contains("/Resources/Audio/")) return;
        var importer = (AudioImporter)assetImporter;
        var s = importer.defaultSampleSettings;
        s.loadType = AudioClipLoadType.DecompressOnLoad;
        s.compressionFormat = AudioCompressionFormat.PCM;
        importer.defaultSampleSettings = s;
        importer.forceToMono = true;
    }
}

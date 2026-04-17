using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Batchmode-friendly WebGL build. Invoke via:
//   Unity -batchmode -nographics -projectPath <path> -executeMethod BuildScript.BuildWebGL -logFile - -quit
public static class BuildScript
{
    static readonly string[] Scenes = { "Assets/Scenes/SampleScene.unity" };

    [MenuItem("CursorKnight/Build WebGL")]
    public static void BuildWebGL()
    {
        string outDir = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "WebGL");
        Directory.CreateDirectory(outDir);

        // WebGL-specific settings for best compatibility + size
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.WebGL.memorySize = 512;
        PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
        PlayerSettings.companyName = "CursorKnight";
        PlayerSettings.productName  = "Cursor Knight";
        PlayerSettings.bundleVersion = "0.1.0";
        PlayerSettings.runInBackground = false;

        // Disable MSAA (cheap perf win for WebGL)
        QualitySettings.antiAliasing = 0;

        var opts = new BuildPlayerOptions {
            scenes = Scenes,
            locationPathName = outDir,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        Debug.Log($"[BuildScript] Building WebGL → {outDir}");
        BuildReport report = BuildPipeline.BuildPlayer(opts);
        BuildSummary s = report.summary;
        Debug.Log($"[BuildScript] Result: {s.result}, size={s.totalSize / 1024}KB, time={s.totalTime}");
        if (s.result != BuildResult.Succeeded) {
            EditorApplication.Exit(1);
        }
    }
}

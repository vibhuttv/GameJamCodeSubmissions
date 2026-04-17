using UnityEditor;
using UnityEngine;
using CursorSamurai.UI;

// Ensures an EditShowcase GameObject exists in every scene load so the Game view
// is never empty in edit mode. The GO is hidden + not saved, so it never touches
// scene YAML.
[InitializeOnLoad]
public static class EditShowcaseBootstrap
{
    static EditShowcaseBootstrap()
    {
        EditorApplication.delayCall += Ensure;
        EditorApplication.hierarchyChanged += Ensure;
    }

    static void Ensure()
    {
        if (Application.isPlaying) return;
        var existing = Object.FindObjectsByType<EditShowcase>(FindObjectsSortMode.None);
        if (existing != null && existing.Length > 0) return;
        var go = new GameObject("__EditShowcase");
        go.hideFlags = HideFlags.HideAndDontSave;   // invisible in hierarchy, not saved to scene
        go.AddComponent<EditShowcase>();
    }
}

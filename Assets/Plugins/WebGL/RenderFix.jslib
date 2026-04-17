mergeInto(LibraryManager.library, {
  // Workaround for Unity 6 URP WebGL render-target init bug.
  // Called from C# after each scene/level load. Toggles fullscreen on/off
  // to force Unity to reallocate render targets, mirroring what the user
  // does manually with the fullscreen button.
  ForceRenderTargetReallocation: function () {
    if (window.unityInstance && window.unityInstance.SetFullscreen) {
      try {
        window.unityInstance.SetFullscreen(1);
        setTimeout(function () {
          window.unityInstance.SetFullscreen(0);
        }, 120);
      } catch (e) {
        console.warn("ForceRenderTargetReallocation failed:", e);
      }
    }
  }
});

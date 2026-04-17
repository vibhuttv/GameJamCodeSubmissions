mergeInto(LibraryManager.library, {
  // Stops the browser context menu from intercepting right-click — right-click
  // is a gameplay input (slide) so must not open the browser menu.
  CS_DisableContextMenu: function () {
    document.addEventListener('contextmenu', function(e) {
      e.preventDefault();
      return false;
    }, { capture: true });
    // Also swallow any text selection drag on the canvas
    var canvas = document.getElementById('unity-canvas') || document.querySelector('canvas');
    if (canvas) {
      canvas.addEventListener('selectstart', function(e) { e.preventDefault(); });
    }
  }
});

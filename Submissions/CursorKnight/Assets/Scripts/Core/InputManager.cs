using UnityEngine;

namespace CursorSamurai.Core
{
    // 2D control scheme:
    //   Mouse Y = vertical lane steering (samurai moves up/down to dodge)
    //   Mouse X = lateral micro-offset (optional — mostly unused in side-scroller)
    //   Left-click  = Jump
    //   Right-click = Slide
    //   (TimelineScrub uses horizontal drag motion instead of Y now)
    public class InputManager : MonoBehaviour
    {
        public Vector2 MouseNDC;
        public Vector2 MousePx;

        public float SteerY;   // -1..1, vertical lane steering
        public float SteerX;   // -1..1, minor horizontal nudge (for boss aiming)
        public float ScrubX;   // -1..1, timeline scrub — only live while Z is held
        public bool  ScrubHeld;

        public bool JumpPressed;
        public bool SlidePressed;
        public bool LeftHeld;
        public bool RightHeld;
        public bool Frozen;

        public System.Action<Vector2> OnClickTarget;

        // Prev mouse for scrub-by-drag detection
        float _lastMouseX;

        void Update()
        {
            float w = Screen.width, h = Screen.height;
            MousePx = Input.mousePosition;
            MouseNDC = new Vector2(
                Mathf.Clamp((MousePx.x / w) * 2f - 1f, -1f, 1f),
                Mathf.Clamp((MousePx.y / h) * 2f - 1f, -1f, 1f)
            );

            SteerX = Mathf.Abs(MouseNDC.x) < 0.03f ? 0f : MouseNDC.x;
            SteerY = Mathf.Abs(MouseNDC.y) < 0.03f ? 0f : MouseNDC.y;
            // Scrub is a held-modifier interaction: press Z (or middle mouse) then move
            // mouse X to rewind/fast-forward. Prevents accidental scrubs from normal
            // cursor drift (which previously conflicted with boss aim-X as well).
            ScrubHeld = Input.GetKey(KeyCode.Z) || Input.GetMouseButton(2);
            ScrubX = ScrubHeld ? MouseNDC.x : 0f;

            JumpPressed = false;
            SlidePressed = false;

            if (Frozen) return;

            if (Input.GetMouseButtonDown(0)) {
                LeftHeld = true; JumpPressed = true;
                OnClickTarget?.Invoke(MouseNDC);
                // Cursor pulse on click — theme polish
                CursorSamurai.GameRoot.I?.CursorFX?.PulseClick(MousePx);
            }
            if (Input.GetMouseButtonUp(0))   LeftHeld = false;
            if (Input.GetMouseButtonDown(1)) { RightHeld = true; SlidePressed = true; }
            if (Input.GetMouseButtonUp(1))   RightHeld = false;

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow))       JumpPressed = true;
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.DownArrow)) SlidePressed = true;
        }
    }
}

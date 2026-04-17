using UnityEngine;

namespace CursorSamurai.Entities
{
    // Soft ellipse under the samurai. Critical "anchor" visual — eye needs this
    // to read the character as grounded. Shrinks + fades when airborne.
    public class GroundShadow : MonoBehaviour
    {
        static Sprite _shadowSprite;
        SpriteRenderer _sr;
        Transform _target;
        float _groundY;
        float _maxJumpY = 5.0f;

        public static GroundShadow AttachTo(Transform samurai, float groundY, float maxJumpHeight)
        {
            EnsureSprite();
            var go = new GameObject("GroundShadow");
            // Attached to the level root, NOT the samurai — so its scale doesn't
            // inherit the samurai's transform changes (squash/stretch/flip).
            go.transform.SetParent(samurai.parent, false);
            var gs = go.AddComponent<GroundShadow>();
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _shadowSprite;
            sr.color = new Color(0, 0, 0, 0.5f);
            sr.sortingOrder = 45;   // under samurai (50), above ground (10)
            gs._sr = sr;
            gs._target = samurai;
            gs._groundY = groundY;
            gs._maxJumpY = maxJumpHeight;
            return gs;
        }

        static void EnsureSprite()
        {
            if (_shadowSprite != null) return;
            // Procedural soft ellipse (64×32) with radial gradient
            const int W = 64, H = 32;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++) {
                    float dx = (x - (W - 1) * 0.5f) / ((W - 1) * 0.5f);
                    float dy = (y - (H - 1) * 0.5f) / ((H - 1) * 0.5f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a;  // soften
                    tex.SetPixel(x, y, new Color(0, 0, 0, a));
                }
            tex.Apply();
            _shadowSprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f);
        }

        void LateUpdate()
        {
            if (_target == null) { Destroy(gameObject); return; }
            // Position: directly under the character's X, fixed at ground Y
            transform.position = new Vector3(_target.position.x, _groundY + 0.02f, _target.position.z + 0.1f);
            // Scale and alpha based on air height
            float airH = Mathf.Max(0, _target.position.y - _groundY);
            float k = Mathf.Clamp01(airH / _maxJumpY);
            float scale = Mathf.Lerp(1.0f, 0.4f, k);
            float alpha = Mathf.Lerp(0.5f, 0.18f, k);
            transform.localScale = new Vector3(scale, scale, 1f);
            var c = _sr.color; c.a = alpha; _sr.color = c;
        }
    }
}

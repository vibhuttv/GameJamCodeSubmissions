using UnityEngine;

namespace CursorSamurai.Core
{
    // Pool-less dust puff: spawn a SpriteRenderer with a procedurally-drawn
    // soft circle, fade out over `life` seconds, then self-destroy.
    public class DustPuff : MonoBehaviour
    {
        static Sprite _puffSprite;

        float _t;
        float _life;
        Vector2 _vel;
        SpriteRenderer _sr;
        Color _baseColor;

        public static void Spawn(Vector3 pos, Color color, float count = 4, float spread = 0.4f)
        {
            EnsureSprite();
            for (int i = 0; i < count; i++) {
                var go = new GameObject("DustPuff");
                go.transform.position = pos + new Vector3(Random.Range(-spread, spread),
                                                          Random.Range(-0.1f, 0.2f), 0);
                var p = go.AddComponent<DustPuff>();
                p._sr = go.AddComponent<SpriteRenderer>();
                p._sr.sprite = _puffSprite;
                p._sr.sortingOrder = 45;
                p._baseColor = color;
                p._sr.color = color;
                p._vel = new Vector2(Random.Range(-1f, 1f) * 1.5f, Random.Range(0.2f, 1.2f));
                p._life = Random.Range(0.35f, 0.6f);
                p.transform.localScale = Vector3.one * Random.Range(0.3f, 0.6f);
            }
        }

        static void EnsureSprite()
        {
            if (_puffSprite != null) return;
            // Build a 32x32 radial-gradient soft circle at runtime
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++) {
                    float dx = x - 15.5f, dy = y - 15.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / 15.5f;
                    float a = Mathf.Clamp01(1f - d) * (1f - d * 0.3f);
                    tex.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            tex.Apply();
            _puffSprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 100f);
        }

        void Update()
        {
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / _life);
            var pos = transform.position;
            pos.x += _vel.x * Time.deltaTime;
            pos.y += _vel.y * Time.deltaTime;
            transform.position = pos;
            transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.2f, k);
            var c = _baseColor; c.a *= (1f - k);
            _sr.color = c;
            if (k >= 1f) Destroy(gameObject);
        }
    }
}

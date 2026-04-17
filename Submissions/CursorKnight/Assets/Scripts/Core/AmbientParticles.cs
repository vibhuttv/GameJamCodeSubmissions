using UnityEngine;

namespace CursorSamurai.Core
{
    // Per-level ambient motion — leaves drifting, bubbles rising, snow falling.
    // Makes static backgrounds feel alive. Small counts (6 on-screen), zero gameplay impact.
    public class AmbientParticles : MonoBehaviour
    {
        public enum Kind { ForestLeaves, WaterBubbles, CaveSnow }

        Kind _kind;
        float _spawnCooldown;
        static Sprite _leaf, _bubble, _snow;

        public static AmbientParticles Create(Transform parent, Kind kind)
        {
            EnsureSprites();
            var go = new GameObject("AmbientParticles_" + kind);
            go.transform.SetParent(parent, false);
            var ap = go.AddComponent<AmbientParticles>();
            ap._kind = kind;
            return ap;
        }

        static void EnsureSprites()
        {
            if (_leaf != null) return;
            _leaf   = BuildLeaf(new Color(0.35f, 0.7f, 0.25f, 1f));
            _bubble = BuildBubble();
            _snow   = BuildSnowflake();
        }

        static Sprite BuildLeaf(Color col)
        {
            const int S = 16;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++) {
                    float dx = x - 7.5f; float dy = y - 7.5f;
                    float d = Mathf.Sqrt(dx * dx * 0.7f + dy * dy);
                    float a = Mathf.Clamp01(1f - d / 7.5f) * 0.9f;
                    tex.SetPixel(x, y, new Color(col.r, col.g, col.b, a));
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite BuildBubble()
        {
            const int S = 16;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++) {
                    float dx = x - 7.5f; float dy = y - 7.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float ring = 1f - Mathf.Clamp01(Mathf.Abs(d - 5.5f));
                    tex.SetPixel(x, y, new Color(0.85f, 0.95f, 1f, ring * 0.8f));
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite BuildSnowflake()
        {
            const int S = 12;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++) {
                    float dx = x - 5.5f; float dy = y - 5.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d / 5f);
                    a = a * a * 0.95f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
        }

        void Update()
        {
            _spawnCooldown -= Time.deltaTime;
            if (_spawnCooldown > 0) return;
            _spawnCooldown = Random.Range(0.25f, 0.6f);
            SpawnOne();
        }

        void SpawnOne()
        {
            var go = new GameObject("amb");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            float yStart;
            Vector2 vel;
            Color col;
            switch (_kind) {
                case Kind.ForestLeaves:
                    sr.sprite = _leaf;
                    // Enter from right, drift left + down
                    go.transform.position = new Vector3(10f, Random.Range(0f, 4f), 1f);
                    vel = new Vector2(Random.Range(-2.5f, -1.3f), Random.Range(-0.8f, -0.2f));
                    col = new Color(
                        Random.Range(0.35f, 0.6f),
                        Random.Range(0.55f, 0.8f),
                        Random.Range(0.2f, 0.35f), 0.85f);
                    break;
                case Kind.WaterBubbles:
                    sr.sprite = _bubble;
                    // Rise from below
                    go.transform.position = new Vector3(Random.Range(-9f, 9f), -4.5f, 1f);
                    vel = new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(0.8f, 1.6f));
                    col = new Color(0.75f, 0.95f, 1f, 0.85f);
                    break;
                default:
                    sr.sprite = _snow;
                    // Drift from above
                    go.transform.position = new Vector3(Random.Range(-9f, 9f), 5f, 1f);
                    vel = new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-1.2f, -0.6f));
                    col = new Color(0.9f, 0.95f, 1f, 0.8f);
                    break;
            }
            sr.color = col;
            sr.sortingOrder = 15;
            var mover = go.AddComponent<AmbientMover>();
            mover.Velocity = vel;
            mover.Life = 12f;
            mover.Spin = Random.Range(-60f, 60f);
            go.transform.localScale = Vector3.one * Random.Range(0.4f, 0.8f);
        }
    }

    public class AmbientMover : MonoBehaviour
    {
        public Vector2 Velocity;
        public float Life = 10f;
        public float Spin;
        SpriteRenderer _sr;
        float _t;
        void Awake() { _sr = GetComponent<SpriteRenderer>(); }
        void Update() {
            _t += Time.deltaTime;
            transform.position += (Vector3)(Velocity * Time.deltaTime);
            transform.Rotate(0, 0, Spin * Time.deltaTime);
            if (_t > Life * 0.7f && _sr != null) {
                var c = _sr.color; c.a *= 0.96f; _sr.color = c;
            }
            if (_t > Life || transform.position.x < -12f || transform.position.y > 7f || transform.position.y < -7f)
                Destroy(gameObject);
        }
    }
}

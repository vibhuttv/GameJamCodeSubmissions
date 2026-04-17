using UnityEngine;
using CursorSamurai.Core;

namespace CursorSamurai.Entities
{
    public class Enemy : MonoBehaviour
    {
        public bool Killed;

        public static Enemy Build()
        {
            var go = new GameObject("Enemy");
            var e = go.AddComponent<Enemy>();
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteCache.Get("Sprites/enemy");
            sr.sortingOrder = 30;
            return e;
        }

        public Bounds GetHitbox() => new Bounds(transform.position + new Vector3(0, 0.1f, 0), new Vector3(0.8f, 1.3f, 1f));
    }
}

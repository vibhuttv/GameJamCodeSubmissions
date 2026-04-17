using UnityEngine;
using CursorSamurai.Core;

namespace CursorSamurai.Entities
{
    public enum CaveKind   { Drum, SpikeBed, Pillar, Beam, DrumStack, DrumGate }
    public enum WaterKind  { Log, Wave, Whirlpool, Raft, Rope, Gap }
    public enum ForestKind { Bamboo, Stump, Vine, Spikes, Wisp, Branch, Sakura, BigTree }

    public class Obstacle : MonoBehaviour
    {
        public Bounds LocalHitbox;
        public bool Fatal;
        // Half the visible sprite height, used so callers can place the obstacle's
        // FEET on the ground line regardless of sprite size.
        public float SpriteHalfHeight;
        // "Floating" obstacles sit above ground (e.g. hanging beam, wisp); skip footing.
        public bool Floating;

        public bool Intersects(Bounds samurai)
        {
            var world = new Bounds(LocalHitbox.center + transform.position, LocalHitbox.size);
            return world.Intersects(samurai);
        }

        public static Obstacle Build(string folder, string kindName, Vector2 hitSize, Vector2 hitOffset, int sortOrder = 10, bool floating = false)
        {
            var go = new GameObject("Ob_" + kindName);
            var ob = go.AddComponent<Obstacle>();
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteCache.Get($"Sprites/Obstacles/{folder}/{kindName}");
            sr.sortingOrder = sortOrder;
            ob.LocalHitbox = new Bounds(hitOffset, hitSize);
            ob.Floating = floating;
            if (sr.sprite != null) {
                // Half-height in world units — used by RunnerLevel to snap feet to ground.
                ob.SpriteHalfHeight = sr.sprite.rect.height / sr.sprite.pixelsPerUnit * 0.5f;
            }
            return ob;
        }

        public static Obstacle BuildCave(CaveKind k)
        {
            // Sprites sourced from striderzz/2D-Platformer-Unity (MIT, Hasan 2023).
            // All cave obstacles sit on the ground — feet at ground top. Hitbox
            // offsets are in local space relative to sprite center; since obstacles
            // are now positioned with feet on ground, hitbox offsets are roughly 0.
            switch (k) {
                case CaveKind.Drum:      return Build("Cave", "drum",      new Vector2(1.4f, 1.8f), new Vector2(0, 0f));   // snowy tree
                case CaveKind.SpikeBed:  return Build("Cave", "spikebed",  new Vector2(1.0f, 0.4f), new Vector2(0, 0f));   // snowy stone (jump)
                case CaveKind.Pillar:    return Build("Cave", "pillar",    new Vector2(0.7f, 0.9f), new Vector2(0, 0f));   // ice cube
                case CaveKind.Beam:      return Build("Cave", "beam",      new Vector2(0.8f, 0.8f), new Vector2(0, 0f));   // crate
                case CaveKind.DrumStack: return Build("Cave", "drumstack", new Vector2(0.8f, 1.4f), new Vector2(0, 0f));   // snowman
                case CaveKind.DrumGate:  return Build("Cave", "drumgate",  new Vector2(2.2f, 1.0f), new Vector2(0, 0f));   // igloo
            }
            return null;
        }

        public static Obstacle BuildWater(WaterKind k)
        {
            Obstacle ob;
            switch (k) {
                case WaterKind.Log:       return Build("Water", "log",       new Vector2(2.2f, 0.6f), new Vector2(0, 0f));
                case WaterKind.Wave:      return Build("Water", "wave",      new Vector2(2.2f, 1.2f), new Vector2(0, 0f));
                case WaterKind.Whirlpool: ob = Build("Water", "whirlpool", new Vector2(1.2f, 0.6f), new Vector2(0, 0f)); ob.Fatal = true; return ob;
                case WaterKind.Raft:      return Build("Water", "raft",      new Vector2(2.2f, 0.9f), new Vector2(0, 0f));
                // Rope hangs from above — floating
                case WaterKind.Rope:      ob = Build("Water", "rope",        new Vector2(2.2f, 1.1f), new Vector2(0, 0f), floating: true); return ob;
                case WaterKind.Gap:       ob = Build("Water", "gap",         new Vector2(2.2f, 0.6f), new Vector2(0, 0f)); ob.Fatal = true; return ob;
            }
            return null;
        }

        public static Obstacle BuildForest(ForestKind k)
        {
            Obstacle ob;
            switch (k) {
                case ForestKind.Bamboo:  return Build("Forest", "bamboo", new Vector2(2.2f, 1.5f), new Vector2(0, 0f));
                case ForestKind.Stump:   return Build("Forest", "stump",  new Vector2(1.9f, 0.7f), new Vector2(0, 0f));
                // Vine hangs from above — floating
                case ForestKind.Vine:    ob = Build("Forest", "vine",   new Vector2(2.2f, 1.1f), new Vector2(0, 0f), floating: true); return ob;
                case ForestKind.Spikes:  return Build("Forest", "spikes", new Vector2(2.2f, 0.6f), new Vector2(0, 0f));
                // Wisp drifts in mid-air — floating
                case ForestKind.Wisp:    ob = Build("Forest", "wisp",   new Vector2(1.1f, 1.4f), new Vector2(0, 0f), floating: true); return ob;
                case ForestKind.Branch:  return Build("Forest", "branch", new Vector2(2.2f, 0.5f), new Vector2(0, 0f));
                case ForestKind.Sakura:  return Build("Forest", "sakura", new Vector2(1.4f, 2.4f), new Vector2(0, 0f));
                case ForestKind.BigTree: return Build("Forest", "tree",   new Vector2(1.3f, 2.2f), new Vector2(0, 0f));
            }
            return null;
        }
    }
}

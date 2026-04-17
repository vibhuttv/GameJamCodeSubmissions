using UnityEngine;
using CursorSamurai.Entities;

namespace CursorSamurai.Levels
{
    public class Level2Water : RunnerLevel
    {
        protected override void Configure()
        {
            LevelFolder = "Water";
            SkyTopColor = new Color(0.42f, 0.65f, 0.84f);
            SkyBotColor = new Color(0.65f, 0.8f, 0.92f);
            GoalTimeSec = 75f; GoalCrystals = 0;   // 75s — and speed peaks harder now
            SpawnInterval = (0.85f, 1.8f);
            CrystalInterval = (1.5f, 3.0f);
            AmbientTrack = "amb_water";
        }

        protected override Obstacle SpawnObstacle()
        {
            float r = Random.value;
            float gapChance = Mathf.Min(0.2f, 0.05f + WorldTime / 200f);
            WaterKind k;
            if      (r < gapChance)           k = WaterKind.Gap;
            else if (r < gapChance + 0.22f)   k = WaterKind.Log;
            else if (r < gapChance + 0.42f)   k = WaterKind.Wave;
            else if (r < gapChance + 0.58f)   k = WaterKind.Whirlpool;
            else if (r < gapChance + 0.78f)   k = WaterKind.Raft;
            else                              k = WaterKind.Rope;
            return Obstacle.BuildWater(k);
        }
    }
}

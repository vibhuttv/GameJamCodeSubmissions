using UnityEngine;
using CursorSamurai.Entities;

namespace CursorSamurai.Levels
{
    // NOTE: class name is historical — this is now shown to the player as LEVEL 1
    // (the forest opens the game because its art hooks the player fastest). Boss
    // was moved to Level1Cave (final level). This level focuses on learning the
    // controls + collecting coins.
    public class Level3Forest : RunnerLevel
    {
        protected override void Configure()
        {
            LevelFolder = "Forest";
            // Sky color matches the repo's sky.PNG (pale mint) so parallax layers
            // composite cleanly over the camera's solid background.
            SkyTopColor = new Color(0.82f, 0.94f, 0.94f);
            SkyBotColor = SkyTopColor;
            GoalTimeSec = 40f;
            GoalCrystals = 10;
            SpawnInterval = (1.1f, 2.0f);
            CrystalInterval = (1.3f, 2.8f);
            AmbientTrack = "amb_forest";
        }

        protected override Obstacle SpawnObstacle()
        {
            float r = Random.value;
            ForestKind k;
            if      (r < 0.15f) k = ForestKind.Sakura;
            else if (r < 0.30f) k = ForestKind.BigTree;
            else if (r < 0.45f) k = ForestKind.Bamboo;
            else if (r < 0.58f) k = ForestKind.Stump;
            else if (r < 0.70f) k = ForestKind.Vine;
            else if (r < 0.82f) k = ForestKind.Spikes;
            else if (r < 0.92f) k = ForestKind.Wisp;
            else                k = ForestKind.Branch;
            return Obstacle.BuildForest(k);
        }
    }
}

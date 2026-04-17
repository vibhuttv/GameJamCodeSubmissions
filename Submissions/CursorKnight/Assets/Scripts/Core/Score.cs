namespace CursorSamurai.Core
{
    public class Score
    {
        public float Points;
        public int Crystals, EnemiesKilled, LevelsCompleted, Deaths;
        public float TimeSurvived;

        public int IntScore => UnityEngine.Mathf.FloorToInt(Points);

        public readonly Combo Combo = new Combo();

        public void Tick(float dt) { TimeSurvived += dt; Points += 10f * dt; Combo.Tick(dt); }
        public void AddCrystal() { Crystals++; Combo.Bump(); Points += 50f * Combo.Multiplier; }
        public void AddEnemyKill() { EnemiesKilled++; Combo.Bump(); Points += 100f * Combo.Multiplier; }
        public void BreakCombo() { Combo.Break(); }
        public void BumpCombo() { Combo.Bump(); }
        public void CompleteLevel(bool clean) { LevelsCompleted++; if (clean) Points += 500f; }
        public void DefeatBoss() { Points += 1000f; }
        public void RegisterDeath() { Deaths++; }
        public void Reset() {
            Points = 0; Crystals = 0; EnemiesKilled = 0;
            TimeSurvived = 0; LevelsCompleted = 0; Deaths = 0;
            Combo.Reset();
        }
    }
}

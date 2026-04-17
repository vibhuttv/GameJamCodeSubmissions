using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using CursorSamurai.Core;
using CursorSamurai.Entities;
using CursorSamurai.Levels;
using CursorSamurai.UI;

namespace CursorSamurai
{
    public class GameRoot : MonoBehaviour
    {
        public static GameRoot I;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void CS_DisableContextMenu();
#endif

        public Score Score;
        public InputManager Input;
        public HUD HUD;
        public Samurai Samurai;
        public Camera Cam;
        public CursorFX CursorFX;
        public PostProcessingManager PP;
        public LevelTransition Transition;
        public TitleScreen Title;
        bool _titleShown;

        RunnerLevel _currentLevel;
        public bool CurrentScrubActive => _currentLevel?.Scrub?.IsActive ?? false;
        public RunnerLevel CurrentLevel => _currentLevel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            var go = new GameObject("GameRoot");
            DontDestroyOnLoad(go);
            go.AddComponent<GameRoot>();
        }

        void Awake()
        {
            I = this;
#if UNITY_WEBGL && !UNITY_EDITOR
            try { CS_DisableContextMenu(); } catch { /* ignore if plugin not wired */ }
#endif

            // Reuse the scene's edit-mode camera if present; otherwise spawn one.
            // This keeps the Game view rendering in edit mode (no "No Cameras
            // rendering" diagnostic) and avoids duplicate-MainCamera warnings.
            Cam = Camera.main;
            if (Cam == null) {
                var camGo = new GameObject("MainCamera");
                camGo.transform.SetParent(transform, false);
                camGo.tag = "MainCamera";
                Cam = camGo.AddComponent<Camera>();
            }
            Cam.orthographic = true;
            Cam.orthographicSize = 4.5f;
            Cam.nearClipPlane = 0.1f;
            Cam.farClipPlane = 100f;
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = new Color(0.22f, 0.3f, 0.42f);
            Cam.transform.position = new Vector3(0, 0.6f, -10f);
            Cam.transform.rotation = Quaternion.identity;
            if (Cam.gameObject.GetComponent<AudioListener>() == null)
                Cam.gameObject.AddComponent<AudioListener>();

            // Screen shake — attach to camera pivot. We wrap the camera in a parent
            // GameObject so shake applies via localPosition without breaking our
            // intentional "Cam at (0, 0.6, -10)" world placement.
            var shakeGo = new GameObject("ScreenShake");
            shakeGo.transform.SetParent(transform, false);
            var shake = shakeGo.AddComponent<ScreenShake>();
            shake.Init(Cam);

            // Audio
            var audioGo = new GameObject("Audio");
            audioGo.transform.SetParent(transform, false);
            audioGo.AddComponent<AudioSystem>();

            // Input
            var inputGo = new GameObject("Input");
            inputGo.transform.SetParent(transform, false);
            Input = inputGo.AddComponent<InputManager>();

            // HUD
            var hudGo = new GameObject("HUD");
            hudGo.transform.SetParent(transform, false);
            HUD = hudGo.AddComponent<HUD>();
            HUD.Init();

            // Hitstop (Celeste-pattern impact freeze)
            gameObject.AddComponent<Hitstop>();

            // Flash FX — hurt-flash on sprite + full-screen flash on big moments
            var flashGo = new GameObject("FlashFX");
            flashGo.transform.SetParent(transform, false);
            var flash = flashGo.AddComponent<FlashFX>();

            // Post Processing — runtime profile (Bloom / Vignette / CA / Grading / Grain)
            var ppGo = new GameObject("PostProcess");
            ppGo.transform.SetParent(transform, false);
            PP = ppGo.AddComponent<PostProcessingManager>();
            PP.Init(Cam);

            // Cursor FX
            var cursorFxGo = new GameObject("CursorFX");
            cursorFxGo.transform.SetParent(transform, false);
            CursorFX = cursorFxGo.AddComponent<CursorFX>();
            CursorFX.Init(HUD.GetCanvas());

            // Wire screen flash on HUD canvas (must happen AFTER HUD.Init)
            flash.InitScreenFlash(HUD.GetCanvas());

            // Level transition (fade + name card)
            var transGo = new GameObject("Transition");
            transGo.transform.SetParent(transform, false);
            Transition = transGo.AddComponent<LevelTransition>();
            var pxFont = Resources.Load<Font>("Fonts/PressStart2P") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Transition.Init(HUD.GetCanvas(), pxFont);

            // Samurai
            var samuraiGo = new GameObject("Samurai");
            samuraiGo.transform.SetParent(transform, false);
            Samurai = samuraiGo.AddComponent<Samurai>();
            Samurai.Init(Input);

            Score = new Score();

            // Title screen — first thing the player sees
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(transform, false);
            Title = titleGo.AddComponent<TitleScreen>();
            Title.Init(HUD.GetCanvas(), pxFont, StartNewRun);
            _titleShown = true;
        }

        void Update()
        {
            if (_currentLevel != null) _currentLevel.Tick(Time.deltaTime);
            RefreshHud();
            if (CursorFX != null && Input != null) {
                CursorFX.Tick(Input.MousePx);
                CursorFX.SetScrubActive(Input.ScrubHeld && (_currentLevel?.AllowScrub ?? false));
            }
            if (_currentLevel != null) {
                HUD.UpdateScrubBar(_currentLevel.Scrub.OffsetSeconds, _currentLevel.AllowScrub, Input.ScrubHeld);
                // Feed the active level's scrub state to Post Processing
                PP?.FeedScrub(_currentLevel.AllowScrub ? _currentLevel.Scrub.OffsetSeconds : 0f,
                              Input.ScrubHeld && _currentLevel.AllowScrub);
            } else {
                PP?.FeedScrub(0, false);
            }
        }

        // Animated score count-up — chases actual points so pickups feel rewarding
        float _displayedScore;
        void RefreshHud()
        {
            if (_currentLevel == null) return;
            float gap = Score.Points - _displayedScore;
            float speed = Mathf.Max(120f, Mathf.Abs(gap) * 6f);
            _displayedScore = Mathf.MoveTowards(_displayedScore, Score.Points, speed * Time.unscaledDeltaTime);

            HUD.SetCoinRow(Score.Crystals, _currentLevel.GoalCrystalsPublic);
            HUD.SetScoreRow(Mathf.FloorToInt(_displayedScore));
            HUD.SetTimeRow(_currentLevel.WorldTimePublic, _currentLevel.GoalTimeSecPublic);
            HUD.SetLevelBadge(_currentLevel.LevelNumber);

            var combo = Score.Combo;
            HUD.SetComboTag(combo.Count, combo.Multiplier);
            HUD.UpdateComboCenter(combo.Count, combo.Multiplier);
        }

        public void StartNewRun() {
            Score.Reset();
            if (Title != null) Title.Hide();
            _titleShown = false;
            StartLevel(1);
        }

        // Gameplay order: Forest (L1, hook visuals) → Water (L2) → Cave (L3 boss finale).
        // Class names are historical — the CLASS Level3Forest is now UI-level 1.
        public void StartLevel(int n)
        {
            switch (n) {
                case 1: SwapLevel(CreateLevel<Level3Forest>()); break;
                case 2: SwapLevel(CreateLevel<Level2Water>()); break;
                case 3: SwapLevel(CreateLevel<Level1Cave>()); break;
                default: ShowVictory(); break;
            }
        }

        RunnerLevel CreateLevel<T>() where T : RunnerLevel
        {
            var go = new GameObject(typeof(T).Name);
            go.transform.SetParent(transform, false);
            var level = go.AddComponent<T>();
            level.Samurai = Samurai;
            level.Input = Input;
            level.Score = Score;
            Samurai.transform.SetParent(go.transform, false);
            return level;
        }

        void SwapLevel(RunnerLevel next)
        {
            // First-time (no previous level): snap in with quick fade, no name card
            if (_currentLevel == null) {
                _currentLevel = next;
                Input.Frozen = false;
                HUD.HideOverlay();
                next.Begin();
                ApplyLevelTint(next);
                return;
            }
            StartCoroutine(TransitionTo(next));
        }

        System.Collections.IEnumerator TransitionTo(RunnerLevel next)
        {
            string cardName = next is Level3Forest ? "I · FOREST"
                             : next is Level2Water ? "II · WATER"
                             : next is Level1Cave  ? "III · ICE CAVE"
                             : "";
            yield return Transition.Run(cardName, () => {
                if (_currentLevel != null) { _currentLevel.End(); Destroy(_currentLevel.gameObject); }
                _currentLevel = next;
                Input.Frozen = false;
                HUD.HideOverlay();
                next.Begin();
                ApplyLevelTint(next);
            });
        }

        void ApplyLevelTint(RunnerLevel lvl)
        {
            // Per-level color-filter through Post Processing unifies art across packs
            if (PP == null) return;
            if (lvl is Level3Forest) PP.SetLevelTint(new Color(0.96f, 1.02f, 0.94f));  // slight green
            else if (lvl is Level2Water) PP.SetLevelTint(new Color(0.92f, 0.98f, 1.06f));  // cool cyan
            else if (lvl is Level1Cave) PP.SetLevelTint(new Color(0.90f, 0.92f, 1.08f));  // cold violet
            else PP.SetLevelTint(Color.white);
        }

        public void OnLevelVictory(RunnerLevel lvl)
        {
            // Victory flash — warm gold tint on level complete
            FlashFX.I?.FullScreenFlash(new Color(1f, 0.92f, 0.55f, 0.85f), 0.35f);
            if      (lvl is Level3Forest) StartLevel(2);
            else if (lvl is Level2Water)  StartLevel(3);
            else                          ShowVictory();
        }

        public void ShowDeathOverlay(string reason, RunnerLevel lvl)
        {
            Input.Frozen = true;
            HUD.ShowOverlay(
                "KNIGHT FALLEN",
                new List<(string, System.Action)> {
                    ("RETRY LEVEL", () => StartLevel(lvl.LevelNumber)),
                    ("RESTART GAME", () => StartNewRun())
                },
                $"{reason}   ·   Score: {Score.IntScore}   ·   Crystals: {Score.Crystals}"
            );
        }

        public void ShowVictory()
        {
            if (_currentLevel != null) { _currentLevel.End(); Destroy(_currentLevel.gameObject); _currentLevel = null; }
            AudioSystem.I?.PlaySfx("victory");
            Input.Frozen = true;
            Samurai.Cheer();
            HUD.ShowOverlay(
                "KNIGHT TRIUMPHANT",
                new List<(string, System.Action)> {
                    ("PLAY AGAIN", () => StartNewRun())
                },
                $"Score: {Score.IntScore}  ·  Crystals: {Score.Crystals}  ·  Slain: {Score.EnemiesKilled}  ·  Deaths: {Score.Deaths}  ·  Time: {Score.TimeSurvived:0.0}s"
            );
        }
    }
}

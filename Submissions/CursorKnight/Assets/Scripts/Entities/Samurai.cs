using UnityEngine;
using CursorSamurai.Core;

namespace CursorSamurai.Entities
{
    public enum SamuraiState { Run, Jump, Slide, Slash, Die, Cheer }

    // 2D samurai. SpriteRenderer cycles through pre-baked frames per state.
    // Vertical steering via mouse Y (cursor-follow). Left-click = jump,
    // right-click = slide. Collides with obstacles as an AABB.
    public class Samurai : MonoBehaviour
    {
        // Samurai default Y (feet on ground), cursor-follow steering adds offset
        // Ground top is at y = -2.4 (ground sprite center -3.2 + half-size 0.8).
        // Samurai sprite is 256px/100ppu * BaseScale = 1.41 tall → half-height 0.7.
        // Center must sit at GroundTop + half = -2.4 + 0.7 = -1.7 so feet touch ground.
        public const float GroundTopY = -2.4f;
        public const float GroundY = -1.65f;
        // Centralized "feet Y" — other systems should read this instead of recomputing.
        public float FootY => transform.position.y - 0.7f;
        public const float MaxUpOffset = 4.3f;
        public const float MaxDownOffset = 0.0f;
        // Knight sprites (~675 px tall) scaled to ~1.48 world units
        const float BaseScale = 0.22f;
        const float MoveLerp = 14f;
        const float JumpPeak = 3.0f;
        const float JumpAirTime = 0.8f;
        const float SlideDuration = 0.7f;
        const float RunFps = 12f;
        const float JumpFps = 8f;
        const float SlideFps = 6f;
        const float SlashFps = 14f;
        const float DieFps = 5f;
        const float CheerFps = 6f;

        public SamuraiState State = SamuraiState.Run;
        public bool Alive = true;

        float _jumpT, _slideT, _slashT;
        float _targetY, _currentY;
        float _frameT;
        int _frameIdx;
        int _jumpsUsed;
        const int MaxJumps = 2;
        float _jumpBufferT;
        const float JumpBufferTime = 0.12f;

        // Jump is an additive vertical offset with real gravity, layered on top of
        // cursor-Y. Cursor remains authoritative for base position; jumps contribute
        // a decaying lift. Resolves the "cursor overrides jump" control conflict.
        float _airOffset;         // world units above cursor-tracked Y
        float _airVel;            // upward velocity
        const float JumpImpulse = 6.8f;     // initial velocity per jump
        const float Gravity = 22f;          // pulls offset back to 0
        const float MaxAirOffset = 5.0f;
        // Intro drop: samurai starts above the ground and falls in cinematically
        float _introT;
        const float IntroDuration = 0.55f;
        const float IntroStartOffset = 4.0f;  // units above GroundY
        SpriteRenderer _sr;
        InputManager _input;

        Sprite[] _run, _jump, _slide, _slash, _die, _cheer;

        GroundShadow _shadow;

        public void Init(InputManager input)
        {
            _input = input;
            _sr = gameObject.GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sortingOrder = 50;
            _sr.flipX = false;

            _run   = SpriteCache.GetFrames("Sprites/Samurai", "run");
            _jump  = SpriteCache.GetFrames("Sprites/Samurai", "jump");
            _slide = SpriteCache.GetFrames("Sprites/Samurai", "slide");
            _slash = SpriteCache.GetFrames("Sprites/Samurai", "slash");
            _die   = SpriteCache.GetFrames("Sprites/Samurai", "die");
            _cheer = SpriteCache.GetFrames("Sprites/Samurai", "cheer");

            if (_run != null && _run.Length > 0) _sr.sprite = _run[0];
            else Debug.LogError("[Samurai] No run frames loaded from Resources/Sprites/Samurai/ — samurai will be invisible. Ensure files run_0.png..run_N.png exist and are imported as Sprites.");
        }

        public void EnsureShadow(float groundY)
        {
            // Called by RunnerLevel.Begin — creates or reattaches the ground shadow
            // under the samurai so it moves with us but stays pinned to ground Y.
            if (_shadow != null) Destroy(_shadow.gameObject);
            _shadow = GroundShadow.AttachTo(transform, groundY, MaxUpOffset);
        }

        public void Reset()
        {
            Alive = true;
            // Start mid-air: samurai drops in, lands, then starts running.
            State = SamuraiState.Jump;
            _jumpT = 0; _slideT = 0; _slashT = 0;
            _targetY = GroundY + IntroStartOffset; _currentY = GroundY + IntroStartOffset;
            _frameT = 0; _frameIdx = 0;
            _jumpsUsed = 0;
            _introT = IntroDuration;
            transform.position = new Vector3(-4f, GroundY + IntroStartOffset, 0);
            transform.rotation = Quaternion.identity;
            if (_sr != null && _jump != null && _jump.Length > 0) _sr.sprite = _jump[1];
        }

        public void TriggerJump()
        {
            if (!Alive) return;
            if (_jumpsUsed >= MaxJumps) return;
            State = SamuraiState.Jump;
            _jumpT = 0; _frameT = 0; _frameIdx = 0;
            _jumpsUsed++;
            AudioSystem.I?.PlaySfx("jump");
        }

        public void TriggerSlash()
        {
            if (!Alive) return;
            _slashT = 0.35f;
            AudioSystem.I?.PlaySfx("slash");
        }

        public void Die()
        {
            Alive = false;
            State = SamuraiState.Die;
            _frameT = 0; _frameIdx = 0;
            AudioSystem.I?.PlaySfx("death");
            ScreenShake.I?.AddTrauma(0.9f);
            Hitstop.I?.Freeze(8);
            FlashFX.HurtFlash(_sr, 5);  // hurt flash before death frames play
            FlashFX.I?.FullScreenFlash(new Color(0.7f, 0.05f, 0.05f, 0.85f), 0.35f);  // red tinted flash
            DustPuff.Spawn(transform.position, new Color(0.6f, 0.1f, 0.1f, 0.8f), 12, spread: 0.8f);
        }

        public void Cheer()
        {
            State = SamuraiState.Cheer;
            _frameT = 0; _frameIdx = 0;
        }

        public void Tick(float dt)
        {
            if (!Alive) {
                AdvanceFrames(dt, _die, DieFps, loop: false);
                return;
            }

            // Intro fall: samurai drops from above, lands with dust puff.
            // Input ignored during intro so the player can't jump away mid-drop.
            if (_introT > 0) {
                _introT -= dt;
                float introK = 1f - Mathf.Clamp01(_introT / IntroDuration);
                // Quadratic ease-in (feels like gravity)
                float introY = Mathf.Lerp(GroundY + IntroStartOffset, GroundY, introK * introK);
                transform.position = new Vector3(-4f, introY, 0);
                transform.localScale = new Vector3(BaseScale, BaseScale, 1f);
                AdvanceFrames(dt, _jump, JumpFps, loop: true);
                if (_introT <= 0) {
                    State = SamuraiState.Run;
                    _frameT = 0; _frameIdx = 0;
                    _jumpsUsed = 0;
                    _currentY = GroundY;
                    _targetY = GroundY;
                    DustPuff.Spawn(new Vector3(-4f, GroundY - 0.6f, 0),
                                   new Color(1f, 0.95f, 0.8f, 0.85f), 10, spread: 0.9f);
                    ScreenShake.I?.AddTrauma(0.2f);
                    AudioSystem.I?.PlaySfx("hit", 0.5f, 0.7f);
                }
                return;
            }

            // Jump: additive impulse. Buffered to catch early presses.
            if (_input.JumpPressed) _jumpBufferT = JumpBufferTime;
            if (_jumpBufferT > 0 && _jumpsUsed < MaxJumps && State != SamuraiState.Slide) {
                _airVel = JumpImpulse;
                _jumpsUsed++;
                _jumpBufferT = 0;
                _frameT = 0; _frameIdx = 0;
                if (State != SamuraiState.Jump) State = SamuraiState.Jump;
                AudioSystem.I?.PlaySfx("jump");
                DustPuff.Spawn(new Vector3(transform.position.x, transform.position.y - 0.5f, 0),
                               new Color(1f, 0.95f, 0.8f, 0.6f), 4);
            }
            if (_jumpBufferT > 0) _jumpBufferT -= dt;
            if (_input.SlidePressed && _airOffset <= 0.05f && State != SamuraiState.Slide) {
                State = SamuraiState.Slide; _slideT = 0; _frameT = 0; _frameIdx = 0;
                AudioSystem.I?.PlaySfx("slide");
            }

            // Integrate jump physics — gravity pulls air offset toward 0
            _airVel -= Gravity * dt;
            _airOffset += _airVel * dt;
            if (_airOffset <= 0) {
                bool wasAirborne = _jumpsUsed > 0;
                _airOffset = 0;
                _airVel = 0;
                if (wasAirborne) {
                    _jumpsUsed = 0;
                    if (State == SamuraiState.Jump) {
                        State = SamuraiState.Run; _frameT = 0; _frameIdx = 0;
                        DustPuff.Spawn(new Vector3(transform.position.x, GroundY - 0.5f, 0),
                                       new Color(1f, 0.95f, 0.8f, 0.7f), 6, spread: 0.5f);
                    }
                }
            }
            _airOffset = Mathf.Min(_airOffset, MaxAirOffset);

            // Cursor-follow: mouse Y NDC → base Y. Always authoritative for *base* position.
            float norm = Mathf.Clamp01((_input.MouseNDC.y + 1f) * 0.5f);
            _targetY = GroundY + norm * MaxUpOffset - (1f - norm) * MaxDownOffset;
            float k = 1f - Mathf.Exp(-MoveLerp * dt);
            _currentY += (_targetY - _currentY) * k;

            // Final rendered Y = cursor-tracked base + decaying air offset from jumps
            float y = _currentY + _airOffset;
            float scaleX = 1f, scaleY = 1f, rotZ = 0f;

            switch (State) {
                case SamuraiState.Jump: {
                    AdvanceFrames(dt, _jump, JumpFps, loop: false);
                    // Lean proportional to vertical velocity (more lift = more forward tilt)
                    float velRatio = Mathf.Clamp(_airVel / JumpImpulse, -1f, 1f);
                    rotZ = -10f * velRatio;
                    scaleY = 1f + velRatio * 0.08f;
                    break;
                }
                case SamuraiState.Slide: {
                    _slideT += dt;
                    AdvanceFrames(dt, _slide, SlideFps, loop: true);
                    float slideK = Mathf.Min(1f, _slideT / 0.12f);
                    rotZ = 22f * slideK;
                    scaleY = Mathf.Lerp(1f, 0.7f, slideK);
                    scaleX = Mathf.Lerp(1f, 1.15f, slideK);
                    if (_slideT >= SlideDuration) { State = SamuraiState.Run; _frameT = 0; _frameIdx = 0; }
                    break;
                }
                default: {
                    int prevFrame = _frameIdx;
                    AdvanceFrames(dt, _run, RunFps, loop: true);
                    // Foot-contact dust on frames 2 and 6 of the 10-frame run cycle
                    if (_airOffset <= 0.1f && _frameIdx != prevFrame && (_frameIdx == 2 || _frameIdx == 6)) {
                        DustPuff.Spawn(new Vector3(transform.position.x - 0.3f, GroundY - 0.3f, 0),
                                       new Color(1f, 0.95f, 0.8f, 0.4f), 1, spread: 0.15f);
                    }
                    // Running bob: squash/stretch + tiny side lean
                    float t = Time.time * RunFps;
                    scaleY = 1f + Mathf.Abs(Mathf.Sin(t)) * 0.05f;
                    scaleX = 1f - Mathf.Abs(Mathf.Sin(t)) * 0.02f;
                    rotZ = Mathf.Sin(t * 0.5f) * 2f;
                    break;
                }
            }

            transform.localScale = new Vector3(scaleX * BaseScale, scaleY * BaseScale, 1f);
            transform.localRotation = Quaternion.Euler(0, 0, rotZ);

            // Slash overlay
            if (_slashT > 0) {
                _slashT -= dt;
                AdvanceFrames(dt, _slash, SlashFps, loop: false);
            }

            transform.position = new Vector3(-4f, y, 0);
        }

        void AdvanceFrames(float dt, Sprite[] frames, float fps, bool loop)
        {
            if (frames == null || frames.Length == 0 || _sr == null) return;
            _frameT += dt * fps;
            while (_frameT >= 1f) {
                _frameT -= 1f;
                _frameIdx++;
                if (_frameIdx >= frames.Length) {
                    if (loop) _frameIdx = 0;
                    else _frameIdx = frames.Length - 1;
                }
            }
            _sr.sprite = frames[_frameIdx];
        }

        static float JumpParabola(float t, float peak, float airTime)
        {
            if (t <= 0 || t >= airTime) return 0;
            float nt = t / airTime;
            return 4 * peak * nt * (1 - nt);
        }

        public Bounds GetHitbox()
        {
            bool sliding = State == SamuraiState.Slide;
            float w = 0.6f;
            float h = sliding ? 0.5f : 1.1f;
            float yOff = sliding ? -0.4f : 0f;
            return new Bounds(
                new Vector3(transform.position.x, transform.position.y + yOff, 0),
                new Vector3(w, h, 1f));
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace CursorSamurai.Core
{
    // Timeline Scrub Runtime mechanic — ported to 2D.
    // Mouse at far-left edge = rewind up to 2s; far-right edge = fast-forward 0.8s.
    // Obstacles sample their past/future X position from a ring buffer while scrubbing.
    public class TimelineScrub
    {
        public const float MaxRewind = 2.0f;
        public const float MaxForward = 0.8f;

        float _offset, _target;
        public float OffsetSeconds => _offset;
        public bool IsActive => Mathf.Abs(_offset) > 0.05f;

        public void Update(float dt, float scrubInput)
        {
            // scrubInput in [-1,1], positive = forward, negative = rewind
            float sign = Mathf.Sign(scrubInput);
            float mag = Mathf.Clamp01(Mathf.Abs(scrubInput));
            _target = mag <= 0 ? 0f : sign * mag * (sign > 0 ? MaxForward : MaxRewind);
            float ease = 1f - Mathf.Pow(0.001f, dt);
            _offset += (_target - _offset) * ease;
        }

        public void Reset() { _offset = 0; _target = 0; }
    }

    public class TimelineChannel
    {
        struct S { public float t, v; }
        readonly List<S> _samples = new List<S>();
        readonly float _cap;
        public TimelineChannel(float capSec = 3f) { _cap = capSec; }

        public void Push(float t, float v)
        {
            _samples.Add(new S { t = t, v = v });
            float cutoff = t - _cap;
            int drop = 0;
            while (drop < _samples.Count && _samples[drop].t < cutoff) drop++;
            if (drop > 0) _samples.RemoveRange(0, drop);
        }

        public bool TrySample(float at, out float result)
        {
            result = 0;
            if (_samples.Count == 0) return false;
            if (at <= _samples[0].t) { result = _samples[0].v; return true; }
            var last = _samples[_samples.Count - 1];
            if (at >= last.t) { result = last.v; return true; }
            int lo = 0, hi = _samples.Count - 1;
            while (lo < hi - 1) {
                int mid = (lo + hi) >> 1;
                if (_samples[mid].t <= at) lo = mid; else hi = mid;
            }
            var a = _samples[lo]; var b = _samples[hi];
            float span = b.t - a.t;
            float k = span > 0 ? (at - a.t) / span : 0;
            result = a.v + (b.v - a.v) * k;
            return true;
        }
    }
}

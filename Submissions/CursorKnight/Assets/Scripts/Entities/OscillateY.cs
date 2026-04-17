using UnityEngine;

namespace CursorSamurai.Entities
{
    // Attach to a floating obstacle so it bobs up and down while scrolling.
    // Introduces a "moving target" dodge requirement at higher difficulty.
    public class OscillateY : MonoBehaviour
    {
        float _baseY;
        float _amplitude;
        float _speed;
        float _phase;

        public void Setup(float baseY, float amplitude, float speed)
        {
            _baseY = baseY;
            _amplitude = amplitude;
            _speed = speed;
            _phase = Random.Range(0f, Mathf.PI * 2);
        }

        void LateUpdate()
        {
            var p = transform.position;
            float offset = Mathf.Sin(Time.time * _speed + _phase) * _amplitude;
            // Adjust baseY when the level scrolled our base (baseY is set at spawn,
            // but level scrolls on X only — Y stays anchored). Keep it simple:
            p.y = _baseY + offset;
            transform.position = p;
        }
    }
}

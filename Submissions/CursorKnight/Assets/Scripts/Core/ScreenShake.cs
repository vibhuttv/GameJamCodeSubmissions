using UnityEngine;

namespace CursorSamurai.Core
{
    // Trauma-based screen shake. Call AddTrauma() to nudge the camera;
    // shake decays automatically. Pattern inspired by AAA platformers —
    // more tolerable than Perlin noise because it doesn't oscillate at
    // steady state. Single-source, easy to layer.
    public class ScreenShake : MonoBehaviour
    {
        public static ScreenShake I;

        [Range(0, 1)] public float Trauma;
        public float MaxOffset = 0.35f;
        public float MaxAngle = 3f;
        public float DecayPerSec = 2.2f;

        Camera _cam;
        Vector3 _basePos;
        Quaternion _baseRot;

        public void Init(Camera cam)
        {
            I = this;
            _cam = cam;
            _basePos = cam.transform.localPosition;
            _baseRot = cam.transform.localRotation;
        }

        public void AddTrauma(float amount)
        {
            Trauma = Mathf.Clamp01(Trauma + amount);
        }

        public void ResetShake()
        {
            Trauma = 0f;
            if (_cam != null) {
                _cam.transform.localPosition = _basePos;
                _cam.transform.localRotation = _baseRot;
            }
        }

        void LateUpdate()
        {
            if (_cam == null) return;
            if (Trauma <= 0.001f) {
                _cam.transform.localPosition = _basePos;
                _cam.transform.localRotation = _baseRot;
                return;
            }
            // trauma^2 gives nice feel — low trauma = subtle, high = violent
            float shake = Trauma * Trauma;
            Vector3 off = new Vector3(
                (Mathf.PerlinNoise(Time.time * 20f, 0f) - 0.5f) * 2f * MaxOffset * shake,
                (Mathf.PerlinNoise(0f, Time.time * 20f) - 0.5f) * 2f * MaxOffset * shake,
                0f);
            float angle = (Mathf.PerlinNoise(Time.time * 20f, 10f) - 0.5f) * 2f * MaxAngle * shake;
            _cam.transform.localPosition = _basePos + off;
            _cam.transform.localRotation = _baseRot * Quaternion.Euler(0, 0, angle);
            Trauma = Mathf.Max(0, Trauma - DecayPerSec * Time.deltaTime);
        }
    }
}

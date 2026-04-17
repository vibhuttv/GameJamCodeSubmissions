using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace CursorSamurai.Core
{
    // Runtime Post Processing setup — no scene asset wiring needed. Creates
    // a PP layer on the camera + a global volume with a built-at-runtime profile
    // containing Bloom / Vignette / ChromaticAberration / ColorGrading / Grain.
    // Timeline Scrub feeds CA + desaturation every frame so the mechanic is felt.
    public class PostProcessingManager : MonoBehaviour
    {
        public static PostProcessingManager I;

        PostProcessVolume _volume;
        ChromaticAberration _ca;
        Vignette _vignette;
        Bloom _bloom;
        ColorGrading _grading;
        Grain _grain;

        // Per-level tint (unifies asset-pack visual differences across 3 levels)
        Color _tint = Color.white;

        public void Init(Camera cam)
        {
            I = this;
            // PP layer on the camera
            var layer = cam.gameObject.GetComponent<PostProcessLayer>();
            if (layer == null) layer = cam.gameObject.AddComponent<PostProcessLayer>();
            layer.volumeLayer = LayerMask.GetMask("Default");
            layer.volumeTrigger = cam.transform;
            layer.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;

            // Global volume + runtime profile
            var vGo = new GameObject("GlobalPPVolume");
            vGo.transform.SetParent(transform, false);
            _volume = vGo.AddComponent<PostProcessVolume>();
            _volume.isGlobal = true;
            _volume.priority = 1;

            var profile = ScriptableObject.CreateInstance<PostProcessProfile>();
            _volume.profile = profile;

            _bloom = profile.AddSettings<Bloom>();
            _bloom.enabled.Override(true);
            _bloom.intensity.Override(0.8f);
            _bloom.threshold.Override(1.05f);
            _bloom.softKnee.Override(0.6f);
            _bloom.color.Override(Color.white);

            _vignette = profile.AddSettings<Vignette>();
            _vignette.enabled.Override(true);
            _vignette.intensity.Override(0.28f);
            _vignette.smoothness.Override(0.45f);
            _vignette.color.Override(Color.black);

            _ca = profile.AddSettings<ChromaticAberration>();
            _ca.enabled.Override(true);
            _ca.intensity.Override(0f);

            _grading = profile.AddSettings<ColorGrading>();
            _grading.enabled.Override(true);
            _grading.saturation.Override(0f);
            _grading.contrast.Override(8f);
            _grading.colorFilter.Override(Color.white);

            _grain = profile.AddSettings<Grain>();
            _grain.enabled.Override(true);
            _grain.intensity.Override(0.08f);
            _grain.size.Override(1.2f);
            _grain.colored.Override(false);
        }

        // Called per-frame by GameRoot with the active level's scrub state.
        public void FeedScrub(float offsetSec, bool scrubbing)
        {
            if (_ca == null) return;
            float abs = Mathf.Abs(offsetSec);
            // Ramp CA 0 → ~0.9 over rewind/forward range
            _ca.intensity.value = Mathf.Clamp01(abs * 0.6f);
            // Desaturate world on rewind (sepia feel), shift toward cyan on fast-fwd
            _grading.saturation.value = Mathf.Lerp(0f, -75f, abs * 0.5f);
            if (offsetSec < -0.1f)      _grading.colorFilter.value = Color.Lerp(_tint, new Color(1f, 0.85f, 0.7f), abs * 0.4f);
            else if (offsetSec > 0.1f)  _grading.colorFilter.value = Color.Lerp(_tint, new Color(0.75f, 1f, 1.05f), abs * 0.5f);
            else                        _grading.colorFilter.value = _tint;
            // Grain intensifies with scrub
            _grain.intensity.value = 0.08f + abs * 0.22f;
            // Vignette tightens
            _vignette.intensity.value = 0.28f + abs * 0.22f;
        }

        public void SetLevelTint(Color c)
        {
            _tint = c;
            if (_grading != null) _grading.colorFilter.value = c;
        }
    }
}

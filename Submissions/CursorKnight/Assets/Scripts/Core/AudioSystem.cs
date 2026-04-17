using System.Collections.Generic;
using UnityEngine;

namespace CursorSamurai.Core
{
    public class AudioSystem : MonoBehaviour
    {
        public static AudioSystem I;
        const int SfxVoices = 12;
        AudioSource _music;
        readonly List<AudioSource> _sfx = new List<AudioSource>();
        readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();

        void Awake()
        {
            I = this;
            DontDestroyOnLoad(gameObject);
            _music = gameObject.AddComponent<AudioSource>();
            _music.loop = true;
            _music.volume = 0.4f;
            for (int i = 0; i < SfxVoices; i++) {
                var s = gameObject.AddComponent<AudioSource>();
                s.loop = false;
                s.playOnAwake = false;
                s.volume = 0.7f;
                _sfx.Add(s);
            }
            foreach (var c in Resources.LoadAll<AudioClip>("Audio")) _clips[c.name] = c;
        }

        public void PlaySfx(string name, float volume = 1f, float pitch = 1f)
        {
            if (!_clips.TryGetValue(name, out var clip)) return;
            AudioSource s = null;
            foreach (var src in _sfx) if (!src.isPlaying) { s = src; break; }
            if (s == null) s = _sfx[Random.Range(0, _sfx.Count)];
            s.pitch = pitch;
            s.volume = 0.7f * volume;
            s.clip = clip;
            s.Play();
        }

        public void PlayMusic(string name)
        {
            if (!_clips.TryGetValue(name, out var clip)) { _music.Stop(); return; }
            if (_music.clip == clip) return;
            _music.clip = clip;
            _music.Play();
        }

        public void StopMusic() { _music.Stop(); _music.clip = null; }
    }
}

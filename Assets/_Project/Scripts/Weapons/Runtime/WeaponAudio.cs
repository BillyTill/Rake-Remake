using UnityEngine;

namespace Lake.Weapons
{
    [RequireComponent(typeof(AudioSource))]
    public class WeaponAudio : MonoBehaviour
    {
        [SerializeField] AudioClip fireSound;
        [SerializeField] AudioClip emptyClick;
        [SerializeField] AudioClip reloadSound;
        [SerializeField] AudioClip shellDropSound;
        [SerializeField] AudioClip[] hitSounds;

        AudioSource _source;

        void Awake() => _source = GetComponent<AudioSource>();

        public void PlayFire()
        {
            if (fireSound != null) _source.PlayOneShot(fireSound, 1f);
            else PlayProcedural(0.1f, 0.6f, 80f); // если нет клипа — процедурный
        }

        public void PlayEmpty() => _source.PlayOneShot(emptyClick != null ? emptyClick : GenerateClick(), 0.5f);
        public void PlayReload() => _source.PlayOneShot(reloadSound != null ? reloadSound : GenerateClick(), 0.7f);
        public void PlayShellDrop() => _source.PlayOneShot(shellDropSound != null ? shellDropSound : GenerateClick(), 0.3f);

        void PlayProcedural(float duration, float volume, float freq)
        {
            int samples = Mathf.CeilToInt(duration * 44100f);
            var clip = AudioClip.Create("procedural_fire", samples, 1, 44100, false);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / 44100f;
                float env = Mathf.Exp(-t * 30f);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * volume;
            }
            clip.SetData(data, 0);
            _source.PlayOneShot(clip);
        }

        AudioClip GenerateClick()
        {
            int samples = 2000;
            var clip = AudioClip.Create("click", samples, 1, 44100, false);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / 44100f;
                float env = Mathf.Exp(-t * 200f);
                data[i] = (Random.value * 2f - 1f) * env * 0.3f;
            }
            clip.SetData(data, 0);
            return clip;
        }
    }
}
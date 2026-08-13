using Lingyan.Core.Audio;
using UnityEngine;

namespace Lingyan.Game.Services
{
    /// <summary>
    /// 音频：程序化拨弦 UI 音（Karplus-Strong 五声音阶）+ 全局主音量。
    /// 无外置音频资产——采样由 PluckSynth 生成，AudioClip 运行时装配。
    /// </summary>
    public sealed class AudioService
    {
        private const int SampleRate = 44100;
        private const double ClickSeconds = 0.22;

        private AudioSource _source;
        private AudioClip[] _clickClips;
        private int _nextNote;

        /// <summary>在宿主对象上装配 AudioSource 并预生成五声点击音。</summary>
        public void Attach(GameObject host)
        {
            _source = host.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f; // UI 音走 2D

            _clickClips = new AudioClip[PluckSynth.PentatonicHz.Length];
            for (int i = 0; i < _clickClips.Length; i++)
            {
                float[] samples = PluckSynth.Pluck(
                    PluckSynth.PentatonicHz[i], ClickSeconds, SampleRate, seed: 17 + i);
                var clip = AudioClip.Create(
                    "pluck_" + i, samples.Length, 1, SampleRate, false);
                clip.SetData(samples, 0);
                _clickClips[i] = clip;
            }
        }

        /// <summary>主音量（0–1），落到 AudioListener 全局生效。</summary>
        public void ApplyVolume(float volume01)
        {
            AudioListener.volume = Mathf.Clamp01(volume01);
        }

        /// <summary>UI 点击音：沿五声音阶轮转，连点成旋律而非单调哒哒。</summary>
        public void PlayClick()
        {
            if (_source == null || _clickClips == null) { return; }
            _source.PlayOneShot(_clickClips[_nextNote], 0.9f);
            _nextNote = (_nextNote + 1) % _clickClips.Length;
        }
    }
}

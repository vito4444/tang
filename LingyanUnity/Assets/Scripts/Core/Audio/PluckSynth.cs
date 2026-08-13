using System;

namespace Lingyan.Core.Audio
{
    /// <summary>
    /// Karplus-Strong 拨弦合成：噪声激励 + 均值滤波延迟线，出弦乐拨奏质感。
    /// 全程纯 C#、给定参数下逐样本确定，方便契约测试与跨端一致。
    /// UI 点击音走五声音阶（宫商角徵羽），配水墨界面的东方气质。
    /// </summary>
    public static class PluckSynth
    {
        /// <summary>A 音下方的五声音阶频率（宫 C4 起）：C4 D4 E4 G4 A4。</summary>
        public static readonly double[] PentatonicHz =
            { 261.63, 293.66, 329.63, 392.00, 440.00 };

        /// <summary>峰值规格化目标（留头顶空间，混音时不削波）。</summary>
        public const float PeakTarget = 0.6f;

        /// <summary>
        /// 合成一段拨弦采样（单声道，float PCM，幅值 ≤ PeakTarget）。
        /// </summary>
        /// <param name="frequency">基频 Hz（20–8000）。</param>
        /// <param name="seconds">时长秒（0.05–5）。</param>
        /// <param name="sampleRate">采样率（8000–96000）。</param>
        /// <param name="seed">噪声种子；同参同种子输出逐样本一致。</param>
        public static float[] Pluck(double frequency, double seconds, int sampleRate, int seed)
        {
            if (frequency < 20 || frequency > 8000)
            {
                throw new ArgumentOutOfRangeException(nameof(frequency));
            }
            if (seconds < 0.05 || seconds > 5)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds));
            }
            if (sampleRate < 8000 || sampleRate > 96000)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleRate));
            }

            int totalSamples = (int)Math.Round(seconds * sampleRate);
            int delayLength = Math.Max(2, (int)Math.Round(sampleRate / frequency));

            var rng = new Random(seed);
            var delayLine = new float[delayLength];
            for (int i = 0; i < delayLength; i++)
            {
                delayLine[i] = (float)(rng.NextDouble() * 2.0 - 1.0);
            }

            // 衰减系数：延迟线越短（音越高）每秒过环次数越多，用固定系数听感尚可。
            const float decay = 0.996f;

            var samples = new float[totalSamples];
            int cursor = 0;
            float peak = 0f;
            for (int i = 0; i < totalSamples; i++)
            {
                float current = delayLine[cursor];
                int nextIndex = (cursor + 1) % delayLength;
                delayLine[cursor] = decay * 0.5f * (current + delayLine[nextIndex]);
                cursor = nextIndex;
                samples[i] = current;
                float magnitude = Math.Abs(current);
                if (magnitude > peak) { peak = magnitude; }
            }

            if (peak > 0f)
            {
                float gain = PeakTarget / peak;
                for (int i = 0; i < totalSamples; i++)
                {
                    samples[i] *= gain;
                }
            }

            // 尾部 10ms 线性淡出，防止截断爆音。
            int fadeSamples = Math.Min(totalSamples, sampleRate / 100);
            for (int i = 0; i < fadeSamples; i++)
            {
                int index = totalSamples - 1 - i;
                samples[index] *= i / (float)fadeSamples;
            }

            return samples;
        }
    }
}

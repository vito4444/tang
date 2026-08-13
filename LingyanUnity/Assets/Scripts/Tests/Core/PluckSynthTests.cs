using System;
using Lingyan.Core.Audio;
using NUnit.Framework;

namespace Lingyan.Core.Tests
{
    [TestFixture]
    public class PluckSynthTests
    {
        [Test]
        public void Length_MatchesDurationTimesRate()
        {
            float[] samples = PluckSynth.Pluck(440.0, 0.25, 44100, seed: 7);
            Assert.That(samples.Length, Is.EqualTo((int)Math.Round(0.25 * 44100)));
        }

        [Test]
        public void Deterministic_SameArgsSameSamples()
        {
            float[] a = PluckSynth.Pluck(392.0, 0.2, 44100, seed: 42);
            float[] b = PluckSynth.Pluck(392.0, 0.2, 44100, seed: 42);
            Assert.That(a, Is.EqualTo(b), "同参同种子必须逐样本一致");
        }

        [Test]
        public void DifferentSeeds_DifferentNoise()
        {
            float[] a = PluckSynth.Pluck(392.0, 0.2, 44100, seed: 1);
            float[] b = PluckSynth.Pluck(392.0, 0.2, 44100, seed: 2);
            Assert.That(a, Is.Not.EqualTo(b));
        }

        [Test]
        public void Peak_NeverExceedsTarget()
        {
            float[] samples = PluckSynth.Pluck(261.63, 0.5, 44100, seed: 9);
            float peak = 0f;
            foreach (float s in samples)
            {
                peak = Math.Max(peak, Math.Abs(s));
            }
            Assert.That(peak, Is.LessThanOrEqualTo(PluckSynth.PeakTarget + 1e-4f));
            Assert.That(peak, Is.GreaterThan(0.1f), "不该整段近无声");
        }

        [Test]
        public void Energy_DecaysLikeAPluck()
        {
            float[] samples = PluckSynth.Pluck(440.0, 1.0, 44100, seed: 3);
            int tenth = samples.Length / 10;
            double headRms = Rms(samples, 0, tenth);
            double tailRms = Rms(samples, samples.Length - tenth, tenth);
            Assert.That(tailRms, Is.LessThan(headRms * 0.5),
                "拨弦必须衰减：尾段能量应远低于起振段");
        }

        [Test]
        public void TailFadesToSilence_NoClick()
        {
            float[] samples = PluckSynth.Pluck(329.63, 0.3, 44100, seed: 5);
            Assert.That(samples[samples.Length - 1], Is.EqualTo(0f).Within(1e-6f));
        }

        [Test]
        public void PentatonicScale_FiveNotesAscending()
        {
            Assert.That(PluckSynth.PentatonicHz.Length, Is.EqualTo(5));
            for (int i = 1; i < PluckSynth.PentatonicHz.Length; i++)
            {
                Assert.That(PluckSynth.PentatonicHz[i],
                    Is.GreaterThan(PluckSynth.PentatonicHz[i - 1]));
            }
        }

        [Test]
        public void OutOfRangeArgs_ThrowLoudly()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => PluckSynth.Pluck(5.0, 0.2, 44100, 0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => PluckSynth.Pluck(440.0, 0.001, 44100, 0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => PluckSynth.Pluck(440.0, 0.2, 100, 0));
        }

        private static double Rms(float[] samples, int start, int count)
        {
            double sum = 0;
            for (int i = start; i < start + count; i++)
            {
                sum += samples[i] * (double)samples[i];
            }
            return Math.Sqrt(sum / count);
        }
    }
}

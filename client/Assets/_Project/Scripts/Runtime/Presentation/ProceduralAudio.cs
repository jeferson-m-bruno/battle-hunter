using System;
using System.Collections.Generic;
using BattleHunter.Core.State.Events;
using UnityEngine;

namespace BattleHunter.Client.Presentation
{
    /// <summary>Som básico gerado em código (dado, ataque, baú, vitória, dano); troca por arquivos de áudio quando houver.</summary>
    public sealed class ProceduralAudio : MonoBehaviour
    {
        private const int Rate = 22050;
        private static readonly Dictionary<string, AudioClip> Clips = new();
        private AudioSource _source;

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.volume = 0.6f;
        }

        public void Play(GameEvent e)
        {
            switch (e)
            {
                case DiceRolled: PlayClip("dice", Dice); break;
                case AttackResolved a when !a.Dodged: PlayClip("hit", Hit); break;
                case AttackResolved: PlayClip("miss", Miss); break;
                case ChestOpened: PlayClip("chest", Chest); break;
                case GoldFound: PlayClip("gold", Gold); break;
                case HunterMarked: PlayClip("treasure", Treasure); break;
                case GameEnded g when g.Reason == Core.State.GameEndReason.TreasureExtracted: PlayClip("win", Win); break;
                case GameEnded: PlayClip("lose", Lose); break;
                case HunterFell: PlayClip("fall", Lose); break;
            }
        }

        private void PlayClip(string key, Func<float[]> generate)
        {
            if (!Clips.TryGetValue(key, out var clip) || clip == null)
            {
                var samples = generate();
                clip = AudioClip.Create(key, samples.Length, 1, Rate, false);
                clip.SetData(samples, 0);
                Clips[key] = clip;
            }

            _source.PlayOneShot(clip);
        }

        private static float[] Tone(float seconds, Func<float, float> wave)
        {
            var n = (int)(Rate * seconds);
            var data = new float[n];
            for (var i = 0; i < n; i++)
            {
                var t = i / (float)Rate;
                var env = 1f - i / (float)n;
                data[i] = Mathf.Clamp(wave(t) * env, -1f, 1f);
            }
            return data;
        }

        private static float Square(float t, float f) => Mathf.Sign(Mathf.Sin(2 * Mathf.PI * f * t)) * 0.3f;
        private static float Sine(float t, float f) => Mathf.Sin(2 * Mathf.PI * f * t) * 0.5f;

        private static float[] Dice() => Tone(0.25f, t => (UnityEngine.Random.value * 2f - 1f) * 0.25f * (Mathf.Sin(t * 60f) > 0 ? 1 : 0.3f));
        private static float[] Hit() => Tone(0.15f, t => Square(t, 110f - t * 300f));
        private static float[] Miss() => Tone(0.12f, t => Sine(t, 600f + t * 2000f) * 0.4f);
        private static float[] Chest() => Tone(0.35f, t => Sine(t, t < 0.12f ? 440f : t < 0.24f ? 554f : 659f));
        private static float[] Gold() => Tone(0.3f, t => Sine(t, 880f) + Sine(t, 1320f) * 0.5f);
        private static float[] Treasure() => Tone(0.6f, t => Sine(t, 523f) + Sine(t, 659f) + Sine(t, 784f));
        private static float[] Win() => Tone(0.9f, t => Sine(t, t < 0.3f ? 523f : t < 0.6f ? 659f : 784f) + Sine(t, 1046f) * 0.3f);
        private static float[] Lose() => Tone(0.6f, t => Square(t, 220f - t * 150f) * 0.6f);
    }
}

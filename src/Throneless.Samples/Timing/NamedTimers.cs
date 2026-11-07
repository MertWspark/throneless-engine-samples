using System;
using System.Collections.Generic;

namespace Throneless.Samples.Timing
{
    /// <summary>
    /// Adlandırılmış 0..1 sayaçlar.
    ///
    /// Oyunda pek çok şey aslında "bir değerin 0'dan 1'e gitmesi": zırhın
    /// kuşanılması, bir efektin solması, bir kapının açılması, uçuş pozuna
    /// geçiş. Her biri için ayrı zamanlayıcı kodu yazmak yerine hepsi bu tek
    /// sistemi kullanıyor.
    ///
    /// Her sayaç bir koşula bağlanabilir. Koşul doğruyken sayaç verilen sürede
    /// 0'dan 1'e çıkar, yanlışken aynı sürede geri iner. Böylece aynı sayaç
    /// hem açılışı hem kapanışı verir. Görsel taraf (animasyon, shader) sadece
    /// değeri okur, kendi durumunu tutmaz.
    ///
    /// Unity'de bir MonoBehaviour her karede Tick(Time.deltaTime) çağırır.
    /// Burada süre dışarıdan verildiği için Unity olmadan test edilebiliyor.
    /// </summary>
    public sealed class NamedTimers
    {
        sealed class Timer
        {
            public float Value;
            public float Duration;
            public Func<bool> Condition;
            public bool ManualTarget;
            public bool OneShot;
            public int Completions;
        }

        readonly Dictionary<string, Timer> _timers = new Dictionary<string, Timer>();
        readonly List<string> _toRemove = new List<string>();

        public int Count => _timers.Count;

        /// <summary>Koşula bağlı bir sayaç kurar ya da var olanı günceller.</summary>
        public void Bind(string name, float duration, Func<bool> condition)
        {
            var t = GetOrCreate(name, duration);
            t.Condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        /// <summary>Elle sürülen sayacı açar (değer 1'e doğru çıkmaya başlar).</summary>
        /// <param name="oneShot">1'e ulaşınca kendiliğinden kapanıp 0'a inince silinsin mi.</param>
        public void Open(string name, float duration, bool oneShot = false)
        {
            var t = GetOrCreate(name, duration);
            t.Condition = null;
            t.ManualTarget = true;
            t.OneShot = oneShot;
        }

        /// <summary>Elle sürülen sayacı kapatır (değer 0'a doğru inmeye başlar).</summary>
        public void Close(string name)
        {
            if (_timers.TryGetValue(name, out var t)) t.ManualTarget = false;
        }

        /// <summary>Sayacın değeri (0..1). Sayaç yoksa 0.</summary>
        public float Value(string name) => _timers.TryGetValue(name, out var t) ? t.Value : 0f;

        /// <summary>
        /// Yumuşatılmış değer (smoothstep). Doğrusal değer animasyonda mekanik
        /// görünür; başta ve sonda yavaşlayan eğri daha doğal duruyor.
        /// </summary>
        public float Smoothed(string name)
        {
            float x = Value(name);
            return x * x * (3f - 2f * x);
        }

        /// <summary>Sayaç kaç kez 1'e ulaştı.</summary>
        public int Completions(string name) => _timers.TryGetValue(name, out var t) ? t.Completions : 0;

        public bool Exists(string name) => _timers.ContainsKey(name);

        /// <summary>Bütün sayaçları ilerletir. Her karede bir kez çağrılır.</summary>
        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f) return;

            foreach (var pair in _timers)
            {
                Timer t = pair.Value;
                bool target = t.Condition != null ? t.Condition() : t.ManualTarget;
                float step = deltaTime / t.Duration;
                float previous = t.Value;

                t.Value = Clamp01(t.Value + (target ? step : -step));

                if (previous < 1f && t.Value >= 1f)
                {
                    t.Completions++;
                    if (t.OneShot) t.ManualTarget = false;
                }

                if (t.OneShot && t.Completions > 0 && t.Value <= 0f && !target)
                    _toRemove.Add(pair.Key);
            }

            foreach (var name in _toRemove) _timers.Remove(name);
            _toRemove.Clear();
        }

        Timer GetOrCreate(string name, float duration)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("İsim boş olamaz.", nameof(name));
            if (duration <= 0f) throw new ArgumentOutOfRangeException(nameof(duration), "Süre pozitif olmalı.");

            if (!_timers.TryGetValue(name, out var t))
            {
                t = new Timer();
                _timers[name] = t;
            }
            t.Duration = duration;
            return t;
        }

        static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}

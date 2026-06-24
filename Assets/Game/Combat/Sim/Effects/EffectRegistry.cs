using System;
using System.Collections.Generic;

namespace MyTurnBase.Combat.Sim
{
    // EffectKey → IEffect 매핑. 키 조회만(결정론 — 열거 순서 비의존).
    // 새 효과 = Register로 등록 → resolver/비트러너 무수정으로 동작(#17 확장성 핵심).
    // 기본 4효과는 static 생성자에서 등록.
    internal static class EffectRegistry
    {
        private static readonly Dictionary<string, IEffect> _byKey = new Dictionary<string, IEffect>();

        static EffectRegistry()
        {
            Register(EffectKeys.Move, new MoveEffect());
            Register(EffectKeys.Damage, new DamageEffect());
            Register(EffectKeys.Guard, new GuardEffect());
            Register(EffectKeys.Charge, new ChargeEffect());
        }

        public static void Register(string key, IEffect effect)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("EffectKey 비어있음", nameof(key));
            _byKey[key] = effect ?? throw new ArgumentNullException(nameof(effect));
        }

        public static bool IsRegistered(string key) => key != null && _byKey.ContainsKey(key);

        public static IEffect Resolve(string key)
        {
            if (key != null && _byKey.TryGetValue(key, out var e)) return e;
            throw new InvalidOperationException($"미등록 EffectKey: '{key}'");
        }
    }
}

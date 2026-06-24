namespace MyTurnBase.Combat.Sim
{
    // 기본 효과 식별 문자열(데이터 ↔ 코드 다리). CardData.Effects[].EffectKey와 매칭.
    // 캐릭터 고유 효과(#38 등)는 자체 키를 EffectRegistry.Register로 추가.
    internal static class EffectKeys
    {
        public const string Move = "move";
        public const string Damage = "damage";
        public const string Guard = "guard";
        public const string Charge = "charge";
    }
}

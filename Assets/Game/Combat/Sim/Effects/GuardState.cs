namespace MyTurnBase.Combat.Sim
{
    // 슬롯 한정 가드 상태(PLACEHOLDER 값 — 완전/기본 구분·경감 수치 = E3).
    // GuardEffect가 ctx.Guards에 등록 → 같은 슬롯 공격 비트(DamageEffect)가 읽는다.
    internal readonly struct GuardState
    {
        public readonly bool Full;
        public readonly float Reduction;
        public GuardState(bool full, float reduction) { Full = full; Reduction = reduction; }
    }
}

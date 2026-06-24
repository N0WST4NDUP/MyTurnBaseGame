namespace MyTurnBase.Combat.Sim
{
    // 카드 효과 컴포넌트(#17, E2). effects[]의 실행 단위.
    // 무상태(stateless) — 모든 상태/입출력은 EffectContext 경유. 레지스트리가 인스턴스를 싱글톤 공유한다.
    internal interface IEffect
    {
        Phase Beat { get; }            // 이 효과가 도는 비트(이동/가드/공격/충전) → 실행 라우팅 소스
        void Apply(EffectContext ctx); // 상태 변경 + 이벤트 emit
    }
}

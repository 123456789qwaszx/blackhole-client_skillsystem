# Combat core sandbox

스킬 수치는 [`Config/SkillCatalog.asset`](../Assets/BlackHole/SkillSystem/Config/SkillCatalog.asset)의
`Skills` 목록에 저장한다. 각 항목은 `Type`과 해당 스킬의 `Stats`를 담는다.
샘플 씬의 `GameHost`가 이 SO 에셋을 참조한다.

| Type | 필요한 수치 | 의미 |
| --- | --- | --- |
| Breaker | Damage, Interval, Radius | 피해, 반복 간격(초), 조준 반경 |
| PiercingLaser | Damage, Interval, Width, TelegraphDuration | 피해, 반복 간격(초), 폭, 예고 길이(초) |

SO의 `Load()`는 목록을 순수 C# `SkillCatalog.Load()`에 넘긴다.
카탈로그는 종류별 수치를 검사하고 복사해 `SkillType → SkillStats`로 보관한다.
사용하는 수치는 유한한 양수여야 하고, 사용하지 않는 수치는 0이어야 한다.
중복 종류나 알 수 없는 종류는 진단과 함께 거부한다. 코어는 순수 C#이다.

테스트 전투는 정의된 스킬을 모두 독립 실행 상태로 만들고 체크박스에서
스킬별 발동을 켜고 끈다. 끄면 공격 타이머와 진행 중인 레이저 예고를 버리고,
다시 켜면 처음부터 실행한다. 각 전투/플레이어는 실행 상태를 공유하지 않는다.
`TestPack`의 적은 HP를 기록하며 위치는 스킬의 적중 판정에 사용한다.

## Enemy Death Effect

공통 전투 수치와 두 처치 버프의 다음 구현 기준은
[스킬 공통 전투 수치·처치 버프 기획 명세](COMBAT_STATS_AND_BUFFS_DESIGN.md)를 참고한다.

[`Config/EnemyCatalog.asset`](../Assets/BlackHole/SkillSystem/Config/EnemyCatalog.asset)에서
적 ID·HP·사망 효과를 저작한다. 코어의 `EnemyCatalog.Load`는 ID 중복, 잘못된 HP·효과 수치를
전투 전에 검사하고 정의를 복사한다. `DeathEffectType.None`은 효과가 없는 일반 적이다.

| 효과 | 필요한 수치 | 처리 |
| --- | --- | --- |
| ChainLightning | Damage, Radius, MaxTargets | 사망 위치에서 가까운 일반 적으로 최대 MaxTargets번 이동. 대상 재방문 금지, 각 이동 거리 ≤ Radius |
| Explosion | Damage, Radius | 사망 위치를 중심으로 Radius 안의 살아 있는 일반 적 전부 1회 피해 |
| AttackHaste | Duration, IntervalMultiplier | 처치한 플레이어의 공격 타이머 진행 속도를 일시적으로 높임. 0.5면 주기가 절반 |
| GuaranteedCritical | Duration, CriticalMultiplier | 처치한 플레이어의 스킬 피해를 일시적으로 배율 적용. 샘플 2배 |

효과 피해는 종류에 관계없이 **다른 사망 효과 보유 적을 대상에서 제외**한다.
따라서 효과 피해가 다른 효과를 재귀적으로 발동시키지 않는다. 직접 공격으로 죽은
효과 보유 적의 효과는 같은 전투 Step의 패시브 공격 다음에 한 번만 처리한다.
피해로 죽은 일반 적 역시 죽은 상태가 되어 재피해를 받지 않는다.

버프는 전투마다 플레이어 ID별로 관리한다. 같은 종류를 다시 얻으면 남은 시간을 갱신하고
더 강한 배율을 유지하며 배율을 중첩하지 않는다. 버프를 준 처치와 같은 Step의 공격에는
적용하지 않고 다음 Step부터 적용한다. 사망 효과로 인한 피해는 플레이어의 치명타 버프를
사용하지 않는다. 전투가 끝나면 실행 상태도 폐기한다.

`DeathEffects.LastActivations`와 `LastHits`는 그 Step의 표현용 기록이며 매 Step 갱신된다.
화면은 이를 읽어 연출할 수 있고, 연출은 피해를 다시 계산하지 않는다.

이 브랜치에는 노드 ID, 구매, 골드, 레벨, 업그레이드 효과, 트리 화면 정보가 없다.
업그레이드를 개발할 때는 별도 브랜치에서 게임 규칙을 연결한다.

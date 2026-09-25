# Skill System — 수치 목록과 노드 그래프 계약

2026-09-25 · blackhole-client_skillsystem / dev

## 목적과 이번 완료 범위

대표 콘텐츠(Breaker, 관통 Laser, 연쇄 번개, Gold 보상)를 실행하면서 후속 콘텐츠와 도구가 공유할 경계를 확정한다. 전체 Skill 목록의 확정을 선행 조건으로 요구하지 않는다.

이번 구현: ST-001/002의 경계 결정, ST-003 수치 주소·목록·Loadout, ST-004 무방향 그래프·구매·검증, 표시 모델의 계약. UI 에디터, 세 번째·네 번째 Skill, 폭발·일시 버프 실행은 다음 단계다. 기존 전투·사망 효과·보상 동작은 유지한다.

## 결정

사용자가 확정한 규칙:
- 전기·폭발·처치 버프·공격 주기 감소는 천체의 사망 효과다.
- 일시 버프는 사망 효과가 Player에게 부여한다. 플레이어의 자동 공격 스킬과 별개다.
- 노드는 수치 주소로 Skill을 보정한다.
- 구매는 전투 밖에서만 가능하고 다음 전투 조립에 적용한다.
- 먼저 수치 목록과 그래프 모델을 완성하고 콘텐츠와 도구를 나란히 개발한다.

이번 구현 선택(원작의 확정 사실을 주장하지 않음):
- 연결은 무방향이고 시작 노드 또는 구매한 이웃 하나가 있으면 공개된다. 순환을 허용한다.
- 노드는 한 번만 구매한다. 가격은 기존 양의 int다.
- 수치 합성: `(기본 + Add 합계) × (1 + Rate 합계)`. Rate 0.25는 +25%다.
- 공격 속도 기본값은 1. `실행 주기 = 정의의 기본 주기 / 최종 AttackSpeed`.
- AttackSpeed는 0.01~100, 예고 시간은 0.01~60초. 이는 개발 상한이며 밸런스 확정값이 아니다.
- 노드 순서·구매 순서는 수치 결과와 스킬 실행 순서에 영향을 주지 않는다. 수치 합산 순서도 정렬한다.
- 스킬 실행 순서는 `GameContent.Skills`의 순서다.
- 시작 Skill의 해금, 중복 해금, 해금을 우회한 강화 구매 경로를 거부한다.
- 해금과 강화가 한 노드에 함께 있는 것은 허용한다.

## 책임

| 계층 | 소유 | 의존하지 않는 것 |
|---|---|---|
| StatCatalog / StatDefinition | 수치 ID, 표시명, 단위, 타입, 범위, 허용 연산, 합성 | 노드·구매·Unity |
| PassiveSkillDefinition | 종류별 기본값·수치 목록·런타임 생성 | 가격·연결·획득 이유 |
| PassiveSkill | Player별 타이머·예고·공격 | 그래프·구매 기록 |
| UpgradeGraph / UpgradePurchase | 연결·공개·구매 판정 | Skill 실행 방식 |
| PlayerLoadout | 구매 기록으로부터 얻은 Skill 목록과 대상별 보정 스냅샷 | 타이머·예고·연출 |
| UpgradeLayout | NodeId별 좌표·표시·리소스 키 | 구매 조건 |
| DeathEffects | 천체 사망에서 시작하는 효과와 연쇄 종료 | 노드·가격 |

`UpgradeModifiers`는 기존 Enemy 수치·보상·공급 적용만 담당한다. Skill 수치 구조체를 만들거나 Skill 종류로 분기하지 않는다.

## 수치 주소와 목록

### gameplay.json의 스킬 경계

스킬 코어는 역직렬화된 `ContentData.Skills`와 `ContentData.StartingSkills`를
`SkillContentLoader.Load(skills, startingSkills)`로 독립 검증한다. 결과가 성공하면
`SkillContent`에서 ID로 정의를 조회하고 종류별 `Stats.Entries`, `BaseValues`를 읽는다.
실패하면 `Content`는 null이고 `Diagnostics`에 `Skills[...]` 또는
`StartingSkills[...]` 경로가 담긴다. 전체 게임 로더도 동일한 스킬 로더를 사용한다.

도구가 내보내는 `gameplay.json`에서는 다음 필드가 이 경계에 해당한다
(아래는 전체 게임 콘텐츠 파일이 아니라 스킬 부분 예시다).

```json
{
  "Skills": [
    { "Id": "breaker", "Kind": "Breaker", "Radius": 2, "Interval": 1, "Damage": 3 },
    { "Id": "laser", "Kind": "PiercingLaser", "Interval": 5, "Damage": 10,
      "Width": 0.5, "TelegraphDuration": 0.4, "BoundaryRadius": 12 }
  ],
  "StartingSkills": ["breaker"]
}
```

이 단계에서 코어는 JSON 파일을 직접 읽지 않는다. 파일 읽기/역직렬화는
Unity 호스트 또는 저작 도구가 담당하며, 실제 `gameplay.json`을 전투에 연결하는
작업은 별도다. `UpgradeLayout`과 화면용 노드 데이터는 스킬 로더의 입력이 아니다.

`StatAddress = SkillId + StatId`. 반사나 C# 프로퍼티 경로를 사용하지 않는다.

| 종류 | 수치 ID | 의미 | 연산 |
|---|---|---|---|
| Breaker | Damage | 피해 | Add, Rate |
| Breaker | Radius | Aim Point 중심 반경 | Add, Rate |
| Breaker | AttackSpeed | 기본 주기에 대한 속도 비율 | Add, Rate |
| PiercingLaser | Damage | 발사 피해 | Add, Rate |
| PiercingLaser | Width | 판정·표시 폭 | Add, Rate |
| PiercingLaser | AttackSpeed | 예고 시작 간격의 속도 비율 | Add, Rate |
| PiercingLaser | TelegraphDuration | 예고 후 발사까지의 초 | Add, Rate |

레이저 BoundaryRadius는 전투 공간 설정이라 보정 목록에 넣지 않는다.
모든 수치는 유한해야 한다. 정수형 수치는 합성 결과도 정수여야 한다. 범위 밖을 조용히 보정하지 않고 오류로 반환한다. 기본 정의는 수정하지 않는다.

예시 저작 데이터:

```json
{
  "Id": "laser-width",
  "Price": 20,
  "IsStart": false,
  "Connections": ["laser-unlock"],
  "Effects": [
    { "Kind": "SkillStat", "Target": "sample-laser", "Stat": "Width", "Operation": "Add", "Value": 0.2 }
  ]
}
```

해금: `{ "Kind": "SkillUnlock", "Target": "sample-laser" }`. 해금에 Stat/Operation을 적거나 0 이외의 Value를 주면 오류다. 현재 DTO에서 생략한 float 값은 0으로 표현된다.

## 그래프와 표시 데이터

규칙 DTO: `UpgradeData(Id, Price, IsStart, Connections, Effects)`.
Connections는 연결선의 저장 목록이다. 한 무방향 선은 한쪽 노드에서 한 번만 기록한다. 역방향 중복도 오류다. 런타임에서는 UpgradeGraph.Edges와 StartingNodes로 읽는다. 연결선이 구매 조건이며 별도의 숨겨진 선행 그래프를 두지 않는다.

검증: 중복 ID, 없는 연결 대상, 자기 연결, 중복 선, 시작점 없는 컴포넌트, 해금 우회. 해금 우회는 해금 노드를 제외한 그래프에서 강화 노드까지 도달할 수 있는지 검사한다.

구매 UI는 `UpgradePurchase.Check/TryPurchase(state, content, nodeId)`와 `GetState`만 사용한다. 정의 객체만 넘겨 그래프 판정을 우회하는 구매 오버로드는 제거했다.

상태: Hidden / Revealed / Purchasable / Owned. 전투 중 공개된 미보유 노드는 Revealed이고 구매는 InBattle로 실패한다. 실패 시 Gold와 구매 기록은 유지된다.

표시 DTO: `UpgradeLayout(Version=1, Nodes)`와 `UpgradeNodeDisplay(NodeId, X, Y, Name, Description, Icon, Group)`. 좌표는 격자 정수다. Validate는 누락·없는 ID·중복 ID·겹친 좌표·지원하지 않는 버전을 검사한다. 구매·전투는 표시 데이터를 읽지 않는다.

파일 교환 형식은 JSON 필드 이름을 위 DTO와 일치시킨다. 실제 파일 IO·마이그레이션·저장/불러오기 UI는 ST-008에서 구현한다. 규칙과 표시 파일은 NodeId로 연결한다.

## 확장 순서

새 Skill은 자기 정의·기본값·수치 목록·실행 클래스와 로더 종류 연결을 추가한다. 그래프·구매·Loadout·HUD에 수치별 분기를 더하지 않는다. 테스트의 PulseDefinition이 이 경계를 검증한다.

다음 두 작업은 같은 계약으로 나란히 진행할 수 있다:
- 콘텐츠: 새로운 Skill, 사망 효과 ID와 수치 목록, 폭발, Player별 일시 버프. 버프는 StatModifier를 사용하되 생성·만료 때 재계산한다. 확정 치명타는 피해 판정 규칙으로 연결한다. 재획득·중첩·타이머 반영 정책은 해당 구현 때 결정한다.
- 도구: JSON IO, 카탈로그 기반 속성 편집, 그래프 캔버스, 공유 검증 호출, 구매/수치 미리보기. 이후 대표 트리를 저작해 실제 불편을 기록하고 다음 기능을 선택한다.

4인 준비: Player별 구매·Loadout·스킬 상태를 분리한다. 피해 출처는 기존 Damage.Source를 유지한다. 보상/공유 월드 보정의 다인 귀속 정책은 기존 제한을 유지하며 네트워크 지원 완료로 간주하지 않는다.

## 마이그레이션

| 기존 | 현재 |
|---|---|
| SkillDamageAdd / LaserDamageAdd | SkillStat, Damage, Add |
| SkillRadiusAdd | SkillStat, Radius, Add |
| LaserWidthAdd | SkillStat, Width, Add |
| SkillIntervalMultiply / LaserIntervalMultiply × m | SkillStat, AttackSpeed, Rate = 1/m - 1 |
| Requires 없음 | IsStart=true |
| Requires=parent | Connections=[parent] |

여러 주기 배율의 합성은 의도적으로 달라진다. 이전 배율끼리 곱하던 방식은 유지하지 않는다. 각 노드를 속도 증가율로 재저작한다. 이전 종류 문자열은 로더에서 오류로 보고한다.

## 검증

`dotnet run --project tests/CoreSmoke/CoreSmoke.csproj --configuration Release`

기존 공격·레이저·사망·성장·구매 계약을 새 데이터 형식으로 이전했다. 추가 계약은 합산 순서 불변, 잘못된 주소·연산·정수·범위, 순환·OR 구매, 해금 우회, 표시 분리, 4인 수치 분리, 새 Skill 추가 경계를 확인한다. Unity PlayMode의 시각 확인은 별도다.

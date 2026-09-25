# Skill System — gameplay.json 계약

## 남긴 범위

- 패시브 Skill: Breaker(마우스 주변 원형 공격), PiercingLaser(예고 후 직선 관통).
- `NodeId`로 스킬 레벨을 한 단계 올리는 전투 밖 업그레이드.
- 스킬이 실제로 적의 HP를 감소시키는 최소 TestPack.

적 이동, 천체의 사망 효과, 보상, 성장, 세션 종료, 골드, 가격, 스킬 트리의
연결 규칙과 화면 배치 정보는 여기서 다루지 않는다. NodeId와 화면상의 위치를
연결할 `upgrade-layout.json` 및 트리 UI는 별도 작업이다.

## 데이터 흐름

```text
Resources/gameplay.json
→ GameplayData (Unity JsonUtility 등 호스트가 역직렬화)
→ SkillCatalog.Load: 유효성 검증 + Dictionary<SkillType, UpgradeStat[]>
→ SkillProgress.Purchase(nodeId): 다음 레벨 구매
→ SkillProgress.Snapshot(): 전투 시작 시 레벨별 완성 수치
→ BreakerRuntime / LaserRuntime → ISkillTarget.Hit
```

코어는 파일 IO나 Unity API를 사용하지 않는다. `SkillCatalog.Load`에는 역직렬화된
`GameplayData`를 전달한다. 샘플 `GameHost`만 Resources에서 파일을 읽는다.

`Skills[].Levels[0]`은 Lv1, `[1]`은 Lv2다. 이 안의 `UpgradeStat`은
증가량이 아닌 **완성된 수치**다. 스킬 종류별로 필요한 필드는 다음과 같다.

| Type | 필수 수치 | 뜻 |
| --- | --- | --- |
| Breaker | Damage, Interval, Radius | 피해, 공격 간격(초), 조준 반경 |
| PiercingLaser | Damage, Interval, Width, TelegraphDuration | 피해, 예고 시작 간격(초), 폭, 예고 시간(초) |

사용하지 않는 수치는 0이어야 한다. 모든 사용 수치는 유한한 양수이며,
중복 스킬·NodeId·동일 스킬 레벨 노드와 정의되지 않은 참조를 거부한다.
`Version`은 1이다. 파일의 실제 예시는
[`gameplay.json`](../Assets/BlackHole/SkillSystem/Resources/gameplay.json)에 있다.

`StartingSkills`의 스킬은 Lv1로 시작한다. 그 밖에는 Lv0(미획득)이다.
`Upgrades[]`는 `Id`, `Skill`, `Level`을 지정한다. `Purchase(nodeId)`는
해당 노드의 `Level`이 현재 레벨보다 정확히 하나 높을 때만 구매한다.
따라서 순서대로 노드를 눌러야 하지만 연결선·가격 조건은 없다.
전투 중에는 구매할 수 없고 전투는 시작 시 찍은 수치 스냅샷만 사용한다.
진행 상태는 플레이어마다 별도이며 `PurchasedNodes`를 저장할 수 있다
(실제 저장/불러오기는 아직 없다).

새 스킬 종류는 `SkillType`, 해당 종류의 필수 수치 검증, `SkillRuntime.Create`의
실행 구현을 추가한다. 레벨 추가와 수치 변경은 JSON 수정만으로 끝난다.

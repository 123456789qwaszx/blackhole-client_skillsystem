# 스킬·천체 사망 효과 인수인계

작성: 2026-09-25  
저장소: [`blackhole-client_skillsystem`](https://github.com/123456789qwaszx/blackhole-client_skillsystem)  
작업 브랜치: `skill-only-sandbox`  
이 문서 작성 직전 기준 커밋: [`12ac69d`](https://github.com/123456789qwaszx/blackhole-client_skillsystem/commit/12ac69d9a5089fb3742eb73a952d5947c42b92ec)

## 1. 목적과 현재 경계

`blackhole-client`의 큰 게임 시스템을 한 번에 가져오는 대신 **스킬 공격과 천체 사망
효과만 독립적으로 확인**하는 브랜치다. 기존의 노드·업그레이드·골드·레벨·웨이브·HQ 성장·
네트워크는 이 브랜치에 없다. UI·아트·사운드, 노드 배치 데이터와 제작 도구도 아직 없다.

- 게임에서 재사용할 규칙: 순수 C# `Core`의 스킬 수치 검증, 공격 실행, 적 정의 검증,
  사망 효과 해결, 플레이어별 한시 버프.
- 시연을 위해 붙인 것: `TestPack`의 고정 적과 전투 조립, `Unity`의 `GameHost`,
  `SampleScene`. **TestPack 자체를 완성 게임의 세션·적 AI·보상 시스템으로 간주하지 않는다.**
- 콘텐츠 입력: Unity ScriptableObject 두 개. `gameplay.json`과 Resources 로딩은 제거했다.

이 브랜치에서 가장 최근에 결정한 **다음 구현 명세**는
[전투 수치·처치 버프 기획](COMBAT_STATS_AND_BUFFS_DESIGN.md)이다. 이 명세의 일부는
현재 코드에 **아직 적용되지 않았다**. 아래 5절이 그 차이를 정리한다.

## 2. 파일·책임 지도

| 위치 | 현재 역할 |
| --- | --- |
| `Assets/BlackHole/SkillSystem/Core/SkillData.cs` | `SkillType`, 스킬별 기본 수치 데이터 |
| `Core/SkillCatalog.cs` | 스킬 목록 검증, `SkillType → SkillStats` 조회용 복사본 |
| `Core/SkillRuntime.cs` | Breaker 원형 주기 공격과 PiercingLaser 예고→관통 발사, 공격 타이머 |
| `Core/EnemyCatalog.cs` | 적 ID·HP·사망 효과 정의 검증, 조회 시 복사 |
| `Core/DeathEffects.cs` | 최초 사망별 효과 처리, 특수 천체 피해 제외, 연쇄 번개·폭발 적중 및 표현 기록 |
| `Core/PlayerBuffs.cs` | 플레이어 ID별 Haste·확정 치명타 배율의 강도·남은 시간 |
| `TestPack/EnemyTarget.cs` | HP, 위치, 마지막 유효 공격자; 스킬과 사망 효과의 최소 표적 |
| `TestPack/SkillBattle.cs` | 테스트 전투 조립, 스킬→버프 시간→사망 효과 처리, 켜기·끄기·종료 |
| `Unity/SkillCatalogAsset.cs`, `Unity/EnemyCatalogAsset.cs` | Inspector에서 수정하는 SO와 순수 코어의 연결 |
| `Unity/GameHost.cs`, `Unity/EnemyView.cs` | 시연용 적 생성·Game 뷰 콘솔·표현, 마우스 조준 |
| `Config/*.asset` | 현재 샘플 스킬·적 정의의 값 |
| `Tests/SkillContracts.cs`, `Tests/SkillEditModeTests.cs` | 순수 C# 계약 13개, Unity EditMode 진입점과 두 SO 읽기 검사 |
| `tests/CoreSmoke/` | Unity 없이 13개 코어 계약을 실행하는 .NET 8 진입점 |

`Core`는 UnityEngine을 참조하지 않는다. 게임 호스트는 정의를 SO에서 읽은 뒤
`SkillCatalog`와 `EnemyCatalog`에 전달한다. 실행 상태는 전투마다 새로 만든다.

## 3. 현재 구현된 동작

### 공격과 콘솔

| 종류 | 실행 |
| --- | --- |
| Breaker | 마우스 조준점 중심의 원형 범위에 주기 피해. 첫 Tick은 전투 시작 시점 |
| PiercingLaser | 무작위 경계 지점에서 예고를 만들고 그때의 조준 방향을 고정. 예고가 끝나면 선분 폭 안의 살아 있는 적을 관통하여 피해 |

좌측 콘솔은 스킬 이름 왼쪽 동그란 버튼으로 켜고 끈다(초록색/회색). 이름을 누르면
왼쪽 아래 별도 창에 그 스킬의 SO 수치가 보인다. 우측 콘솔은 각 적의 현재/최대 HP와
`Restart test`를 표시하며 스크롤바는 없다. 재시작은 적과 스킬 타이머를 새로 만들고
스킬 켜짐 상태는 유지한다. 버프 남은 시간은 활성화된 동안 좌측에 표시된다.

### 천체 사망 효과

| SO의 적 ID | 값 | 현재 처리 |
| --- | --- | --- |
| `normal` | HP 14 | 사망 효과 없음 |
| `electric` | HP 6, 피해 5, 거리 2.5, 최대 3회 | 사망 위치에서 가까운 일반 적으로 번개가 이동. 한 실행에서 같은 적 재적중 금지 |
| `explosive` | HP 6, 피해 7, 반경 2.5 | 사망 위치 주위의 살아 있는 일반 적 모두에게 피해 |
| `haste` | HP 6, 5초, 주기 배율 0.5 | 처치자의 스킬 공격 타이머를 빠르게 진행 |
| `critical` | HP 6, 5초, 피해 배율 2 | 처치자의 스킬 피해에 배율 적용. 현재는 치명타 확률 판정이 아님 |

효과 피해는 다른 사망 효과 보유 적에게 들어가지 않는다. 직접 스킬 공격은 그 적에게
피해를 줄 수 있다. 죽은 적은 재피해·재사망 처리되지 않고, 효과는 한 번만 해결된다.
두 버프는 처치 플레이어 ID에 귀속되며 같은 종류는 배율을 곱하지 않고 강한 값과
긴 남은 시간을 유지한다. 폭발·번개 피해에는 플레이어의 치명타 배율을 적용하지 않는다.

`DeathEffects.LastActivations`와 `LastHits`는 **마지막 Step의 기록**이다. 새 Step에서
내용이 지워지므로 완성 게임의 연출 소비자가 뒤늦게 읽으려면 이벤트 보존·시퀀스
계약이 추가로 필요하다. Game 뷰에서 폭발 원과 번개 선을 보려면 Gizmos를 켠다.

## 4. 확인 방법과 확인 상태

1. Unity에서 `Assets/Scenes/SampleScene.unity`를 열고 `GameHost`가 두 Catalog SO를
   참조하는지 본다. `Config/SkillCatalog.asset`과 `Config/EnemyCatalog.asset`을 선택하면
   수치를 Inspector에서 편집할 수 있다.
2. Play 후 조준점을 `normal`과 특수 천체 위로 옮겨 HP·효과를 확인한다.
   `electric`·`explosive`를 처치하면 일반 적 HP가 줄어야 하고, 두 버프 천체를
   처치하면 좌측에 남은 시간이 떠야 한다. 특수 천체는 다른 효과의 피해를 받지 않는다.
3. Unity Test Runner의 **EditMode**에서 `SkillEditModeTests`를 실행한다. 코어 계약과
   실제 SO 두 개의 로드를 확인한다.
4. 저장소 루트에서 `dotnet run --project tests/CoreSmoke/CoreSmoke.csproj --configuration Release`를
   실행한다. `.github/workflows/core-contracts.yml`도 `skill-only-sandbox` 푸시에 같은
   명령을 실행하도록 작성돼 있다.

| 검증 항목 | 이 인수인계 시점의 근거 |
| --- | --- |
| 13개 코어 계약 | 소스에 작성됨. 현재 작업 환경에는 .NET SDK가 없어 **실행 통과 미확인** |
| Unity EditMode 3개 테스트 진입점 | 소스에 작성됨. Unity 실행 결과 **미확인** |
| 최신 SO·씬 참조 | 에셋 GUID, `.meta`, 씬 직렬화 참조를 정적으로 점검. Unity import **미확인** |
| Game 뷰 Play | 사용자가 과거 스킬 시연 버전의 Play를 확인했으나, **SO 전환·천체 효과 추가 이후에는 재확인 필요** |
| GitHub CI | 워크플로 정의는 존재. 마지막 커밋의 완료 결과는 **확인하지 못함** |

계약이 존재한다는 사실과 통과했다는 주장을 구분한다. 첫 인수 작업은 위 순서대로
실제로 실행하고 결과를 기록하는 것이다.

## 5. 확정 기획과 현재 코드의 차이

[전투 수치·처치 버프 기획](COMBAT_STATS_AND_BUFFS_DESIGN.md)은 다음을 결정한다.

- 치명타 확률·피해 배율·공격 간격은 **플레이어 공통 전투 수치**다. 스킬 고유 수치는
  Breaker 반경, Laser 폭·예고 시간처럼 공격 형상에 속한다. 두 스킬을 묶으려고 새로운
  `IAttackSkill` 인터페이스를 만들지 않는다.
- 치명타는 **공격 실행 1회당 한 번** 판정한다. Breaker 한 Tick과 Laser 한 발의
  다중 적중은 동일 결과를 쓰며, 유효한 적중 대상이 없으면 치명타 난수를 쓰지 않는다.
- 확정 치명타 버프는 최종 확률을 100%로 만든다. 치명타 배율은 한 번만 적용한다.
  Laser의 치명타는 예고 시작 때가 아니라 발사 때 결정한다.
- Haste는 Breaker 공격 간격과 Laser **예고 시작 간격**만 줄인다. 이미 진행 중인
  예고의 방향·길이는 바꾸지 않고, 공격 타이머의 진행률을 보존한다.
- 효과 만료 시각의 Step 분할, 별도 레이저/치명타 난수 스트림, 표시용 `IsCritical`
  적중 기록, 최소 유효 공격 간격과 긴 프레임 처리 규칙이 명시돼 있다.

**아직 구현되지 않은 사항:** 공통 `CritChance`/`CritMultiplier` 원본 수치, 확률
판정과 `IsCritical` 기록, 난수 스트림 분리, 최소 간격·Step 분할·프레임 초과 시간
이월. 현재 `PlayerBuffs.DamageMultiplier()`가 버프 중 직접 피해를 곱한다. 문서의
확정 치명타 의미로 바꾸려면 이 경로를 먼저 정리해야 한다. 이 차이를 별도 기획
변경으로 취급하지 말고, 최근 확정된 명세에 맞춘 **후속 구현**으로 다룬다.

## 6. 다음 담당자의 작업 순서

1. **검증 게이트:** .NET 코어 계약, Unity EditMode, 샘플 씬 Play를 실행한다. 컴파일·SO
   로드·Game 뷰가 틀어지면 다음 기능 전에 먼저 수정한다. 실패한 계약명과 실제 결과를
   남긴다.
2. **스킬의 공통 전투 수치:** 플레이어 기본 수치의 저작 위치를 마련하고 Breaker/Laser의
   적중을 한 경로로 확정한다. 치명타는 공격당 1회 판정하고 소유자·최종 피해·치명타
   여부를 기록한다. 기존 피해·사망 1회 규칙을 유지한다.
3. **한시 버프 정합성:** Haste의 진행률·Laser 예고와 확정 치명타의 발사 시점·버프
   만료 경계·반복 획득·플레이어별 분리를 문서 10절의 예시로 검증한다. 기존 13개
   계약에 회귀가 없어야 한다.
4. **그 다음 스킬 flow:** 조준→주기/예고→공격→피해·치명타→최초 사망→천체 효과→
   플레이어 버프의 시간/소유권 경계를 설계하고, 필요한 곳만 코드로 확정한다.

노드 구매, `gameplay.json` 출력 도구, 트리 UI, 골드 강화는 현재 작업의 선행 조건이
아니다. 효과를 완성 게임에 붙일 때는 `Core`를 재사용하고 `TestPack` 대신 실제
전투·적·보상 시스템의 연결 경계를 구현한다.

## 7. 이 브랜치의 주요 변경 이력

| 커밋 | 결과 |
| --- | --- |
| [`1079646`](https://github.com/123456789qwaszx/blackhole-client_skillsystem/commit/1079646493f39176301ca9f0be09fc58d20e2f56) | 스킬 수치 JSON → SO, 샘플 씬 연결 |
| [`de0e778`](https://github.com/123456789qwaszx/blackhole-client_skillsystem/commit/de0e77845c5ec1744eb18c5df2273c15843b30d4) | 원형 켜기·끄기와 이름 클릭 시 수치 창 분리 |
| [`7af8c85`](https://github.com/123456789qwaszx/blackhole-client_skillsystem/commit/7af8c85d21ba2fb7ab899379259634238a365a90) | 콘솔 불필요 문구 제거, 우측 HP 스크롤 제거 |
| [`87c1788`](https://github.com/123456789qwaszx/blackhole-client_skillsystem/commit/87c1788308355b83a37a89cae0deff70aee2646f) | 적 SO, 번개·폭발·두 버프 코어 및 샘플 연결 |
| [`ae1766e`](https://github.com/123456789qwaszx/blackhole-client_skillsystem/commit/ae1766e94b58be1ce127166fb130ae87e6978852) | 플레이어 버프 상태를 사망 효과 처리 파일에서 분리 |
| [`12ac69d`](https://github.com/123456789qwaszx/blackhole-client_skillsystem/commit/12ac69d9a5089fb3742eb73a952d5947c42b92ec) | 레퍼런스 근거와 다음 단계 전투 수치·버프 명세 |

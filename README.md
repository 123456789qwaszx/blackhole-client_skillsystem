# BlackHole Skill Sandbox

`skill-only-sandbox` 브랜치는 스킬 공격과 천체 사망 효과를 확인합니다. 노드·업그레이드·레벨은 없습니다.

- `Assets/BlackHole/SkillSystem/Core`: Breaker, PiercingLaser, 스킬 데이터 검증
- `Assets/BlackHole/SkillSystem/TestPack`: HP가 있는 고정 적과 재현 가능한 전투 환경
- `Assets/BlackHole/SkillSystem/Config/SkillCatalog.asset`: Inspector에서 수정하는 두 스킬의 기본 수치
- `Assets/BlackHole/SkillSystem/Config/EnemyCatalog.asset`: 적 HP와 사망 효과 수치
- `Assets/BlackHole/SkillSystem/Unity`: Unity Play 확인용 콘솔과 적 표시

Unity의 `Assets/Scenes/SampleScene.unity`를 Play하고 마우스로 적을 조준합니다.
수치를 바꾸려면 `Config/SkillCatalog.asset`을 선택해 각 스킬의 `Stats`를 편집하고 Play하세요.
새 에셋은 `Assets > Create > BlackHole > Skill Catalog`에서 만들고 `GameHost`의 `Skill Catalog`에 연결할 수 있습니다.
좌측 스킬 콘솔에서 이름 왼쪽의 동그란 버튼을 눌러 공격을 켜고 끌 수 있습니다(초록색=켜짐, 회색=꺼짐).
스킬 이름을 누르면 좌측 콘솔 아래의 별도 창에 해당 스킬의 수치가 표시됩니다.
우측 콘솔에는 스크롤 없이 적마다 현재/최대 HP가 표시됩니다.
`electric`을 처치하면 근처 일반 적에게 최대 3번 연쇄 번개가 전파되고, `explosive`를 처치하면 반경 안 일반 적에게 폭발 피해가 들어갑니다.
`haste` 또는 `critical`을 처치하면 처치한 플레이어에게 5초 동안 공격 주기 단축 또는 확정 치명타가 적용됩니다. 치명타는 공격 1회당 한 번 판정합니다.
사망 효과를 가진 적은 다른 사망 효과의 피해를 받지 않습니다. 일반 스킬 공격에는 피해를 받습니다.
우측 적 콘솔의 `Restart test`는 적의 HP와 스킬 타이머를 다시 시작하며 체크 상태는 유지합니다.
Play 중 `GameHost/Enemy`를 선택하면 Inspector에서도 HP를 볼 수 있습니다.
콘솔 글자 크기는 기본 26이며 `GameHost` Inspector의 Console Font Size로 조절합니다.
Game 뷰가 작으면 좌측 스킬 콘솔과 수치 창에서 스크롤하세요. 레이저 경로는 Gizmos를 켜면 보입니다.

코어 계약 확인: `dotnet run --project tests/CoreSmoke/CoreSmoke.csproj --configuration Release`.
Unity EditMode 테스트는 같은 계약과 두 SO 에셋 로딩을 검사합니다.
스킬 데이터 형식은 [스킬 명세](docs/SKILL_SYSTEM_SPEC.md)를 참고하세요.
치명타·Haste·처치 버프의 적용 기준은 [공통 전투 수치 기획 명세](docs/COMBAT_STATS_AND_BUFFS_DESIGN.md)에 정리했습니다.
이전 작업 시점의 인수인계는 [2026-09-25 문서](docs/HANDOFF_SKILL_SYSTEM_2026-09-25.md)를 참고하세요.

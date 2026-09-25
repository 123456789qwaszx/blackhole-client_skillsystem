# BlackHole Skill System

Unity에서 두 패시브 스킬의 동작과 레벨별 업그레이드를 확인하는 독립 샘플입니다.

`Assets/BlackHole/SkillSystem/` 안에 스킬 코어(`Core`), 최소 적 피격 환경(`TestPack`),
Unity Play 확인용 진입점(`Unity`), 계약 테스트(`Tests`), 예시 콘텐츠(`Resources/gameplay.json`)가 있습니다.

Unity의 `Assets/Scenes/SampleScene.unity`를 Play하면 마우스 위치를 조준합니다.
빨간 구는 Game 뷰에 표시되는 테스트 적입니다. Play 중에는 Hierarchy의
`GameHost/Enemy`를 선택하면 Inspector에서도 현재 HP를 볼 수 있습니다.
좌측 콘솔의 Available Skills 체크박스로 보유 스킬의 발동을 켜고 끕니다.
Lv0 스킬은 잠겨 있어 먼저 해금해야 합니다. 콘솔에서 전투를 끝내고
NodeId 버튼을 눌러 레벨을 올린 뒤 다시 시작하면 새 수치가 적용됩니다.
콘솔 글자 크기는 기본 26이며 `GameHost` Inspector의 Console Font Size로 조절할 수 있습니다.
Game 뷰가 작으면 콘솔 안에서 스크롤하세요.
조준점과 레이저 경로는 Gizmos를 켜면 볼 수 있습니다. 적 표시는 Gizmos와 무관합니다.

코어 계약 테스트: `dotnet run --project tests/CoreSmoke/CoreSmoke.csproj --configuration Release`.
Unity EditMode 테스트도 같은 계약을 실행합니다.

데이터·구매 규칙은 [스킬 시스템 명세](docs/SKILL_SYSTEM_SPEC.md)를 참고하세요.

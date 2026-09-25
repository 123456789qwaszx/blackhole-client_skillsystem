# BlackHole Skill System

Unity에서 두 패시브 스킬의 동작과 레벨별 업그레이드를 확인하는 독립 샘플입니다.

`Assets/BlackHole/SkillSystem/` 안에 스킬 코어(`Core`), 최소 적 피격 환경(`TestPack`),
Unity Play 확인용 진입점(`Unity`), 계약 테스트(`Tests`), 예시 콘텐츠(`Resources/gameplay.json`)가 있습니다.

Unity의 `Assets/Scenes/SampleScene.unity`를 Play하면 마우스 위치를 조준합니다.
빨간 구는 체력이 있는 테스트 적입니다. 좌측 디버그 패널에서 전투를 끝내고
NodeId 버튼을 눌러 레벨을 올린 뒤 다시 시작하면 새 수치가 적용됩니다.
에디터/Game 뷰의 Gizmos를 켜면 조준점과 레이저 경로를 볼 수 있습니다.

코어 계약 테스트: `dotnet run --project tests/CoreSmoke/CoreSmoke.csproj --configuration Release`.
Unity EditMode 테스트도 같은 계약을 실행합니다.

데이터·구매 규칙은 [스킬 시스템 명세](docs/SKILL_SYSTEM_SPEC.md)를 참고하세요.

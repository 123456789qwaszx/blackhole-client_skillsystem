# BlackHole Skill Sandbox

`skill-only-sandbox` 브랜치는 스킬의 실행만 확인합니다. 노드·업그레이드·레벨은 없습니다.

- `Assets/BlackHole/SkillSystem/Core`: Breaker, PiercingLaser, 스킬 데이터 검증
- `Assets/BlackHole/SkillSystem/TestPack`: HP가 있는 고정 적과 재현 가능한 전투 환경
- `Assets/BlackHole/SkillSystem/Resources/gameplay.json`: 두 스킬의 기본 수치
- `Assets/BlackHole/SkillSystem/Unity`: Unity Play 확인용 콘솔과 적 표시

Unity의 `Assets/Scenes/SampleScene.unity`를 Play하고 마우스로 적을 조준합니다.
좌측 스킬 콘솔에서 스킬마다 체크/해제해 공격을 켜고 끌 수 있습니다.
우측 Enemy Stats 콘솔에는 적마다 현재/최대 HP, 좌표, 생존 상태와 마지막 공격자가 표시됩니다.
`Restart test`는 적의 HP와 스킬 타이머를 다시 시작하며 체크 상태는 유지합니다.
Play 중 `GameHost/Enemy`를 선택하면 Inspector에서도 HP를 볼 수 있습니다.
콘솔 글자 크기는 기본 26이며 `GameHost` Inspector의 Console Font Size로 조절합니다.
Game 뷰가 작으면 각 콘솔 안에서 스크롤하세요. 레이저 경로는 Gizmos를 켜면 보입니다.

코어 계약 확인: `dotnet run --project tests/CoreSmoke/CoreSmoke.csproj --configuration Release`.
Unity EditMode 테스트도 같은 계약을 실행합니다.
스킬 데이터 형식은 [스킬 명세](docs/SKILL_SYSTEM_SPEC.md)를 참고하세요.

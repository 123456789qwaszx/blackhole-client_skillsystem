# Skill-only sandbox

`gameplay.json`에는 `Version: 1`과 `Skills` 목록만 있다.
각 항목은 `Type`과 해당 스킬의 `Stats`를 담는다. 샘플 파일은
[`Resources/gameplay.json`](../Assets/BlackHole/SkillSystem/Resources/gameplay.json)에 있다.

| Type | 필요한 수치 | 의미 |
| --- | --- | --- |
| Breaker | Damage, Interval, Radius | 피해, 반복 간격(초), 조준 반경 |
| PiercingLaser | Damage, Interval, Width, TelegraphDuration | 피해, 예고 간격(초), 폭, 예고 길이(초) |

`SkillCatalog.Load`는 종류별 수치를 검사해 `SkillType → SkillStats`로 보관한다.
사용하는 수치는 유한한 양수여야 하고, 사용하지 않는 수치는 0이어야 한다.
중복 종류나 알 수 없는 종류는 진단과 함께 거부한다. 파일 IO/역직렬화는
Unity 호스트가 맡으며 코어는 순수 C#이다.

테스트 전투는 정의된 스킬을 모두 독립 실행 상태로 만들고 체크박스에서
스킬별 발동을 켜고 끈다. 끄면 공격 타이머와 진행 중인 레이저 예고를 버리고,
다시 켜면 처음부터 실행한다. 각 전투/플레이어는 실행 상태를 공유하지 않는다.
`TestPack`의 적은 HP와 마지막 공격자만 기록한다.

이 브랜치에는 노드 ID, 구매, 골드, 레벨, 업그레이드 효과, 트리 화면 정보가 없다.
업그레이드를 개발할 때는 별도 브랜치에서 게임 규칙을 연결한다.

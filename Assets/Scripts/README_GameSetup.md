# MeteoRun 2D 설정 가이드

이 프로젝트에는 현재 다음 기능의 게임플레이 스크립트가 포함되어 있습니다.
- 행성 둘레를 따라 이동하는 플레이어
- 무작위 운석 생성 및 낙하
- 충돌 처리와 게임 오버
- 점수 UI
- 키보드와 홀드 버튼 입력
- 간단한 패럴랙스 배경 연출
- 기본 오디오 연결 구조

## 1. 씬 오브젝트 구성

1. `Assets/Scenes/MainGame.unity`를 엽니다.
2. `Planet` 게임 오브젝트를 생성합니다.
   - `SpriteRenderer` 추가
   - `CircleCollider2D` 추가
   - `Is Trigger`는 끔
   - `PlanetSurface` 스크립트 추가
3. `Player` 게임 오브젝트를 생성합니다.
   - `SpriteRenderer` 추가
   - `CircleCollider2D` 추가
   - `Is Trigger`는 켜는 것을 권장
   - `Rigidbody2D` 추가 후 `Body Type = Kinematic`으로 설정
   - `PlayerOrbitalController` 스크립트 추가
   - `Planet Center`에 `Planet`의 Transform 연결
   - `Move Action Reference`는 필요하면 `Assets/InputSystem_Actions.inputactions`의 `Player/Move` 액션을 연결
   - 참조를 비워 두면 스크립트가 새 Input System 기준 기본 이동 액션을 자동 생성
4. `MeteorSpawner` 게임 오브젝트를 생성합니다.
   - `MeteorSpawner` 스크립트 추가
   - `Meteor Prefab`에 운석 프리팹 연결

## 2. 운석 프리팹 구성

1. `Meteor.prefab`을 생성합니다.
   - `SpriteRenderer` 추가
   - `CircleCollider2D` 추가
   - `Is Trigger`는 켜는 것을 권장
   - `Rigidbody2D` 추가
   - `Gravity Scale = 0`으로 설정
   - `Meteor` 스크립트 추가
2. 선택 사항으로 시각 효과를 추가할 수 있습니다.
   - `TrailRenderer`를 붙이면 간단한 꼬리 효과를 만들 수 있습니다.

## 3. UI 구성

1. `Canvas`를 생성하고 `Screen Space - Overlay`로 둡니다.
2. 좌측 상단에 `ScoreText`(`TextMeshPro - Text (UI)`)를 추가합니다.
3. 중앙에 `GameOverPanel`을 만들고 기본 상태를 비활성화합니다.
   - `FinalScoreText`(`TextMeshPro - Text (UI)`) 추가
   - `RestartButton`(`UI/Button`) 추가
   - 버튼 내부 라벨 텍스트는 `TextMeshPro - Text (UI)`로 구성
   - 버튼의 `OnClick`에 `GameManager.RestartGame` 연결
4. `GameManager` 오브젝트를 생성합니다.
   - `GameManager` 스크립트 추가
   - `Score Text`, `Game Over Panel`, `Final Score Text`에 TMP UI 참조 연결

## 4. 모바일 버튼 입력

1. UI 버튼 두 개를 추가합니다.
   - `LeftButton`
   - `RightButton`
2. 각 버튼에 `HoldButtonInput`을 추가합니다.
   - 왼쪽 버튼은 `Direction = Left`
   - 오른쪽 버튼은 `Direction = Right`
3. 씬에 `EventSystem`이 존재하는지 확인합니다.

이 버튼 입력은 키보드의 방향키와 `A`, `D` 입력과 함께 동작합니다.
모든 키보드 입력 처리는 구형 Input Manager가 아니라 새 `Input System` 기준으로 동작합니다.

## 5. 배경 연출

1. 배경에 별이나 구름 스프라이트를 배치합니다.
2. 각 배경 레이어에 `ParallaxScroller`를 추가합니다.
3. 레이어마다 `Speed` 값을 다르게 주면 깊이감이 생깁니다.

## 6. 오디오 구성

1. 씬 오브젝트 하나에 `GameAudio`를 추가합니다.
   - 보통 `GameManager` 오브젝트에 함께 두면 관리가 쉽습니다.
2. 다음 오디오 클립을 연결합니다.
   - `Ambience Clip`: 반복 재생할 우주 배경음
   - `Movement Clip`: 플레이어 이동 효과음
   - `Meteor Pass Clip`: 운석 회피 또는 통과 효과음
   - `Impact Clip`: 충돌 효과음
3. 필요하면 `GameManager`와 `PlayerOrbitalController`에 같은 `GameAudio` 참조를 연결합니다.

## 7. 빌드 설정

1. `MainGame` 씬을 Build Settings에 추가합니다.
2. 재시작 시 바로 현재 씬을 다시 불러오려면 `MainGame`의 Build Index를 0으로 두는 것이 안전합니다.

게임 오버 후에는 새 Input System 기준으로 키보드 `R` 키를 눌러도 다시 시작할 수 있습니다.

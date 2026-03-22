# MeteoRun

`MeteoRun`은 플레이어가 행성 표면을 따라 회전하며 날아오는 운석을 피하는 Unity 2D 모바일 게임 프로토타입입니다. 이 프로젝트는 적은 수의 역할 중심 `MonoBehaviour` 스크립트로 구성되어 있어, Unity 에디터에서 빠르게 배치하고 바로 플레이 가능한 상태로 만들 수 있도록 설계되었습니다.

## 코드로 구현된 내용

현재 구현은 아래 6개의 스크립트를 중심으로 구성되어 있습니다.

- `GameManager.cs`: 전체 게임 흐름, 게임오버 처리, 재시작 처리, 그리고 점수/UI/운석 스폰 시스템 간의 최상위 연결을 담당합니다.
- `PlayerController.cs`: 각도 값을 기반으로 플레이어를 행성 표면에 고정하고, `cos`와 `sin`을 이용해 월드 좌표를 계산합니다.
- `MeteorSpawner.cs`: 화면 밖 임의 방향에서 운석을 생성하고, 행성 중심으로 날아오게 하며, 시간이 지날수록 난이도를 높이고, 동시에 화면에 존재할 수 있는 운석 수를 제한합니다.
- `Meteor.cs`: 각 운석을 행성 중심 방향으로 이동시키고, 플레이어 및 행성과의 트리거 충돌을 처리합니다.
- `ScoreManager.cs`: 생존 시간에 따라 점수를 지속적으로 올리고, 운석이 행성에 맞았을 때 보너스 점수를 지급하며, 게임오버 이후에는 점수 증가를 막습니다.
- `UIManager.cs`: 점수 UI를 갱신하고, 게임오버 패널을 제어하며, 재시작 버튼 입력을 처리합니다.

추가로 다음 기능도 포함되어 있습니다.

- 새 Input System 기반 키보드 입력
- 화면 좌우 절반을 기준으로 한 모바일 터치 입력
- 모바일 터치와 동일하게 동작하는 에디터 마우스 홀드 입력
- UI 버튼과 `R` 키를 통한 재시작
- 궤도 반지름, 이동 속도, 스폰 주기, 운석 수, 운석 속도 편차 등을 Inspector에서 조정 가능

## 에디터에서 직접 설정해야 하는 내용

코드가 씬을 자동으로 구성해주지는 않습니다. Unity 에디터에서 아래 작업은 직접 해야 합니다.

- 필요한 씬 오브젝트 생성
- 행성과 플레이어의 위치 및 크기 배치
- 운석 프리팹 생성
- Canvas 및 TextMeshPro UI 구성
- 필요한 태그 추가
- 올바른 GameObject에 스크립트 부착
- Inspector 참조 연결
- Player Settings에서 새 Input System 활성화

## 씬 구조

예를 들어 `Assets/Scenes/MainGame.unity` 같은 씬을 만들고, 최소한 아래 구조를 갖추면 됩니다.

1. `GameManager`
2. `Planet`
3. `Player`
4. `MeteorSpawner`
5. `ScoreManager`
6. `UIManager`
7. `Canvas`
8. `EventSystem`
9. `Main Camera`

## 필요한 태그

`Project Settings > Tags and Layers`에서 아래 태그를 추가하세요.

- `Player`
- `Planet`
- `Meteor`

태그는 아래처럼 지정합니다.

- `Player` 오브젝트 -> `Player`
- `Planet` 오브젝트 -> `Planet`
- `Meteor` 프리팹 루트 -> `Meteor`

## 오브젝트별 상세 설정

### GameManager 오브젝트

빈 GameObject를 만들고 이름을 `GameManager`로 지정합니다.

부착할 컴포넌트:

- `GameManager`

Inspector 연결:

- `Score Manager` -> `ScoreManager` 오브젝트 드래그
- `UI Manager` -> `UIManager` 오브젝트 드래그
- `Meteor Spawner` -> `MeteorSpawner` 오브젝트 드래그

### ScoreManager 오브젝트

빈 GameObject를 만들고 이름을 `ScoreManager`로 지정합니다.

부착할 컴포넌트:

- `ScoreManager`

중요 Inspector 값:

- `Score Per Second` -> 기본값 `5`

### UIManager 오브젝트

빈 GameObject를 만들고 이름을 `UIManager`로 지정합니다.

부착할 컴포넌트:

- `UIManager`

Inspector 연결:

- `Score Text` -> HUD 점수용 `TextMeshProUGUI`
- `Game Over Panel` -> 게임오버 패널 루트 오브젝트
- `Final Score Text` -> 최종 점수용 `TextMeshProUGUI`
- `Restart Button` -> 재시작 `Button`

### Planet 오브젝트

화면 중앙에 `Planet`이라는 이름의 GameObject를 생성합니다.

부착할 컴포넌트:

- `SpriteRenderer`
- `CircleCollider2D`

필수 설정:

- Tag -> `Planet`
- Position -> `(0, 0, 0)`
- 콜라이더가 보이는 행성 크기를 충분히 덮도록 설정
- `CircleCollider2D > Is Trigger` -> 이 프로토타입에서는 활성화하는 것이 가장 단순함

### Player 오브젝트

`Player`라는 이름의 GameObject를 생성합니다.

부착할 컴포넌트:

- `SpriteRenderer`
- `CircleCollider2D`
- `Rigidbody2D`
- `PlayerController`

필수 설정:

- Tag -> `Player`
- `Rigidbody2D > Body Type` -> `Kinematic`
- `Rigidbody2D > Gravity Scale` -> `0`
- `CircleCollider2D > Is Trigger` -> 활성화

Inspector 연결:

- `Planet Center` -> `Planet`의 Transform
- `Planet Radius` -> 행성 중심에서 플레이어 중심까지의 거리
- `Angular Speed Degrees` -> 플레이어 회전 속도 조정
- `Start Angle Degrees` -> 시작 위치 각도 설정

### MeteorSpawner 오브젝트

빈 GameObject를 만들고 이름을 `MeteorSpawner`로 지정합니다.

부착할 컴포넌트:

- `MeteorSpawner`

Inspector 연결:

- `Meteor Prefab` -> 운석 프리팹
- `Planet Center` -> `Planet`의 Transform
- `Target Camera` -> `Main Camera`

권장 시작값:

- `Starting Spawn Interval` -> `1.2`
- `Minimum Spawn Interval` -> `0.45`
- `Spawn Ramp Duration` -> `90`
- `Starting Max Visible Meteors` -> `3`
- `Maximum Visible Meteors` -> `10`
- `Visible Meteor Increase Interval` -> `15`
- `Base Meteor Speed` -> `4`
- `Maximum Speed Variation` -> `2`
- `Speed Variation Unlock Time` -> `20`
- `Speed Variation Ramp Duration` -> `70`
- `Planet Hit Bonus Score` -> `10`
- `Spawn Padding` -> `1.5`

### Meteor 프리팹

예를 들어 `Meteor.prefab` 같은 이름으로 프리팹을 만듭니다.

프리팹 루트에 부착할 컴포넌트:

- `SpriteRenderer`
- `CircleCollider2D`
- `Rigidbody2D`
- `Meteor`

필수 설정:

- Tag -> `Meteor`
- `Rigidbody2D > Body Type` -> `Kinematic`
- `Rigidbody2D > Gravity Scale` -> `0`
- `CircleCollider2D > Is Trigger` -> 활성화
- 현재 단계에서는 모든 운석의 스케일을 동일하게 유지

### Canvas

`Screen Space - Overlay` 모드의 `Canvas`를 생성합니다.

아래 UI 자식 오브젝트를 만듭니다.

- `ScoreText` (`TextMeshPro - Text`)
- `GameOverPanel`
- `FinalScoreText` (`GameOverPanel` 내부)
- `RestartButton` (`GameOverPanel` 내부)

권장 배치:

- `ScoreText`는 화면 상단 근처에 배치
- `GameOverPanel`은 화면 중앙에 배치
- 에디터에서는 패널을 켜 둔 상태로 배치 작업을 해도 되지만, 플레이 시작 시 스크립트가 자동으로 숨김

### EventSystem

씬에 `EventSystem`이 반드시 있어야 합니다. 보통 UI를 만들면 Unity가 자동 생성합니다.

### Main Camera

권장 카메라 설정:

- Orthographic 카메라 사용
- 행성이 화면 중앙에 완전히 보이도록 배치
- 운석이 화면 밖에서 생성되어 안쪽으로 들어올 수 있도록 여백 확보

## 입력 방식

조작 방식은 아래와 같습니다.

- 왼쪽 이동: `A` 또는 `Left Arrow`
- 오른쪽 이동: `D` 또는 `Right Arrow`
- 재시작: `R`
- 모바일 터치: 화면 왼쪽 절반을 누르고 있으면 왼쪽 회전
- 모바일 터치: 화면 오른쪽 절반을 누르고 있으면 오른쪽 회전
- 에디터 테스트: 마우스 왼쪽 버튼을 화면 좌/우 절반에 홀드

좌우가 동시에 눌린 경우 플레이어는 회전하지 않습니다.

## 새 Input System 설정

스크립트에서 `UnityEngine.InputSystem`을 사용하므로, 아래 설정이 필요합니다.

- `Project Settings > Player > Active Input Handling` -> `Input System Package (New)` 또는 `Both`

## Inspector 참조 연결표

씬 연결 시 아래 기준으로 참조를 연결합니다.

- `GameManager.ScoreManager` -> 씬의 `ScoreManager`
- `GameManager.UIManager` -> 씬의 `UIManager`
- `GameManager.MeteorSpawner` -> 씬의 `MeteorSpawner`
- `UIManager.ScoreText` -> HUD 점수 TMP 텍스트
- `UIManager.GameOverPanel` -> 게임오버 패널 루트
- `UIManager.FinalScoreText` -> 최종 점수 TMP 텍스트
- `UIManager.RestartButton` -> 재시작 버튼
- `PlayerController.PlanetCenter` -> `Planet`의 Transform
- `MeteorSpawner.MeteorPrefab` -> 운석 프리팹
- `MeteorSpawner.PlanetCenter` -> `Planet`의 Transform
- `MeteorSpawner.TargetCamera` -> `Main Camera`

## 설정 체크리스트

- 메인 씬을 생성하고 저장했는가
- `Player`, `Planet`, `Meteor` 태그를 추가했는가
- `Planet` 오브젝트를 만들고 중앙에 배치했는가
- `Player` 오브젝트를 만들고 궤도 반지름을 맞췄는가
- 운석 프리팹에 콜라이더, Rigidbody2D, `Meteor` 스크립트를 붙였는가
- `GameManager`, `ScoreManager`, `UIManager`, `MeteorSpawner`를 만들었는가
- Canvas, 점수 텍스트, 게임오버 패널, 최종 점수 텍스트, 재시작 버튼을 만들었는가
- 모든 Inspector 참조를 연결했는가
- 새 Input System을 활성화했는가
- 씬을 Build Settings에 추가하고 빌드 인덱스를 `0`으로 유지했는가

## 플레이 테스트 체크리스트

- 플레이어가 시작 시 행성 표면에 정확히 붙어 있는가
- 키보드 조작으로 플레이어가 올바르게 회전하는가
- 화면 왼쪽 절반 홀드 시 왼쪽으로 회전하는가
- 화면 오른쪽 절반 홀드 시 오른쪽으로 회전하는가
- 에디터 마우스 홀드 동작이 모바일 터치와 동일한가
- 운석이 화면 밖 모든 방향에서 생성되는가
- 운석이 행성 중심 방향으로 이동하는가
- 운석이 플레이어에 닿으면 즉시 게임오버가 되는가
- 운석이 행성에 닿으면 파괴되고 보너스 점수를 주는가
- 생존 시간에 따라 점수가 계속 증가하는가
- 게임오버 이후 점수가 더 이상 오르지 않는가
- 게임오버 패널에 최종 점수가 표시되는가
- 재시작 버튼이 씬을 정상적으로 다시 불러오는가
- `R` 키가 씬을 정상적으로 다시 불러오는가
- 시간이 지날수록 운석 수와 속도 편차를 통해 난이도가 상승하는가

## 다음 기능 제안

- HP 시스템: 한 번 맞고 끝나는 대신 여러 번 버틸 수 있도록 확장
- 운석 경고 표시: 빠른 운석이 들어오기 전에 가장자리 경고 제공
- 사운드: 피격, 생성, UI 클릭, 배경음 등 추가
- 캐릭터 스킨: 이동 로직은 유지한 채 외형만 교체 가능하게 확장
- 운석 종류 확장: 무거운 운석, 분열 운석, 곡선 궤적 운석 등 추가
- 행성 업그레이드: 보호막, 슬로우 모션, 점수 배수 등 추가
- 모바일 UX 개선: 진동, 일시정지 메뉴, 터치 친화적인 게임오버 UI 추가

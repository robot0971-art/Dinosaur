# Core 시스템 개요

이 문서는 Dino Grow 3D의 현재 Core 로직과 테스트 씬 연결 상태를 설명한다.  
팀원이나 AI 에이전트가 현재 구현된 스크립트의 역할을 빠르게 이해하고 다음 작업을 이어가기 위한 기준 문서다.

## 현재 구현 상태

현재 프로젝트에는 아래 흐름까지 구현되어 있다.

```text
플레이어가 적 공룡과 충돌
→ EatResolver가 레벨 비교
→ 플레이어 레벨 >= 적 레벨이면 먹기 성공
→ 먹은 적 레벨 x 10 만큼 EXP 획득
→ EXP가 100 이상이면 레벨업
→ 플레이어 크기 갱신
→ 플레이어 레벨 < 적 레벨이면 즉시 GAME OVER
```

테스트 씬 `Assets/Scenes/SampleScene.unity`에는 임시 플레이어와 적이 배치되어 있다.

- `Player`: 플레이어 캡슐
- `Enemy_Lv1`: 먹을 수 있는 적
- `Enemy_Lv2`: 시작 상태에서는 충돌 시 게임오버
- `Enemy_Lv3`: 시작 상태에서는 충돌 시 게임오버
- `GameHudCanvas`: 레벨, EXP, 상태 텍스트 표시
- `GameRoot`: VContainer `GameLifetimeScope`
- `CinemachineCamera`: `PlayerCameraTarget`을 Follow/LookAt

## 게임 규칙

최종 기획 기준의 핵심 규칙은 다음과 같다.

- 플레이어 레벨 >= 적 레벨: 먹기 성공
- 플레이어 레벨 < 적 레벨: 즉시 게임오버
- 같은 레벨 적도 먹을 수 있다.
- 체력 시스템은 없다.
- 피해량, 방어력, 무적 시간은 첫 버전에 없다.
- 획득 EXP는 `적 레벨 x 10`이다.
- EXP가 100 이상이면 레벨이 1 오른다.
- 레벨업 후 남은 EXP는 유지한다.
- 레벨 20에 도달하면 클리어 처리한다.

## 폴더 구조

현재 주요 스크립트 구조는 다음과 같다.

```text
Assets/Scripts
 ├─ Core
 │   ├─ Combat
 │   │   ├─ EatResolver.cs
 │   │   └─ EatResult.cs
 │   ├─ Growth
 │   │   ├─ GrowthResult.cs
 │   │   ├─ GrowthSystem.cs
 │   │   └─ PlayerProgress.cs
 │   └─ Stage
 │       ├─ GameState.cs
 │       ├─ GameStateController.cs
 │       └─ StageRule.cs
 ├─ Gameplay
 │   ├─ Enemy
 │   │   └─ DinoEnemy.cs
 │   └─ Player
 │       └─ PlayerDinoController.cs
 ├─ Infrastructure
 │   ├─ DI
 │   │   └─ GameLifetimeScope.cs
 │   └─ Events
 │       └─ GameEventBus.cs
 └─ UI
     └─ GameHud.cs
```

## Core 스크립트 역할

### EatResult

파일:

```text
Assets/Scripts/Core/Combat/EatResult.cs
```

먹기 판정 결과 enum이다.

현재 값:

```text
Eat
GameOver
```

현재 기획에서는 같은 레벨도 먹을 수 있으므로 `Bounce`는 없다.

### EatResolver

파일:

```text
Assets/Scripts/Core/Combat/EatResolver.cs
```

플레이어 레벨과 적 레벨을 비교해 결과만 반환한다.

규칙:

```text
playerLevel >= enemyLevel → Eat
playerLevel < enemyLevel  → GameOver
```

하지 않는 일:

- 적 삭제
- EXP 지급
- UI 갱신
- 사운드 재생
- 씬 오브젝트 검색

### PlayerProgress

파일:

```text
Assets/Scripts/Core/Growth/PlayerProgress.cs
```

플레이어의 성장 상태를 가진다.

보유 상태:

- 현재 레벨
- 현재 EXP
- 최대 레벨
- 레벨업 필요 EXP

기본값:

```text
startLevel = 1
currentExp = 0
maxLevel = 20
expToLevelUp = 100
```

주의:

VContainer가 매개변수 생성자를 선택하지 않도록 `GameLifetimeScope`에서 `RegisterInstance(new PlayerProgress())`로 등록한다.

### GrowthSystem

파일:

```text
Assets/Scripts/Core/Growth/GrowthSystem.cs
```

EXP 계산과 레벨업 처리를 담당한다.

규칙:

```text
gainedExp = enemyLevel * 10
while currentExp >= 100 && level < 20
    currentExp -= 100
    level += 1
```

결과는 `GrowthResult`로 반환한다.

### GrowthResult

파일:

```text
Assets/Scripts/Core/Growth/GrowthResult.cs
```

성장 처리 결과값이다.

포함 정보:

- 획득 EXP
- 레벨업 횟수
- 현재 레벨
- 현재 EXP
- 최대 레벨 도달 여부

UI, 사운드, 카메라 반응은 이 결과를 보고 처리하면 된다.

### GameState

파일:

```text
Assets/Scripts/Core/Stage/GameState.cs
```

현재 게임 상태 enum이다.

현재 값:

```text
Ready
Playing
GameOver
Clear
```

### GameStateController

파일:

```text
Assets/Scripts/Core/Stage/GameStateController.cs
```

게임 상태 전환만 담당한다.

역할:

- 게임 시작
- 게임오버
- 클리어
- 리셋
- 현재 플레이 가능 여부 제공

주의:

이 클래스가 거대한 `GameManager`가 되면 안 된다.

### StageRule

파일:

```text
Assets/Scripts/Core/Stage/StageRule.cs
```

스테이지 클리어 조건을 판단한다.

현재 규칙:

```text
playerLevel >= 20 → Clear
```

## Infrastructure 스크립트 역할

### GameEventBus

파일:

```text
Assets/Scripts/Infrastructure/Events/GameEventBus.cs
```

게임 규칙을 결정하지 않고, 결과를 다른 시스템에 알리는 반응용 이벤트 버스다.

현재 이벤트:

- `EnemyEaten`
- `PlayerGrowthChanged`
- `GameStateChanged`

사용 목적:

- UI 갱신
- 사운드 재생
- 이펙트 재생
- 카메라 반응
- 게임오버/클리어 화면 표시

사용하지 말아야 할 곳:

- 먹기 가능 여부 판단
- EXP 계산
- 레벨업 가능 여부 판단
- 게임오버 여부 결정

### GameLifetimeScope

파일:

```text
Assets/Scripts/Infrastructure/DI/GameLifetimeScope.cs
```

VContainer 등록 지점이다.

현재 등록 서비스:

- `GameEventBus`
- `EatResolver`
- `GrowthSystem`
- `PlayerProgress`
- `GameStateController`
- `StageRule`

씬 컴포넌트 등록:

- `PlayerDinoController`
- `GameHud`

`PlayerProgress`는 다음처럼 등록한다.

```text
RegisterInstance(new PlayerProgress())
```

이유는 VContainer가 `PlayerProgress(int, int, int, int)` 생성자를 선택해 `int`를 주입하려고 하지 않게 하기 위해서다.

## Gameplay 스크립트 역할

### PlayerDinoController

파일:

```text
Assets/Scripts/Gameplay/Player/PlayerDinoController.cs
```

플레이어 오브젝트를 실제로 움직이고 충돌을 Core 로직에 연결한다.

VContainer로 주입받는 것:

- `EatResolver`
- `GrowthSystem`
- `PlayerProgress`
- `GameStateController`
- `StageRule`
- `GameEventBus`

담당:

- WASD / 방향키 이동
- Rigidbody 이동
- 이동 방향으로 회전
- 적과 Trigger 충돌 처리
- 먹기 성공 시 적 삭제 요청
- EXP 획득과 레벨업 호출
- 레벨업 후 플레이어 크기 변경
- 높은 레벨 적과 충돌 시 게임오버 처리
- 레벨 20 도달 시 클리어 처리

입력은 Unity New Input System을 사용한다.

```text
Keyboard.current
```

`Input.GetAxisRaw`는 사용하지 않는다.  
현재 프로젝트는 Input System 패키지 방식이기 때문이다.

### DinoEnemy

파일:

```text
Assets/Scripts/Gameplay/Enemy/DinoEnemy.cs
```

적 공룡의 레벨을 가진다.

현재 역할:

- 적 레벨 보관
- 레벨 설정
- 먹혔을 때 GameObject 삭제

현재는 AI가 없다.  
첫 테스트 씬에서는 정지한 구체로 사용한다.

## UI 스크립트 역할

### GameHud

파일:

```text
Assets/Scripts/UI/GameHud.cs
```

HUD 텍스트를 갱신한다.

VContainer로 주입받는 것:

- `PlayerProgress`
- `GameEventBus`

표시 항목:

- 현재 레벨
- 현재 EXP
- GAME OVER
- LEVEL 20 CLEAR

반응 이벤트:

- `PlayerGrowthChanged`
- `GameStateChanged`

## 테스트 씬 동작 확인

현재 `SampleScene`에서 확인된 흐름:

```text
Player Lv.1이 Enemy_Lv1과 충돌
→ 먹기 성공
→ EXP 10 / 100 표시
```

```text
Player Lv.1이 Enemy_Lv2와 충돌
→ GAME OVER 표시
```

`dotnet build Assembly-CSharp.csproj` 기준 컴파일 오류는 없다.

## 현재 한계

아직 구현되지 않은 것:

- 적 AI
- 자동 스폰 시스템
- 레벨 라벨이 항상 카메라를 바라보는 처리
- 레벨업 이펙트
- 먹기 이펙트
- 사운드
- 게임오버 후 다시 시작 버튼
- 클리어 화면 UI 완성
- 실제 공룡 모델로 플레이어/적 교체
- 카메라 거리 자동 조정

## 다음 작업 추천

다음으로 하면 좋은 작업 순서:

1. 적 머리 위 레벨 표시를 UI 또는 Billboard 방식으로 개선
2. 플레이어 이동을 카메라 기준 이동으로 변경
3. Cinemachine 카메라 거리와 높이를 레벨에 따라 조정
4. 레벨업 시 카메라/스케일/이펙트 반응 추가
5. `DinoLevelStats` 데이터 추가
6. `DinoSpawner`와 `SpawnRule` 구현
7. 테스트용 구체를 실제 공룡 프리팹으로 교체

## 중요한 작업 규칙

- 게임 로직에서 `FindObjectOfType`, `GameObject.Find`를 사용하지 않는다.
- Core 로직은 Unity 씬 오브젝트에 의존하지 않는다.
- Scene 연결은 `GameLifetimeScope`와 `[SerializeField]`를 통해 명시적으로 한다.
- EventBus는 반응용으로만 쓴다.
- 핵심 규칙은 직접 호출로 명확하게 유지한다.

# AGENTS.md

## 프로젝트 개요

이 프로젝트는 Unity 3D로 만드는 `Dino Grow 3D`다.

플레이어는 3인칭 시점에서 공룡을 조작한다.  
플레이어보다 레벨이 낮거나 같은 공룡은 먹을 수 있고, 플레이어보다 레벨이 높은 공룡과 충돌하면 즉시 게임오버된다.  
공룡을 먹으면 EXP를 얻고, EXP가 100 이상이면 레벨업한다. 최종 목표는 레벨 20에 도달하는 것이다.

## 최종 게임 규칙

- 플레이어 레벨 >= 적 공룡 레벨: 먹기 성공
- 플레이어 레벨 < 적 공룡 레벨: 즉시 게임오버
- 같은 레벨 공룡은 먹을 수 있다.
- 체력 시스템은 없다.
- 피해량, 방어력, 무적 시간, 회복 아이템은 첫 버전에 만들지 않는다.
- EXP 최대치는 100이다.
- 획득 EXP는 `먹은 공룡 레벨 x 10`이다.
- EXP가 100 이상이면 100을 소모하고 레벨이 1 오른다.
- EXP가 100을 넘으면 남은 EXP를 유지한다.
- 레벨업 처리는 `while currentExp >= 100 && level < 20` 방식으로 한다.
- 레벨 20이 되는 순간 클리어 처리한다.
- 레벨 20 이후 EXP는 더 이상 성장에 사용하지 않는다.

## 핵심 아키텍처 방향

이 프로젝트는 처음부터 VContainer를 사용한다.

목표는 Unity 씬 검색에 기대지 않는 구조다.  
게임 로직에서 아래 코드는 사용하지 않는다.

- `FindObjectOfType`
- `FindAnyObjectByType`
- `FindFirstObjectByType`
- `GameObject.Find`
- gameplay용 전역 싱글톤 `Instance`
- 숨겨진 씬 검색으로 의존성 찾기

대신 아래 방식을 사용한다.

- Core 게임 규칙은 순수 C# 클래스로 작성하고 VContainer로 주입한다.
- MonoBehaviour는 Unity 입력, 충돌, Transform, Rigidbody, Animator 같은 Unity 연결만 담당한다.
- 씬 안에서 직접 연결해야 하는 Unity 컴포넌트는 `[SerializeField]`를 사용한다.
- UI, 사운드, 이펙트, 카메라 반응은 EventBus를 통해 반응한다.
- 먹기 판정, EXP 계산, 레벨업, 게임오버 판정은 이벤트로 숨기지 말고 직접 메서드 호출로 처리한다.

## 추천 폴더 구조

```text
Assets/Scripts
 ├─ Core
 │   ├─ Dino
 │   ├─ Growth
 │   ├─ Combat
 │   ├─ Spawn
 │   └─ Stage
 ├─ Infrastructure
 │   ├─ DI
 │   ├─ Events
 │   └─ Data
 ├─ Gameplay
 │   ├─ Player
 │   ├─ Enemy
 │   └─ Spawning
 ├─ UI
 └─ Camera
```

폴더 역할:

- `Core`: Unity 씬에 의존하지 않는 게임 규칙
- `Infrastructure`: DI, EventBus, 데이터 로딩, 저장소
- `Gameplay`: 실제 씬 오브젝트와 MonoBehaviour
- `UI`: HUD, 게임오버, 클리어 화면
- `Camera`: Cinemachine 제어, 레벨업 줌, 카메라 이벤트 반응

## Core 로직

Core는 가능한 순수 C#으로 작성한다.

우선 필요한 Core 클래스:

- `EatResolver`
- `GrowthSystem`
- `StageRule`
- `SpawnRule`
- `DinoLevelData`
- `PlayerProgress`
- `GameStateController`

첫 버전에서는 `HealthSystem`을 만들지 않는다.  
체력 시스템이 없기 때문이다.

첫 버전에서는 `Bounce` 판정도 만들지 않는다.  
같은 레벨 공룡도 먹을 수 있기 때문이다.

먹기 판정은 단순해야 한다.

```csharp
if (playerLevel >= enemyLevel)
{
    // Eat
}
else
{
    // GameOver
}
```

## EatResolver

`EatResolver`는 플레이어와 적의 레벨을 비교해서 결과만 반환한다.

역할:

- 플레이어 레벨과 적 레벨 비교
- 먹기 성공 또는 게임오버 결정

하지 말아야 할 일:

- 적 삭제
- EXP 지급
- UI 갱신
- 사운드 재생
- 씬 오브젝트 검색

추천 결과 타입:

```csharp
public enum EatResult
{
    Eat,
    GameOver
}
```

## GrowthSystem

`GrowthSystem`은 EXP와 레벨업만 담당한다.

규칙:

- EXP 획득량은 `enemyLevel * 10`
- EXP가 100 이상이면 레벨업
- 남은 EXP는 유지
- 레벨 20이면 더 이상 레벨업하지 않음

하지 말아야 할 일:

- 적 삭제
- 게임오버 처리
- UI 직접 조작
- 사운드 직접 재생
- 카메라 직접 조작

레벨업 이후 UI, 사운드, 카메라 반응은 EventBus 이벤트로 알린다.

## GameStateController

게임 상태 전환을 담당한다.

추천 상태:

```csharp
public enum GameState
{
    Ready,
    Playing,
    GameOver,
    Clear
}
```

역할:

- 게임 시작
- 게임오버
- 클리어
- 플레이 가능 여부 제공

`GameStateController`는 거대한 `GameManager`가 되면 안 된다.  
성장, 스폰, UI, 카메라, 사운드 책임을 가져오지 않는다.

## VContainer 사용 규칙

메인 게임 씬에는 우선 하나의 `GameLifetimeScope`를 둔다.

위치:

```text
Assets/Scripts/Infrastructure/DI/GameLifetimeScope.cs
```

등록할 Core 서비스 예시:

- `GameEventBus`
- `EatResolver`
- `GrowthSystem`
- `StageRule`
- `SpawnRule`
- `GameStateController`
- `DinoDataRepository`

씬 컴포넌트 예시:

- `PlayerDinoController`
- `DinoSpawner`
- `GameHud`
- `DinoCameraController`

규칙:

- 아무 게임 코드에서나 컨테이너를 직접 꺼내 쓰는 Service Locator 방식은 피한다.
- 가능하면 생성자 주입 또는 `[Inject]` 메서드 주입을 사용한다.
- 동적으로 생성하는 프리팹은 `IObjectResolver.Instantiate`를 사용한다.
- 씬 컴포넌트는 명시적으로 등록하거나 `[SerializeField]`로 연결한다.

## SOLID 원칙

SOLID는 실용적으로 적용한다.  
초반부터 인터페이스와 추상화를 남발하지 않는다.

### 단일 책임 원칙

각 클래스는 하나의 명확한 역할만 가진다.

예시:

- `EatResolver`: 먹기 성공/게임오버 결과만 판단
- `GrowthSystem`: EXP와 레벨업만 처리
- `DinoSpawner`: 공룡 생성만 담당
- `GameHud`: UI 표시만 담당
- `DinoCameraController`: 카메라 반응만 담당

### 의존성 역전 원칙

게임 로직은 씬 검색에 의존하지 않는다.

사용:

- 순수 C# 서비스는 VContainer 주입
- MonoBehaviour는 `[Inject]` 메서드 주입
- Unity 컴포넌트 참조는 `[SerializeField]`

금지:

- `FindObjectOfType`
- `GameObject.Find`
- 숨겨진 싱글톤 접근

### 개방 폐쇄 원칙

새 공룡 행동, 스폰 규칙, 아이템 효과를 추가할 때 거대한 매니저 하나를 계속 키우지 않는다.

가능하면 작은 클래스, 전략 객체, 데이터 테이블 추가로 확장한다.

### 인터페이스 분리 원칙

처음부터 인터페이스를 남발하지 않는다.

인터페이스는 아래 경우에만 만든다.

- 구현체가 2개 이상 필요할 때
- 테스트에서 대체 구현이 필요할 때
- 외부 시스템과 경계를 나눌 때

### 리스코프 치환 원칙

상속을 사용할 경우 자식 클래스가 부모 클래스처럼 안전하게 동작해야 한다.

공룡 AI나 아이템 효과는 깊은 상속보다 조합, 상태, 전략 객체를 우선한다.

## EventBus 사용 규칙

EventBus는 반응용으로 사용한다.

사용하기 좋은 곳:

- UI 갱신
- 사운드 재생
- 이펙트 재생
- 카메라 흔들림
- 레벨업 연출
- EXP 획득 텍스트
- 게임오버 화면
- 클리어 화면

사용하지 말아야 할 곳:

- 먹을 수 있는지 판단
- EXP 계산
- 레벨업 가능 여부 계산
- 게임오버 여부 결정
- 스폰 규칙 결정

핵심 게임 규칙은 이벤트로 숨기지 말고 직접 호출로 명확하게 처리한다.

## Cinemachine 사용 규칙

카메라는 Cinemachine을 메인으로 사용한다.

구조:

- `Main Camera`: 실제 렌더링 카메라, `CinemachineBrain` 보유
- `CinemachineCamera`: 플레이어를 Follow/LookAt
- `PlayerCameraTarget`: 플레이어 자식 오브젝트로 두는 추적 대상
- `DinoCameraController`: 레벨업 줌, 게임오버 연출, 카메라 흔들림 제어

카메라 로직은 직접 `Camera.main` 검색에 의존하지 않는다.

레벨업 시 카메라 거리와 높이를 조정한다.

추천값:

```text
Lv. 1~4   거리 6,  높이 4
Lv. 5~8   거리 7,  높이 4.5
Lv. 9~12  거리 8,  높이 5
Lv. 13~16 거리 9,  높이 6
Lv. 17~20 거리 10, 높이 7
```

스테이지별 맵 경계가 생기면 `CameraBounds`를 두고 카메라가 맵 밖을 지나치게 보지 않도록 제한한다.

## 데이터 관리

게임 밸런스는 데이터 기반으로 관리한다.

예상 테이블:

- `DinoTable`
- `GrowthTable`
- `SpawnTable`
- `StageTable`

엑셀 컨버터 위치:

```text
Tools/Dino Game/Excel Converter
```

생성 데이터 위치:

```text
Assets/GameData/Generated
```

데이터 규칙:

- 공룡 ID는 중복되면 안 된다.
- 공룡 레벨은 1~20 범위여야 한다.
- EXP 보상은 기본적으로 `level * 10`을 따른다.
- 이동속도는 0보다 커야 한다.
- 프리팹 참조는 비어 있으면 안 된다.

## 스테이지와 맵

첫 버전은 초원 맵 하나로 시작한다.

나중에 스테이지별 맵을 만들 경우 추천 구조:

```text
Gameplay.unity
 ├─ GameLifetimeScope
 ├─ Main Camera
 ├─ CinemachineCamera
 ├─ Player
 ├─ UI
 └─ StageLoader

StageMap Prefab
 ├─ VisualRoot
 ├─ CollisionRoot
 ├─ SpawnRoot
 ├─ CameraRoot
 └─ StageInfo
```

맵은 로우폴리 스타일을 우선한다.

중요한 맵 규칙:

- 시작 지점 근처에는 먹을 수 있는 공룡이 있어야 한다.
- 높은 레벨 공룡은 시작 지점 바로 옆에 두지 않는다.
- 플레이어가 레벨업으로 커져도 통로를 지나갈 수 있어야 한다.
- 숫자와 UI 가독성을 방해할 정도로 배경을 복잡하게 만들지 않는다.

## 1차 프로토타입 우선순위

처음에는 핵심 재미만 만든다.

1. VContainer 설치 및 `GameLifetimeScope` 구성
2. 플레이어 Capsule 이동
3. Cinemachine 3인칭 카메라 Follow/LookAt 연결
4. 플레이어 레벨과 EXP 상태 구현
5. 적 공룡 레벨 1~3 임시 오브젝트 배치
6. 적 머리 위 레벨 표시
7. 충돌 시 먹기 또는 게임오버 처리
8. EXP 100 기준 레벨업
9. 레벨업 시 플레이어 크기 증가
10. HUD에 레벨, EXP 표시
11. 게임오버 UI 표시
12. 레벨 20 클리어 처리

복잡한 아트, 보스전, 스킨, 도감, 고급 AI, 아이템은 나중에 만든다.

## 코딩 규칙

- 스크립트는 작고 역할이 명확해야 한다.
- 씬 검색보다 명시적인 참조와 DI를 사용한다.
- 게임 시스템에 static 상태를 남발하지 않는다.
- 이름은 역할이 드러나게 작성한다.
- 주석은 꼭 필요한 경우에만 짧게 작성한다.
- 관련 없는 Unity 설정은 함부로 바꾸지 않는다.
- `Library`, `Temp`, `Logs`, `UserSettings`는 커밋하지 않는다.
- Unity 자동 생성 `.csproj`, `.sln`, `.slnx` 파일은 직접 편집하지 않는다.

## 현재 게임 방향

플레이어가 항상 바로 판단할 수 있어야 한다.

```text
저 공룡을 먹을 수 있나?
아니면 도망가야 하나?
```

모든 게임 로직, UI, 이펙트, 카메라는 이 판단을 쉽게 만드는 방향으로 설계한다.

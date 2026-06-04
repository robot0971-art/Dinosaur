# AGENTS.md

## 프로젝트 개요

이 프로젝트는 Unity 3D로 만드는 `Dino Grow 3D`다.

플레이어는 3인칭 시점에서 공룡을 조작한다. 플레이어보다 레벨이 낮거나 같은 공룡은 먹을 수 있고, 플레이어보다 레벨이 높은 공룡과 부딪히면 위험 상황이 된다. 공룡을 먹으면 EXP를 얻고, EXP가 100 이상이면 레벨업한다. 장기 목표는 레벨 20에 도달하는 것이다.

현재 프로젝트는 초기 문서와 달리 하트 UI, 하트 드롭, 피격 쿨다운, 스테이지 전환, 여러 맵 데이터가 이미 들어와 있다. 앞으로 작업할 때는 "원래 없던 시스템"으로 되돌리기보다, 현재 구현을 기준으로 책임을 작게 나누고 성능과 구조를 정리한다.

## 현재 게임 규칙

- 플레이어 레벨 >= 적 공룡 레벨: 먹기 성공
- 플레이어 레벨 < 적 공룡 레벨: 위험 충돌
- 같은 레벨 공룡은 먹을 수 있다.
- EXP 최대치는 100이다.
- 획득 EXP는 `먹은 공룡 레벨 x 10`이다.
- EXP가 100 이상이면 100을 소모하고 레벨이 1 오른다.
- EXP가 100을 넘으면 남은 EXP를 유지한다.
- 레벨업 처리는 `while currentExp >= 100 && level < 20` 방식으로 한다.
- 레벨 20이 되는 순간 클리어 처리한다.
- 레벨 20 이후 EXP는 더 이상 성장에 사용하지 않는다.

## 현재 하트 / 피격 규칙

현재 빌드에는 하트 시스템이 있다.

- `GameHudHeartUI`가 현재 하트 수와 최대 하트 수를 표시한다.
- 플레이어가 더 높은 레벨의 적에게 물리면 하트를 1개 잃는다.
- 하트가 없거나 하트 차감에 실패하면 게임오버된다.
- 하트 피격에는 `damageContactCooldown`을 둔다.
- 적을 먹으면 일정 확률로 하트 드롭이 생성될 수 있다.
- 하트 드롭은 `HeartPickup`, `HeartDropMotion`, `HeartDropSpawnService`가 담당한다.

주의:

- 하트 시스템은 현재 구현된 기능이므로 무단 삭제하지 않는다.
- 하트 시스템을 확장할 때도 먹기 판정, EXP 계산, 레벨업 로직과 섞지 않는다.
- 하트는 실수 완충 장치이고, 핵심 판단은 여전히 "먹을 수 있나, 도망가야 하나"다.
- 방어력, 복잡한 피해량 공식, 무적 아이템, 회복 아이템 종류 확장은 아직 우선순위가 낮다.

## 핵심 아키텍처 방향

이 프로젝트는 VContainer를 사용한다.

목표는 Unity 씬 검색에 기대지 않는 구조다. 게임 로직에서 아래 코드는 사용하지 않는다.

- `FindObjectOfType`
- `FindAnyObjectByType`
- `FindFirstObjectByType`
- `GameObject.Find`
- `Camera.main`
- gameplay용 전역 싱글톤 `Instance`
- 숨겨진 씬 검색으로 의존성 찾기

대신 아래 방식을 사용한다.

- Core 게임 규칙은 순수 C# 클래스로 작성하고 VContainer로 주입한다.
- MonoBehaviour는 Unity 입력, 충돌, Transform, Rigidbody, Animator 같은 Unity 연결만 담당한다.
- 씬에서 직접 연결해야 하는 Unity 컴포넌트는 `[SerializeField]`를 사용한다.
- UI, 사운드, 이펙트, 카메라 반응은 EventBus를 통해 반응한다.
- 먹기 판정, EXP 계산, 레벨업, 게임오버/피격 판정은 이벤트로 숨기지 말고 직접 메서드 호출로 처리한다.
- 동적으로 생성하는 프리팹은 가능하면 `IObjectResolver.Instantiate` 또는 `IObjectPoolService`를 사용한다.
- 데이터, 엑셀 컨버터, 사운드 같은 매니저급 시스템은 가능한 인터페이스나 작은 서비스로 경계를 나눈다.

## 현재 폴더 구조

```text
Assets/Scripts
 ├─ Core
 │   ├─ Combat
 │   ├─ Data
 │   ├─ Enemy
 │   ├─ Growth
 │   └─ Stage
 ├─ Infrastructure
 │   ├─ Data
 │   ├─ DI
 │   ├─ Events
 │   └─ Pooling
 ├─ Gameplay
 │   ├─ Animation
 │   ├─ Enemy
 │   ├─ Items
 │   ├─ Player
 │   ├─ Spawning
 │   ├─ Stage
 │   └─ VFX
 ├─ UI
 └─ Camera
```

폴더 역할:

- `Core`: Unity 씬에 의존하지 않는 게임 규칙
- `Infrastructure`: DI, EventBus, 데이터 로딩, 풀링, 저장소
- `Gameplay`: 실제 씬 오브젝트와 MonoBehaviour
- `Gameplay/Items`: 하트 드롭, 픽업, 아이템성 오브젝트
- `UI`: HUD, 하트, EXP, 게임오버, 클리어 표시
- `Camera`: Cinemachine 제어, 빌보드, 환경 반응

## Core 로직

Core는 가능한 순수 C#으로 작성한다.

현재 핵심 Core 클래스:

- `EatResolver`
- `GrowthSystem`
- `StageRule`
- `PlayerProgress`
- `GameStateController`
- `EnemyBehaviorResolver`
- `EnemySpawnLevelRule`
- `EnemySpawnStatsRule`

먹기 판정은 단순해야 한다.

```csharp
if (playerLevel >= enemyLevel)
{
    // Eat
}
else
{
    // Hit or GameOver depending on heart state
}
```

## EatResolver

`EatResolver`는 플레이어와 적의 레벨을 비교해서 결과만 반환한다.

역할:

- 플레이어 레벨과 적 레벨 비교
- 먹기 성공 또는 위험 충돌 결과 결정

하지 말아야 할 일:

- 적 삭제
- EXP 지급
- 하트 차감
- UI 갱신
- 사운드 재생
- 씬 오브젝트 검색

현재 결과 타입:

```csharp
public enum EatResult
{
    Eat,
    GameOver
}
```

이름은 `GameOver`지만 현재 플레이어 컨트롤러에서는 하트가 있으면 즉시 게임오버가 아니라 피격으로 처리될 수 있다. enum 이름을 바꿀 경우 영향 범위를 확인하고 한 번에 정리한다.

## GrowthSystem

`GrowthSystem`은 EXP와 레벨업만 담당한다.

규칙:

- EXP 획득량은 `enemyLevel * 10`
- EXP가 100 이상이면 레벨업
- 남은 EXP는 유지
- 레벨 20이면 더 이상 레벨업하지 않음

하지 말아야 할 일:

- 적 삭제
- 하트 처리
- 게임오버 처리
- UI 직접 조작
- 사운드 직접 재생
- 카메라 직접 조작

레벨업 이후 UI, 사운드, 카메라 반응은 EventBus 이벤트로 알린다.

## GameStateController

게임 상태 전환을 담당한다.

현재 상태:

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

`GameStateController`는 거대한 `GameManager`가 되면 안 된다. 성장, 스폰, UI, 카메라, 사운드 책임을 가져오지 않는다.

## VContainer 사용 규칙

메인 게임 씬에는 `GameLifetimeScope`를 둔다.

위치:

```text
Assets/Scripts/Infrastructure/DI/GameLifetimeScope.cs
```

등록 또는 연결 대상 예시:

- `GameEventBus`
- `EatResolver`
- `GrowthSystem`
- `StageRule`
- `GameStateController`
- `DinoDataRepository`
- `SpawnDataRepository`
- `StageDataRepository`
- `PlayerGrowthDataRepository`
- `IObjectPoolService`
- `PlayerDinoController`
- `EnemySpawner`
- `GameHud`
- `GameHudHeartUI`
- `StageMapSceneLoader`
- `CameraReference`

규칙:

- 아무 게임 코드에서나 컨테이너를 직접 꺼내 쓰는 Service Locator 방식은 피한다.
- 가능하면 생성자 주입 또는 `[Inject]` 메서드 주입을 사용한다.
- 씬 컴포넌트는 명시적으로 등록하거나 `[SerializeField]`로 연결한다.
- 동적 프리팹 생성은 풀링 가능한 경우 `IObjectPoolService`를 우선 고려한다.

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
- 하트 UI 변경 반응

사용하지 말아야 할 곳:

- 먹을 수 있는지 판단
- EXP 계산
- 레벨업 가능 여부 계산
- 하트 차감 여부 결정
- 게임오버 여부 결정
- 스폰 규칙 결정

핵심 게임 규칙은 이벤트로 숨기지 말고 직접 호출로 명확하게 처리한다.

## 성능 / 최적화 규칙

현재 프로젝트는 맵 오브젝트, 적 AI, 레벨 텍스트, 하트 드롭, 스테이지 전환이 늘어나고 있으므로 성능을 항상 같이 본다.

우선순위:

- 런타임 코드에서 `Find*`, `GameObject.Find`, `Camera.main` 사용 금지
- 매 프레임 모든 적을 순회하는 UI 로직은 0.1~0.2초 주기 또는 이벤트 기반으로 낮추기
- `Physics.RaycastAll`은 반복 호출 구간에서 `RaycastNonAlloc`으로 교체 검토
- 적 레벨 텍스트 빌보드는 거리 기반 업데이트 또는 갱신 간격 적용
- 적 AI는 생각 주기, NavMesh 재경로 주기, 애니메이션 저품질 갱신을 적극 사용
- 스폰/디스폰은 풀링을 우선 사용
- `GetComponent`와 `GetComponents`는 Awake/초기화/상태 전환 시점에 캐싱하고 매 프레임 반복하지 않는다.
- `Update`, `FixedUpdate`, `LateUpdate`를 모두 쓰는 스크립트는 책임 분리 후보로 본다.
- 맵 청크, 프리팹, 머티리얼, 씬 에셋 변경은 자동 생성물과 수작업 변경을 구분한다.

## Cinemachine 사용 규칙

카메라는 Cinemachine을 메인으로 사용한다.

구조:

- `Main Camera`: 실제 렌더링 카메라, `CinemachineBrain` 보유
- `CinemachineCamera`: 플레이어를 Follow/LookAt
- `PlayerCameraTarget`: 플레이어 자식 오브젝트로 두는 추적 대상
- `CinemachineThirdPersonOrbit`: 현재 3인칭 추적 카메라 제어
- `BillboardToCamera`: UI/월드 텍스트가 카메라를 바라보게 하는 보조 컴포넌트

카메라 로직은 직접 `Camera.main` 검색에 의존하지 않는다. `CameraReference` 또는 명시적 `[SerializeField]` 참조를 사용한다.

레벨업 시 카메라 거리와 높이를 조정할 경우 추천값:

```text
Lv. 1~4   거리 6,  높이 4
Lv. 5~8   거리 7,  높이 4.5
Lv. 9~12  거리 8,  높이 5
Lv. 13~16 거리 9,  높이 6
Lv. 17~20 거리 10, 높이 7
```

스테이지별 맵 경계가 생기면 카메라가 맵 밖을 지나치게 보지 않도록 제한한다.

## 데이터 관리

게임 밸런스는 데이터 기반으로 관리한다.

예상 또는 현재 테이블:

- `DinoTable`
- `GrowthTable`
- `SpawnTable`
- `StageTable`

엑셀 컨버터 위치:

```text
Tools/Dino Game/Excel Converter
```

NPOI 기반 데이터 메뉴:

```text
Tools/Dino Game/Data/Create Dino Excel Template
Tools/Dino Game/Data/Convert Dino Excel To ScriptableObject
```

생성 데이터 위치:

```text
Assets/GameData/Generated
```

데이터 서비스 규칙:

- 엑셀 데이터 로직은 `IDataService` 인터페이스 뒤에 둔다.
- 기본 구현체는 `ExcelDataService`다.
- `ExcelDataService`는 NPOI로 `.xlsx` 파일을 읽고 쓴다.
- 에디터 메뉴는 진입점일 뿐이고 실제 로직은 데이터 서비스에 둔다.
- 엑셀에서 변환된 런타임 데이터는 ScriptableObject로 관리한다.
- 에디터 전용 `AssetDatabase` 코드는 `Assets/Editor` 또는 `Assets/Scripts/Editor` 아래에만 둔다.

데이터 규칙:

- 공룡 ID는 중복되면 안 된다.
- 공룡 레벨은 1~20 범위여야 한다.
- EXP 보상은 기본적으로 `level * 10`을 따른다.
- 이동속도는 0보다 커야 한다.
- 프리팹 참조는 비어 있으면 안 된다.

## 스테이지와 맵

현재 프로젝트에는 여러 맵과 스테이지 전환 코드가 있다. 특히 `map7`과 생성된 맵 청크 데이터가 활발히 변경되고 있다.

관련 구조:

```text
Assets/Scenes
 ├─ map7.unity
 └─ other map scenes

Assets/Scripts/Gameplay/Stage
 └─ StageMapSceneLoader.cs

Assets/GameData/Generated
 └─ Map7GroundChunks
```

맵 규칙:

- 시작 지점 근처에는 먹을 수 있는 공룡이 있어야 한다.
- 높은 레벨 공룡은 시작 지점 바로 옆에 두지 않는다.
- 플레이어가 레벨업으로 커져도 통로를 지나갈 수 있어야 한다.
- 숫자, 하트, EXP, 위험 경고 UI 가독성을 방해할 정도로 배경을 복잡하게 만들지 않는다.
- 대량 맵 오브젝트는 가능하면 청크, 결합 메시, 정적 배칭, LOD, 컬라이더 단순화를 고려한다.
- 자동 생성된 맵 에셋은 변경 범위가 크므로 커밋 전 의도된 변경인지 반드시 확인한다.

## 현재 우선순위

현재는 1차 프로토타입을 넘어, 플레이 가능한 구조를 다듬는 단계다.

우선순위:

1. 먹기/피격/하트/게임오버 규칙을 명확히 유지
2. 레벨, EXP, 하트, 위험 경고 UI 가독성 개선
3. 적 AI와 스폰 성능 개선
4. `EnemyWanderMovement`처럼 큰 MonoBehaviour 책임 분리
5. `RaycastAll`, 매 프레임 전체 순회, 반복 `GetComponents` 같은 런타임 비용 줄이기
6. 스테이지 전환과 로딩 오버레이 안정화
7. 맵별 스폰 밸런스 조정
8. 레벨 20 클리어 흐름 polish

나중으로 미룰 것:

- 복잡한 전투 스탯
- 방어력/공격력 공식
- 여러 종류의 회복 아이템
- 보스전
- 도감
- 스킨
- 고급 퀘스트 시스템

## 코딩 규칙

- 스크립트는 작고 역할이 명확해야 한다.
- 씬 검색보다 명시적인 참조와 DI를 사용한다.
- 게임 시스템에 static 상태를 남발하지 않는다.
- 단, `DinoEnemy.Active`처럼 성능상 명확한 런타임 레지스트리는 제한적으로 허용하되 책임을 작게 유지한다.
- 이름은 역할이 드러나게 작성한다.
- 주석은 꼭 필요한 경우에만 짧게 작성한다.
- 관련 없는 Unity 설정은 함부로 바꾸지 않는다.
- `Library`, `Temp`, `Logs`, `UserSettings`는 커밋하지 않는다.
- Unity 자동 생성 `.csproj`, `.sln`, `.slnx` 파일은 직접 편집하지 않는다.
- 인코딩은 UTF-8을 사용한다.

## 현재 게임 방향

플레이어가 항상 바로 판단할 수 있어야 한다.

```text
저 공룡을 먹을 수 있나?
아니면 도망가야 하나?
하트가 남아 있나?
```

모든 게임 로직, UI, 이펙트, 카메라는 이 판단을 쉽게 만드는 방향으로 설계한다.

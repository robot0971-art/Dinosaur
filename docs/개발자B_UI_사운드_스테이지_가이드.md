# 개발자(B) 전용 — UI & 사운드 & 스테이지 시스템 완전 정복 가이드

> **대상:** Unity 6에서 Dino Grow 3D의 UI, 사운드, 스테이지를 만드는 사람
> **난이도:** 진짜 초보자 기준 (모든 단계를 하나하나 설명)

---

## 📖 이 문서의 목적

이 문서는 **개발자 B**가 해야 할 모든 일을 **가장 작은 기능 단위**로 쪼개고,  
각 기능을 어떻게 Unity에서 만들고 테스트하는지 **초등학생도 따라 할 수 있게** 설명한다.

---

## 🧱 기초 지식 (꼭 읽고 시작하세요)

### 이 게임에서 UI가 하는 일

```
┌─────────────────────────────────────────┐
│  Lv. 5         EXP  70 / 100  ⭐ Stage 1│  ← HUD
│  ─────────────────────────────────────  │
│                                          │
│     [Lv.3]       👆 여기 적 머리 위     │
│      🦕 ← 먹으면 EXP +30 뜸             │
│                                          │
│  ═════════════════════════════════════  │
│  🟢 초록 = 먹을 수 있음                  │
│  🔵 파랑 = 같은 레벨 (먹을 수 있음)       │
│  🔴 빨강 = 위험! 게임오버!               │
└─────────────────────────────────────────┘
```

### VContainer와 EventBus가 뭐예요?

| 개념 | 설명 (쉽게) |
|---|---|
| **VContainer** | 물건(스크립트)을 서로 찾아주는 전화번호부 |
| **EventBus** | "야, EXP 올랐어!" 하고 소리치면 듣고 있는 애들이 반응함 |
| **[Inject]** | "나 이거 필요해!" 하고 VContainer에 말하는 방법 |

우리는 VContainer를 통해 Core 쪽(A개발자가 만듦)에서 보내는 이벤트를 받아서  
UI를 업데이트하고 사운드를 재생할 거다.

---

## 🎯 스테이지 시스템 이해하기

### 스테이지 3개 난이도 구성

| 스테이지 | 난이도 | 등장 공룡 레벨 | 적 AI | 설명 |
|---|---|---|---|---|
| **Stage 1** | ⭐ 쉬움 | Lv.1~5 | 정지형 + 느린 배회 | 초보자용, 시작하기 좋음 |
| **Stage 2** | ⭐⭐ 보통 | Lv.1~10 | 배회형 + 도망형 | 중급자용 |
| **Stage 3** | ⭐⭐⭐ 어려움 | Lv.1~20 | 추격형 + 보스형 | 고수용 |

> **지금은 Stage 1만 만듭니다!**  
> 하지만 나중에 Stage 2, 3을 쉽게 추가할 수 있도록 **StageData** ScriptableObject를 먼저 만들어요.

### 스테이지 데이터 구조

```csharp
// StageData.cs
// 이 파일 하나만 고치면 Stage 2, 3을 추가할 수 있어요!
[CreateAssetMenu(fileName = "NewStageData", menuName = "Dino/StageData")]
public class StageData : ScriptableObject
{
    public int stageNumber;          // 1, 2, 3
    public string stageName;         // "푸른 초원", "바위 숲" 등
    public Difficulty difficulty;    // 쉬움, 보통, 어려움
    public int minLevel;             // 등장 최소 레벨
    public int maxLevel;             // 등장 최대 레벨
    public AudioClip bgm;            // 이 스테이지의 배경음악
    public Color uiThemeColor;       // UI 테마 색상
}
    
public enum Difficulty
{
    Easy,      // 쉬움
    Normal,    // 보통
    Hard       // 어려움
}
```

---

# 📂 파트 1: UI 기본 세팅 (Day 1 작업)

---

## 기능 1: UI 캔버스 만들기

### 📋 이 기능이 하는 일
게임의 모든 UI(레벨, EXP바, 버튼 등)를 그릴 종이(Canvas)를 만든다.

### 🎮 Unity에서 만드는 방법

**Step 1:** Hierarchy 창에서 빈 공간에 마우스 오른쪽 버튼 클릭  
**Step 2:** `UI → Canvas` 선택  

**Step 3:** Canvas를 선택하고 Inspector 창에서 다음 값을 설정:

| 항목 | 설정할 값 | 이유 |
|---|---|---|
| **Render Mode** | `Screen Space - Overlay` | 화면 위에 UI를 그림 |
| **UI Scale Mode** | `Scale With Screen Size` | 화면 크기가 바뀌어도 UI 크기 유지 |
| **Reference Resolution** | X=`1920`, Y=`1080` | 기준 해상도 |
| **Screen Match Mode** | `0.5` | 가로세로 비율에 맞게 |

### ✅ 테스트 방법
1. 게임 실행 (Play 버튼)
2. Game 창 크기를 여기저기 늘렸다 줄였다 해본다
3. Canvas가 화면에 잘 붙어 있으면 성공

---

## 기능 2: EventBus 이벤트 구독 준비

### 📋 이 기능이 하는 일
A 개발자가 "EXP 올랐어!" 하고 알려주면 우리 UI가 알아서 업데이트되도록 귀를 기울이는 것.

### 🔌 필요한 스크립트 (A 개발자가 만듦)
- `GameEventBus` 클래스
- 이벤트 종류:
  - `ExpChanged(int currentExp)` - EXP 바뀜
  - `LevelChanged(int newLevel)` - 레벨 바뀜  
  - `EatSuccess(int expGained)` - 먹기 성공
  - `GameOver` - 게임오버
  - `GameClear` - 클리어
  - `StageCleared` - 스테이지 클리어
  - `LevelUp` - 레벨업

### 🎮 우리가 할 일
모든 UI 스크립트에서 EventBus를 받을 준비를 한다.

```csharp
// 모든 UI 스크립트 위에 쓸 코드 (예시)
public class GameHud : MonoBehaviour
{
    private GameEventBus _eventBus;
    
    [Inject]
    public void Construct(GameEventBus eventBus)
    {
        _eventBus = eventBus;
        // 이벤트 구독 (귀 기울이기)
        _eventBus.ExpChanged += OnExpChanged;
        _eventBus.LevelChanged += OnLevelChanged;
    }
    
    private void OnExpChanged(int currentExp)
    {
        // EXP가 바뀌면 여기가 실행됨
    }
    
    private void OnLevelChanged(int newLevel)
    {
        // 레벨이 바뀌면 여기가 실행됨
    }
}
```

---

# 📂 파트 2: 플레이어 HUD (Day 2~3 작업)

---

## 기능 3: GameHud 오브젝트 만들기

### 📋 이 기능이 하는 일
게임 화면에 항상 보이는 정보(레벨, EXP바)를 보여주는 UI 묶음.

### 🎮 Hierarchy 구조

```
Canvas
└── GameHud (빈 게임오브젝트)
    ├── LevelText (TextMeshPro - Text)
    │   내용: "Lv. 1"
    │   위치: 왼쪽 위
    │   폰트 크기: 48
    │   색상: 흰색
    │
    ├── ExpBarPanel (Image - 반투명 배경)
    │   ├── ExpBarLabel (TextMeshPro)
    │   │   내용: "EXP"
    │   ├── ExpBarFill (Slider)
    │   │   ├── Fill Area
    │   │   │   └── Fill (Image - 초록색)
    │   │   └── Handle Slide Area (지워도 됨)
    │   └── ExpText (TextMeshPro)
    │       내용: "0 / 100"
    │
    └── GuideText (TextMeshPro)
        내용: "🟢 내 레벨 이하 공룡을 먹으세요!"
```

### 📝 만드는 순서

**Step 1:** Canvas 선택 → 마우스 우클릭 → `Create Empty` → 이름을 `GameHud`로 바꿈

**Step 2:** GameHud 선택 → 우클릭 → `UI → Text - TextMeshPro` → 이름 `LevelText`
- Inspector에서 다음 설정:
  - Text: `Lv. 1`
  - Font Size: `48`
  - Color: 하얀색 (`#FFFFFF`)
  - Alignment: 왼쪽 정렬
  - Rect Transform: Pos X=`30`, Pos Y=`-30`, Width=`200`, Height=`60`

**Step 3:** GameHud 선택 → 우클릭 → `UI → Slider` → 이름 `ExpBarFill`
- Slider 컴포넌트:
  - Min Value: `0`
  - Max Value: `100`
  - Value: `70` (테스트용)
  - Direction: `Left To Right`
- 자식 중 `Handle Slide Area`는 삭제 (Handle 안 씀)
- Fill Area → Fill의 Image 색상: 초록 (`#00FF44`)

**Step 4:** GameHud → 우클릭 → `UI → Text - TextMeshPro` → 이름 `ExpText`
- Text: `0 / 100`
- Font Size: `24`
- Rect Transform: ExpBarFill 옆에 붙임

**Step 5:** GameHud → 우클릭 → `UI → Text - TextMeshPro` → 이름 `GuideText`
- Text: `🟢 내 레벨 이하 공룡을 먹으세요!`
- Font Size: `20`
- Color: 노란색 (`#FFDD00`)
- Rect Transform: Pos Y=`-80`, 가운데 정렬
- Alignment: 가운데 정렬

### ✅ 테스트 방법
1. Play 버튼 누름
2. 화면 왼쪽 위에 "Lv. 1"이 보이는가?
3. EXP바가 보이는가? (초록색 막대)
4. 안내 문구가 보이는가?

---

## 기능 4: GameHud 스크립트 만들기

### 📋 이 기능이 하는 일
EventBus에서 보내는 신호를 받아서 HUD의 숫자와 막대를 실시간으로 업데이트한다.

### 📁 만드는 순서

**Step 1:** `Assets/Scripts/UI/` 폴더 생성  
(폴더가 이미 있으면 그냥 사용)

**Step 2:** 폴더에서 마우스 우클릭 → `Create → C# Script` → 이름 `GameHud`

**Step 3:** `GameHud` 스크립트를 더블클릭해서 열고 아래 코드를 **모두 복사해서 붙여넣기**:

```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class GameHud : MonoBehaviour
{
    [Header("🔗 인스펙터에서 연결하세요")]
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private Slider _expBar;
    [SerializeField] private TextMeshProUGUI _expText;
    [SerializeField] private TextMeshProUGUI _guideText;

    private GameEventBus _eventBus;
    private StageManager _stageManager;

    [Inject]
    public void Construct(GameEventBus eventBus, StageManager stageManager)
    {
        _eventBus = eventBus;
        _stageManager = stageManager;

        _eventBus.LevelChanged += OnLevelChanged;
        _eventBus.ExpChanged += OnExpChanged;
        _eventBus.EatSuccess += OnEatSuccess;
        _eventBus.GameOver += OnGameOver;
        _eventBus.GameClear += OnGameClear;
        _eventBus.StageCleared += OnStageCleared;
    }

    private void Start()
    {
        // 처음 시작할 때 UI 초기화
        _levelText.text = "Lv. 1";
        _expBar.value = 0;
        _expText.text = "0 / 100";
        _guideText.gameObject.SetActive(true);
    }

    private void OnLevelChanged(int newLevel)
    {
        _levelText.text = $"Lv. {newLevel}";
    }

    private void OnExpChanged(int currentExp)
    {
        _expBar.value = currentExp;
        _expText.text = $"{currentExp} / 100";
    }

    private void OnEatSuccess(int expGained)
    {
        // 먹기 성공! 안내 문구 잠깐 숨김
        Invoke(nameof(HideGuide), 2f);
    }

    private void HideGuide()
    {
        if (_guideText != null)
            _guideText.gameObject.SetActive(false);
    }

    private void OnGameOver()
    {
        _guideText.text = "💀 게임오버!";
        _guideText.gameObject.SetActive(true);
    }

    private void OnGameClear()
    {
        _guideText.text = "🎉 레벨 20 달성! 클리어!";
        _guideText.gameObject.SetActive(true);
    }

    private void OnStageCleared()
    {
        _guideText.text = "⭐ 스테이지 클리어!";
        _guideText.gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        if (_eventBus != null)
        {
            _eventBus.LevelChanged -= OnLevelChanged;
            _eventBus.ExpChanged -= OnExpChanged;
            _eventBus.EatSuccess -= OnEatSuccess;
            _eventBus.GameOver -= OnGameOver;
            _eventBus.GameClear -= OnGameClear;
            _eventBus.StageCleared -= OnStageCleared;
        }
    }
}
```

**Step 4:** GameHud 오브젝트에 이 스크립트를 **드래그해서 붙임**

### 🔌 인스펙터 연결 방법

GameHud 오브젝트를 선택하면 Inspector 창에 `Game Hud (Script)`가 보인다.
거기에 아래처럼 연결:

| 필드 이름 | 드래그해서 넣을 것 |
|---|---|
| **Level Text** | Hierarchy에서 `LevelText` 오브젝트 |
| **Exp Bar** | Hierarchy에서 `ExpBarFill` 오브젝트 (Slider 컴포넌트가 있는 거) |
| **Exp Text** | Hierarchy에서 `ExpText` 오브젝트 |
| **Guide Text** | Hierarchy에서 `GuideText` 오브젝트 |

> ⚠️ 팁: Hierarchy에서 오브젝트 이름 옆에 👁️ 아이콘을 클릭하면 잠깐 숨길 수 있어요.  
> 연결할 때는 오브젝트를 보이게 해야 드래그가 됩니다.

### ✅ 테스트 방법
1. Play 버튼 누름
2. "Lv. 1"이 보이는가?
3. EXP바가 0/100으로 보이는가? (값을 70으로 바꿔보고 Slider가 움직이는지 확인)
4. 안내 문구가 보이는가?

---

## 기능 5: 스테이지 정보 UI

### 📋 이 기능이 하는 일
화면에 현재 스테이지 번호와 난이도를 표시한다.  
(예: "Stage 1 | ⭐ 쉬움")

### 🎮 Hierarchy에 추가

```
GameHud (이미 있는 오브젝트)
└── StageInfoText (TextMeshPro - 새로 만듦)
    내용: "Stage 1 | ⭐ 쉬움"
    위치: 오른쪽 위
    폰트 크기: 28
```

### 📝 만드는 순서

**Step 1:** GameHud 선택 → 우클릭 → `UI → Text - TextMeshPro` → 이름 `StageInfoText`

**Step 2:** Inspector 설정:
- Text: `Stage 1 | ⭐ 쉬움`
- Font Size: `28`
- Color: 하얀색
- Alignment: 오른쪽 정렬
- Rect Transform: Pos X=`-30`, Pos Y=`-30`, Width=`250`, Height=`50`
- Anchor: 우측 상단 (앵커 프리셋에서 오른쪽 위 선택)

### 📁 StageManager 스크립트 만들기

**Step 1:** `Assets/Scripts/UI/` 폴더에 새 C# 스크립트 → 이름 `StageManager`

```csharp
using UnityEngine;
using VContainer;

public class StageManager : MonoBehaviour
{
    [Header("🎯 현재 스테이지 설정")]
    [SerializeField] private StageData _currentStage;
    
    [Header("🔗 UI 연결")]
    [SerializeField] private TextMeshProUGUI _stageInfoText;

    private GameEventBus _eventBus;

    public StageData CurrentStage => _currentStage;

    [Inject]
    public void Construct(GameEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    private void Start()
    {
        if (_currentStage != null)
        {
            UpdateStageUI();
        }
    }

    private void UpdateStageUI()
    {
        string difficultyName = _currentStage.difficulty switch
        {
            Difficulty.Easy => "쉬움",
            Difficulty.Normal => "보통",
            Difficulty.Hard => "어려움",
            _ => "???",
        };
        
        string difficultyStars = _currentStage.difficulty switch
        {
            Difficulty.Easy => "⭐",
            Difficulty.Normal => "⭐⭐",
            Difficulty.Hard => "⭐⭐⭐",
            _ => "",
        };
        
        _stageInfoText.text = $"Stage {_currentStage.stageNumber} | {difficultyStars} {difficultyName}";
    }

    // 다음 스테이지로 이동 (나중에 Stage 2, 3 추가할 때 사용)
    public void GoToNextStage()
    {
        // 여기는 나중에 Stage 2, 3 만들 때 채움
        Debug.Log("다음 스테이지로 이동!");
    }
}
```

**Step 2:** StageManager 스크립트를 **GameHud 오브젝트에 붙임**

### 🔌 인스펙터 연결 방법

| 필드 이름 | 드래그해서 넣을 것 |
|---|---|
| **Current Stage** | 아래에서 만들 Stage1_Data (ScriptableObject) |
| **Stage Info Text** | Hierarchy에서 `StageInfoText` 오브젝트 |

### 📁 StageData ScriptableObject 만들기

**Step 1:** `Assets/Scripts/UI/` 폴더에 새 C# 스크립트 → 이름 `StageData`

```csharp
using UnityEngine;

public enum Difficulty
{
    Easy,    // 쉬움  = 0
    Normal,  // 보통  = 1
    Hard     // 어려움 = 2
}

[CreateAssetMenu(fileName = "NewStageData", menuName = "Dino/StageData")]
public class StageData : ScriptableObject
{
    [Header("📌 스테이지 기본 정보")]
    public int stageNumber = 1;
    public string stageName = "푸른 초원";
    public Difficulty difficulty = Difficulty.Easy;

    [Header("🎯 공룡 레벨 범위")]
    public int minLevel = 1;
    public int maxLevel = 5;

    [Header("🎵 배경음악")]
    public AudioClip bgm;

    [Header("🎨 UI 테마 색상")]
    public Color uiThemeColor = Color.green;
}
```

### 📁 Stage 1 데이터 에셋 만들기

**Step 1:** Project 창에서 마우스 우클릭  
**Step 2:** `Create → Dino → StageData` 선택  
**Step 3:** 이름을 `Stage1_Data`로 변경  

**Step 4:** Inspector에서 설정:

| 필드 | 값 |
|---|---|
| **Stage Number** | `1` |
| **Stage Name** | `푸른 초원` |
| **Difficulty** | `Easy` (0) |
| **Min Level** | `1` |
| **Max Level** | `5` |
| **BGM** | (나중에 연결, 비워둬도 됨) |
| **UI Theme Color** | 초록색 (`#44FF44`) |

### ✅ 테스트 방법
1. StageManager의 `Current Stage`에 `Stage1_Data`가 연결되어 있는지 확인
2. Play 버튼 누름
3. 화면 오른쪽 위에 `Stage 1 | ⭐ 쉬움`이 보이는가?
4. 텍스트가 제대로 보이는 위치인가?

---

# 📂 파트 3: 적 레벨 표시 (Day 3~4 작업)

---

## 기능 6: EnemyLevelLabel - 적 머리 위 레벨 표시

### 📋 이 기능이 하는 일
적 공룡 머리 위에 `Lv. 4` 같은 숫자를 띄워서 플레이어가 먹을 수 있는지 바로 알게 한다.

### 🎮 만드는 순서

**Step 1:** Project 창에서 마우스 우클릭 → `Create → UI Toolkit → Canvas` 대신,  
Hierarchy에서 마우스 우클릭 → `UI → Canvas`를 만든다  
(1번 기능에서 만든 메인 Canvas와는 별도로, 이건 World Space로 설정)

**Step 2:** 새 Canvas 이름을 `EnemyLevelCanvas`로 변경

**Step 3:** Inspector에서 설정:

| 항목 | 값 |
|---|---|
| **Render Mode** | `World Space` |
| **Event Camera** | Main Camera (드래그해서 연결) |
| **Width** | `2` |
| **Height** | `1` |

**Step 4:** EnemyLevelCanvas 선택 → 우클릭 → `UI → Text - TextMeshPro` → 이름 `LevelLabel`

**Step 5:** LevelLabel Inspector 설정:
- Text: `Lv. 4`
- Font Size: `12`
- Color: 흰색
- Alignment: 가운데 정렬

### 🔄 프리팹으로 만들기 (재사용)

**Step 1:** Hierarchy에서 `EnemyLevelCanvas`를 Project 창으로 드래그  
**Step 2:** 프리팹 이름을 `EnemyLevelLabel`로 변경  
**Step 3:** Hierarchy의 원본은 삭제 (프리팹이 Project에 저장됨)

### 📁 EnemyLevelLabel 스크립트

```csharp
using TMPro;
using UnityEngine;

public class EnemyLevelLabel : MonoBehaviour
{
    [Header("🔗 자동 연결됨")]
    private TextMeshProUGUI _levelText;
    private Canvas _canvas;
    private Camera _mainCamera;

    private void Awake()
    {
        // 자식에서 TextMeshPro 찾기
        _levelText = GetComponentInChildren<TextMeshProUGUI>();
        _canvas = GetComponent<Canvas>();
        
        // Canvas 설정
        _canvas.renderMode = RenderMode.WorldSpace;
        
        // 메인 카메라 찾기
        _mainCamera = Camera.main;
        if (_mainCamera != null)
            _canvas.worldCamera = _mainCamera;
    }

    private void Start()
    {
        // Canvas 크기 설정
        RectTransform rect = _canvas.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(2f, 1f);
    }

    private void Update()
    {
        // 항상 카메라를 바라보도록 (Billboard 효과)
        if (_mainCamera != null)
        {
            transform.LookAt(transform.position + _mainCamera.transform.rotation * Vector3.forward,
                            _mainCamera.transform.rotation * Vector3.up);
        }
    }

    // 적 레벨 설정 (적 스크립트에서 호출)
    public void SetLevel(int level)
    {
        if (_levelText != null)
        {
            _levelText.text = $"Lv. {level}";
        }
    }

    // 색상 설정 (초록/파랑/빨강)
    public void SetColor(Color color)
    {
        if (_levelText != null)
        {
            _levelText.color = color;
        }
    }
}
```

### 🔗 다른 개발자가 적 프리팹에 붙이는 방법

이 스크립트는 **C 개발자**가 적 프리팹을 만들 때 사용한다.  
우리는 프리팹만 만들어서 전달하면 된다.

**전달할 것:**
1. `EnemyLevelLabel` 프리팹
2. `EnemyLevelLabel.cs` 스크립트

**C 개발자가 할 일:**
1. 적 프리팹의 자식으로 `EnemyLevelLabel` 프리팹을 넣음
2. 위치를 머리 위로 조정 (Y=1.5 정도)
3. EnemyDinoController에서 `GetComponentInChildren<EnemyLevelLabel>().SetLevel(enemyLevel)` 호출

### ✅ 테스트 방법
1. C 개발자가 적 프리팹에 붙인 후에 테스트 가능
2. 적 공룡 머리 위에 `Lv. 숫자`가 보이는가?
3. 카메라를 돌려도 글자가 항상 정면을 바라보는가? (Billboard 효과)

---

## 기능 7: 적 레벨 색상 시스템

### 📋 이 기능이 하는 일
플레이어 레벨과 비교해서 적 레벨 텍스트 색상을 바꾼다.

| 상황 | 색상 | 의미 |
|---|---|---|
| 적 레벨 < 플레이어 레벨 | 🟢 초록 | 안전하게 먹기 가능 |
| 적 레벨 = 플레이어 레벨 | 🔵 파랑 | 먹을 수 있음 |
| 적 레벨 > 플레이어 레벨 | 🔴 빨강 | 위험! 게임오버! |

### 📁 이 기능은 어떻게 구현하나요?

이건 **C 개발자**가 EnemyDinoController에서 처리할 내용이지만,  
우리는 **색상 데이터**와 **확인 방법**을 제공한다.

```csharp
// 이 코드는 C 개발자의 EnemyDinoController에 들어감
// 우리는 알려주기만 하면 됨

using UnityEngine;

public class EnemyLevelColorHelper
{
    // 플레이어 레벨과 적 레벨을 비교해서 색상 반환
    public static Color GetLevelColor(int playerLevel, int enemyLevel)
    {
        if (enemyLevel < playerLevel)
            return Color.green;      // 🟢 초록 - 먹을 수 있음
        else if (enemyLevel == playerLevel)
            return Color.blue;       // 🔵 파랑 - 같은 레벨
        else
            return Color.red;        // 🔴 빨강 - 위험!
    }
}
```

### 🎮 우리가 확인할 테스트 방법
1. 게임 실행
2. 플레이어보다 낮은 레벨 적 찾기 → 레벨 숫자가 **초록색**인가?
3. 플레이어와 같은 레벨 적 찾기 → 레벨 숫자가 **파란색**인가?
4. 플레이어보다 높은 레벨 적 찾기 → 레벨 숫자가 **빨간색**인가?

---

# 📂 파트 4: EXP 획득 표시 (Day 4 작업)

---

## 기능 8: ExpGainText - "+40" 떠다니는 텍스트

### 📋 이 기능이 하는 일
공룡을 먹었을 때 `EXP +40`이라는 숫자가 먹은 위치에서 위로 올라가며 사라지는 효과.

### 📁 ExpGainText 스크립트

```csharp
using TMPro;
using UnityEngine;

public class ExpGainText : MonoBehaviour
{
    [Header("⚙️ 설정")]
    [SerializeField] private float _floatSpeed = 1f;     // 올라가는 속도
    [SerializeField] private float _fadeDuration = 0.8f; // 사라지는 시간
    [SerializeField] private Color _textColor = Color.yellow; // 글자 색상

    private TextMeshPro _textMesh;
    private float _elapsedTime = 0f;

    private void Awake()
    {
        _textMesh = GetComponent<TextMeshPro>();
        if (_textMesh == null)
            _textMesh = gameObject.AddComponent<TextMeshPro>();
        
        _textMesh.color = _textColor;
        _textMesh.fontSize = 4;
        _textMesh.alignment = TextAlignmentOptions.Center;
    }

    public void Initialize(int expAmount)
    {
        _textMesh.text = $"EXP +{expAmount}";
        _textMesh.color = _textColor;
    }

    private void Update()
    {
        // 위로 올라가기
        transform.position += Vector3.up * _floatSpeed * Time.deltaTime;
        
        // 점점 투명해지기
        _elapsedTime += Time.deltaTime;
        float alpha = Mathf.Lerp(1f, 0f, _elapsedTime / _fadeDuration);
        Color color = _textMesh.color;
        color.a = alpha;
        _textMesh.color = color;
        
        // 다 사라지면 제거
        if (_elapsedTime >= _fadeDuration)
        {
            Destroy(gameObject);
        }
    }
}
```

### 📁 ExpGainText 프리팹 만들기

**Step 1:** Hierarchy → 우클릭 → `Create Empty` → 이름 `ExpGainText`
**Step 2:** `ExpGainText.cs` 스크립트 붙임
**Step 3:** 아래 컴포넌트 추가:
- `TextMeshPro` 컴포넌트 추가 (ExpGainText.cs가 자동으로 추가함)
- `RectTransform`은 필요 없음 (World Space에서 쓸 거라서)

**Step 4:** Hierarchy에서 Project 창으로 드래그 → 프리팹 생성 → 이름 `ExpGainText`

### 🔗 다른 개발자가 사용하는 방법

```csharp
// 이 코드는 A 개발자가 PlayerDinoController에서 사용함
// 우리는 프리팹만 전달하면 됨

// 먹기 성공했을 때 호출할 코드
void ShowExpGain(Vector3 enemyPosition, int expAmount)
{
    GameObject expText = Instantiate(expGainPrefab, enemyPosition, Quaternion.identity);
    expText.GetComponent<ExpGainText>().Initialize(expAmount);
}
```

### ✅ 테스트 방법
1. 게임 실행
2. 적 공룡과 충돌해서 먹기 성공
3. `EXP +40` 텍스트가 나타나는가?
4. 텍스트가 위로 올라가면서 점점 사라지는가?
5. 완전히 사라지면 오브젝트가 없어지는가? (Hierarchy 확인)

---

# 📂 파트 5: 게임오버 / 클리어 화면 (Day 4~5 작업)

---

## 기능 9: GameOverPanel - 게임오버 화면

### 📋 이 기능이 하는 일
높은 레벨 공룡과 충돌했을 때 나타나는 화면.  
화면이 붉게 변하고 "GAME OVER" 글자와 다시 시작 버튼이 보인다.

### 🎮 Hierarchy 구조

```
Canvas
└── GameOverPanel (기본적으로 비활성화 = 체크 해제)
    ├── Background (Image)
    │   색상: 검정, 반투명 (#000000, Alpha 0.7)
    │   전체 화면을 덮음
    │
    ├── GameOverText (TextMeshPro)
    │   내용: "GAME OVER"
    │   폰트 크기: 72
    │   색상: 빨강 (#FF0000)
    │
    ├── SubText (TextMeshPro)
    │   내용: "더 강한 공룡을 만났습니다..."
    │   폰트 크기: 24
    │
    └── RestartButton (Button)
        ├── 텍스트: "다시 시작"
        └── Button 컴포넌트
```

### 📝 만드는 순서

**Step 1:** Canvas 선택 → 우클릭 → `UI → Panel` → 이름 `GameOverPanel`

**Step 2:** GameOverPanel Inspector 설정:
- Image 색상: 검정, Alpha 0.7 (R=0, G=0, B=0, A=178)
- Rect Transform: Stretch로 설정 (앵커 프리셋에서 전체 채움 선택)

**Step 3:** ⚠️ **GameOverPanel 왼쪽에 있는 체크박스 해제** → 처음에는 안 보이게

**Step 4:** GameOverPanel 선택 → 우클릭 → `UI → Text - TextMeshPro` → 이름 `GameOverText`
- Text: `GAME OVER`
- Font Size: `72`
- Color: 빨강 (#FF0000)
- Alignment: 가운데 정렬
- Rect Transform: 가운데 정렬

**Step 5:** GameOverPanel 선택 → 우클릭 → `UI → Text - TextMeshPro` → 이름 `SubText`
- Text: `더 강한 공룡을 만났습니다...`
- Font Size: `24`
- Color: 흰색

**Step 6:** GameOverPanel 선택 → 우클릭 → `UI → Button` → 이름 `RestartButton`
- Button 자식의 TextMeshPro: `다시 시작`
- Button 위치: GameOverText 아래

### 📁 GameOverPanel 스크립트

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer;

public class GameOverPanel : MonoBehaviour
{
    [Header("🔗 인스펙터에서 연결")]
    [SerializeField] private GameObject _panel;      // GameOverPanel (전체)
    [SerializeField] private Button _restartButton;   // 다시 시작 버튼
    [SerializeField] private Image _flashImage;       // 붉게 변하는 효과용

    private GameEventBus _eventBus;

    [Inject]
    public void Construct(GameEventBus eventBus)
    {
        _eventBus = eventBus;
        _eventBus.GameOver += OnGameOver;
    }

    private void Start()
    {
        // 처음에는 숨김
        _panel.SetActive(false);
        if (_restartButton != null)
            _restartButton.onClick.AddListener(RestartGame);
    }

    private void OnGameOver()
    {
        // 게임오버 패널 보이기
        _panel.SetActive(true);
        
        // 붉게 변하는 효과 (Flash)
        if (_flashImage != null)
        {
            _flashImage.color = new Color(1f, 0f, 0f, 0.3f); // 반투명 빨강
            Invoke(nameof(ClearFlash), 0.3f);
        }
    }

    private void ClearFlash()
    {
        if (_flashImage != null)
            _flashImage.color = Color.clear; // 투명
    }

    private void RestartGame()
    {
        // 현재 씬 다시 로드 (게임 재시작)
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnDestroy()
    {
        if (_eventBus != null)
            _eventBus.GameOver -= OnGameOver;
    }
}
```

### 🔌 인스펙터 연결 방법

`GameOverPanel` 오브젝트에 `GameOverPanel.cs`를 붙인 후:

| 필드 이름 | 드래그해서 넣을 것 |
|---|---|
| **Panel** | `GameOverPanel` 자신 (Hierarchy에서 끌어다 넣음) |
| **Restart Button** | `RestartButton` (자식 오브젝트) |
| **Flash Image** | 새로 만든 `FlashImage` (아래 설명) |

**FlashImage 추가:**

1. Canvas 선택 → 우클릭 → `UI → Image` → 이름 `FlashImage`
2. Inspector에서 Image 색상: 투명 (Alpha=0)
3. RectTransform: 화면 전체를 덮도록 Stretch
4. Raycast Target: 체크 해제 (버튼 클릭 방해 안 하게)

### ✅ 테스트 방법
1. 게임 실행
2. 플레이어보다 높은 레벨 공룡과 부딪힘
3. 화면이 잠깐 붉게 변하는가?
4. "GAME OVER" 텍스트와 "다시 시작" 버튼이 보이는가?
5. "다시 시작" 버튼을 누르면 게임이 다시 시작되는가?

---

## 기능 10: LevelUpPopup - 레벨업 팝업

### 📋 이 기능이 하는 일
EXP가 100이 차서 레벨업했을 때 "레벨업!" 하고 축하 팝업을 띄운다.

### 🎮 Hierarchy 구조

```
Canvas
└── LevelUpPopup (기본적으로 비활성화)
    ├── Background (Image - 반투명)
    ├── LevelUpText (TextMeshPro)
    │   내용: "🎉 LEVEL UP!"
    ├── NewLevelText (TextMeshPro)
    │   내용: "Lv. 5 → Lv. 6"
    └── CloseButton (Button - "닫기")
```

### 📁 LevelUpPopup 스크립트

```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class LevelUpPopup : MonoBehaviour
{
    [Header("🔗 인스펙터 연결")]
    [SerializeField] private GameObject _popup;
    [SerializeField] private TextMeshProUGUI _newLevelText;

    private GameEventBus _eventBus;
    private int _currentLevel = 1;

    [Inject]
    public void Construct(GameEventBus eventBus)
    {
        _eventBus = eventBus;
        _eventBus.LevelChanged += OnLevelChanged;
    }

    private void Start()
    {
        _popup.SetActive(false);
    }

    private void OnLevelChanged(int newLevel)
    {
        _newLevelText.text = $"Lv. {_currentLevel} → Lv. {newLevel}";
        _currentLevel = newLevel;
        
        // 팝업 보여주기
        _popup.SetActive(true);
        
        // 2초 후 자동으로 닫기
        Invoke(nameof(ClosePopup), 2f);
    }

    private void ClosePopup()
    {
        _popup.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_eventBus != null)
            _eventBus.LevelChanged -= OnLevelChanged;
    }
}
```

### ✅ 테스트 방법
1. 게임 실행 (레벨업할 때까지 공룡을 먹음)
2. EXP가 100이 차면 "🎉 LEVEL UP!" 팝업이 뜨는가?
3. "Lv. 5 → Lv. 6" 같이 이전 레벨과 새 레벨이 표시되는가?
4. 2초 후에 자동으로 사라지는가?

---

## 기능 11: StageClearPanel - 스테이지 클리어 화면

### 📋 이 기능이 하는 일
스테이지의 목표 레벨에 도달하면 (Stage 1은 Lv.5) "스테이지 클리어!" 표시.

> 참고: 스테이지 1은 Lv.5까지 성장하면 클리어  
> (나중에 Stage 2는 Lv.10, Stage 3는 Lv.20)

### 🎮 Hierarchy 구조

```
Canvas
└── StageClearPanel (기본적으로 비활성화)
    ├── Background (Image - 반투명 검정)
    ├── ClearText (TextMeshPro - "⭐ STAGE CLEAR! ⭐")
    ├── DetailText (TextMeshPro - "Stage 1 클리어! Lv.5 달성!")
    └── NextStageButton (Button - "다음 스테이지")
        (Stage 1 이후에는 "게임 클리어!"로 표시)
```

### 📁 StageClearPanel 스크립트

```csharp
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer;

public class StageClearPanel : MonoBehaviour
{
    [Header("🔗 인스펙터 연결")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private TextMeshProUGUI _clearTitleText;
    [SerializeField] private TextMeshProUGUI _detailText;
    [SerializeField] private Button _nextButton;
    [SerializeField] private TextMeshProUGUI _buttonText;

    private GameEventBus _eventBus;
    private StageManager _stageManager;

    [Inject]
    public void Construct(GameEventBus eventBus, StageManager stageManager)
    {
        _eventBus = eventBus;
        _stageManager = stageManager;
        _eventBus.StageCleared += OnStageCleared;
    }

    private void Start()
    {
        _panel.SetActive(false);
        
        if (_nextButton != null)
            _nextButton.onClick.AddListener(OnNextButtonClick);
    }

    private void OnStageCleared()
    {
        _panel.SetActive(true);
        
        StageData currentStage = _stageManager.CurrentStage;
        
        _clearTitleText.text = $"⭐ STAGE CLEAR! ⭐";
        _detailText.text = $"Stage {currentStage.stageNumber} 클리어! " +
                          $"Lv.{currentStage.maxLevel} 달성!";
        
        // Stage 1이 마지막이므로 "게임 클리어" 버튼 텍스트
        _buttonText.text = "게임 클리어!";
    }

    private void OnNextButtonClick()
    {
        // 지금은 Stage 1만 있으므로 타이틀로 이동
        // 나중에 Stage 2가 추가되면 여기를 수정
        SceneManager.LoadScene("TitleScene");
    }

    private void OnDestroy()
    {
        if (_eventBus != null)
            _eventBus.StageCleared -= OnStageCleared;
    }
}
```

### ✅ 테스트 방법
1. 게임 실행
2. Stage 1 목표 레벨(Lv.5)까지 성장
3. "STAGE CLEAR!" 패널이 뜨는가?
4. "Stage 1 클리어! Lv.5 달성!"이 표시되는가?
5. 버튼을 누르면 타이틀 화면으로 가는가?

---

## 기능 12: GameClearPanel - 최종 클리어 화면 (Lv.20)

### 📋 이 기능이 하는 일
레벨 20에 도달하면 "🎉 GAME CLEAR! 🎉" 화면 표시.

### 🎮 Hierarchy 구조

```
Canvas
└── GameClearPanel (기본적으로 비활성화)
    ├── Background (Image - 반투명 금색)
    ├── ClearText (TextMeshPro - "🎉 GAME CLEAR! 🎉")
    ├── DetailText (TextMeshPro - "Lv.20 달성! 모든 공룡의 정상에 올랐습니다!")
    └── TitleButton (Button - "타이틀로")
```

### 📁 GameClearPanel 스크립트

```csharp
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer;

public class GameClearPanel : MonoBehaviour
{
    [Header("🔗 인스펙터 연결")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private TextMeshProUGUI _clearText;
    [SerializeField] private Button _titleButton;

    private GameEventBus _eventBus;

    [Inject]
    public void Construct(GameEventBus eventBus)
    {
        _eventBus = eventBus;
        _eventBus.GameClear += OnGameClear;
    }

    private void Start()
    {
        _panel.SetActive(false);
        
        if (_titleButton != null)
            _titleButton.onClick.AddListener(GoToTitle);
    }

    private void OnGameClear()
    {
        _panel.SetActive(true);
        _clearText.text = "🎉 GAME CLEAR! 🎉";
    }

    private void GoToTitle()
    {
        SceneManager.LoadScene("TitleScene");
    }

    private void OnDestroy()
    {
        if (_eventBus != null)
            _eventBus.GameClear -= OnGameClear;
    }
}
```

### ✅ 테스트 방법
1. (테스트용으로) LevelText에 "Lv. 20"이 표시되도록 만듦
2. 또는 A 개발자에게 레벨 20을 바로 테스트할 수 있는 치트를 만들어달라고 요청
3. GameClear 패널이 보이는가?
4. 버튼을 누르면 타이틀로 이동하는가?

---

# 📂 파트 6: 타이틀 화면 (Day 5 작업)

---

## 기능 13: TitleScreen

### 📋 이 기능이 하는 일
게임을 처음 켰을 때 보이는 화면.  
게임 로고와 "시작" 버튼이 있다.

### 🎮 새 Scene 만들기

**Step 1:** `File → New Scene` → 템플릿: `Basic (Built-in)`  
**Step 2:** `File → Save As` → 이름 `TitleScene` → `Assets/Scenes/` 폴더에 저장

### 🎮 Hierarchy 구조

```
TitleScene
├── EventSystem (자동 생성됨)
├── Main Camera
└── Canvas
    ├── BackgroundImage (Image - 전체 화면)
    │   공룡 실루엣 같은 배경 이미지 (나중에 추가)
    │
    ├── GameTitleText (TextMeshPro)
    │   내용: "Dino Grow 3D"
    │   폰트 크기: 96
    │   색상: 황금색 (#FFD700)
    │   위치: 위쪽 중앙
    │
    ├── SubTitleText (TextMeshPro)
    │   내용: "먹고, 성장하고, 정상에 서라!"
    │   폰트 크기: 32
    │
    ├── StartButton (Button)
    │   내용: "게임 시작"
    │   폰트 크기: 48
    │   위치: 중앙
    │
    └── VersionText (TextMeshPro)
        내용: "v1.0"
        위치: 오른쪽 아래
```

### 📁 TitleScreen 스크립트

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleScreen : MonoBehaviour
{
    [Header("🔗 인스펙터 연결")]
    [SerializeField] private Button _startButton;

    private void Start()
    {
        if (_startButton != null)
            _startButton.onClick.AddListener(StartGame);
    }

    private void StartGame()
    {
        // 게임 씬 로드
        SceneManager.LoadScene("GameScene");
    }
}
```

### 🎮 빌드 세팅 (Scene 등록)

**Step 1:** `File → Build Settings`  
**Step 2:** `Scenes In Build`에 두 Scene 등록:
1. `TitleScene` (Index 0)
2. `GameScene` (Index 1)

**Step 3:** TitleScene의 Build Index가 0인지 확인

### ✅ 테스트 방법
1. Play 버튼 누름 (TitleScene에서)
2. "Dino Grow 3D" 제목이 보이는가?
3. "게임 시작" 버튼을 누르면 GameScene으로 넘어가는가?

---

# 📂 파트 7: 스테이지 시스템 UI 통합 (Day 6~7)

---

## 기능 14: Stage 1 전용 설정

### 📋 이 기능이 하는 일
Stage 1의 모든 UI 관련 설정을 완료한다.

### 📝 체크리스트

- [ ] `Stage1_Data` ScriptableObject 만들어짐
- [ ] StageManager에 Stage1_Data 연결됨
- [ ] HUD에 `Stage 1 | ⭐ 쉬움` 표시됨
- [ ] Stage 1 배경음악 준비 (나중에)
- [ ] Stage 1 목표 레벨 = 5로 설정됨

### 🎮 Stage 1에서만 보이는 특징

| 요소 | Stage 1 설정 | 이유 |
|---|---|---|
| 등장 공룡 레벨 | Lv.1 ~ Lv.5 | 초보자용 |
| 적 AI | 정지형 + 느린 배회 | 너무 어렵지 않게 |
| 적 이동속도 | 느림 (2~3) | 초보자가 피하기 쉬움 |
| 스폰 개수 | 최대 8마리 | 너무 많지 않게 |
| 배경음악 | 잔잔한 음악 | 초원 분위기 |

---

## 기능 15: Stage 2 설정 (미래에 추가)

### 📋 이 기능이 하는 일
나중에 Stage 2를 추가할 때 이 문서를 보고 따라 하면 된다.

### 📝 추가 방법

**Step 1:** Project 창 → 우클릭 → `Create → Dino → StageData`  
**Step 2:** 이름 `Stage2_Data`, Inspector 설정:

| 필드 | 값 |
|---|---|
| **Stage Number** | `2` |
| **Stage Name** | `바위 숲` |
| **Difficulty** | `Normal` (1) |
| **Min Level** | `1` |
| **Max Level** | `10` |
| **UI Theme Color** | 파란색 |

**Step 3:** StageManager의 Current Stage를 `Stage2_Data`로 바꿈  
**Step 4:** 필요하면 BGM도 연결

---

## 기능 16: Stage 3 설정 (미래에 추가)

### 📝 추가 방법

**Step 1:** Project 창 → 우클릭 → `Create → Dino → StageData`  
**Step 2:** 이름 `Stage3_Data`, Inspector 설정:

| 필드 | 값 |
|---|---|
| **Stage Number** | `3` |
| **Stage Name** | `화산 평원` |
| **Difficulty** | `Hard` (2) |
| **Min Level** | `1` |
| **Max Level** | `20` |
| **UI Theme Color** | 빨간색 |

---

## 기능 17: Stage 1 난이도 세부 설정 (UI 관점)

### 📋 난이도가 UI에 미치는 영향

| UI 요소 | Stage 1 (쉬움) | Stage 2 (보통) | Stage 3 (어려움) |
|---|---|---|---|
| 난이도 표시 | ⭐ 쉬움 | ⭐⭐ 보통 | ⭐⭐⭐ 어려움 |
| UI 테마 색상 | 초록 | 파랑 | 빨강 |
| 안내 문구 | "천천히 성장하세요" | "조심히 탐험하세요" | "끝까지 살아남으세요!" |
| 게임오버 메시지 | "더 강한 공룡을 만났습니다" | "위험을 간과했습니다" | "정상은 멀었습니다..." |

### 📁 StageDifficultyDisplay 스크립트

```csharp
using TMPro;
using UnityEngine;

public class StageDifficultyDisplay : MonoBehaviour
{
    [Header("🔗 인스펙터 연결")]
    [SerializeField] private TextMeshProUGUI _guideText;
    [SerializeField] private StageManager _stageManager;

    private void Start()
    {
        if (_stageManager == null || _stageManager.CurrentStage == null)
            return;

        // 난이도에 따라 안내 문구 변경
        switch (_stageManager.CurrentStage.difficulty)
        {
            case Difficulty.Easy:
                _guideText.text = "🌿 천천히 성장하세요! 낮은 레벨 공룡부터 먹어보세요.";
                break;
            case Difficulty.Normal:
                _guideText.text = "🌲 조심히 탐험하세요! 위험한 공룡이 늘었습니다.";
                break;
            case Difficulty.Hard:
                _guideText.text = "🌋 끝까지 살아남으세요! 모두가 적입니다!";
                break;
        }
    }
}
```

---

# 📂 파트 8: 사운드 시스템 (Day 6~8 작업)

---

## 기능 18: SoundManager - 사운드 매니저

### 📋 이 기능이 하는 일
게임의 모든 효과음과 배경음악을 관리한다.  
"이 사운드 재생해줘!" 하면 알아서 틀어준다.

### 🎮 만드는 순서

**Step 1:** Hierarchy → 우클릭 → `Create Empty` → 이름 `SoundManager`

**Step 2:** SoundManager 선택 → Inspector에서 `Audio Source` 컴포넌트 2개 추가:
- `BgmSource` (배경음악용)
  - Loop: ✅ 체크
  - Play On Awake: ❌ 해제
  - Volume: `0.5`
- `SfxSource` (효과음용)
  - Loop: ❌ 해제
  - Play On Awake: ❌ 해제
  - Volume: `0.8`

### 📁 SoundManager 스크립트

```csharp
using UnityEngine;
using VContainer;

public class SoundManager : MonoBehaviour
{
    [Header("🎵 배경음악")]
    [SerializeField] private AudioSource _bgmSource;
    
    [Header("🔊 효과음")]
    [SerializeField] private AudioSource _sfxSource;
    
    [Header("🎯 효과음 클립")]
    [SerializeField] private AudioClip _eatSfx;        // 먹기 성공
    [SerializeField] private AudioClip _expGainSfx;     // EXP 획득
    [SerializeField] private AudioClip _levelUpSfx;     // 레벨업
    [SerializeField] private AudioClip _gameOverSfx;    // 게임오버
    [SerializeField] private AudioClip _clearSfx;       // 클리어
    [SerializeField] private AudioClip _buttonClickSfx; // 버튼 클릭

    private GameEventBus _eventBus;
    private StageManager _stageManager;

    [Inject]
    public void Construct(GameEventBus eventBus, StageManager stageManager)
    {
        _eventBus = eventBus;
        _stageManager = stageManager;

        // 이벤트 구독
        _eventBus.EatSuccess += OnEatSuccess;
        _eventBus.LevelUp += OnLevelUp;
        _eventBus.GameOver += OnGameOver;
        _eventBus.GameClear += OnGameClear;
        _eventBus.StageCleared += OnStageCleared;
    }

    private void Start()
    {
        // 스테이지 BGM 재생
        PlayStageBGM();
    }

    private void PlayStageBGM()
    {
        if (_stageManager != null && _stageManager.CurrentStage != null)
        {
            PlayBGM(_stageManager.CurrentStage.bgm);
        }
    }

    // 배경음악 재생
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || _bgmSource == null) return;
        
        if (_bgmSource.isPlaying)
            _bgmSource.Stop();
            
        _bgmSource.clip = clip;
        _bgmSource.Play();
    }

    // 효과음 재생
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || _sfxSource == null) return;
        _sfxSource.PlayOneShot(clip);
    }

    // --- 이벤트 핸들러 ---
    private void OnEatSuccess(int exp)
    {
        PlaySFX(_eatSfx);
    }

    private void OnLevelUp()
    {
        PlaySFX(_levelUpSfx);
    }

    private void OnGameOver()
    {
        PlaySFX(_gameOverSfx);
        if (_bgmSource != null)
            _bgmSource.Stop();
    }

    private void OnGameClear()
    {
        PlaySFX(_clearSfx);
    }

    private void OnStageCleared()
    {
        PlaySFX(_clearSfx);
    }

    // 버튼 클릭용 (Button에서 직접 연결)
    public void PlayButtonClick()
    {
        PlaySFX(_buttonClickSfx);
    }

    private void OnDestroy()
    {
        if (_eventBus != null)
        {
            _eventBus.EatSuccess -= OnEatSuccess;
            _eventBus.LevelUp -= OnLevelUp;
            _eventBus.GameOver -= OnGameOver;
            _eventBus.GameClear -= OnGameClear;
            _eventBus.StageCleared -= OnStageCleared;
        }
    }
}
```

### 🔌 인스펙터 연결 방법

SoundManager 오브젝트 선택:

| 필드 이름 | 드래그해서 넣을 것 |
|---|---|
| **Bgm Source** | SoundManager의 자식 중 `BgmSource` (AudioSource 컴포넌트) |
| **Sfx Source** | SoundManager의 자식 중 `SfxSource` (AudioSource 컴포넌트) |
| **Eat Sfx** | 먹기 효과음 파일 (.wav 또는 .mp3) |
| **Exp Gain Sfx** | EXP 획득 효과음 파일 |
| **Level Up Sfx** | 레벨업 효과음 파일 |
| **Game Over Sfx** | 게임오버 효과음 파일 |
| **Clear Sfx** | 클리어 효과음 파일 |
| **Button Click Sfx** | 버튼 클릭 효과음 파일 |

### 🔊 사운드 파일 준비 방법

**Step 1:** 인터넷에서 무료 효과음 다운로드 (freesound.org, Pixabay 등)

**Step 2:** 다운로드한 파일을 `Assets/Resources/Sounds/` 폴더에 넣음  
(폴더가 없으면 직접 만듦)

**Step 3:** 파일을 선택하고 Inspector에서 설정:
- Load Type: `Decompress On Load`
- Compression Format: `Vorbis` (품질 0.5 정도)
- **Force To Mono**: ✅ 체크 (모노가 게임 효과음에 좋음)

### ✅ 테스트 방법
1. Play 버튼 누름
2. Stage 1의 BGM이 재생되는가?
3. 공룡을 먹으면 `_eatSfx` 효과음이 나는가?
4. 레벨업하면 `_levelUpSfx` 효과음이 나는가?
5. 게임오버되면 `_gameOverSfx` 효과음이 나고 BGM이 멈추는가?
6. 클리어하면 `_clearSfx` 효과음이 나는가?

---

## 기능 19: 버튼에 클릭 사운드 연결

### 📋 이 기능이 하는 일
모든 버튼을 누를 때 "딸깍" 소리가 나게 한다.

### 🎮 연결 방법 (모든 버튼에 반복)

**Step 1:** Hierarchy에서 버튼 선택 (예: RestartButton)  
**Step 2:** Inspector에서 Button 컴포넌트 찾기  
**Step 3:** `OnClick()` 리스트에서 `+` 버튼 누름  
**Step 4:** Hierarchy의 `SoundManager` 오브젝트를 `None (Object)` 칸에 드래그  
**Step 5:** 함수 선택 드롭다운에서: `SoundManager → PlayButtonClick()` 선택

### ✅ 테스트 방법
1. 게임 실행
2. 모든 버튼을 하나씩 눌러봄
3. 각 버튼에서 "딸깍" 효과음이 나는가?

---

# 📂 파트 9: 난이도별 차이 완성 (Day 7~8)

---

## 기능 20: 난이도별 UI 색상 변화

### 📋 이 기능이 하는 일
스테이지 난이도에 따라 UI 색상이 바뀐다.

| 난이도 | 메인 색상 | 느낌 |
|---|---|---|
| 쉬움 | 🟢 초록 | 안전함, 편안함 |
| 보통 | 🔵 파랑 | 탐험, 도전 |
| 어려움 | 🔴 빨강 | 위험, 긴장 |

### 📁 UIEffectController 스크립트

```csharp
using UnityEngine;
using UnityEngine.UI;

public class UIEffectController : MonoBehaviour
{
    [Header("🔗 색상을 바꿀 UI 요소들")]
    [SerializeField] private Image[] _themeImages;     // 테마 색상을 받을 Image들
    [SerializeField] private StageManager _stageManager;

    private void Start()
    {
        if (_stageManager == null || _stageManager.CurrentStage == null)
            return;

        Color themeColor = _stageManager.CurrentStage.uiThemeColor;
        
        // 모든 Image의 색상을 테마 색상으로 변경
        foreach (Image img in _themeImages)
        {
            if (img != null)
            {
                Color originalColor = img.color;
                img.color = new Color(themeColor.r, themeColor.g, themeColor.b, originalColor.a);
            }
        }
    }
}
```

---

## 기능 21: 게임오버 화면 난이도별 차이

### 📋 게임오버 메시지 변경

| 난이도 | 게임오버 부제목 |
|---|---|
| 쉬움 (Stage 1) | "더 강한 공룡을 만났습니다..." |
| 보통 (Stage 2) | "위험을 간과했습니다..." |
| 어려움 (Stage 3) | "정상은 멀었습니다..." |

이 내용은 `GameOverPanel.cs`의 `Start()`에서 StageManager를 참조해서 변경하면 된다.

---

# 📂 파트 10: 모든 기능 테스트 체크리스트

---

## ✅ 최종 테스트 체크리스트

### UI 테스트

| 번호 | 테스트 항목 | 확인 |
|---|---|---|
| 1 | Canvas가 화면에 제대로 표시되는가? | [ ] |
| 2 | 왼쪽 위에 "Lv. 1"이 보이는가? | [ ] |
| 3 | EXP 바가 0/100으로 표시되는가? | [ ] |
| 4 | EXP 획득 시 EXP 바가 증가하는가? | [ ] |
| 5 | 오른쪽 위에 "Stage 1 | ⭐ 쉬움"이 보이는가? | [ ] |
| 6 | 적 머리 위에 "Lv. 숫자"가 보이는가? | [ ] |
| 7 | 적 레벨 색상이 초록/파랑/빨강으로 구분되는가? | [ ] |
| 8 | 공룡 먹으면 "EXP +40" 텍스트가 뜨는가? | [ ] |
| 9 | 게임오버 시 패널이 나타나는가? | [ ] |
| 10 | "다시 시작" 버튼이 동작하는가? | [ ] |
| 11 | 레벨업 팝업이 나타나는가? | [ ] |
| 12 | 스테이지 클리어 패널이 나타나는가? | [ ] |
| 13 | 타이틀 화면이 정상 표시되는가? | [ ] |
| 14 | "게임 시작" 버튼이 씬을 전환하는가? | [ ] |
| 15 | 모든 UI가 다른 해상도에서도 잘 보이는가? | [ ] |

### 사운드 테스트

| 번호 | 테스트 항목 | 확인 |
|---|---|---|
| 1 | Stage 1 BGM이 재생되는가? | [ ] |
| 2 | 공룡 먹기 성공 시 효과음이 나는가? | [ ] |
| 3 | 레벨업 시 효과음이 나는가? | [ ] |
| 4 | 게임오버 시 효과음이 나고 BGM이 멈추는가? | [ ] |
| 5 | 클리어 시 효과음이 나는가? | [ ] |
| 6 | 버튼 클릭 시 효과음이 나는가? | [ ] |
| 7 | 사운드가 너무 크거나 작지 않은가? | [ ] |

### Stage 1 전용 테스트

| 번호 | 테스트 항목 | 확인 |
|---|---|---|
| 1 | Stage 1에서 Lv.1~5 공룡만 등장하는가? | [ ] |
| 2 | Lv.5에 도달하면 Stage Clear가 뜨는가? | [ ] |
| 3 | Stage 1의 안내 문구가 "천천히 성장하세요"인가? | [ ] |
| 4 | UI 테마 색상이 초록색인가? | [ ] |

---

## 📦 최종 산출물 목록

### 개발자 B가 만들어서 전달할 것

```
📁 Assets/Scripts/UI/
    ├── GameHud.cs              (HUD 전체 관리)
    ├── GameOverPanel.cs         (게임오버 화면)
    ├── StageClearPanel.cs       (스테이지 클리어)
    ├── GameClearPanel.cs        (최종 클리어)
    ├── LevelUpPopup.cs          (레벨업 팝업)
    ├── ExpGainText.cs           (떠다니는 EXP 텍스트)
    ├── EnemyLevelLabel.cs       (적 레벨 표시)
    ├── SoundManager.cs          (사운드 관리)
    ├── StageManager.cs          (스테이지 관리)
    ├── StageData.cs             (스테이지 데이터)
    ├── TitleScreen.cs           (타이틀 화면)
    ├── UIEffectController.cs    (UI 효과)
    └── StageDifficultyDisplay.cs (난이도 표시)

📁 Assets/Resources/Sounds/
    ├── eat_sfx.wav              (먹기 효과음)
    ├── exp_gain_sfx.wav         (EXP 획득 효과음)
    ├── levelup_sfx.wav          (레벨업 효과음)
    ├── gameover_sfx.wav         (게임오버 효과음)
    ├── clear_sfx.wav            (클리어 효과음)
    ├── button_click_sfx.wav     (버튼 클릭 효과음)
    └── stage1_bgm.wav           (Stage 1 배경음악)

📁 Assets/Resources/StageData/
    ├── Stage1_Data.asset        (Stage 1 설정)
    ├── Stage2_Data.asset        (Stage 2 설정 - 미래)
    └── Stage3_Data.asset        (Stage 3 설정 - 미래)

📁 Assets/Scenes/
    ├── TitleScene.unity          (타이틀 씬)
    └── GameScene.unity           (게임 씬)
```

---

## ⚠️ 자주 하는 실수 TOP 5

### 실수 1: Canvas를 잊어버림
UI 요소는 반드시 Canvas 자식이어야 한다.  
Canvas 없이 TextMeshPro를 만들면 화면에 안 보인다.

### 실수 2: 프리팹 연결 안 함
Inspector에 `None`이라고 뜨는 건 아직 안 연결했다는 뜻.  
Hierarchy에서 오브젝트를 드래그해서 넣어야 한다.

### 실수 3: 이벤트 구독 해제 안 함
`OnDestroy()`에서 `-=`로 이벤트 구독을 해제하지 않으면  
씬을 다시 로드할 때 오류가 날 수 있다.

### 실수 4: activeSelf / SetActive 헷갈림
- `gameObject.SetActive(false)` → 오브젝트를 **숨김**
- `gameObject.activeSelf` → 현재 **보이는지** 확인
- 처음에 숨길 UI는 Inspector에서 체크 해제

### 실수 5: TextMeshPro 폰트가 안 보임
TextMeshPro는 처음에 폰트 에셋이 필요할 수 있다.  
`Window → TextMeshPro → Font Asset Creator`에서 기본 폰트를 만들거나  
Import TMP Essentials를 해야 한다.

> **💡 TMP Essentials 가져오기:**  
> Window 메뉴 → TextMeshPro → Import TMP Essential Resources

---

## 🎯 요약: 처음부터 이 순서로 만들면 됩니다

```
1일차: Canvas + 기본 설정
2일차: GameHud (LevelText, ExpBar) + StageInfo UI
3일차: HUD EventBus 연결 + EnemyLevelLabel 프리팹
4일차: GameOverPanel + ExpGainText
5일차: TitleScreen + StageClearPanel + GameClearPanel
6일차: SoundManager + 효과음 적용
7일차: StageData 완성 + BGM + 난이도별 UI 차이
8일차: 효과음 최종 적용 + UI 애니메이션
9일차: 전체 UI/사운드 QA
10일차: 최종 마무리
```

> **가장 중요한 원칙:**  
> **하나 만들고 → 테스트하고 → 다음으로 넘어가기**  
> 한 번에 여러 개 만들지 말 것!

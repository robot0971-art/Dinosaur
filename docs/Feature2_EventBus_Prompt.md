# 기능 2 프롬프트: EventBus 이벤트 구독 준비

아래 프롬프트를 그대로 복사해서 사용하세요.

```text
나는 초보자이고, Unity 6으로 3D 공룡 성장 게임(Dino Grow 3D)을 만들고 있어.

이번에는 기능 목록 2번인 "EventBus 이벤트 구독 준비"만 만들고 싶어.

참고 문서 위치:
- C:\Users\admin\Desktop\Dino\docs\AGENTS.md
- C:\Users\admin\Desktop\Dino\docs\개발자B_UI_사운드_스테이지_가이드.md
- C:\Users\admin\Desktop\Dino\docs\dino_3d_growth_game_design.md

작업 폴더:
- C:\Users\admin\Desktop\Dino

Unity 프로젝트 폴더:
- C:\Users\admin\Desktop\Dino

현재 상태:
- 기능 1번 UI 캔버스 만들기는 완료했어.
- Canvas가 화면에 잘 표시되고 있어.
- VContainer가 설치되어 있어 (필요하면 설치 방법 알려줘).
- 아직 GameEventBus 클래스가 없어.
- 아직 UI 스크립트들이 없어.
- A 개발자가 만든 Core 로직은 아직 없지만, EventBus 인터페이스는 먼저 만들 거야.

목표:
게임에서 발생하는 이벤트(레벨업, EXP 획득, 게임오버 등)를 다른 스크립트에게 알려주는 EventBus 시스템을 만들고 싶어.

조건:
- Unity 6 기준으로 설명해줘.
- 이번에는 기능 2번 "EventBus 이벤트 구독 준비"만 만들어줘.
- 기능 1번에서 만든 Canvas는 절대 망가뜨리지 말아줘.
- 초보자도 이해할 수 있게 쉬운 말로 설명해줘.
- 어려운 영어 용어가 나오면 한국말 뜻도 같이 설명해줘.
- 전체 코드를 빠짐없이 작성해줘.
- 코드마다 주석을 초보자도 알아보기 쉽게 달아줘.
- 어느 GameObject에 스크립트를 붙이는지 알려줘.
- Inspector에서 뭘 설정해야 하는지 알려줘.
- 자주 틀리는 부분도 알려줘.

규칙 (반드시 지켜줘):
1. 의존성 주입(DI) 필수 적용
   - GameObject.Find() 사용 금지
   - 하드코딩된 Tag 사용 금지
   - 외부 객체 참조는 반드시 [SerializeField] 또는 Interface 사용

2. C# 이벤트(event) 적극 활용
   - FSM 안에서 GameManager 등 코어 스크립트 직접 호출 금지
   - 보스 사망, 기믹 발동 등은 Action 또는 UnityEvent만 Invoke
   - 실제 처리는 코어 시스템이 구독해서 담당

3. SRP 준수 (단일 책임 원칙)
   - 하나의 스크립트에 합치지 말 것
   - EventBus는 이벤트 전달만 담당

4. 수정 범위 제한
   - 다른 코드는 건드리지 말고 필요한 부분만 최소 수정

EventBus 구현 방식:
- GameEventBus 클래스를 만들어줘.
- GameEventBus는 C#의 event와 delegate를 사용해줘.
- 싱글톤이 아니라 VContainer로 주입받는 방식으로 만들어줘.
- 다음 이벤트들을 포함해줘:
  - ExpChanged(int currentExp) - EXP가 바뀔 때
  - LevelChanged(int newLevel) - 레벨이 바뀔 때
  - EatSuccess(int expGained) - 먹기 성공했을 때
  - GameOver - 게임오버됐을 때
  - GameClear - 레벨 20 클리어했을 때
  - StageCleared - 스테이지 클리어했을 때
  - LevelUp - 레벨업했을 때

이벤트 구독 예시 스크립트:
- GameEventBus를 구독하는 예시 스크립트도 만들어줘.
- 예시 스크립트 이름은 EventBusSubscriberExample.cs로 해줘.
- 이 스크립트는 나중에 지워도 되는 참고용이야.
- [Inject]로 GameEventBus를 주입받는 방법을 보여줘.
- 이벤트 구독과 해제 방법을 보여줘.

코드 작성 조건:
- GameEventBus.cs를 새로 만들어줘.
- EventBusSubscriberExample.cs를 새로 만들어줘.
- Assets/Scripts/Infrastructure/Events/ 폴더에 만들어줘.
- [Header("한글 설명")]을 사용해줘.
- [Tooltip("초보자용 설명")]도 한글로 추가해줘.
- 변수 이름은 Unity/C# 규칙 때문에 영어로 유지해줘.
- 인스펙터에 보이는 설명은 초보자가 이해하기 쉽게 한글로 작성해줘.
- 코드 주석은 한글로 작성해줘.
- 어려운 문법보다 쉬운 문법으로 작성해줘.
- 이 함수는 언제 호출되는지 설명해줘.
- 이 변수가 어떤 역할인지 설명해줘.
- 코드 실행 흐름을 순서대로 알려줘.
- 초보자가 실수하기 쉬운 부분도 알려줘.

자동저장 조건:
- 기능 하나가 완성되면 Unity에서 Scene(씬)을 저장하라고 알려줘.
- Ctrl + S로 저장하는 방법도 알려줘.

이번 기능에서 원하는 쉬운 구현 방향:
- GameEventBus.cs를 새로 만들어줘.
- GameEventBus는 순수 C# 클래스야 (MonoBehaviour 아님).
- VContainer에서 싱글톤으로 등록할 수 있게 해줘.
- 각 이벤트는 C# event 키워드를 사용해줘.
- 이벤트 타입은 System.Action을 사용해줘.
- EventBusSubscriberExample.cs는 MonoBehaviour로 만들어줘.
- [Inject]로 GameEventBus를 주입받는 예시를 보여줘.
- OnEnable/OnDisable에서 이벤트 구독/해제하는 예시를 보여줘.

GameLifetimeScope 등록:
- GameLifetimeScope.cs에 GameEventBus를 등록하는 코드도 추가해줘.
- 기존 GameLifetimeScope.cs가 있으면 수정하고, 없으면 새로 만들어줘.
- Assets/Scripts/Infrastructure/DI/ 폴더에 있어야 해.

VContainer 설치 확인:
- VContainer가 이미 설치되어 있는지 확인하는 방법을 알려줘.
- 없으면 설치 방법도 알려줘.
- Package Manager에서 설치하는 방법을 알려줘.

반드시 아래 형식으로 답해줘:

1. 기능 설명
2. 전체 코드
3. 코드 설명
4. 유니티 적용 방법
5. 오류 체크 포인트
6. 초보자용으로 필요한 C# 스크립트 파일 이름
7. Inspector에서 조절할 변수
8. 테스트 성공 기준
9. 다음 기능으로 넘어가기 전 체크리스트

설명 스타일:
- 초등학생도 이해할 수 있게 차근차근 설명해줘.
- 코드만 알려주지 말고 코드 설명도 해줘.
- 한 줄씩 설명하고 설치 순서도 알려줘.
- 오브젝트 세팅 순서도 알려줘.
- 오류날 수 있는 부분도 알려줘.
- 함수별 설명을 해줘.
- 이 코드가 왜 이렇게 동작하는지 초보자도 이해하게 설명해줘.
- 실행 순서를 번호로 설명해줘.
- 변수, 함수, if문이 각각 무슨 역할인지 알려줘.
- 그림처럼 비유해서 설명해줘.
- 마지막에 내가 외워야 할 핵심만 짧게 뽑아줘.

주의:
- 이번에는 기능 2번만 만들어줘.
- 기능 1번에서 만든 Canvas는 건드리지 마.
- 아직 GameHud, GameOverPanel 등은 만들지 마.
- 아직 SoundManager는 만들지 마.
- 아직 StageManager는 만들지 마.
- GameEventBus 인터페이스와 예시 구독만 만들어줘.
```

## 사용 방법

1. 위 `text` 박스 안의 내용을 전부 복사합니다.
2. 다음 작업 요청으로 그대로 붙여넣습니다.
3. 기능 2번이 성공하면 다음에는 기능 3번 프롬프트를 만들면 됩니다.

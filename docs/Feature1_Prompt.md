# 기능 1 프롬프트: UI 캔버스 만들기

아래 프롬프트를 그대로 복사해서 사용하세요.

```text
나는 초보자이고, Unity 6으로 3D 공룡 성장 게임 "Dino Grow 3D"를 만들고 있어.

이번에는 기능 목록 1번인 "UI 캔버스 만들기"만 만들고 싶어.

참고 문서 위치:
- C:\Users\admin\Desktop\Dino\docs\dino_3d_growth_game_design.md
- C:\Users\admin\Desktop\Dino\docs\개발자B_UI_사운드_스테이지_가이드.md

작업 폴더:
- C:\Users\admin\Desktop\Dino\docs

Unity 프로젝트 폴더:
- C:\Users\admin\Desktop\Dino\Timproject\DinoUnity\Dino

현재 상태:
- Unity 프로젝트는 있지만 아직 아무 기능도 만들지 않았어.
- Assets 폴더에는 Scenes와 Settings만 있어.
- UI, 사운드, 스테이지 기능은 아직 하나도 없어.
- VContainer는 아직 설치하지 않았어.
- TextMeshPro는 아직 Import하지 않았을 수 있어.
- EventBus도 아직 없어.
- EventSystem도 아직 없어.

목표:
게임의 모든 UI(레벨 표시, EXP바, 버튼 등)를 그릴 Canvas를 만들고 싶어.
Canvas는 화면 위에 UI를 그리는 종이 같은 거야.

조건:
- Unity 6 3D 프로젝트 기준으로 설명해줘.
- 이번에는 기능 1번 "UI 캔버스 만들기"만 만들어줘.
- 기능 2번(EventBus)은 아직 만들지 마. 그건 다음에 만들 거야.
- 기능 3번(GameHud)도 아직 만들지 마. 그건 기능 2번 다음에 만들 거야.
- Canvas만 만들고, Canvas 안에 들어갈 UI 요소(텍스트, 버튼, 슬라이더 등)는 아직 만들지 마.
- 초보자도 이해할 수 있게 쉬운 말로 설명해줘.
- 어려운 영어 용어가 나오면 한국말 뜻도 같이 설명해줘.
- 전체 코드를 빠짐없이 작성해줘.
- 코드마다 주석을 초보자도 알아보기 쉽게 달아줘.
- 어느 GameObject에 스크립트를 붙이는지 알려줘.
- Inspector에서 뭘 설정해야 하는지 알려줘.
- 자주 틀리는 부분도 알려줘.

Canvas 설정 방식:
- Canvas는 Screen Space - Overlay로 만들어줘.
- Screen Space - Overlay는 화면 바로 위에 UI를 덮어쓰는 방식이라는 뜻이야.
- UI Scale Mode는 Scale With Screen Size로 해줘.
- Scale With Screen Size는 화면 크기에 맞춰서 UI 크기를 조절하는 방식이야.
- Reference Resolution은 1920 x 1080으로 해줘.
- Reference Resolution은 UI 크기의 기준이 되는 해상도야.
- Screen Match Mode는 0.5로 해줘.
- Screen Match Mode는 가로세로 비율 중간값을 맞추는 설정이야.
- Canvas 이름은 "GameCanvas"로 해줘.
- CanvasScaler 컴포넌트가 자동으로 붙게 해줘.
- CanvasScaler는 UI 크기를 화면에 맞게 조절하는 컴포넌트야.
- GraphicRaycaster 컴포넌트가 자동으로 붙게 해줘.
- GraphicRaycaster는 UI 버튼 클릭 같은 입력을 감지하는 컴포넌트야.

자동 세팅 방식:
- Editor 폴더에 MenuEditor 스크립트를 만들어서 Unity 메뉴에서 자동으로 Canvas를 세팅할 수 있게 해줘.
- 메뉴 경로는 "Dino > Setup UI Canvas"로 해줘.
- 메뉴를 누르면 GameCanvas가 자동으로 만들어지고 설정이 완료되게 해줘.
- 이미 GameCanvas가 있으면 새로 만들지 말고 "이미 있습니다"라고 알려줘.
- EventSystem도 자동으로 만들어줘.
- EventSystem은 UI 버튼 클릭 같은 입력을 처리하는 데 필요한 오브젝트야.
- EventSystem이 이미 있으면 만들지 마.

코드 작성 조건:
- Assets/Editor/ 폴더를 만들고 거기에 GameCanvasSetup.cs를 넣어줘.
- GameCanvasSetup.cs는 Editor 전용 스크립트라서 게임 실행 중에는 동작하지 않아.
- UnityEditor 네임스페이스를 사용해서 메뉴 버튼을 만들어줘.
- [MenuItem("Dino/Setup UI Canvas")]를 사용해서 메뉴 버튼을 만들어줘.
- 코드 주석은 한글로 작성해줘.
- 어려운 문법보다 쉬운 문법으로 작성해줘.
- 이 함수는 언제 호출되는지 설명해줘.
- 이 변수가 어떤 역할인지 설명해줘.
- 코드 실행 흐름을 순서대로 알려줘.
- 초보자가 실수하기 쉬운 부분도 알려줘.
- [Header("한글 설명")]을 사용해줘.
- [Tooltip("초보자용 설명")]도 한글로 추가해줘.
- 변수 이름은 Unity/C# 규칙 때문에 영어로 유지해줘.
- 인스펙터에 보이는 설명은 초보자가 이해하기 쉽게 한글로 작성해줘.

자동저장 조건:
- 기능 하나가 완성되면 Unity에서 Scene(씬)을 저장하라고 알려줘.
- Ctrl + S로 저장하는 방법도 알려줘.
- 자동 세팅 메뉴를 사용한 후에는 씬을 저장해야 한다고 알려줘.

이번 기능에서 원하는 쉬운 구현 방향:
- Assets/Editor/GameCanvasSetup.cs를 새로 만들어줘.
- Unity 메뉴에서 "Dino > Setup UI Canvas"를 누르면 Canvas가 자동으로 생기게 해줘.
- Canvas 설정값은 인스펙터에서 확인할 수 있게 해줘.
- Play를 누르면 Canvas가 화면에 보여야 해.
- Canvas가 없으면 새로 만들고, 이미 있으면 사용하게 해줘.

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
- 이번에는 기능 1번만 만들어줘.
- Canvas 안에 UI 요소(텍스트, 버튼, 슬라이더 등)는 아직 만들지 마.
- EventBus는 아직 만들지 마.
- GameHud 스크립트는 아직 만들지 마.
- SoundManager는 아직 만들지 마.
- StageManager는 아직 만들지 마.
- StageData는 아직 만들지 마.
- VContainer 설정은 아직 하지 마.
- 기존 Scenes, Settings 폴더는 건드리지 마.
```

## 사용 방법

1. 위 `text` 박스 안의 내용을 전부 복사합니다.
2. 다음 작업 요청으로 그대로 붙여넣습니다.
3. 기능 1번이 성공하면 다음에는 기능 2번 프롬프트를 만들면 됩니다.

## 기능 목록 (참고용)

| 번호 | 기능 | 상태 |
|---:|---|---|
| 1 | UI 캔버스 만들기 | 대기중 |
| 2 | EventBus 이벤트 구독 준비 | 대기중 |
| 3 | GameHud 오브젝트 만들기 | 대기중 |
| 4 | GameHud 스크립트 만들기 | 대기중 |
| 5 | 스테이지 정보 UI | 대기중 |
| 6 | EnemyLevelLabel - 적 머리 위 레벨 표시 | 대기중 |
| 7 | 적 레벨 색상 시스템 | 대기중 |
| 8 | ExpGainText - "+40" 떠다니는 텍스트 | 대기중 |
| 9 | GameOverPanel - 게임오버 화면 | 대기중 |
| 10 | LevelUpPopup - 레벨업 팝업 | 대기중 |
| 11 | StageClearPanel - 스테이지 클리어 화면 | 대기중 |
| 12 | GameClearPanel - 최종 클리어 화면 (Lv.20) | 대기중 |
| 13 | TitleScreen | 대기중 |
| 14 | Stage 1 전용 설정 | 대기중 |
| 15 | Stage 2 설정 (미래에 추가) | 대기중 |
| 16 | Stage 3 설정 (미래에 추가) | 대기중 |
| 17 | Stage 1 난이도 세부 설정 | 대기중 |
| 18 | SoundManager - 사운드 매니저 | 대기중 |
| 19 | 버튼에 클릭 사운드 연결 | 대기중 |
| 20 | 난이도별 UI 색상 변화 | 대기중 |
| 21 | 게임오버 화면 난이도별 차이 | 대기중 |

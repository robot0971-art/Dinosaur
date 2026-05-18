using UnityEngine;
using VContainer;

namespace DinoGrow.Infrastructure.Events
{
    /// <summary>
    /// GameEventBus 구독 방법을 보여주는 예시 스크립트입니다.
    /// 실제 게임 기능에는 필수가 아니며, 학습 후 삭제해도 됩니다.
    /// </summary>
    [AddComponentMenu("Dino/Event Bus Subscriber Example")]
    [DisallowMultipleComponent]
    public sealed class EventBusSubscriberExample : MonoBehaviour
    {
        [Header("테스트 출력 설정")]
        [Tooltip("켜져 있으면 이벤트를 받을 때 Console 창에 로그를 출력합니다.")]
        [SerializeField] private bool logEvents = true;

        private GameEventBus eventBus;
        private bool isSubscribed;

        [Inject]
        public void Construct(GameEventBus injectedEventBus)
        {
            eventBus = injectedEventBus;

            if (isActiveAndEnabled)
            {
                SubscribeEvents();
            }
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            if (eventBus == null || isSubscribed)
            {
                return;
            }

            eventBus.ExpChanged += OnExpChanged;
            eventBus.LevelChanged += OnLevelChanged;
            eventBus.EatSuccess += OnEatSuccess;
            eventBus.GameOver += OnGameOver;
            eventBus.GameClear += OnGameClear;
            eventBus.StageCleared += OnStageCleared;
            eventBus.LevelUp += OnLevelUp;

            isSubscribed = true;
            Log("이벤트 구독 완료");
        }

        private void UnsubscribeEvents()
        {
            if (eventBus == null || !isSubscribed)
            {
                return;
            }

            eventBus.ExpChanged -= OnExpChanged;
            eventBus.LevelChanged -= OnLevelChanged;
            eventBus.EatSuccess -= OnEatSuccess;
            eventBus.GameOver -= OnGameOver;
            eventBus.GameClear -= OnGameClear;
            eventBus.StageCleared -= OnStageCleared;
            eventBus.LevelUp -= OnLevelUp;

            isSubscribed = false;
            Log("이벤트 구독 해제 완료");
        }

        private void OnExpChanged(int currentExp)
        {
            Log($"EXP 변경: {currentExp}");
        }

        private void OnLevelChanged(int newLevel)
        {
            Log($"레벨 변경: {newLevel}");
        }

        private void OnEatSuccess(int expGained)
        {
            Log($"먹기 성공: EXP +{expGained}");
        }

        private void OnGameOver()
        {
            Log("게임오버 이벤트 수신");
        }

        private void OnGameClear()
        {
            Log("게임 클리어 이벤트 수신");
        }

        private void OnStageCleared()
        {
            Log("스테이지 클리어 이벤트 수신");
        }

        private void OnLevelUp()
        {
            Log("레벨업 이벤트 수신");
        }

        private void Log(string message)
        {
            if (!logEvents)
            {
                return;
            }

            Debug.Log($"[EventBusSubscriberExample] {message}", this);
        }
    }
}

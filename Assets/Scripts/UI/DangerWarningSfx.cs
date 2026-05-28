// ============================================================
// DangerWarningSfx.cs
// ============================================================
// 이 스크립트가 하는 일:
// 높은 레벨 공룡이 플레이어에게 가까이 오면
// "삐용삐용" 또는 심장박동 같은 위험 경고음을 반복 재생합니다.
// 공룡이 멀어지면 경고음을 멈춥니다.
// 기능 56: 위험 경고 효과음
// ============================================================

using UnityEngine;
using DinoGrow.Gameplay.Enemy;
using DinoGrow.Gameplay.Player;

// ============================================================
// [이 스크립트가 필요한 이유]
// ============================================================
// 위험한 공룡이 가까이 왔을 때 화면 경고만 있으면 놓칠 수 있습니다.
// 경고음이 같이 나면 플레이어가 귀로도 위험을 빠르게 알 수 있습니다.
// ============================================================

// ============================================================
// [어디에 붙이나요?]
// ============================================================
// - GameSceneUI 씬의 GameBgmPlayer 아래 빈 오브젝트에 붙입니다.
// - 오브젝트 이름 추천: DangerWarningSfx
// ============================================================

// ============================================================
// [Inspector에서 연결할 것]
// ============================================================
// - warningSound: 재생할 위험 경고음 파일 (AudioClip)
// - sfxVolume: 효과음 볼륨 (기본값 0.5)
// - detectionRadius: 위험 공룡 감지 범위 (기본값 15)
// - playerController: 비워두면 자동으로 찾음
// ============================================================

// 이 스크립트를 붙이면 AudioSource가 자동으로 같이 붙습니다.
// AudioSource는 Unity에서 소리를 재생하는 스피커 같은 컴포넌트입니다.
[RequireComponent(typeof(AudioSource))]
public class DangerWarningSfx : MonoBehaviour
{
    // ============================================================
    // Inspector에서 설정할 변수들
    // ============================================================

    [Header("위험 경고음 설정")]
    [Tooltip("높은 레벨 공룡이 가까이 올 때 반복 재생할 경고음 파일을 연결하세요")]
    [SerializeField] private AudioClip warningSound;

    [Tooltip("경고음 볼륨입니다. 0은 무음, 1은 최대 볼륨입니다. 기본값 0.5")]
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 0.5f;

    [Header("감지 설정")]
    [Tooltip("이 거리 안에 있는 높은 레벨 공룡을 감지합니다. 기본값 15")]
    [SerializeField] private float detectionRadius = 15f;

    [Header("플레이어 연결")]
    [Tooltip("비워두면 게임 시작 시 자동으로 PlayerDinoController를 찾습니다")]
    [SerializeField] private PlayerDinoController playerController;

    // ============================================================
    // 내부 상태 변수
    // ============================================================

    // 실제로 소리를 재생하는 AudioSource입니다.
    private AudioSource audioSource;

    // 현재 경고음이 재생 중인지 기억합니다.
    private bool isPlaying;

    // ============================================================
    // Unity 생명주기 함수
    // ============================================================

    /// <summary>
    /// Awake()는 Start()보다 먼저 호출됩니다.
    /// AudioSource를 가져오고 기본 설정을 합니다.
    /// </summary>
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        SetupAudioSource();
    }

    /// <summary>
    /// Start()는 씬이 시작될 때 한 번 호출됩니다.
    /// playerController를 연결하지 않았다면 자동으로 찾습니다.
    /// </summary>
    private void Start()
    {
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerDinoController>();

            if (playerController == null)
            {
                Debug.LogWarning("[DangerWarningSfx] PlayerDinoController를 찾지 못했습니다. GameScene과 함께 실행 중인지 확인하세요.", this);
            }
        }
    }

    /// <summary>
    /// Update()는 매 프레임마다 호출됩니다.
    /// 위험한 공룡이 가까이 있는지 확인하고 경고음을 켜거나 끕니다.
    /// </summary>
    private void Update()
    {
        if (playerController == null)
        {
            return;
        }

        bool hasDanger = CheckDangerNearby();

        if (hasDanger && !isPlaying)
        {
            PlayWarningSound();
        }
        else if (!hasDanger && isPlaying)
        {
            StopWarningSound();
        }
    }

    // ============================================================
    // AudioSource 설정 함수
    // ============================================================

    /// <summary>
    /// SetupAudioSource()는 AudioSource의 기본 설정을 합니다.
    /// </summary>
    private void SetupAudioSource()
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.clip = warningSound;
        audioSource.volume = sfxVolume;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
    }

    // ============================================================
    // 위험 감지 함수
    // ============================================================

    /// <summary>
    /// CheckDangerNearby()는 플레이어 주변에 높은 레벨 공룡이 있는지 확인합니다.
    /// GameHudDangerWarning.cs와 같은 방식입니다.
    /// </summary>
    private bool CheckDangerNearby()
    {
        int playerLevel = playerController.Level;
        Vector3 playerPosition = playerController.transform.position;
        float radiusSqr = detectionRadius * detectionRadius;

        DinoEnemy[] enemies = FindObjectsByType<DinoEnemy>(FindObjectsSortMode.None);

        for (int i = 0; i < enemies.Length; i++)
        {
            DinoEnemy enemy = enemies[i];

            if (enemy == null || enemy.IsDying)
            {
                continue;
            }

            if (enemy.Level <= playerLevel)
            {
                continue;
            }

            float distanceSqr = (enemy.transform.position - playerPosition).sqrMagnitude;
            if (distanceSqr <= radiusSqr)
            {
                return true;
            }
        }

        return false;
    }

    // ============================================================
    // 테스트용 공개 함수
    // ============================================================

    /// <summary>
    /// TestPlayWarningSound()는 버튼 테스트용 함수입니다.
    /// Button OnClick()에서 연결해서 경고음을 직접 재생할 수 있습니다.
    /// </summary>
    public void TestPlayWarningSound()
    {
        PlayWarningSound();
    }

    /// <summary>
    /// TestStopWarningSound()는 버튼 테스트용 함수입니다.
    /// Button OnClick()에서 연결해서 경고음을 직접 멈출 수 있습니다.
    /// </summary>
    public void TestStopWarningSound()
    {
        StopWarningSound();
    }

    // ============================================================
    // 효과음 재생/중지 함수
    // ============================================================

    /// <summary>
    /// PlayWarningSound()는 위험 경고음을 반복 재생합니다.
    /// </summary>
    private void PlayWarningSound()
    {
        if (audioSource == null)
        {
            return;
        }

        if (warningSound == null)
        {
            Debug.LogWarning("[DangerWarningSfx] 경고음 파일이 연결되지 않았습니다. Inspector에서 Warning Sound를 연결하세요.", this);
            return;
        }

        audioSource.clip = warningSound;
        audioSource.volume = sfxVolume;
        audioSource.loop = true;
        audioSource.Play();
        isPlaying = true;
    }

    /// <summary>
    /// StopWarningSound()는 위험 경고음을 멈춥니다.
    /// </summary>
    private void StopWarningSound()
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.Stop();
        isPlaying = false;
    }
}

// ============================================================
// DangerApproachSfx.cs
// ============================================================
// 이 스크립트가 하는 일:
// 높은 레벨 공룡이 플레이어에게 가까이 오면 낮은 포효음을 재생합니다.
// 공룡이 범위 밖으로 나가면 효과음을 멈춥니다.
// GameHudDangerWarning.cs와 같은 감지 방식을 사용합니다.
// 기능 52: 높은 레벨 접근 효과음
// ============================================================

using UnityEngine;
using DinoGrow.Gameplay.Player;
using DinoGrow.Gameplay.Enemy;

// ============================================================
// [이 스크립트가 필요한 이유]
// ============================================================
// 높은 레벨 공룡이 가까이 오면 긴장감을 줘야 합니다.
// 소리가 있으면 "위험하다!"를 귀로도 느낄 수 있습니다.
// ============================================================

// ============================================================
// [어디에 붙이나요?]
// ============================================================
// - GameSceneUI 씬의 GameBgmPlayer 아래 빈 오브젝트에 붙입니다.
// - 오브젝트 이름: DangerApproachSfx
// ============================================================

// ============================================================
// [Inspector에서 연결할 것]
// ============================================================
// - dangerSound: 재생할 효과음 파일 (AudioClip)
// - sfxVolume: 효과음 볼륨 (기본값 0.6)
// - detectionRadius: 감지 범위 (기본값 15)
// - playerController: 비워두면 자동으로 찾음
// ============================================================

[RequireComponent(typeof(AudioSource))]
public class DangerApproachSfx : MonoBehaviour
{
    // ============================================================
    // Inspector에서 설정할 변수들
    // ============================================================

    [Header("접근 효과음 설정")]
    [Tooltip("높은 레벨 공룡이 가까이 올 때 재생할 효과음 파일을 연결하세요")]
    [SerializeField] private AudioClip dangerSound;

    [Tooltip("효과음 볼륨입니다. 0은 무음, 1은 최대 볼륨입니다. 기본값 0.6")]
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 0.6f;

    [Header("감지 설정")]
    [Tooltip("이 거리 안에 있는 높은 레벨 공룡을 감지합니다 (미터 단위)")]
    [SerializeField] private float detectionRadius = 15f;

    [Header("플레이어 연결")]
    [Tooltip("비워두면 게임 시작 시 자동으로 PlayerDinoController를 찾습니다")]
    [SerializeField] private PlayerDinoController playerController;

    // ============================================================
    // 내부 상태 변수
    // ============================================================

    // AudioSource는 소리를 재생하는 Unity 컴포넌트입니다.
    private AudioSource audioSource;

    // 현재 효과음이 재생 중인지 확인합니다.
    private bool isPlaying;

    // ============================================================
    // Unity 생명주기 함수
    // ============================================================

    /// <summary>
    /// Awake()는 Start()보다 먼저 호출됩니다.
    /// AudioSource를 준비합니다.
    /// </summary>
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        SetupAudioSource();
    }

    /// <summary>
    /// Start()는 Play를 누른 뒤 한 번 호출됩니다.
    /// playerController가 Inspector에 연결되지 않았으면 자동으로 찾습니다.
    /// </summary>
    private void Start()
    {
        // Inspector에 playerController를 연결하지 않았으면 자동으로 찾습니다.
        if (playerController == null)
        {
            PlayerDinoController found = FindFirstObjectByType<PlayerDinoController>();
            if (found != null)
            {
                playerController = found;
            }
            else
            {
                Debug.LogWarning("[DangerApproachSfx] PlayerDinoController를 찾지 못했습니다.");
            }
        }
    }

    /// <summary>
    /// Update()는 매 프레임마다 호출됩니다.
    /// 플레이어 주변에 위험한 공룡이 있는지 확인하고 효과음을 재생/중지합니다.
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
            // 위험한 공룡이 범위 안에 들어왔으면 효과음 재생
            PlayDangerSound();
        }
        else if (!hasDanger && isPlaying)
        {
            // 위험한 공룡이 범위 밖으로 나갔으면 효과음 중지
            StopDangerSound();
        }
    }

    // ============================================================
    // AudioSource 설정 함수
    // ============================================================

    /// <summary>
    /// SetupAudioSource()는 AudioSource의 설정을 구성합니다.
    /// </summary>
    private void SetupAudioSource()
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.clip = dangerSound;
        audioSource.volume = sfxVolume;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
    }

    // ============================================================
    // 위험 감지 함수
    // ============================================================

    /// <summary>
    /// CheckDangerNearby()는 플레이어 주변에 위험한 공룡이 있는지 확인합니다.
    /// GameHudDangerWarning.cs와 같은 감지 방식을 사용합니다.
    /// </summary>
    private bool CheckDangerNearby()
    {
        int playerLevel = playerController.Level;
        Vector3 playerPos = playerController.transform.position;

        DinoEnemy[] enemies = FindObjectsByType<DinoEnemy>(FindObjectsSortMode.None);
        float radiusSqr = detectionRadius * detectionRadius;

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

            Vector3 enemyPos = enemy.transform.position;
            float distanceSqr = (enemyPos - playerPos).sqrMagnitude;

            if (distanceSqr <= radiusSqr)
            {
                return true;
            }
        }

        return false;
    }

    // ============================================================
    // 효과음 재생/중지 함수
    // ============================================================

    /// <summary>
    /// TestPlayDangerSound()는 버튼 테스트용 공개 함수입니다.
    /// Unity Button의 OnClick()에서 이 함수를 선택하면 효과음을 테스트할 수 있습니다.
    /// 실제 게임 감지는 Update()에서 자동으로 처리합니다.
    /// </summary>
    public void TestPlayDangerSound()
    {
        PlayDangerSound();
    }

    /// <summary>
    /// TestStopDangerSound()는 버튼 테스트용 공개 함수입니다.
    /// Unity Button의 OnClick()에서 이 함수를 선택하면 효과음을 멈출 수 있습니다.
    /// 실제 게임 감지는 Update()에서 자동으로 처리합니다.
    /// </summary>
    public void TestStopDangerSound()
    {
        StopDangerSound();
    }

    /// <summary>
    /// PlayDangerSound()는 위험 접근 효과음을 재생합니다.
    /// </summary>
    private void PlayDangerSound()
    {
        if (audioSource == null || audioSource.clip == null)
        {
            return;
        }

        audioSource.volume = sfxVolume;
        audioSource.Play();
        isPlaying = true;
    }

    /// <summary>
    /// StopDangerSound()는 위험 접근 효과음을 중지합니다.
    /// </summary>
    private void StopDangerSound()
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.Stop();
        isPlaying = false;
    }
}

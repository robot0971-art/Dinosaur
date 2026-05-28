// ============================================================
// DinoRoarSfx.cs
// ============================================================
// 이 스크립트가 하는 일:
// 큰 공룡이 등장할 때 깊고 무서운 포효음을 한 번 재생합니다.
// GameSceneUI에서 버튼으로 먼저 테스트할 수 있고,
// 나중에는 큰 공룡 프리팹이나 등장 연출 오브젝트에도 붙일 수 있습니다.
// 기능 57: 공룡 포효 효과음
// ============================================================

using UnityEngine;

// ============================================================
// [이 스크립트가 필요한 이유]
// ============================================================
// 큰 공룡이 등장했을 때 아무 소리도 없으면 위압감이 약합니다.
// 깊은 포효음이 재생되면 플레이어가 "큰 공룡이 나왔다"는 것을
// 귀로도 바로 느낄 수 있습니다.
// ============================================================

// ============================================================
// [어디에 붙이나요?]
// ============================================================
// 테스트할 때:
// - GameSceneUI 씬의 GameBgmPlayer 아래 빈 오브젝트에 붙입니다.
// - 오브젝트 이름 추천: DinoRoarSfx
//
// 실제 게임에 적용할 때:
// - 큰 공룡 프리팹
// - 큰 공룡 등장 연출 오브젝트
// - 보스 등장 오브젝트
// 등에 붙일 수 있습니다.
// ============================================================

// ============================================================
// [Inspector에서 연결할 것]
// ============================================================
// - roarSound: 재생할 포효음 파일 (AudioClip)
// - sfxVolume: 포효음 볼륨 (기본값 0.6)
// - playOnStart: 시작할 때 자동 재생할지 여부
// ============================================================

// 이 스크립트를 붙이면 AudioSource가 자동으로 같이 붙습니다.
// AudioSource는 Unity에서 소리를 재생하는 스피커 같은 컴포넌트입니다.
[RequireComponent(typeof(AudioSource))]
public class DinoRoarSfx : MonoBehaviour
{
    // ============================================================
    // Inspector에서 설정할 변수들
    // ============================================================

    [Header("공룡 포효 효과음 설정")]
    [Tooltip("큰 공룡 등장 시 재생할 포효음 파일을 연결하세요")]
    [SerializeField] private AudioClip roarSound;

    [Tooltip("포효음 볼륨입니다. 0은 무음, 1은 최대 볼륨입니다. 기본값 0.6")]
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 0.6f;

    [Tooltip("Play 시작 또는 오브젝트 활성화 시 자동으로 포효음을 재생할지 정합니다")]
    [SerializeField] private bool playOnStart;

    // ============================================================
    // 내부 상태 변수
    // ============================================================

    // 실제로 소리를 재생하는 AudioSource입니다.
    private AudioSource audioSource;

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
    /// Start()는 오브젝트가 시작될 때 한 번 호출됩니다.
    /// playOnStart가 켜져 있으면 포효음을 자동으로 재생합니다.
    /// </summary>
    private void Start()
    {
        if (playOnStart)
        {
            PlayRoarSound();
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

        // 시작하자마자 AudioSource가 혼자 재생하지 않게 끕니다.
        audioSource.playOnAwake = false;

        // 포효음은 기본적으로 한 번만 재생합니다.
        audioSource.loop = false;
    }

    // ============================================================
    // 공개 함수
    // ============================================================

    /// <summary>
    /// PlayRoarSound()는 공룡 포효음을 한 번 재생합니다.
    /// Button OnClick() 테스트에도 연결할 수 있습니다.
    /// </summary>
    public void PlayRoarSound()
    {
        if (audioSource == null)
        {
            return;
        }

        if (roarSound == null)
        {
            Debug.LogWarning("[DinoRoarSfx] 포효음 파일이 연결되지 않았습니다. Inspector에서 Roar Sound를 연결하세요.", this);
            return;
        }

        // PlayOneShot은 효과음을 한 번 재생할 때 사용하기 좋은 함수입니다.
        audioSource.PlayOneShot(roarSound, sfxVolume);
    }

    /// <summary>
    /// StopRoarSound()는 현재 재생 중인 포효음을 멈춥니다.
    /// 긴 포효음을 테스트하다가 중간에 멈추고 싶을 때 사용합니다.
    /// </summary>
    public void StopRoarSound()
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.Stop();
    }
}

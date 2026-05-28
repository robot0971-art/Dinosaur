// ============================================================
// EatSuccessSfx.cs
// ============================================================
// 이 스크립트가 하는 일:
// 공룡을 먹었을 때 "꿀꺽" 같은 효과음을 재생합니다.
// 싱글톤 패턴으로 다른 스크립트에서 쉽게 접근할 수 있습니다.
// PlayerDinoController의 Eat() 함수에서 호출합니다.
// 기능 49: 먹기 성공 효과음
// ============================================================

using UnityEngine;

// ============================================================
// [이 스크립트가 필요한 이유]
// ============================================================
// 공룡을 먹었을 때 소리가 없으면 먹은 느낌이 안 납니다.
// 효과음이 있으면 플레이어가 "아, 공룡을 먹었구나"를 알 수 있습니다.
// ============================================================

// ============================================================
// [어디에 붙이나요?]
// ============================================================
// - GameSceneUI 씬의 Canvas 아래 빈 오브젝트에 붙입니다.
// - 오브젝트 이름: EatSuccessSfx
// ============================================================

// ============================================================
// [Inspector에서 연결할 것]
// ============================================================
// - eatSound: 재생할 효과음 파일 (AudioClip)
// - sfxVolume: 효과음 볼륨 (기본값 0.7)
// ============================================================

// ============================================================
// [RequireComponent 설명]
// ============================================================
// 이 스크립트를 붙이면 AudioSource 컴포넌트가 자동으로 추가됩니다.
// ============================================================
[RequireComponent(typeof(AudioSource))]
public class EatSuccessSfx : MonoBehaviour
{
    // ============================================================
    // 싱글톤 패턴
    // ============================================================
    // 다른 스크립트에서 EatSuccessSfx.Instance로 접근할 수 있습니다.
    // DontDestroyOnLoad는 사용하지 않습니다 (GameSceneUI 전용).
    public static EatSuccessSfx Instance { get; private set; }

    // ============================================================
    // Inspector에서 설정할 변수들
    // ============================================================

    [Header("먹기 효과음 설정")]
    [Tooltip("공룡을 먹었을 때 재생할 효과음 파일을 여기에 연결하세요")]
    [SerializeField] private AudioClip eatSound;

    [Tooltip("효과음 볼륨입니다. 0은 무음, 1은 최대 볼륨입니다. 기본값 0.7")]
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 0.7f;

    // ============================================================
    // 내부 상태 변수
    // ============================================================

    // AudioSource는 소리를 재생하는 Unity 컴포넌트입니다.
    private AudioSource audioSource;

    // ============================================================
    // Unity 생명주기 함수
    // ============================================================

    /// <summary>
    /// Awake()는 Start()보다 먼저 호출됩니다.
    /// 싱글톤 설정과 AudioSource를 준비합니다.
    /// </summary>
    private void Awake()
    {
        // 싱글톤 설정: 이미 다른 EatSuccessSfx가 있으면 자신을 삭제합니다.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // 자신을 Instance로 등록합니다.
        Instance = this;

        // RequireComponent로 이미 추가된 AudioSource를 가져옵니다.
        audioSource = GetComponent<AudioSource>();

        // AudioSource 설정을 합니다.
        SetupAudioSource();
    }

    // ============================================================
    // AudioSource 설정 함수
    // ============================================================

    /// <summary>
    /// SetupAudioSource()는 AudioSource의 설정을 구성합니다.
    /// Awake()에서 한 번 호출됩니다.
    /// </summary>
    private void SetupAudioSource()
    {
        if (audioSource == null)
        {
            return;
        }

        // playOnAwake를 false로 설정합니다.
        // 시작 시 자동으로 재생하지 않습니다.
        audioSource.playOnAwake = false;

        // loop를 false로 설정합니다.
        // 효과음은 한 번만 재생하면 됩니다.
        audioSource.loop = false;
    }

    // ============================================================
    // 공개 함수
    // ============================================================

    /// <summary>
    /// PlayEatSound()는 먹기 성공 효과음을 재생합니다.
    /// PlayerDinoController의 Eat() 함수에서 호출합니다.
    /// 
    /// [사용 방법]
    /// PlayerDinoController.cs의 Eat() 함수 안에 아래 코드를 추가:
    /// EatSuccessSfx.Instance?.PlayEatSound();
    /// </summary>
    public void PlayEatSound()
    {
        if (audioSource == null)
        {
            return;
        }

        if (eatSound == null)
        {
            Debug.LogWarning("[EatSuccessSfx] 효과음 파일이 연결되지 않았습니다. Inspector에서 Eat Sound를 연결하세요.");
            return;
        }

        // PlayOneShot으로 효과음을 한 번 재생합니다.
        // PlayOneShot은 같은 소리를 겹쳐서 재생할 수 있습니다.
        audioSource.PlayOneShot(eatSound, sfxVolume);
    }
}

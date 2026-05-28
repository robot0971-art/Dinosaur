// ============================================================
// GameBgmPlayer.cs
// ============================================================
// 이 스크립트가 하는 일:
// 게임이 시작되면 배경음악을 자동으로 재생합니다.
// 배경음악은 반복 재생됩니다.
// 게임오버 시 StopBgm()을 호출하면 배경음악이 멈춥니다.
// 기능 48: 게임 배경음악
// ============================================================

using System.Collections;
using UnityEngine;

// ============================================================
// [이 스크립트가 필요한 이유]
// ============================================================
// 게임 중에 음악이 없으면 조용해서 심심합니다.
// 밝고 경쾌한 배경음악이 있으면 게임 분위기를 느낄 수 있습니다.
// ============================================================

// ============================================================
// [어디에 붙이나요?]
// ============================================================
// - GameSceneUI 씬의 Canvas 아래 빈 오브젝트에 붙입니다.
// - 오브젝트 이름: GameBgmPlayer
// - TitleBgmPlayer.cs와 같은 구조입니다.
// ============================================================

// ============================================================
// [Inspector에서 연결할 것]
// ============================================================
// - bgmClip: 재생할 배경음악 파일 (AudioClip)
// - bgmVolume: 배경음악 볼륨 (기본값 0.4)
// ============================================================

// ============================================================
// [RequireComponent 설명]
// ============================================================
// 이 스크립트를 붙이면 AudioSource 컴포넌트가 자동으로 추가됩니다.
// Inspector에서 AudioSource를 직접 추가할 필요가 없습니다.
// ============================================================
[RequireComponent(typeof(AudioSource))]
public class GameBgmPlayer : MonoBehaviour
{
    // ============================================================
    // Inspector에서 설정할 변수들
    // ============================================================

    [Header("배경음악 설정")]
    [Tooltip("재생할 배경음악 파일을 여기에 연결하세요. Project 창의 음악 파일을 드래그하세요")]
    [SerializeField] private AudioClip bgmClip;

    [Tooltip("배경음악 볼륨입니다. 0은 무음, 1은 최대 볼륨입니다. 기본값 0.4")]
    [Range(0f, 1f)]
    [SerializeField] private float bgmVolume = 0.4f;

    [Header("페이드 설정")]
    [Tooltip("배경음악이 서서히 커지거나 작아지는 시간입니다. 기본값 1초")]
    [SerializeField] private float fadeDuration = 1f;

    // ============================================================
    // 싱글톤 패턴
    // ============================================================
    // 다른 씬에서 GameBgmPlayer를 찾을 수 있도록 static 변수를 만듭니다.
    // DontDestroyOnLoad로 씬 전환 시에도 유지됩니다.
    public static GameBgmPlayer Instance { get; private set; }

    // ============================================================
    // 내부 상태 변수
    // ============================================================

    // AudioSource는 소리를 재생하는 Unity 컴포넌트입니다.
    // 스피커 같은 역할을 합니다.
    private AudioSource audioSource;

    // 현재 실행 중인 페이드 코루틴을 기억합니다.
    // 새 페이드를 시작할 때 이전 페이드를 멈추기 위해 필요합니다.
    private Coroutine fadeRoutine;

    // ============================================================
    // Unity 생명주기 함수
    // ============================================================

    /// <summary>
    /// Awake()는 Start()보다 먼저 호출됩니다.
    /// AudioSource 컴포넌트를 가져와서 설정합니다.
    /// DontDestroyOnLoad로 씬 전환 시에도 유지됩니다.
    /// </summary>
    private void Awake()
    {
        // 싱글톤 설정: 이미 다른 GameBgmPlayer가 있으면 자신을 삭제합니다.
        // 이렇게 하면 씬을 다시 로드해도 중복으로 생기지 않습니다.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // 자신을 Instance로 등록합니다.
        Instance = this;

        // 씬 전환 시에도 이 오브젝트가 삭제되지 않게 합니다.
        // 이렇게 하면 GameOver 씬으로 이동해도 BGM이 계속 재생됩니다.
        DontDestroyOnLoad(gameObject);

        // RequireComponent로 이미 추가된 AudioSource를 가져옵니다.
        audioSource = GetComponent<AudioSource>();

        // AudioSource 설정을 합니다.
        SetupAudioSource();
    }

    /// <summary>
    /// Start()는 Play를 누른 뒤 한 번 호출됩니다.
    /// 배경음악을 자동으로 재생합니다.
    /// </summary>
    private void Start()
    {
        // 배경음악을 재생합니다.
        PlayBgm();
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
        // audioSource가 없으면 아무것도 하지 않습니다.
        // RequireComponent로 자동 추가되므로 보통은 null이 아닙니다.
        if (audioSource == null)
        {
            return;
        }

        // AudioClip을 연결합니다.
        // AudioClip은 Inspector에서 연결한 음악 파일입니다.
        audioSource.clip = bgmClip;

        // 페이드 인을 위해 시작 볼륨은 0으로 설정합니다.
        audioSource.volume = 0f;

        // loop를 true로 설정하면 음악이 끝나면 다시 처음부터 재생됩니다.
        // 반복 재생입니다.
        audioSource.loop = true;

        // playOnAwake를 false로 설정합니다.
        // Awake에서 자동으로 재생하지 않겠습니다.
        // Start에서 직접 PlayBgm()을 호출할 것이기 때문입니다.
        audioSource.playOnAwake = false;
    }

    // ============================================================
    // 공개 함수
    // ============================================================

    /// <summary>
    /// PlayBgm()은 배경음악을 재생합니다.
    /// Start()에서 자동으로 호출됩니다.
    /// 다른 스크립트에서 호출할 수도 있습니다.
    /// </summary>
    public void PlayBgm()
    {
        // audioSource가 없으면 아무것도 하지 않습니다.
        if (audioSource == null)
        {
            return;
        }

        // AudioClip이 연결되지 않았으면 경고를 표시합니다.
        if (audioSource.clip == null)
        {
            Debug.LogWarning("[GameBgmPlayer] AudioClip이 연결되지 않았습니다. Inspector에서 BGM Clip을 연결하세요.");
            return;
        }

        // 배경음악을 0 볼륨에서 시작한 뒤 목표 볼륨까지 서서히 키웁니다.
        audioSource.volume = 0f;
        audioSource.Play();
        StartFade(bgmVolume, fadeDuration, false);
    }

    /// <summary>
    /// StopBgm()은 배경음악을 멈춥니다.
    /// 게임오버 시 호출됩니다.
    /// 다른 스크립트에서 이 함수를 호출해서 배경음악을 멈출 수 있습니다.
    /// 
    /// [사용 방법]
    /// 1. Inspector에서 Button 컴포넌트의 OnClick()을 찾습니다.
    /// 2. + 버튼을 클릭합니다.
    /// 3. GameBgmPlayer 오브젝트를 연결합니다.
    /// 4. 함수를 GameBgmPlayer → StopBgm()으로 선택합니다.
    /// </summary>
    public void StopBgm()
    {
        // audioSource가 없으면 아무것도 하지 않습니다.
        if (audioSource == null)
        {
            return;
        }

        // 배경음악을 바로 끄지 않고 0까지 서서히 줄인 뒤 멈춥니다.
        StartFade(0f, fadeDuration, true);
    }

    /// <summary>
    /// StopBgmImmediate()는 배경음악을 즉시 멈춥니다.
    /// 페이드를 기다리면 안 되는 상황에서 사용합니다.
    /// </summary>
    public void StopBgmImmediate()
    {
        if (audioSource == null)
        {
            return;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        audioSource.Stop();
        audioSource.volume = 0f;
    }

    /// <summary>
    /// SetVolume()은 배경음악 볼륨을 변경합니다.
    /// 다른 스크립트에서 이 함수를 호출해서 볼륨을 바꿀 수 있습니다.
    /// 
    /// [예시]
    /// SetVolume(0.2f);  // 볼륨을 0.2로 변경
    /// </summary>
    public void SetVolume(float volume)
    {
        // 볼륨이 0~1 사이가 되도록 제한합니다.
        bgmVolume = Mathf.Clamp01(volume);

        // audioSource가 있으면 볼륨을 적용합니다.
        if (audioSource != null)
        {
            audioSource.volume = bgmVolume;
        }
    }

    /// <summary>
    /// StartFade()는 기존 페이드를 멈추고 새 페이드를 시작합니다.
    /// </summary>
    private void StartFade(float targetVolume, float duration, bool stopAfterFade)
    {
        if (audioSource == null)
        {
            return;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(FadeVolume(targetVolume, duration, stopAfterFade));
    }

    /// <summary>
    /// FadeVolume()은 AudioSource.volume을 천천히 바꾸는 코루틴입니다.
    /// 코루틴은 여러 프레임에 걸쳐 일을 나눠서 실행하는 Unity 기능입니다.
    /// </summary>
    private IEnumerator FadeVolume(float targetVolume, float duration, bool stopAfterFade)
    {
        float startVolume = audioSource.volume;
        float time = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (time < safeDuration)
        {
            time += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(time / safeDuration);
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, progress);
            yield return null;
        }

        audioSource.volume = targetVolume;

        if (stopAfterFade)
        {
            audioSource.Stop();
        }

        fadeRoutine = null;
    }
}

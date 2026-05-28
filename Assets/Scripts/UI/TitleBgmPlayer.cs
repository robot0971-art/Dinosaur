// ============================================================
// TitleBgmPlayer.cs
// ============================================================
// 이 스크립트가 하는 일:
// 타이틀 화면에 진입하면 배경음악을 자동으로 재생합니다.
// 배경음악은 반복 재생됩니다.
// 게임 시작 버튼을 누르면 배경음악이 멈춥니다.
// 기능 45: 타이틀 배경음악 재생
// ============================================================

using System.Collections;
using UnityEngine;

// ============================================================
// [이 스크립트가 필요한 이유]
// ============================================================
// 타이틀 화면에 음악이 없으면 조용해서 심심합니다.
// 배경음악이 재생되면 플레이어가 게임 분위기를 느낄 수 있습니다.
// ============================================================

// ============================================================
// [어디에 붙이나요?]
// ============================================================
// - TitleScene 씬의 빈 오브젝트에 붙입니다.
// - 오브젝트 이름: TitleBgmPlayer
// ============================================================

// ============================================================
// [Inspector에서 연결할 것]
// ============================================================
// - bgmClip: 재생할 배경음악 파일 (AudioClip)
// - bgmVolume: 배경음악 볼륨 (기본값 0.5)
// ============================================================

// ============================================================
// [RequireComponent 설명]
// ============================================================
// 이 스크립트를 붙이면 AudioSource 컴포넌트가 자동으로 추가됩니다.
// Inspector에서 AudioSource를 직접 추가할 필요가 없습니다.
// ============================================================
[RequireComponent(typeof(AudioSource))]
public class TitleBgmPlayer : MonoBehaviour
{
    // ============================================================
    // Inspector에서 설정할 변수들
    // ============================================================

    [Header("배경음악 설정")]
    [Tooltip("재생할 배경음악 파일을 여기에 연결하세요. Project 창의 음악 파일을 드래그하세요")]
    [SerializeField] private AudioClip bgmClip;

    [Tooltip("배경음악 볼륨입니다. 0은 무음, 1은 최대 볼륨입니다")]
    [Range(0f, 1f)]
    [SerializeField] private float bgmVolume = 0.5f;

    [Header("페이드 설정")]
    [Tooltip("배경음악이 서서히 커지거나 작아지는 시간입니다. 기본값 1초")]
    [SerializeField] private float fadeDuration = 1f;

    // ============================================================
    // 내부 상태 변수
    // ============================================================

    // AudioSource는 소리를 재생하는 Unity 컴포넌트입니다.
    // 스피커 같은 역할을 합니다.
    private AudioSource audioSource;

    // 현재 실행 중인 페이드 코루틴을 기억합니다.
    private Coroutine fadeRoutine;

    // ============================================================
    // Unity 생명주기 함수
    // ============================================================

    /// <summary>
    /// Awake()는 Start()보다 먼저 호출됩니다.
    /// AudioSource 컴포넌트를 가져와서 설정합니다.
    /// </summary>
    private void Awake()
    {
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
            Debug.LogWarning("[TitleBgmPlayer] AudioClip이 연결되지 않았습니다. Inspector에서 BGM Clip을 연결하세요.");
            return;
        }

        // 배경음악을 0 볼륨에서 시작한 뒤 목표 볼륨까지 서서히 키웁니다.
        audioSource.volume = 0f;
        audioSource.Play();
        StartFade(bgmVolume, fadeDuration, false);
    }

    /// <summary>
    /// StopBgm()은 배경음악을 멈춥니다.
    /// 게임 시작 버튼을 누를 때 호출됩니다.
    /// 다른 스크립트에서 이 함수를 호출해서 배경음악을 멈출 수 있습니다.
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
    /// SetVolume(0.3f);  // 볼륨을 0.3으로 변경
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

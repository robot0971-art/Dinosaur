// ============================================================
// GameHudDangerWarning.cs
// ============================================================
// 이 스크립트가 하는 일:
// 플레이어 주변에 높은 레벨 공룡이 가까이 오면
// 화면 상단 중앙에 "위험! 더 큰 공룡이에요!" 텍스트를 표시합니다.
// 공룡이 멀어지면 텍스트를 숨깁니다.
// 기능 39: 위험 경고 텍스트 표시
// ============================================================

using UnityEngine;
using TMPro;
using DinoGrow.Gameplay.Player;
using DinoGrow.Gameplay.Enemy;

// ============================================================
// [이 스크립트가 필요한 이유]
// ============================================================
// 플레이어보다 높은 레벨의 공룡이 가까이 오면 위험하다는 것을
// 플레이어에게 알려주기 위해 필요합니다.
// 없으면 플레이어가 큰 공룡이 다가오는지 모를 수 있습니다.
// ============================================================

// ============================================================
// [어디에 붙이나요?]
// ============================================================
// - GameSceneUI 씬의 UI Canvas 오브젝트에 붙입니다.
// ============================================================

// ============================================================
// [Inspector에서 연결할 것]
// ============================================================
// - dangerWarningText: 위험 경고 텍스트 (TextMeshProUGUI)
// - playerController: 비워두면 자동으로 찾음 (다른 씬에 있어서 연결 불가)
// - detectionRadius: 감지 범위 (기본값 15)
// ============================================================

public class GameHudDangerWarning : MonoBehaviour
{
    // ============================================================
    // Inspector에서 설정할 변수들
    // ============================================================

    [Header("위험 경고 텍스트 연결")]
    [Tooltip("화면 상단 중앙에 표시할 위험 경고 텍스트를 연결하세요")]
    [SerializeField] private TextMeshProUGUI dangerWarningText;

    [Header("플레이어 연결")]
    [Tooltip("비워두면 게임 시작 시 자동으로 PlayerDinoController를 찾습니다")]
    [SerializeField] private PlayerDinoController playerController;

    [Header("감지 설정")]
    [Tooltip("이 거리 안에 있는 높은 레벨 공룡을 감지합니다 (미터 단위)")]
    [SerializeField] private float detectionRadius = 15f;

    // ============================================================
    // Unity 생명주기 함수
    // ============================================================

    /// <summary>
    /// Start()는 Play를 누른 뒤 한 번 호출됩니다.
    /// 시작할 때 경고 텍스트를 숨깁니다.
    /// playerController가 Inspector에 연결되지 않았으면 자동으로 찾습니다.
    /// </summary>
    private void Start()
    {
        // Inspector에 playerController를 연결하지 않았으면 자동으로 찾습니다.
        // GameScene과 GameSceneUI가 다른 씬이면 Inspector 연결이 안 되므로
        // 이 방법으로 자동 찾기를 합니다.
        if (playerController == null)
        {
            // 씬에 있는 PlayerDinoController를 찾습니다.
            PlayerDinoController found = FindFirstObjectByType<PlayerDinoController>();
            if (found != null)
            {
                playerController = found;
            }
            else
            {
                //PlayerDinoController가 하나도 없으면 경고를 표시
                Debug.LogWarning("[GameHudDangerWarning] PlayerDinoController를 찾지 못했습니다. GameScene에 Player가 있는지 확인하세요.");
            }
        }

        // 게임 시작 시 경고 텍스트를 숨깁니다.
        SetWarningVisible(false);
    }

    /// <summary>
    /// Update()는 매 프레임마다 호출됩니다.
    /// 플레이어 주변에 높은 레벨 공룡이 있는지 확인합니다.
    /// </summary>
    private void Update()
    {
        // 플레이어가 연결되지 않았으면 아무것도 하지 않습니다.
        if (playerController == null)
        {
            return;
        }

        // 위험한 공룡이 주변에 있는지 확인합니다.
        bool hasDanger = CheckDangerNearby();

        // 위험한 공룡이 있으면 텍스트를 보이고, 없으면 숨깁니다.
        SetWarningVisible(hasDanger);
    }

    // ============================================================
    // 위험 감지 함수
    // ============================================================

    /// <summary>
    /// CheckDangerNearby()는 플레이어 주변에 위험한 공룡이 있는지 확인합니다.
    /// 위험한 공룡 = 플레이어 레벨보다 높은 레벨의 공룡
    /// 
    /// [반환값]
    /// - true: 위험한 공룡이 가까이 있음
    /// - false: 위험한 공룡이 없음
    /// </summary>
    private bool CheckDangerNearby()
    {
        // 플레이어의 현재 레벨을 가져옵니다.
        int playerLevel = playerController.Level;

        // 플레이어의 현재 위치를 가져옵니다.
        Vector3 playerPos = playerController.transform.position;

        // 씬에 있는 모든 적 공룡을 찾습니다.
        // FindObjectsByType은 Unity 6에서 권장하는 방식입니다.
        DinoEnemy[] enemies = FindObjectsByType<DinoEnemy>(FindObjectsSortMode.None);

        // 감지 범위의 제곱값을 미리 계산합니다.
        // 제곱으로 비교하면 루트 계산을 안 해도 되어서 더 빠릅니다.
        float radiusSqr = detectionRadius * detectionRadius;

        // 모든 적 공룡을 하나씩 확인합니다.
        for (int i = 0; i < enemies.Length; i++)
        {
            DinoEnemy enemy = enemies[i];

            // 적이 null이거나 죽고 있는 상태면 건너뜁니다.
            if (enemy == null || enemy.IsDying)
            {
                continue;
            }

            // 적의 레벨이 플레이어 레벨보다 낮거나 같으면 위험하지 않습니다.
            if (enemy.Level <= playerLevel)
            {
                continue;
            }

            // 플레이어와 적 사이의 거리를 계산합니다.
            Vector3 enemyPos = enemy.transform.position;
            float distanceSqr = (enemyPos - playerPos).sqrMagnitude;

            // 거리가 감지 범위 안이면 위험한 공룡이 있는 것입니다.
            if (distanceSqr <= radiusSqr)
            {
                return true;
            }
        }

        // 위험한 공룡이 없으면 false를 반환합니다.
        return false;
    }

    // ============================================================
    // 텍스트 표시 함수
    // ============================================================

    /// <summary>
    /// SetWarningVisible()은 위험 경고 텍스트를 보이거나 숨깁니다.
    /// true면 보이고, false면 숨깁니다.
    /// </summary>
    private void SetWarningVisible(bool visible)
    {
        if (dangerWarningText != null)
        {
            dangerWarningText.gameObject.SetActive(visible);
        }
    }
}

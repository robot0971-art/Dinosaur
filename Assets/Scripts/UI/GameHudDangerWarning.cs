using DinoGrow.Gameplay.Enemy;
using DinoGrow.Gameplay.Player;
using TMPro;
using UnityEngine;
using VContainer;

public sealed class GameHudDangerWarning : MonoBehaviour
{
    [Header("Warning")]
    [SerializeField] private TextMeshProUGUI dangerWarningText;
    [SerializeField] private string warningMessage = "DANGER";

    [Header("References")]
    [SerializeField] private PlayerDinoController playerController;

    [Header("Detection")]
    [SerializeField] private float detectionRadius = 15f;

    [Inject]
    public void Construct(PlayerDinoController player)
    {
        playerController = player;
    }

    private void Start()
    {
        if (dangerWarningText != null)
        {
            dangerWarningText.text = warningMessage;
        }

        SetWarningVisible(false);
    }

    private void Update()
    {
        if (playerController == null)
        {
            SetWarningVisible(false);
            return;
        }

        SetWarningVisible(CheckDangerNearby());
    }

    private bool CheckDangerNearby()
    {
        var playerLevel = playerController.Level;
        var playerPosition = playerController.transform.position;
        var radiusSqr = detectionRadius * detectionRadius;
        var enemies = DinoEnemy.Active;

        for (var i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || enemy.IsDying || enemy.Level <= playerLevel)
            {
                continue;
            }

            var distanceSqr = (enemy.transform.position - playerPosition).sqrMagnitude;
            if (distanceSqr <= radiusSqr)
            {
                return true;
            }
        }

        return false;
    }

    private void SetWarningVisible(bool visible)
    {
        if (dangerWarningText != null)
        {
            dangerWarningText.gameObject.SetActive(visible);
        }
    }
}

using UnityEngine;

namespace DinoGrow.Gameplay.Animation
{
    public sealed class DinoAnimatorView : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
        private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int JumpHash = Animator.StringToHash("Jump");

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        private void Reset()
        {
            animator = GetComponentInChildren<Animator>();
        }

        public void SetMove(float speed, bool isRunning)
        {
            if (animator == null)
            {
                return;
            }

            animator.SetFloat(SpeedHash, Mathf.Clamp01(speed));
            animator.SetBool(IsMovingHash, speed > 0.01f);
            animator.SetBool(IsRunningHash, isRunning);
        }

        public void SetDead(bool isDead)
        {
            if (animator == null)
            {
                return;
            }

            animator.SetBool(IsDeadHash, isDead);
        }

        public void PlayAttack()
        {
            if (animator != null)
            {
                animator.SetTrigger(AttackHash);
            }
        }

        public void PlayJump()
        {
            if (animator != null)
            {
                animator.SetTrigger(JumpHash);
            }
        }
    }
}

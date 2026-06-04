using System.Collections;
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
        private static readonly int DeadHash = Animator.StringToHash("Dead");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int JumpHash = Animator.StringToHash("Jump");
        private int idleStateHash;
        private int walkStateHash;
        private int runStateHash;
        private int deathStateHash;
        private int attackStateHash;
        private AnimationClip attackClip;
        private AnimationClip deathClip;
        private Coroutine deathClipRoutine;
        private bool isDead;
        private float suppressLocomotionUntil;
        private AnimatorUpdateThrottle updateThrottle;

        private void Awake()
        {
            EnsureAnimatorReference();
        }

        private void OnEnable()
        {
            EnsureAnimatorReference();
            RestoreAnimatorPlayback();
        }

        private void Reset()
        {
            animator = GetComponentInChildren<Animator>();
        }

        public void SetMove(float speed, bool isRunning)
        {
            EnsureAnimatorReference();
            if (animator == null)
            {
                return;
            }

            if (isDead)
            {
                return;
            }

            if (Application.isPlaying && Time.time < suppressLocomotionUntil)
            {
                return;
            }

            if (!CanUpdateLowDetailAnimator())
            {
                return;
            }

            EnsureAnimatorActive();
            animator.SetFloat(SpeedHash, Mathf.Clamp01(speed));
            animator.SetBool(IsMovingHash, speed > 0.01f);
            animator.SetBool(IsRunningHash, isRunning);
            PlayLocomotionState(speed, isRunning);
        }

        public void SetPlaybackSpeed(float speed)
        {
            EnsureAnimatorReference();
            if (animator == null || isDead)
            {
                return;
            }

            if (!CanUpdateLowDetailAnimator())
            {
                return;
            }

            EnsureAnimatorActive();
            animator.speed = Mathf.Clamp(speed, 0.1f, 2.5f);
        }

        public void SetLowDetailUpdates(bool enabled, float updateInterval)
        {
            updateThrottle.Configure(enabled, updateInterval);
        }

        public void SetDead(bool isDead)
        {
            EnsureAnimatorReference();
            if (animator == null)
            {
                return;
            }

            if (isDead)
            {
                this.isDead = true;
                PlayDeath();
                return;
            }

            this.isDead = false;
            StopDeathClipRoutine();
            ResetAnimator();
        }

        private void PlayDeath()
        {
            StopDeathClipRoutine();
            CacheDeathClip();
            RestoreAnimatorPlayback();
            animator.applyRootMotion = false;
            animator.SetFloat(SpeedHash, 0f);
            animator.SetBool(IsMovingHash, false);
            animator.SetBool(IsRunningHash, false);
            animator.SetBool(IsDeadHash, true);
            animator.SetBool(DeadHash, true);

            if (deathClip != null)
            {
                deathClipRoutine = StartCoroutine(SampleDeathClipWithoutRootDrift());
                return;
            }

            if (PlayDeathState())
            {
                return;
            }

            animator.Update(0f);
            Debug.LogWarning($"Death animation state was not found on Animator '{animator.name}'.", animator);
        }

        private IEnumerator SampleDeathClipWithoutRootDrift()
        {
            animator.enabled = false;
            var sampleRoot = animator.gameObject;
            var lockedLocalPosition = sampleRoot.transform.localPosition;
            var lockedLocalRotation = sampleRoot.transform.localRotation;
            var lockedLocalScale = sampleRoot.transform.localScale;

            var time = 0f;
            var length = Mathf.Max(0.01f, deathClip.length);
            while (time < length)
            {
                deathClip.SampleAnimation(sampleRoot, time);
                LockSampleRoot(sampleRoot.transform, lockedLocalPosition, lockedLocalRotation, lockedLocalScale);
                time += Time.deltaTime;
                yield return null;
            }

            deathClip.SampleAnimation(sampleRoot, length);
            LockSampleRoot(sampleRoot.transform, lockedLocalPosition, lockedLocalRotation, lockedLocalScale);
            deathClipRoutine = null;
        }

        private void StopDeathClipRoutine()
        {
            if (deathClipRoutine == null)
            {
                return;
            }

            StopCoroutine(deathClipRoutine);
            deathClipRoutine = null;
        }

        private void CacheDeathClip()
        {
            if (deathClip != null || animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            deathClip = DinoAnimationStateResolver.FindClipContaining(animator.runtimeAnimatorController, "death");
        }

        private static void LockSampleRoot(
            Transform sampleRoot,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale)
        {
            sampleRoot.localPosition = localPosition;
            sampleRoot.localRotation = localRotation;
            sampleRoot.localScale = localScale;
        }

        private void CacheLocomotionStates()
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            if (idleStateHash != 0 && walkStateHash != 0 && runStateHash != 0)
            {
                return;
            }

            var controller = animator.runtimeAnimatorController;
            idleStateHash = DinoAnimationStateResolver.GetStateHash(controller, "idle");
            walkStateHash = DinoAnimationStateResolver.GetStateHash(controller, "walk");
            runStateHash = DinoAnimationStateResolver.GetStateHash(controller, "run");
            deathStateHash = DinoAnimationStateResolver.GetStateHash(controller, "death");
            attackStateHash = DinoAnimationStateResolver.GetStateHash(controller, "attack");
        }

        private void PlayLocomotionState(float speed, bool isRunning)
        {
            var targetHash = speed <= 0.01f
                ? idleStateHash
                : isRunning
                    ? runStateHash
                    : walkStateHash;

            if (targetHash == 0 || !animator.HasState(0, targetHash))
            {
                return;
            }

            if (IsPlayingOrTransitioningTo(targetHash))
            {
                return;
            }

            animator.CrossFade(targetHash, 0.12f, 0);
        }

        private bool IsPlayingOrTransitioningTo(int stateHash)
        {
            var current = animator.GetCurrentAnimatorStateInfo(0);
            if (current.fullPathHash == stateHash)
            {
                return true;
            }

            if (!animator.IsInTransition(0))
            {
                return false;
            }

            var next = animator.GetNextAnimatorStateInfo(0);
            return next.fullPathHash == stateHash;
        }

        private void EnsureAnimatorActive()
        {
            RestoreAnimatorPlayback();
        }

        private void ResetAnimator()
        {
            RestoreAnimatorPlayback();
            animator.Rebind();
            animator.Update(0f);
            animator.SetFloat(SpeedHash, 0f);
            animator.SetBool(IsMovingHash, false);
            animator.SetBool(IsRunningHash, false);
            animator.SetBool(IsDeadHash, false);
            animator.SetBool(DeadHash, false);
            PlayLocomotionState(0f, false);
        }

        private void RestoreAnimatorPlayback()
        {
            if (animator == null)
            {
                return;
            }

            animator.enabled = true;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            animator.speed = 1f;
            animator.applyRootMotion = false;
        }

        private bool CanUpdateLowDetailAnimator()
        {
            return updateThrottle.CanUpdate();
        }

        private bool PlayDeathState()
        {
            return DinoAnimationStateResolver.PlayDeathState(animator, deathStateHash);
        }

        public void PlayAttack()
        {
            EnsureAnimatorReference();
            if (animator == null || isDead)
            {
                return;
            }

            suppressLocomotionUntil = Application.isPlaying ? Time.time + GetAttackLockDuration() : 0f;
            EnsureAnimatorActive();
            animator.SetFloat(SpeedHash, 0f);
            animator.SetBool(IsMovingHash, false);
            animator.SetBool(IsRunningHash, false);
            animator.ResetTrigger(AttackHash);
            animator.SetTrigger(AttackHash);

            if (PlayAttackState())
            {
                return;
            }

            animator.Update(0f);
        }

        public void PlayJump()
        {
            EnsureAnimatorReference();
            if (animator != null)
            {
                animator.SetTrigger(JumpHash);
            }
        }

        private void EnsureAnimatorReference()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            CacheLocomotionStates();
            CacheDeathClip();
            CacheAttackClip();
        }

        private void CacheAttackClip()
        {
            if (attackClip != null || animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            attackClip = DinoAnimationStateResolver.FindClipContaining(animator.runtimeAnimatorController, "attack");
        }

        private float GetAttackLockDuration()
        {
            CacheAttackClip();
            return attackClip != null
                ? Mathf.Clamp(attackClip.length * 0.55f, 0.18f, 0.8f)
                : 0.35f;
        }

        private bool PlayAttackState()
        {
            return DinoAnimationStateResolver.PlayAttackState(animator, attackStateHash);
        }
    }
}

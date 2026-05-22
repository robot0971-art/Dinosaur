using System.Collections;
using System.Linq;
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
        private static readonly int[] DeathStateHashes =
        {
            Animator.StringToHash("Base Layer.Armature_Velociraptor_Death"),
            Animator.StringToHash("Armature_Velociraptor_Death"),
            Animator.StringToHash("Base Layer.Armature_TRex_Death"),
            Animator.StringToHash("Armature_TRex_Death"),
            Animator.StringToHash("Base Layer.Armature_Triceratops_Death"),
            Animator.StringToHash("Armature_Triceratops_Death"),
            Animator.StringToHash("Base Layer.Armature_Stegosaurus_Death"),
            Animator.StringToHash("Armature_Stegosaurus_Death"),
            Animator.StringToHash("Base Layer.Armature_Parasaurolophus_Death"),
            Animator.StringToHash("Armature_Parasaurolophus_Death"),
            Animator.StringToHash("Base Layer.Armature_Apatosaurus_Death"),
            Animator.StringToHash("Armature_Apatosaurus_Death")
        };

        private static readonly string[] DeathStateNames =
        {
            "Armature_Velociraptor_Death",
            "Armature_TRex_Death",
            "Armature_Triceratops_Death",
            "Armature_Stegosaurus_Death",
            "Armature_Parasaurolophus_Death",
            "Armature_Apatosaurus_Death"
        };

        private AnimationClip deathClip;
        private Coroutine deathClipRoutine;
        private int idleStateHash;
        private int walkStateHash;
        private int runStateHash;

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

            EnsureAnimatorActive();
            animator.SetFloat(SpeedHash, Mathf.Clamp01(speed));
            animator.SetBool(IsMovingHash, speed > 0.01f);
            animator.SetBool(IsRunningHash, isRunning);
            PlayLocomotionState(speed, isRunning);
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
                PlayDeath();
                return;
            }

            StopDeathClipRoutine();
            ResetAnimator();
        }

        private void PlayDeath()
        {
            StopDeathClipRoutine();
            CacheDeathClip();

            animator.applyRootMotion = false;
            animator.SetFloat(SpeedHash, 0f);
            animator.SetBool(IsMovingHash, false);
            animator.SetBool(IsRunningHash, false);
            animator.SetBool(IsDeadHash, true);
            animator.SetBool(DeadHash, true);

            if (deathClip != null)
            {
                deathClipRoutine = StartCoroutine(SampleDeathClip());
                return;
            }

            PlayDeathState();
        }

        private IEnumerator SampleDeathClip()
        {
            animator.enabled = false;
            var sampleRoot = animator.gameObject;

            var time = 0f;
            var length = Mathf.Max(0.01f, deathClip.length);
            while (time < length)
            {
                deathClip.SampleAnimation(sampleRoot, time);
                time += Time.deltaTime;
                yield return null;
            }

            deathClip.SampleAnimation(sampleRoot, length);
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

            deathClip = animator.runtimeAnimatorController.animationClips
                .FirstOrDefault(clip => clip != null && clip.name.ToLowerInvariant().Contains("death"));
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

            idleStateHash = GetStateHash("idle");
            walkStateHash = GetStateHash("walk");
            runStateHash = GetStateHash("run");
        }

        private int GetStateHash(string stateNamePart)
        {
            var clip = animator.runtimeAnimatorController.animationClips
                .FirstOrDefault(candidate =>
                    candidate != null
                    && candidate.name.ToLowerInvariant().Contains(stateNamePart));

            return clip == null ? 0 : Animator.StringToHash($"Base Layer.{clip.name}");
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
            animator.speed = 1f;
            animator.applyRootMotion = false;
        }

        private void PlayDeathState()
        {
            for (var i = 0; i < DeathStateHashes.Length; i++)
            {
                var stateHash = DeathStateHashes[i];
                if (!animator.HasState(0, stateHash))
                {
                    continue;
                }

                animator.enabled = true;
                animator.speed = 1f;
                animator.Play(stateHash, 0, 0f);
                animator.Update(0f);
                return;
            }

            for (var i = 0; i < DeathStateNames.Length; i++)
            {
                var stateName = DeathStateNames[i];
                animator.enabled = true;
                animator.speed = 1f;
                animator.Play(stateName, 0, 0f);
                animator.Update(0f);

                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName(stateName))
                {
                    return;
                }
            }

            Debug.LogWarning($"Death animation state was not found on Animator '{animator.name}'.", animator);
        }

        public void PlayAttack()
        {
            EnsureAnimatorReference();
            if (animator != null)
            {
                animator.SetTrigger(AttackHash);
            }
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

            CacheDeathClip();
            CacheLocomotionStates();
        }
    }
}

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

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            CacheDeathClip();
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

            if (isDead)
            {
                PlayDeath();
                return;
            }

            StopDeathClipRoutine();
            animator.enabled = true;
            animator.SetBool(IsDeadHash, isDead);
            animator.SetBool(DeadHash, isDead);
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

            var time = 0f;
            var length = Mathf.Max(0.01f, deathClip.length);
            while (time < length)
            {
                deathClip.SampleAnimation(gameObject, time);
                time += Time.deltaTime;
                yield return null;
            }

            deathClip.SampleAnimation(gameObject, length);
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

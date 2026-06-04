using UnityEngine;

namespace DinoGrow.Gameplay.Animation
{
    internal static class DinoAnimationStateResolver
    {
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

        private static readonly int[] AttackStateHashes =
        {
            Animator.StringToHash("Base Layer.Armature_Velociraptor_Attack"),
            Animator.StringToHash("Armature_Velociraptor_Attack"),
            Animator.StringToHash("Base Layer.Armature_TRex_Attack"),
            Animator.StringToHash("Armature_TRex_Attack"),
            Animator.StringToHash("Base Layer.Armature_Triceratops_Attack"),
            Animator.StringToHash("Armature_Triceratops_Attack"),
            Animator.StringToHash("Base Layer.Armature_Stegosaurus_Attack"),
            Animator.StringToHash("Armature_Stegosaurus_Attack"),
            Animator.StringToHash("Base Layer.Armature_Parasaurolophus_Attack"),
            Animator.StringToHash("Armature_Parasaurolophus_Attack"),
            Animator.StringToHash("Base Layer.Armature_Apatosaurus_Attack"),
            Animator.StringToHash("Armature_Apatosaurus_Attack")
        };

        public static AnimationClip FindClipContaining(RuntimeAnimatorController controller, string namePart)
        {
            if (controller == null)
            {
                return null;
            }

            var clips = controller.animationClips;
            for (var i = 0; i < clips.Length; i++)
            {
                var clip = clips[i];
                if (clip != null && ContainsIgnoreCase(clip.name, namePart))
                {
                    return clip;
                }
            }

            return null;
        }

        public static int GetStateHash(RuntimeAnimatorController controller, string stateNamePart)
        {
            var clip = FindClipContaining(controller, stateNamePart);
            return clip == null ? 0 : Animator.StringToHash($"Base Layer.{clip.name}");
        }

        public static bool PlayDeathState(Animator animator, int cachedDeathStateHash)
        {
            if (animator == null)
            {
                return false;
            }

            if (cachedDeathStateHash != 0 && animator.HasState(0, cachedDeathStateHash))
            {
                animator.enabled = true;
                animator.speed = 1f;
                animator.CrossFade(cachedDeathStateHash, 0.04f, 0, 0f);
                animator.Update(0f);
                return true;
            }

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
                return true;
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
                    return true;
                }
            }

            return false;
        }

        public static bool PlayAttackState(Animator animator, int cachedAttackStateHash)
        {
            if (animator == null)
            {
                return false;
            }

            if (cachedAttackStateHash != 0 && animator.HasState(0, cachedAttackStateHash))
            {
                animator.Play(cachedAttackStateHash, 0, 0f);
                animator.Update(0f);
                return true;
            }

            for (var i = 0; i < AttackStateHashes.Length; i++)
            {
                var stateHash = AttackStateHashes[i];
                if (!animator.HasState(0, stateHash))
                {
                    continue;
                }

                animator.Play(stateHash, 0, 0f);
                animator.Update(0f);
                return true;
            }

            return false;
        }

        private static bool ContainsIgnoreCase(string text, string value)
        {
            return !string.IsNullOrEmpty(text)
                && !string.IsNullOrEmpty(value)
                && text.IndexOf(value, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}

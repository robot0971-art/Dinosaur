using UnityEngine;

namespace DinoGrow.Gameplay.Player
{
    public sealed class PlayerStepSoundController
    {
        private readonly GameObject owner;
        private readonly AudioClip sprintClip;
        private readonly float sprintVolume;
        private readonly float sprintPitch;
        private readonly float sprintLoopDelay;
        private readonly AudioClip walkClip;
        private readonly float walkVolume;
        private readonly float walkPitch;
        private readonly float walkLoopDelay;

        private AudioSource sprintSource;
        private AudioSource walkSource;
        private float nextSprintPlayTime;
        private float nextWalkPlayTime;

        public PlayerStepSoundController(
            GameObject owner,
            AudioClip sprintClip,
            AudioSource sprintSource,
            float sprintVolume,
            float sprintPitch,
            float sprintLoopDelay,
            AudioClip walkClip,
            AudioSource walkSource,
            float walkVolume,
            float walkPitch,
            float walkLoopDelay)
        {
            this.owner = owner;
            this.sprintClip = sprintClip;
            this.sprintSource = sprintSource;
            this.sprintVolume = sprintVolume;
            this.sprintPitch = sprintPitch;
            this.sprintLoopDelay = sprintLoopDelay;
            this.walkClip = walkClip;
            this.walkSource = walkSource;
            this.walkVolume = walkVolume;
            this.walkPitch = walkPitch;
            this.walkLoopDelay = walkLoopDelay;
        }

        public void Configure()
        {
            ConfigureStepSource(ref sprintSource, sprintClip, sprintVolume, sprintPitch);
            ConfigureStepSource(ref walkSource, walkClip, walkVolume, walkPitch);
        }

        public void UpdateLoops(bool isMoving, bool isSprinting)
        {
            UpdateStepLoop(sprintSource, isMoving && isSprinting, sprintVolume, sprintPitch, sprintLoopDelay, ref nextSprintPlayTime);
            UpdateStepLoop(walkSource, isMoving && !isSprinting, walkVolume, walkPitch, walkLoopDelay, ref nextWalkPlayTime);
        }

        public void Stop()
        {
            UpdateStepLoop(sprintSource, false, sprintVolume, sprintPitch, sprintLoopDelay, ref nextSprintPlayTime);
            UpdateStepLoop(walkSource, false, walkVolume, walkPitch, walkLoopDelay, ref nextWalkPlayTime);
        }

        private void ConfigureStepSource(ref AudioSource source, AudioClip clip, float volume, float pitch)
        {
            if (clip == null || owner == null)
            {
                return;
            }

            if (source == null)
            {
                source = owner.AddComponent<AudioSource>();
            }

            source.clip = clip;
            source.loop = false;
            source.playOnAwake = false;
            source.volume = volume;
            source.pitch = pitch;
            source.spatialBlend = 0f;
        }

        private static void UpdateStepLoop(
            AudioSource source,
            bool shouldPlay,
            float volume,
            float pitch,
            float loopDelay,
            ref float nextPlayTime)
        {
            if (source == null)
            {
                return;
            }

            if (shouldPlay)
            {
                source.volume = volume;
                source.pitch = pitch;
                if (!source.isPlaying && Time.time >= nextPlayTime)
                {
                    source.Play();
                    var playbackLength = source.clip != null ? source.clip.length / Mathf.Max(0.01f, Mathf.Abs(pitch)) : 0f;
                    nextPlayTime = Time.time + playbackLength + loopDelay;
                }

                return;
            }

            nextPlayTime = 0f;
            if (source.isPlaying)
            {
                source.Stop();
            }
        }
    }
}

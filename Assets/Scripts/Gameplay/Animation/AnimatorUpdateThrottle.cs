using UnityEngine;

namespace DinoGrow.Gameplay.Animation
{
    internal struct AnimatorUpdateThrottle
    {
        private bool enabled;
        private float updateInterval;
        private float nextUpdateTime;
        private int updateFrame;

        public void Configure(bool enabled, float updateInterval)
        {
            this.enabled = enabled;
            this.updateInterval = Mathf.Max(0.05f, updateInterval);
            if (!enabled)
            {
                nextUpdateTime = 0f;
                updateFrame = -1;
            }
        }

        public bool CanUpdate()
        {
            if (!Application.isPlaying || !enabled)
            {
                return true;
            }

            if (Time.frameCount == updateFrame)
            {
                return true;
            }

            if (Time.time < nextUpdateTime)
            {
                return false;
            }

            nextUpdateTime = Time.time + updateInterval;
            updateFrame = Time.frameCount;
            return true;
        }
    }
}

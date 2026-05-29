using System.Collections;
using DinoGrow.Infrastructure.Pooling;
using UnityEngine;

namespace DinoGrow.Gameplay.VFX
{
    public sealed class PooledOneShotVfx : MonoBehaviour
    {
        private Coroutine returnRoutine;
        private IObjectPoolService poolService;
        private Transform pooledRoot;

        public void Play(IObjectPoolService pool, Transform root)
        {
            poolService = pool;
            pooledRoot = root != null ? root : transform;

            if (returnRoutine != null)
            {
                StopCoroutine(returnRoutine);
            }

            var delay = 0.1f;
            foreach (var particle in pooledRoot.GetComponentsInChildren<ParticleSystem>(true))
            {
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                var main = particle.main;
                main.loop = false;
                delay = Mathf.Max(delay, main.duration + main.startLifetime.constantMax);
                particle.Play(true);
            }

            returnRoutine = StartCoroutine(ReturnAfterDelay(delay));
        }

        private IEnumerator ReturnAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            returnRoutine = null;

            if (poolService != null && pooledRoot != null)
            {
                poolService.Despawn(pooledRoot);
                yield break;
            }

            Destroy(gameObject);
        }
    }
}

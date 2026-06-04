using System;
using UnityEngine;
using UnityEngine.AI;

namespace DinoGrow.Gameplay.Enemy
{
    public sealed class EnemyNavAgentController
    {
        private readonly GameObject owner;
        private readonly Transform transform;
        private NavMeshAgent agent;
        private bool useNavMeshAgent;
        private float nextRepathTime;
        private Vector3 lastDestination = Vector3.positiveInfinity;

        public EnemyNavAgentController(GameObject owner, Transform transform, NavMeshAgent agent)
        {
            this.owner = owner;
            this.transform = transform;
            this.agent = agent;
        }

        public NavMeshAgent Agent => agent;

        public bool Configure(
            bool requestedUseNavMeshAgent,
            float moveSpeed,
            float turnSpeed,
            float groundColliderRadius,
            float groundColliderHeight,
            float baseOffset,
            float navSampleDistance,
            float navVerticalSampleDistance)
        {
            useNavMeshAgent = requestedUseNavMeshAgent;
            if (!useNavMeshAgent)
            {
                Disable();
                return false;
            }

            var navSearchRadius = Mathf.Max(navSampleDistance, navVerticalSampleDistance);
            if (!NavMesh.SamplePosition(transform.position, out var navHit, navSearchRadius, NavMesh.AllAreas))
            {
                Disable();
                return false;
            }

            transform.position = navHit.position;
            if (agent == null)
            {
                agent = owner.GetComponent<NavMeshAgent>();
                if (agent == null)
                {
                    agent = owner.AddComponent<NavMeshAgent>();
                }
            }

            if (agent == null)
            {
                Disable();
                return false;
            }

            agent.speed = Mathf.Max(0.1f, moveSpeed);
            agent.angularSpeed = turnSpeed;
            agent.acceleration = Mathf.Max(8f, moveSpeed * 4f);
            agent.stoppingDistance = 0f;
            agent.autoBraking = false;
            agent.updateRotation = false;
            agent.updateUpAxis = false;
            agent.radius = Mathf.Max(0.1f, groundColliderRadius);
            agent.height = Mathf.Max(agent.radius * 2f, groundColliderHeight);
            agent.baseOffset = baseOffset;
            agent.enabled = true;
            agent.Warp(navHit.position);
            ResetRefreshState();
            return true;
        }

        public void ResetPath()
        {
            if (CanUse())
            {
                agent.ResetPath();
            }
        }

        public bool CanUse()
        {
            return useNavMeshAgent && agent != null && agent.enabled && agent.isOnNavMesh;
        }

        public bool IsStalled(Vector3 desiredMoveDirection, float desiredMoveSpeed)
        {
            if (!CanUse() || desiredMoveDirection.sqrMagnitude <= 0.001f || desiredMoveSpeed <= 0.001f)
            {
                return false;
            }

            if (!agent.hasPath && !agent.pathPending)
            {
                return true;
            }

            if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                return true;
            }

            var velocity = agent.velocity;
            velocity.y = 0f;
            return !agent.pathPending
                && agent.remainingDistance > agent.stoppingDistance + 0.1f
                && velocity.sqrMagnitude <= 0.0001f;
        }

        public void ApplyMoveDestination(
            Vector3 currentPosition,
            Vector3 desiredMoveDirection,
            float desiredMoveSpeed,
            float destinationDistance,
            float sampleDistance,
            float repathInterval,
            Func<Vector3, Vector3> clampDestination)
        {
            if (!CanUse())
            {
                return;
            }

            agent.speed = Mathf.Max(0.1f, desiredMoveSpeed);
            if (desiredMoveDirection.sqrMagnitude <= 0.001f || desiredMoveSpeed <= 0.001f)
            {
                agent.ResetPath();
                ResetRefreshState();
                return;
            }

            var destination = currentPosition + desiredMoveDirection * destinationDistance;
            ApplyDestination(clampDestination(destination), desiredMoveSpeed, sampleDistance, repathInterval);
        }

        public void ApplyDestination(Vector3 destination, float speed, float sampleDistance, float repathInterval)
        {
            if (!CanUse())
            {
                return;
            }

            agent.speed = Mathf.Max(0.1f, speed);
            if (!ShouldRefreshDestination(destination))
            {
                return;
            }

            if (!NavMesh.SamplePosition(destination, out var hit, sampleDistance, NavMesh.AllAreas))
            {
                Disable();
                return;
            }

            if (!agent.SetDestination(hit.position))
            {
                Disable();
                return;
            }

            MarkDestinationRefreshed(hit.position, repathInterval);
        }

        public void RotateWithVelocity(float turnSpeed)
        {
            if (!CanUse())
            {
                return;
            }

            var velocity = agent.desiredVelocity;
            velocity.y = 0f;
            if (velocity.sqrMagnitude <= 0.001f)
            {
                return;
            }

            var targetRotation = Quaternion.LookRotation(velocity.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        public void Disable()
        {
            useNavMeshAgent = false;
            if (agent != null)
            {
                if (agent.enabled && agent.isOnNavMesh)
                {
                    agent.ResetPath();
                }

                agent.enabled = false;
            }
        }

        private void ResetRefreshState()
        {
            nextRepathTime = 0f;
            lastDestination = Vector3.positiveInfinity;
        }

        private bool ShouldRefreshDestination(Vector3 destination)
        {
            if (Time.time >= nextRepathTime)
            {
                return true;
            }

            if (float.IsInfinity(lastDestination.x))
            {
                return true;
            }

            return (destination - lastDestination).sqrMagnitude > 1f;
        }

        private void MarkDestinationRefreshed(Vector3 destination, float repathInterval)
        {
            lastDestination = destination;
            nextRepathTime = Time.time + Mathf.Max(0.05f, repathInterval);
        }
    }
}

using Blackout.Detection;
using UnityEngine;
using UnityEngine.AI;

namespace Blackout.AI
{
    /// <summary>
    /// Scavenger: patrols, goes to check the strongest light or noise it perceives,
    /// searches the spot for a while, then gives up and resumes the patrol.
    /// It heads for where the stimulus WAS, not where the player is now.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class Looter : MonoBehaviour
    {
        public enum State { Patrol, Investigate, Search }

        [Header("Patrol")]
        [Tooltip("Visited in order, looping. Empty = stands still.")]
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField, Min(0f)] private float patrolWait = 2f;
        [SerializeField, Min(0f)] private float patrolSpeed = 2f;

        [Header("Investigate")]
        [SerializeField, Min(0f)] private float investigateSpeed = 3.5f;
        [Tooltip("Seconds spent at the last known position before giving up.")]
        [SerializeField, Min(0f)] private float searchDuration = 5f;
        [Tooltip("How far from a stimulus the NavMesh may be sampled (lamps can be off-mesh).")]
        [SerializeField, Min(0.1f)] private float navSampleRadius = 3f;

        [Header("Perception")]
        [SerializeField, Min(0.05f)] private float perceptionInterval = 0.2f;
        [SerializeField] private float eyeHeight = 1.5f;
        [Tooltip("Layers that block line of sight to lights.")]
        [SerializeField] private LayerMask occlusionMask = ~0;

        public State Current { get; private set; } = State.Patrol;

        private NavMeshAgent agent;
        private float nextPerception;
        private float timer;
        private int patrolIndex;
        private bool waitingAtPoint;
        private Vector3 lastKnown;

        private void Awake() => agent = GetComponent<NavMeshAgent>();

        private void Start() => EnterPatrol();

        private void Update()
        {
            if (Time.time >= nextPerception)
            {
                nextPerception = Time.time + perceptionInterval;
                if (TryPerceive(out StimulusSource best))
                    BeginInvestigate(best.Position);
            }

            switch (Current)
            {
                case State.Patrol: UpdatePatrol(); break;
                case State.Investigate: UpdateInvestigate(); break;
                case State.Search: UpdateSearch(); break;
            }
        }

        private bool TryPerceive(out StimulusSource best)
        {
            best = null;
            float bestScore = 0f;
            Vector3 eye = transform.position + Vector3.up * eyeHeight;
            var sources = StimulusSource.All;

            for (int i = 0; i < sources.Count; i++)
            {
                StimulusSource s = sources[i];
                if (s.Radius <= 0f)
                    continue;

                float dist = Vector3.Distance(transform.position, s.Position);
                if (dist > s.Radius)
                    continue;
                if (s.Kind == StimulusKind.Light && !HasLineOfSight(eye, s))
                    continue;

                float score = s.Intensity * (1f - dist / s.Radius);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = s;
                }
            }
            return best != null;
        }

        private bool HasLineOfSight(Vector3 eye, StimulusSource s)
        {
            if (!Physics.Linecast(eye, s.Position, out RaycastHit hit, occlusionMask, QueryTriggerInteraction.Ignore))
                return true;
            // The source's own collider (the lamp body) must not hide the lamp.
            return hit.transform.IsChildOf(s.transform);
        }

        // --- Patrol ---

        private void EnterPatrol()
        {
            Current = State.Patrol;
            agent.speed = patrolSpeed;
            waitingAtPoint = false;
            GoToPatrolPoint();
        }

        private void GoToPatrolPoint()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
                return;
            agent.isStopped = false;
            agent.SetDestination(patrolPoints[patrolIndex].position);
        }

        private void UpdatePatrol()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
                return;

            if (waitingAtPoint)
            {
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    waitingAtPoint = false;
                    patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                    GoToPatrolPoint();
                }
            }
            else if (Arrived())
            {
                waitingAtPoint = true;
                timer = patrolWait;
            }
        }

        // --- Investigate ---

        private void BeginInvestigate(Vector3 position)
        {
            lastKnown = position;
            Current = State.Investigate;
            agent.speed = investigateSpeed;
            agent.isStopped = false;

            Vector3 target = NavMesh.SamplePosition(position, out NavMeshHit hit, navSampleRadius, NavMesh.AllAreas)
                ? hit.position
                : position;
            agent.SetDestination(target);
        }

        private void UpdateInvestigate()
        {
            if (!Arrived())
                return;
            Current = State.Search;
            timer = searchDuration;
        }

        private void UpdateSearch()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
                EnterPatrol();
        }

        private bool Arrived()
        {
            return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Current switch
            {
                State.Investigate => Color.yellow,
                State.Search => new Color(1f, 0.5f, 0f),
                _ => Color.green
            };
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2.2f, 0.25f);

            if (Application.isPlaying && Current != State.Patrol)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position + Vector3.up, lastKnown);
            }
        }
    }
}

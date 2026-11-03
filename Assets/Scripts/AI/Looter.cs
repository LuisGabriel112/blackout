using Blackout.Detection;
using Blackout.Player;
using UnityEngine;
using UnityEngine.AI;

namespace Blackout.AI
{
    /// <summary>
    /// Scavenger. Patrols; approaches lights and noises slowly (caution); searches the spot,
    /// then gives up. If it SEES the player it chases and hits. The player is seen only when
    /// close, or when lit by a light, in its field of view and with a clear line: darkness hides you.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class Looter : MonoBehaviour
    {
        public enum State { Patrol, Investigate, Search, Chase }

        [Header("Patrol")]
        [Tooltip("Visited in order, looping. Empty = stands still.")]
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField, Min(0f)] private float patrolWait = 2f;
        [SerializeField, Min(0f)] private float patrolSpeed = 2f;

        [Header("Investigate")]
        [Tooltip("Slow on purpose: it approaches lights and noises with caution.")]
        [SerializeField, Min(0f)] private float investigateSpeed = 2.5f;
        [Tooltip("Seconds spent at the last known position before giving up.")]
        [SerializeField, Min(0f)] private float searchDuration = 5f;
        [Tooltip("How far from a stimulus the NavMesh may be sampled (lamps can be off-mesh).")]
        [SerializeField, Min(0.1f)] private float navSampleRadius = 3f;
        [Tooltip("Seconds a stimulus that has not moved is ignored after the looter finished searching it. Stops a lit lamp from pinning it forever.")]
        [SerializeField, Min(0f)] private float habituationTime = 20f;

        [Header("Sight")]
        [SerializeField, Min(0f)] private float sightRange = 12f;
        [SerializeField, Range(10f, 360f)] private float fieldOfView = 110f;
        [Tooltip("Inside this distance it notices the player even in the dark and outside its view cone.")]
        [SerializeField, Min(0f)] private float closeSenseRange = 2.5f;

        [Header("Chase")]
        [Tooltip("Keep below the player's walk speed: running away works, but it is loud.")]
        [SerializeField, Min(0f)] private float chaseSpeed = 3.6f;
        [Tooltip("Seconds without seeing the player before it stops chasing and goes to where it last saw them.")]
        [SerializeField, Min(0f)] private float loseSightTime = 5f;
        [SerializeField, Min(0f)] private float turnSpeed = 360f;

        [Header("Attack")]
        [SerializeField, Min(0.1f)] private float attackRange = 1.8f;
        [SerializeField, Min(0f)] private float attackDamage = 15f;
        [Tooltip("Delay between reaching the player and the first hit.")]
        [SerializeField, Min(0f)] private float attackWindup = 0.5f;
        [SerializeField, Min(0.1f)] private float attackCooldown = 1.2f;

        [Header("Perception")]
        [SerializeField, Min(0.05f)] private float perceptionInterval = 0.2f;
        [SerializeField] private float eyeHeight = 1.5f;
        [Tooltip("Layers that block line of sight.")]
        [SerializeField] private LayerMask occlusionMask = ~0;

        public State Current { get; private set; } = State.Patrol;

        private NavMeshAgent agent;
        private PlayerHealth player;
        private CharacterController playerBody;
        private float nextPerception;
        private float timer;
        private float lastSeenTime;
        private float attackTimer;
        private int patrolIndex;
        private bool waitingAtPoint;
        private Vector3 lastKnown;
        private StimulusSource investigated;
        private Vector3 investigatedPos;
        private float forgetTime;

        private void Awake() => agent = GetComponent<NavMeshAgent>();

        private void Start()
        {
            player = FindFirstObjectByType<PlayerHealth>();
            if (player != null)
                playerBody = player.GetComponent<CharacterController>();
            else
                Debug.LogWarning("[Looter] No PlayerHealth in the scene: it will never chase or attack.", this);

            EnterPatrol();
        }

        private void Update()
        {
            bool playerAlive = player != null && !player.IsDead;

            if (Time.time >= nextPerception)
            {
                nextPerception = Time.time + perceptionInterval;

                if (playerAlive && CanSeePlayer())
                {
                    lastSeenTime = Time.time;
                    lastKnown = player.transform.position;
                    if (Current != State.Chase)
                        BeginChase();
                    else
                        agent.SetDestination(lastKnown);
                }
                else if (Current != State.Chase && TryPerceive(out StimulusSource best))
                {
                    BeginInvestigate(best.Position, best);
                }
            }

            switch (Current)
            {
                case State.Patrol: UpdatePatrol(); break;
                case State.Investigate: UpdateInvestigate(); break;
                case State.Search: UpdateSearch(); break;
                case State.Chase: UpdateChase(); break;
            }
        }

        // --- Perception ---

        private bool TryPerceive(out StimulusSource best)
        {
            best = null;
            float bestScore = 0f;
            Vector3 eye = transform.position + Vector3.up * eyeHeight;
            var sources = StimulusSource.All;

            for (int i = 0; i < sources.Count; i++)
            {
                StimulusSource s = sources[i];
                if (s.Radius <= 0f || IsHabituated(s))
                    continue;

                float dist = Vector3.Distance(transform.position, s.Position);
                if (dist > s.Radius)
                    continue;
                if (s.Kind == StimulusKind.Light && !IsClear(eye, s.Position, s.transform))
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

        private bool CanSeePlayer()
        {
            Vector3 eye = transform.position + Vector3.up * eyeHeight;
            Vector3 target = playerBody != null ? playerBody.bounds.center : player.transform.position + Vector3.up;
            Vector3 toPlayer = target - eye;
            float dist = toPlayer.magnitude;
            if (dist > sightRange)
                return false;

            bool close = dist <= closeSenseRange;
            if (!close)
            {
                Vector3 flat = Vector3.ProjectOnPlane(toPlayer, Vector3.up);
                if (Vector3.Angle(transform.forward, flat) > fieldOfView * 0.5f)
                    return false;
            }

            if (!IsClear(eye, target, null))
                return false;

            return close || IsLit(target);
        }

        /// <summary>True when an active light reaches the point with nothing in between.</summary>
        private bool IsLit(Vector3 point)
        {
            var sources = StimulusSource.All;
            for (int i = 0; i < sources.Count; i++)
            {
                StimulusSource s = sources[i];
                if (s.Kind != StimulusKind.Light || s.Radius <= 0f)
                    continue;
                if (Vector3.Distance(s.Position, point) > s.Radius)
                    continue;
                if (IsClear(s.Position, point, s.transform))
                    return true;
            }
            return false;
        }

        /// <summary>Line is clear unless something other than the player or the endpoint object is hit.</summary>
        private bool IsClear(Vector3 from, Vector3 to, Transform endpoint)
        {
            if (!Physics.Linecast(from, to, out RaycastHit hit, occlusionMask, QueryTriggerInteraction.Ignore))
                return true;
            if (player != null && hit.transform.IsChildOf(player.transform))
                return true;
            return endpoint != null && hit.transform.IsChildOf(endpoint);
        }

        // --- Patrol ---

        private void EnterPatrol()
        {
            Current = State.Patrol;
            agent.speed = patrolSpeed;
            agent.isStopped = false;
            waitingAtPoint = false;
            GoToPatrolPoint();
        }

        private void GoToPatrolPoint()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
                return;
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

        private void BeginInvestigate(Vector3 position, StimulusSource source = null)
        {
            lastKnown = position;
            investigated = source;
            if (source != null)
                investigatedPos = source.Position;
            forgetTime = float.PositiveInfinity;
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
            {
                forgetTime = Time.time + habituationTime;
                EnterPatrol();
            }
        }

        /// <summary>A stimulus that has not moved since it was investigated carries no new information.</summary>
        private bool IsHabituated(StimulusSource s)
        {
            return s == investigated
                && Time.time < forgetTime
                && (s.Position - investigatedPos).sqrMagnitude < 0.25f;
        }

        // --- Chase and attack ---

        private void BeginChase()
        {
            Current = State.Chase;
            investigated = null;
            waitingAtPoint = false;
            agent.speed = chaseSpeed;
            agent.isStopped = false;
            attackTimer = attackWindup;
            agent.SetDestination(lastKnown);
        }

        private void UpdateChase()
        {
            if (player == null || player.IsDead)
            {
                EnterPatrol();
                return;
            }

            if (Time.time - lastSeenTime > loseSightTime)
            {
                BeginInvestigate(lastKnown);
                return;
            }

            Vector3 toPlayer = player.transform.position - transform.position;
            float planar = new Vector2(toPlayer.x, toPlayer.z).magnitude;

            if (planar <= attackRange && Mathf.Abs(toPlayer.y) < 2f)
            {
                agent.isStopped = true;
                FaceTowards(player.transform.position);

                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0f)
                {
                    player.TakeDamage(attackDamage);
                    attackTimer = attackCooldown;
                }
            }
            else
            {
                agent.isStopped = false;
                attackTimer = attackWindup;
            }
        }

        private void FaceTowards(Vector3 worldPoint)
        {
            Vector3 dir = Vector3.ProjectOnPlane(worldPoint - transform.position, Vector3.up);
            if (dir.sqrMagnitude < 0.001f)
                return;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
        }

        private bool Arrived()
        {
            return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Current switch
            {
                State.Chase => Color.red,
                State.Investigate => Color.yellow,
                State.Search => new Color(1f, 0.5f, 0f),
                _ => Color.green
            };
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2.2f, 0.25f);

            Gizmos.color = new Color(1f, 1f, 1f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, sightRange);
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, closeSenseRange);
            Gizmos.color = new Color(1f, 0f, 0f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, attackRange);

            if (Application.isPlaying && Current != State.Patrol)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position + Vector3.up, lastKnown);
            }
        }
    }
}

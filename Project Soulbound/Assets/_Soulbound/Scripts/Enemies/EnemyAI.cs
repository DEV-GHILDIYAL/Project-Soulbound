using UnityEngine;
using UnityEngine.AI;

namespace Soulbound
{
    [RequireComponent(typeof(NavMeshAgent), typeof(Health), typeof(EnemyGeneration))]
    public sealed class EnemyAI : MonoBehaviour
    {
        [SerializeField] private float detectionRange = 10f;
        [SerializeField] private float attackRange = 1.6f;
        [SerializeField] private float attackCooldown = 1.2f;
        [SerializeField] private float attackWindup = 0.35f;
        [SerializeField] private float memoryDuration = 3f;
        [SerializeField,Range(30,160)] private float visionAngle=110f;
        private float idleUntil, roamUntil, staggerStarted, staggerUntil, strikeStarted, recoveryUntil;
        private bool striking, impactDone;
        private Vector3 dodgeStart;
        private float dodgeStarted;
        public float HitReaction => Time.time<staggerUntil?Mathf.Sin(Mathf.Clamp01((Time.time-staggerStarted)/.38f)*Mathf.PI):0;
        public float AttackSwing => striking||lunging?Mathf.Clamp01((Time.time-strikeStarted)/.25f):0;
        public bool IsWindingUp => windingUp;
        public bool IsRecovering => Time.time<recoveryUntil;
        private NavMeshAgent agent;
        private Health health, targetHealth;
        private EnemyGeneration generation;
        private Transform target;
        private RunState run;
        private Vector3[] patrol;
        private Vector3 lastSeen;
        private float memoryUntil, nextAttack, strikeAt;
        private bool windingUp;
        private bool lunging;
        private Vector3 lungeDirection;
        private float lungeUntil;
        private float readyAt;
        private float nextPathRefresh;
        private float nextDodge;
        private Vector3 dodgeDestination;
        private float dodgeUntil;
        private System.Random behaviour;
        private float decisionUntil, pace = 1f, hitReactionUntil;
        private int approach;
        private Vector3 approachDestination;
        private bool sawPlayer;
        private float Range(float min,float max) => min+(float)behaviour.NextDouble()*(max-min);
        public bool CombatStance { get; private set; }
        public float DodgeLean { get; private set; }
        private Renderer[] bodies;
        private MaterialPropertyBlock bodyProperties;
        public string State { get; private set; } = "Roaming";
        public event System.Action<Vector3, int> Defeated;
        public void HearNoise(Vector3 point, float radius)
        {
            if (health == null || !health.IsAlive || target == null || (transform.position - point).sqrMagnitude > radius * radius) return;
            // Sound gives a last-known position, never permission to attack through walls.
            lastSeen = point; memoryUntil = Time.time + memoryDuration; nextPathRefresh = 0;
        }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<Health>();
            generation = GetComponent<EnemyGeneration>();
            bodies = GetComponentsInChildren<Renderer>();
            bodyProperties = new MaterialPropertyBlock();
        }
        private void OnEnable() { health.Died += Die; health.Damaged += ReactToHit; }
        private void OnDisable() { health.Died -= Die; health.Damaged -= ReactToHit; }
        private void ReactToHit(float damage)
        {
            if (!health.IsAlive || behaviour == null || Time.time < hitReactionUntil) return;
            hitReactionUntil=Time.time+Range(.8f,1.5f);
            staggerStarted=Time.time;staggerUntil=Time.time+.38f;
            windingUp=false;lunging=false;striking=false;dodgeUntil=0;ShowWindup(false);
            lastSeen=target.position;memoryUntil=Time.time+memoryDuration;
            if(behaviour.NextDouble()<.7)nextDodge=Mathf.Min(nextDodge,Time.time+Range(.15f,.4f));
            decisionUntil=Time.time;
        }
        public void Configure(Transform player, Health playerHealth, RunState runState, Vector3[] points)
        {
            target = player; targetHealth = playerHealth; run = runState; patrol = points;
            readyAt = Time.time + (generation.Generation > 0 ? 1f : 0f);
            behaviour=new System.Random(unchecked(System.Guid.NewGuid().GetHashCode()+generation.Generation*31));
            nextDodge=readyAt+Range(1.1f,3.3f);decisionUntil=readyAt;
            idleUntil=readyAt+Range(4f,9f);
        }
        private bool CanSeePlayer()
        {
            Vector3 origin = transform.position + Vector3.up * 1.4f;
            Vector3 endpoint = target.position + Vector3.up * 1.4f;
            if(!EnemyVision.InCone(transform.forward,endpoint-origin,detectionRange,visionAngle))return false;
            if (!Physics.Linecast(origin, endpoint, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore)) return true;
            return hit.transform == target || hit.transform.IsChildOf(target);
        }
        private void Update()
        {
            if (!agent.isOnNavMesh || target == null || targetHealth == null) return;
            if (!health.IsAlive || !targetHealth.IsAlive || (run != null && run.HasEnded))
            { agent.isStopped = true; windingUp = false; lunging = false;striking=false;recoveryUntil=0; ShowWindup(false); State = "Stopped"; return; }
            if (Time.time < readyAt) { agent.isStopped = true; State = "Spawning"; return; }
            agent.speed = generation.Speed;
            float distance = Vector3.Distance(transform.position, target.position);
            bool visible = distance <= detectionRange && CanSeePlayer();
            CombatStance = visible || Time.time < memoryUntil;
            if(visible && !sawPlayer) { approach=0;decisionUntil=Time.time+Range(.25f,.65f); }
            sawPlayer=visible;
            if (visible) { lastSeen = target.position; memoryUntil = Time.time + memoryDuration; }
            if(Time.time<staggerUntil) { agent.isStopped=true;State="Hit stagger";return; }
            if(Time.time<recoveryUntil) { agent.isStopped=true;State="Attack recovery";return; }
            if (!windingUp && !lunging && !striking && visible && distance > 2.6f && distance < 8f && Time.time >= nextDodge)
            {
                nextDodge = Time.time + Range(1.6f,4.2f);
                Vector3 toward = target.position - transform.position; toward.y = 0; toward.Normalize();
                Vector3 side = Vector3.Cross(Vector3.up, toward);
                if (behaviour.NextDouble() < .5) side = -side;
                var camera=target.GetComponentInChildren<Camera>();
                bool aimed=camera!=null && Vector3.Dot(camera.transform.forward,-toward)>.94f;
                // Sometimes evade sideways, sometimes step diagonally into the gap.
                Vector3 offset=(side+ (behaviour.NextDouble()<.4 ? toward*.85f : Vector3.zero)).normalized*Range(.85f,1.25f);
                if (behaviour.NextDouble()<(aimed?.72:.32) && ClearStep(transform.position+offset,out Vector3 spot))
                {
                    dodgeDestination = spot;dodgeStart=transform.position;dodgeStarted=Time.time; dodgeUntil = Time.time + Range(.45f,.60f);
                    DodgeLean = Vector3.Dot(side, transform.right) > 0 ? -8f : 8f;
                    decisionUntil=dodgeUntil+Range(.2f,.6f);
                }
            }
            if (!windingUp && !lunging && !striking && Time.time < dodgeUntil)
            {
                State = "Dodging"; agent.isStopped = true;
                agent.updateRotation = false;
                Vector3 facing = target.position - transform.position; facing.y = 0;
                if (facing.sqrMagnitude > 0.01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(facing), 360f * Time.deltaTime);
                float t=Mathf.Clamp01((Time.time-dodgeStarted)/(dodgeUntil-dodgeStarted));
                Vector3 desired=Vector3.Lerp(dodgeStart,dodgeDestination,Mathf.SmoothStep(0,1,t));
                if(ClearStep(desired,out Vector3 safe))agent.Move(safe-transform.position);
                return;
            }
            DodgeLean = Mathf.MoveTowards(DodgeLean, 0f, Time.deltaTime * 40f);
            agent.updateRotation = true;
            agent.acceleration = CombatStance ? 7f : 5f;
            if (lunging)
            {
                State = "Lunging";
                Vector3 step = lungeDirection * (generation.Speed * 2.5f) * Mathf.Min(Time.deltaTime, Mathf.Max(0f, lungeUntil - Time.time));
                if (NavMesh.Raycast(transform.position, transform.position + step, out NavMeshHit edge, agent.areaMask))
                    step = Vector3.ClampMagnitude(edge.position - transform.position, step.magnitude);
                agent.Move(step);
                if (Time.time >= lungeUntil)
                {
                    lunging = false; ShowWindup(false);
                    if (Vector3.Distance(transform.position, target.position) <= attackRange && CanSeePlayer())
                        targetHealth.TakeDamage(generation.Damage);
                    recoveryUntil=Time.time+.45f;
                }
                return;
            }
            if(striking)
            {
                State="Claw strike";agent.isStopped=true;
                if(!impactDone && Time.time-strikeStarted>=.12f)
                { impactDone=true;if(distance<=attackRange && CanSeePlayer())targetHealth.TakeDamage(generation.Damage); }
                if(Time.time-strikeStarted>=.25f) { striking=false;recoveryUntil=Time.time+.45f; }
                return;
            }
            if (windingUp)
            {
                State = "Attack windup";
                ShowWindup(true);
                if (Time.time >= strikeAt)
                {
                    windingUp = false;
                    if (generation.Generation > 0)
                    {
                        lunging = true; lungeUntil = Time.time + 0.25f;
                        strikeStarted=Time.time;
                        lungeDirection = target.position - transform.position;
                        lungeDirection.y = 0; lungeDirection.Normalize();
                        return;
                    }
                    ShowWindup(false);
                    striking=true;impactDone=false;strikeStarted=Time.time;
                }
                return;
            }
            if (visible && distance <= (generation.Generation > 0 ? 2.7f : attackRange))
            {
                agent.isStopped = true;
                State = "Attacking";
                Vector3 direction = target.position - transform.position;
                direction.y = 0;
                if (direction.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), 360f * Time.deltaTime);
                if (Time.time >= nextAttack)
                { windingUp = true; strikeAt = Time.time + (generation.Generation > 0 ? 0.45f : attackWindup); nextAttack = strikeAt + attackCooldown + (generation.Generation > 0 ? 0.25f : 0f); }
                return;
            }
            agent.isStopped = false;
            if(visible && Time.time>=decisionUntil)
            {
                int previous=approach;float roll=Range(0,1);
                approach=distance>7 ? 1 : roll<.15f && previous!=0 ? 0 : roll<.58f ? 1 : roll<.84f ? 2 : 3;
                pace=approach==3?Range(1.55f,2.1f):Range(.65f,1.05f);
                decisionUntil=Time.time+(approach==0?Range(.3f,.8f):approach==3?Range(.45f,.9f):Range(.8f,1.8f));
                Vector3 toward=(target.position-transform.position);toward.y=0;toward.Normalize();
                Vector3 side=Vector3.Cross(Vector3.up,toward)*(behaviour.NextDouble()<.5?-1:1);
                if(approach==2 && !ClearStep(transform.position+toward*1.15f+side*Range(.5f,1.1f),out approachDestination))approach=1;
            }
            if(visible && approach==0)
            { State="Watching";agent.isStopped=true;agent.updateRotation=false;FacePlayer();return; }
            if(visible && approach==2)
            {
                State="Circling";agent.updateRotation=false;agent.speed=generation.Speed*pace;agent.stoppingDistance=.12f;
                FacePlayer();agent.SetDestination(approachDestination);return;
            }
            if (visible || Time.time < memoryUntil)
            {
                State = visible ? approach==3?"Closing in":"Stalking" : "Searching";
                agent.speed=generation.Speed*(visible?pace:1.1f);
                agent.stoppingDistance = attackRange * 0.75f;
                Vector3 destination = lastSeen;
                // Lead a moving player slightly, without predicting through walls.
                var playerBody = target.GetComponent<CharacterController>();
                if (visible && playerBody != null && NavMesh.SamplePosition(lastSeen + playerBody.velocity * 0.3f, out NavMeshHit lead, 0.5f, agent.areaMask)
                    && !NavMesh.Raycast(lastSeen, lead.position, out NavMeshHit blocked, agent.areaMask)) destination = lead.position;
                if (Time.time >= nextPathRefresh || !agent.hasPath)
                { nextPathRefresh = Time.time + 0.15f; agent.SetDestination(destination); }
            }
            else if (patrol != null && patrol.Length > 0)
            {
                if(Time.time<roamUntil && agent.hasPath && (agent.pathPending || agent.remainingDistance>.3f))
                { State="Short patrol";agent.isStopped=false;return; }
                agent.isStopped=true;State="Idle";
                if(Time.time>=idleUntil)
                {
                    idleUntil=Time.time+Range(7f,12f);int first=behaviour.Next(patrol.Length);
                    for(int i=0;i<patrol.Length;i++)
                    {
                        Vector3 point=patrol[(first+i)%patrol.Length];float length=Vector3.Distance(transform.position,point);
                        if(length<1f || length>6f)continue;
                        var path=new NavMeshPath();
                        if(agent.CalculatePath(point,path)&&path.status==NavMeshPathStatus.PathComplete)
                        { agent.stoppingDistance=.2f;agent.isStopped=false;agent.SetPath(path);roamUntil=Time.time+Range(1.5f,3f);State="Short patrol";break; }
                    }
                }
            }
        }
        private bool ClearStep(Vector3 destination,out Vector3 result)
        {
            result=transform.position;
            if(!NavMesh.SamplePosition(destination,out NavMeshHit hit,.25f,agent.areaMask) || NavMesh.Raycast(transform.position,hit.position,out NavMeshHit edge,agent.areaMask))return false;
            if(Physics.CheckCapsule(hit.position+Vector3.up*.4f,hit.position+Vector3.up*1.6f,.32f,1<<1,QueryTriggerInteraction.Ignore))return false;
            result=hit.position;return true;
        }
        private void FacePlayer()
        {
            Vector3 direction=target.position-transform.position;direction.y=0;
            if(direction.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(direction),180f*Time.deltaTime);
        }
        private void Die()
        {
            State = "Defeated";
            if (agent.isOnNavMesh) agent.isStopped = true;
            gameObject.SetActive(false);
            Defeated?.Invoke(transform.position, generation.Generation);
            Destroy(gameObject);
        }
        private void ShowWindup(bool active)
        {
            if (bodies == null) return;
            if (!active) { foreach (var body in bodies) if (body != null && body.enabled) body.SetPropertyBlock(null); return; }
            foreach (var body in bodies) if (body != null && body.enabled)
            {
                Color baseColor=body.sharedMaterial.GetColor("_BaseColor");
                bodyProperties.SetColor("_BaseColor",Color.Lerp(baseColor,new Color(.65f,.35f,.22f),.18f));
                body.SetPropertyBlock(bodyProperties);
            }
        }
    }
}

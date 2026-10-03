using UnityEngine;
using UnityEngine.AI;

public class CowAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CowLogic cowLogic;
    private NavMeshAgent agent;
    private Animator animator;

    [Header("AI Settings")]
    [SerializeField] private float searchInterval = 1.0f;
    [SerializeField] private float detectionRadius = 15f; 
    [SerializeField] private float maxReachDistance = 2f; 
    [SerializeField] private float troughReachDistance = 1f; 
    
    [Header("Wander Settings")]
    [SerializeField] private float wanderRadius = 5f;
    [SerializeField] private float minWanderWait = 2f;
    [SerializeField] private float maxWanderWait = 5f;

    [Header("Herding Settings")]
    [SerializeField] private float normalWalkSpeed = 1.2f;
    [SerializeField] private float herdingSpeed = 1.8f; 
    [SerializeField] private float reverseSpeed = 1.0f;
    [SerializeField] private float angularSpeed = 120f; 

    [Header("Reaction Distances")]
    [SerializeField] private float frontReactionDistance = 1.8f; // Longer for the neck/head
    [SerializeField] private float leftReactionDistance = 1f;  // Shorter for the ribs
    [SerializeField] private float rightReactionDistance = 1f; // Shorter for the ribs
    
    [Header("Herd Mentality Settings")]
    [SerializeField] private float herdFollowRadius = 15f;
    [SerializeField] private float herdFollowSpeed = 1.5f; 

    private Transform currentTarget;
    private Transform herdLeader; 
    private Vector3 currentWalkTarget; 
    private float searchTimer = 0f;
    private float wanderTimer = 0f;
    private float currentWaitTime = 0f;

    private float pressureBuffer = 0.2f; 
    private float frontPlayerTimer, rearPlayerTimer, leftPlayerTimer, rightPlayerTimer;
    private float cowRearTimer; 
    private Vector3 lastPlayerPos;

    private float reactionCooldownTimer = 0f;
    private float reactionPauseTimer = 0f;

    public bool HasPlayerPressure { get; private set; }
    public bool IsMovingForward { get; private set; } 
    public Transform player;

    void Start()
    {
        // Find the player automatically using the Main Camera (which is on your XR Rig)
        if (player == null && Camera.main != null)
        {
            player = Camera.main.transform;
        }
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>(); 
        if (cowLogic == null) cowLogic = GetComponent<CowLogic>();

        agent.speed = normalWalkSpeed;
        agent.angularSpeed = angularSpeed;
        agent.acceleration = 6f; 
        currentWaitTime = Random.Range(minWanderWait, maxWanderWait);
    }

    void Update()
        {
            UpdateTimers();

            // --- 1. CHECK REACTION DISTANCE FIRST ---
            if (player != null && reactionCooldownTimer <= 0)
            {
                // Flatten the Y-axis so VR headset height doesn't break the distance math
                Vector3 flatPlayerPos = player.position;
                flatPlayerPos.y = transform.position.y;
                
                float distanceToPlayer = Vector3.Distance(transform.position, flatPlayerPos);
                
                // Convert flat player position to the cow's local space to determine direction FIRST
                Vector3 localPlayerPos = transform.InverseTransformPoint(flatPlayerPos);

                bool triggerAnimation = false;
                FlightZoneType reactionZone = FlightZoneType.Front;

                // Check WHICH side the player is on, then apply the specific distance threshold
                if (localPlayerPos.z > 0 && Mathf.Abs(localPlayerPos.x) < localPlayerPos.z)
                {
                    if (distanceToPlayer <= frontReactionDistance)
                    {
                        triggerAnimation = true;
                        reactionZone = FlightZoneType.Front;
                    }
                }
                else if (localPlayerPos.x < 0)
                {
                    if (distanceToPlayer <= leftReactionDistance)
                    {
                        triggerAnimation = true;
                        reactionZone = FlightZoneType.Left;
                    }
                }
                else
                {
                    if (distanceToPlayer <= rightReactionDistance)
                    {
                        triggerAnimation = true;
                        reactionZone = FlightZoneType.Right;
                    }
                }

                // If any of the specific distance thresholds were met, execute the reaction
                if (triggerAnimation)
                {
                    // Stop movement immediately
                    if (agent.isActiveAndEnabled)
                    {
                        agent.isStopped = true;
                        agent.velocity = Vector3.zero;
                    }
                    
                    TriggerReaction(reactionZone);
                    UpdateAnimator(); // Force animator to register 0 speed before exiting
                    return; // EXIT EARLY: Completely ignore herding this frame
                }
            }

            // --- 2. CHECK ONGOING STATES ---
            bool isConsuming = cowLogic != null && (cowLogic.IsEating || cowLogic.IsDrinking);
            bool isReacting = reactionPauseTimer > 0f;

            // Handle Eating, Drinking, or Reacting State
            if (isConsuming || isReacting)
            {
                if (agent.isActiveAndEnabled && !agent.isStopped) 
                {
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                }
                UpdateAnimator();
                return; // EXIT EARLY: Do not process herding while busy
            }
            else
            {
                if (agent.isActiveAndEnabled && agent.isStopped) 
                    agent.isStopped = false;
            }

            // --- 3. PROCESS HERDING ---
            // This now ONLY runs if the player is safely outside the reaction distance 
            // and the cow is not currently eating or frozen in a reaction pause.
            ProcessHerding();

            // --- 4. PROCESS WANDERING / SURVIVAL ---
            if (!HasPlayerPressure && cowRearTimer <= 0)
            {
                agent.updateRotation = true;
                ProcessSurvivalAI();
            }

            UpdateAnimator();
        }

    public void ApplyContinuousPressure(FlightZoneType zone, Vector3 pusherPosition, bool isPlayer)
    {
        if (isPlayer)
        {
            lastPlayerPos = pusherPosition;
            
            /*
            float distanceToPlayer = Vector3.Distance(transform.position, pusherPosition);

            // Trigger close-proximity animations
            if (distanceToPlayer <= reactionDistance && reactionCooldownTimer <= 0f)
            {
                TriggerReaction(zone);
            }
            */

            switch (zone)
            {
                case FlightZoneType.Front: frontPlayerTimer = pressureBuffer; break;
                case FlightZoneType.Rear: rearPlayerTimer = pressureBuffer; break;
                case FlightZoneType.Left: leftPlayerTimer = pressureBuffer; break;
                case FlightZoneType.Right: rightPlayerTimer = pressureBuffer; break;
            }
        }
        else
        {
            if (zone == FlightZoneType.Rear)
                cowRearTimer = pressureBuffer;
        }
    }

    private void TriggerReaction(FlightZoneType zone)
    {
        if (animator == null) return;

        reactionCooldownTimer = 3.0f; // Prevent spamming

        switch (zone)
        {
            case FlightZoneType.Front: 
                animator.SetTrigger("HeadShake"); 
                reactionPauseTimer = 1.5f; // Matches 1.5s HeadShake
                break;
            case FlightZoneType.Left: 
                animator.SetTrigger("LeftKick"); 
                reactionPauseTimer = 1.0f; // Matches 1.0s Kick
                break;
            case FlightZoneType.Right: 
                animator.SetTrigger("RightKick"); 
                reactionPauseTimer = 1.0f; // Matches 1.0s Kick
                break;
        }
    }

    private void UpdateTimers()
    {
        float dt = Time.deltaTime;
        frontPlayerTimer -= dt;
        rearPlayerTimer -= dt;
        leftPlayerTimer -= dt;
        rightPlayerTimer -= dt;
        cowRearTimer -= dt;
        
        if (reactionCooldownTimer > 0) reactionCooldownTimer -= dt;
        if (reactionPauseTimer > 0) reactionPauseTimer -= dt;
    }

    private void ProcessHerding()
    {
        HasPlayerPressure = frontPlayerTimer > 0 || rearPlayerTimer > 0 || leftPlayerTimer > 0 || rightPlayerTimer > 0;
        bool hasCowRearPressure = cowRearTimer > 0;

        if (!HasPlayerPressure && !hasCowRearPressure) 
        {
            IsMovingForward = false;
            return;
        }

        herdLeader = null; 
        Vector3 moveDirection = Vector3.zero;
        currentTarget = null; 
        IsMovingForward = true;

        if (HasPlayerPressure)
        {
            agent.speed = herdingSpeed;
            agent.updateRotation = true; 

            Vector3 awayFromPlayer = (transform.position - lastPlayerPos);
            awayFromPlayer.y = 0;
            Vector3 awayDir = (awayFromPlayer.sqrMagnitude < 0.001f) ? transform.forward : awayFromPlayer.normalized;

            if (rearPlayerTimer > 0) moveDirection += awayDir;
            if (leftPlayerTimer > 0 || rightPlayerTimer > 0) moveDirection += (transform.forward + awayDir * 1.5f).normalized;

            if (frontPlayerTimer > 0)
            {
                moveDirection = -transform.forward;
                agent.speed = reverseSpeed;
                agent.updateRotation = false; 
                IsMovingForward = false; 
            }
        }
        else if (hasCowRearPressure)
        {
            agent.speed = herdFollowSpeed;
            agent.updateRotation = true;
            moveDirection = transform.forward;
        }

        if (moveDirection != Vector3.zero)
        {
            Vector3 targetPosition = transform.position + (moveDirection.normalized * 4f);
            if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 4f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }
    }

    private void ProcessSurvivalAI()
    {
        searchTimer += Time.deltaTime;

        if (searchTimer >= searchInterval)
        {
            searchTimer = 0f;
            FindHerdLeader();

            if (herdLeader == null && cowLogic != null)
            {
                if (currentTarget != null && !cowLogic.IsThirsty && currentTarget.GetComponent<WaterTrough>() != null)
                {
                    currentTarget = null;
                }
                
                if (currentTarget == null) 
                {
                    if (cowLogic.IsThirsty) FindNearestTrough();
                    if (currentTarget == null && cowLogic.IsHungry) FindNearestFood();
                }
            }
            else if (herdLeader == null)
            {
                currentTarget = null;
            }

            if (herdLeader != null)
            {
                agent.speed = herdFollowSpeed; 
                Vector3 predictedHerdPoint = herdLeader.position + (herdLeader.forward * 3f);
                agent.SetDestination(predictedHerdPoint);
            }
            else if (currentTarget != null)
            {
                agent.speed = normalWalkSpeed;
                agent.SetDestination(currentWalkTarget);
            }
        }

        if (herdLeader == null && currentTarget == null)
        {
            agent.speed = normalWalkSpeed;
            Wander();
        }
    }

    private void FindHerdLeader()
    {
        herdLeader = null;
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, herdFollowRadius);
        
        foreach (Collider col in nearbyColliders)
        {
            if (col.CompareTag("Cow") && col.transform.root != transform.root)
            {
                CowAI otherCow = col.GetComponentInParent<CowAI>();
                if (otherCow != null && otherCow.HasPlayerPressure && otherCow.IsMovingForward)
                {
                    Vector3 dirToOther = (otherCow.transform.position - transform.position).normalized;
                    if (Vector3.Dot(transform.forward, dirToOther) > -0.2f) 
                    {
                        herdLeader = otherCow.transform;
                        break; 
                    }
                }
            }
        }
    }

    private void FindNearestFood()
    {
        FeedItem[] allFood = FindObjectsByType<FeedItem>(FindObjectsSortMode.None);
        float closestDistance = Mathf.Infinity;
        Transform bestTarget = null;
        Vector3 bestWalkPoint = Vector3.zero;
        NavMeshPath path = new NavMeshPath();

        foreach (FeedItem food in allFood)
        {
            if (food == null || food.isBeingEaten || food.isHeld) continue;

            float distanceToFood = Vector3.Distance(transform.position, food.transform.position);
            if (distanceToFood < closestDistance && distanceToFood <= detectionRadius)
            {
                if (NavMesh.SamplePosition(food.transform.position, out NavMeshHit hit, detectionRadius, NavMesh.AllAreas))
                {
                    agent.CalculatePath(hit.position, path);
                    if (path.status == NavMeshPathStatus.PathComplete || path.status == NavMeshPathStatus.PathPartial)
                    {
                        if (path.corners.Length > 0)
                        {
                            Vector3 pathEnd = path.corners[path.corners.Length - 1];
                            if (Vector3.Distance(pathEnd, food.transform.position) <= maxReachDistance)
                            {
                                closestDistance = distanceToFood;
                                bestTarget = food.transform;
                                bestWalkPoint = pathEnd;
                            }
                        }
                    }
                }
            }
        }

        if (bestTarget != null)
        {
            currentTarget = bestTarget;
            currentWalkTarget = bestWalkPoint;
        }
    }

    private void FindNearestTrough()
    {
        WaterTrough[] troughs = FindObjectsByType<WaterTrough>(FindObjectsSortMode.None);
        float closestDistance = Mathf.Infinity;
        Transform bestTarget = null;
        Vector3 bestWalkPoint = Vector3.zero;
        NavMeshPath path = new NavMeshPath();

        foreach (WaterTrough trough in troughs)
        {
            if (trough == null || !trough.HasWater) continue;

            float distanceToTrough = Vector3.Distance(transform.position, trough.transform.position);
            if (distanceToTrough < closestDistance && distanceToTrough <= detectionRadius)
            {
                if (NavMesh.SamplePosition(trough.transform.position, out NavMeshHit hit, detectionRadius, NavMesh.AllAreas))
                {
                    agent.CalculatePath(hit.position, path);
                    if (path.status == NavMeshPathStatus.PathComplete || path.status == NavMeshPathStatus.PathPartial)
                    {
                        if (path.corners.Length > 0)
                        {
                            Vector3 pathEnd = path.corners[path.corners.Length - 1];
                            if (Vector3.Distance(pathEnd, trough.transform.position) <= troughReachDistance) 
                            {
                                closestDistance = distanceToTrough;
                                bestTarget = trough.transform;
                                bestWalkPoint = pathEnd;
                            }
                        }
                    }
                }
            }
        }

        if (bestTarget != null)
        {
            currentTarget = bestTarget;
            currentWalkTarget = bestWalkPoint;
        }
    }

    private void Wander()
    {
        if (agent.pathPending || agent.remainingDistance > agent.stoppingDistance) return;

        wanderTimer += Time.deltaTime;

        if (wanderTimer >= currentWaitTime)
        {
            wanderTimer = 0f;
            currentWaitTime = Random.Range(minWanderWait, maxWanderWait); 
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius + transform.position;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            {
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    agent.SetDestination(hit.position);
                }
            }
        }
    }

    private void UpdateAnimator()
    {
        if (animator != null)
        {
            float speed = agent.velocity.magnitude;
            animator.SetFloat("Speed", speed);
            
            bool isGrazing = speed < 0.1f && currentTarget == null && herdLeader == null && !HasPlayerPressure;
            animator.SetBool("IsGrazing", isGrazing);
            
            animator.SetBool("IsMovingForward", IsMovingForward);
            
            if (cowLogic != null)
            {
                animator.SetBool("IsConsuming", cowLogic.IsEating || cowLogic.IsDrinking);
                animator.SetBool("IsSick", cowLogic.IsSick); 
                animator.SetBool("IsLyingDown", cowLogic.IsLyingDown); 
            }
        }
    }
}
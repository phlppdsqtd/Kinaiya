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

    public bool HasPlayerPressure { get; private set; }
    public bool IsMovingForward { get; private set; } 

    void Start()
    {
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
        UpdatePressureTimers();
        ProcessHerding();

        // Handle Eating/Drinking State
        if (cowLogic != null && (cowLogic.IsEating || cowLogic.IsDrinking))
        {
            if (agent.isActiveAndEnabled && !agent.isStopped) 
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
            UpdateAnimator();
            return;
        }
        else
        {
            if (agent.isActiveAndEnabled && agent.isStopped) 
                agent.isStopped = false;
        }

        // Arrival Logic: Check if we reached our target and should consume it
        if (currentTarget != null && !HasPlayerPressure && herdLeader == null)
        {
            float distToWalkTarget = Vector3.Distance(transform.position, currentWalkTarget);
            
            // If the agent has arrived near the final path point
            if (distToWalkTarget <= agent.stoppingDistance + 1f)
            {
                WaterTrough trough = currentTarget.GetComponent<WaterTrough>();
                if (trough != null && cowLogic.IsThirsty)
                {
                    cowLogic.ConsumeWater(trough);
                    currentTarget = null; // Clear target after interacting
                }
                
                FeedItem food = currentTarget.GetComponent<FeedItem>();
                if (food != null && cowLogic.IsHungry)
                {
                    cowLogic.ConsumeFood(food);
                    currentTarget = null; // Clear target after interacting
                }
            }
        }

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

    private void UpdatePressureTimers()
    {
        frontPlayerTimer -= Time.deltaTime;
        rearPlayerTimer -= Time.deltaTime;
        leftPlayerTimer -= Time.deltaTime;
        rightPlayerTimer -= Time.deltaTime;
        cowRearTimer -= Time.deltaTime;
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
                // Only search for a new target if we don't already have one
                if (currentTarget == null) 
                {
                    if (cowLogic.IsThirsty)
                    {
                        FindNearestTrough();
                    }
                    
                    if (currentTarget == null && cowLogic.IsHungry)
                    {
                        FindNearestFood();
                    }
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
        }
    }
}
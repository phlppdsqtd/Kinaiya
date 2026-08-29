using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class CowAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CowLogic cowLogic;
    private NavMeshAgent agent;
    private Animator animator;

    [Header("AI Settings")]
    [SerializeField] private float searchInterval = 1.0f;
    [Tooltip("How close food needs to be for the cow to notice it.")]
    [SerializeField] private float detectionRadius = 15f;
    [Tooltip("How far the cow can reach its head past the edge of the walkable area.")]
    [SerializeField] private float maxReachDistance = 3.0f; 
    
    [Header("Wander Settings")]
    [SerializeField] private float wanderRadius = 10f;
    [SerializeField] private float minWanderWait = 2f;
    [SerializeField] private float maxWanderWait = 7f;

    private Transform currentTarget;
    private Vector3 currentWalkTarget; 
    private float searchTimer = 0f;
    private float wanderTimer = 0f;
    private float currentWaitTime = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>(); 
        if (cowLogic == null) cowLogic = GetComponent<CowLogic>();

        currentWaitTime = Random.Range(minWanderWait, maxWanderWait);
    }

    void Update()
    {
        searchTimer += Time.deltaTime;

        // 1. Search for food if hungry
        if (searchTimer >= searchInterval)
        {
            searchTimer = 0f;

            if (cowLogic != null && cowLogic.IsHungry)
            {
                FindNearestFood();
            }
            else
            {
                currentTarget = null;
            }
        }

        // 2. Move to food or wander
        if (currentTarget != null)
        {
            // Use a wider search radius here (10f) so tracking doesn't break if food rolls slightly
            if (NavMesh.SamplePosition(currentTarget.position, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                currentWalkTarget = hit.position;
            }
            agent.SetDestination(currentWalkTarget);
        }
        else
        {
            Wander();
        }

        // 3. Update Animator
        if (animator != null)
        {
            float speed = agent.velocity.magnitude;
            animator.SetFloat("Speed", speed);
            
            animator.SetBool("IsGrazing", speed < 0.1f && currentTarget == null);
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
            if (food == null || food.isBeingEaten) continue;

            bool isInTrough = false;
            if (food.TryGetComponent<XRGrabInteractable>(out var grab))
            {
                var interactor = grab.firstInteractorSelecting as MonoBehaviour;
                if (interactor != null && interactor.GetComponent<XRSocketInteractor>() != null)
                {
                    isInTrough = true;
                }
            }

            if (food.isHeld && !isInTrough) continue;

            float distanceToFood = Vector3.Distance(transform.position, food.transform.position);
            
            if (distanceToFood < closestDistance && distanceToFood <= detectionRadius)
            {
                // Find ANY navmesh point near the food using the full detection radius
                if (NavMesh.SamplePosition(food.transform.position, out NavMeshHit hit, detectionRadius, NavMesh.AllAreas))
                {
                    // Calculate a path to that point
                    agent.CalculatePath(hit.position, path);
                    
                    // ACCEPT BOTH Complete AND Partial paths
                    if (path.status == NavMeshPathStatus.PathComplete || path.status == NavMeshPathStatus.PathPartial)
                    {
                        if (path.corners.Length > 0)
                        {
                            // Get the very last point the cow can physically walk to
                            Vector3 pathEnd = path.corners[path.corners.Length - 1];
                            
                            // The true test: Is the end of the cow's path close enough to reach the food?
                            if (Vector3.Distance(pathEnd, food.transform.position) <= maxReachDistance)
                            {
                                closestDistance = distanceToFood;
                                bestTarget = food.transform;
                                bestWalkPoint = pathEnd; // The cow stops right at the edge of the walkable area
                            }
                        }
                    }
                }
            }
        }

        currentTarget = bestTarget;
        currentWalkTarget = bestWalkPoint;
    }

    private void Wander()
    {
        if (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
            return;

        wanderTimer += Time.deltaTime;

        if (wanderTimer >= currentWaitTime)
        {
            wanderTimer = 0f;
            currentWaitTime = Random.Range(minWanderWait, maxWanderWait); 
            
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius + transform.position;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }
    }
}
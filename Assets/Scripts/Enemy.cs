using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// FSM States for the enemy
public enum EnemyState { STATIC, CHASE, REST, MOVING, DEFAULT };

public enum EnemyBehavior { EnemyBehavior1, EnemyBehavior2, EnemyBehavior3 };

public class Enemy : MonoBehaviour
{
    // Pathfinding
    protected PathFinder pathFinder;
    public GenerateMap mapGenerator;
    protected Queue<Tile> path;
    protected GameObject playerGameObject;

    public Tile currentTile;
    protected Tile targetTile;
    public Vector3 velocity;

    // Properties
    public float speed = 1.0f;
    public float visionDistance = 5f;
    public int maxCounter = 5;
    protected int playerCloseCounter;

    protected EnemyState state = EnemyState.DEFAULT;
    protected Material material;

    public EnemyBehavior behavior = EnemyBehavior.EnemyBehavior1;

    // Start is called before the first frame update
    void Start()
    {
        path = new Queue<Tile>();
        pathFinder = new PathFinder();
        playerGameObject = GameObject.FindWithTag("Player");
        playerCloseCounter = maxCounter;
        material = GetComponent<MeshRenderer>().material;
    }

    // Update is called once per frame
    void Update()
    {
        if (mapGenerator.state == MapState.DESTROYED) return;

        // Stop moving the enemy if the player has reached the goal or is dead
        if (playerGameObject.GetComponent<Player>().IsGoalReached() || playerGameObject.GetComponent<Player>().IsPlayerDead())
        {
            return;
        }

        switch (behavior)
        {
            case EnemyBehavior.EnemyBehavior1:
                HandleEnemyBehavior1();
                break;
            case EnemyBehavior.EnemyBehavior2:
                HandleEnemyBehavior2();
                break;
            case EnemyBehavior.EnemyBehavior3:
                HandleEnemyBehavior3();
                break;
            default:
                break;
        }
    }

    public void Reset()
{
    Debug.Log("Enemy reset");

    // Check if pathFinder is initialized
    if (pathFinder == null)
    {
        Debug.LogError("pathFinder is not initialized!");
        return;
    }

    // Check if mapGenerator is assigned
    if (mapGenerator == null)
    {
        Debug.LogError("mapGenerator is not assigned!");
        return;
    }

    path.Clear();
    state = EnemyState.DEFAULT;

    // Check if FindWalkableTile() returns a valid tile
    currentTile = FindWalkableTile();
    if (currentTile == null)
    {
        Debug.LogError("currentTile is null after calling FindWalkableTile!");
        return;
    }

    transform.position = currentTile.transform.position;
}


    Tile FindWalkableTile()
    {
        Tile newTarget = null;
        int randomIndex = 0;
        while (newTarget == null || !newTarget.mapTile.Walkable)
        {
            randomIndex = (int)(Random.value * mapGenerator.width * mapGenerator.height - 1);
            newTarget = GameObject.Find("MapGenerator").transform.GetChild(randomIndex).GetComponent<Tile>();
        }
        return newTarget;
    }

    // Dumb Enemy: Keeps walking in a random direction, does not chase player
    private void HandleEnemyBehavior1()
    {
        switch (state)
        {
            case EnemyState.DEFAULT: // Generate random path
                material.color = Color.white; // Color differentiation for behavior 1

                if (path.Count <= 0) path = pathFinder.RandomPath(currentTile, 20);

                if (path.Count > 0)
                {
                    targetTile = path.Dequeue();
                    state = EnemyState.MOVING;
                }
                break;

            case EnemyState.MOVING:
                // Move toward target
                velocity = targetTile.gameObject.transform.position - transform.position;
                transform.position = transform.position + (velocity.normalized * speed) * Time.deltaTime;

                // Check if target reached
                if (Vector3.Distance(transform.position, targetTile.gameObject.transform.position) <= 0.05f)
                {
                    currentTile = targetTile;
                    state = EnemyState.DEFAULT;
                }
                break;

            default:
                state = EnemyState.DEFAULT;
                break;
        }
    }

    // Enemy chases the player when nearby
    private void HandleEnemyBehavior2()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerGameObject.transform.position);

        switch (state)
        {
            case EnemyState.DEFAULT:
                material.color = Color.red; // Color differentiation for behavior 2

                if (distanceToPlayer <= visionDistance)
                {
                    playerCloseCounter = maxCounter;
                    path = pathFinder.FindPathAStar(currentTile, GetPlayerTile());
                    if (path.Count > 0)
                    {
                        targetTile = path.Dequeue();
                        state = EnemyState.CHASE;
                    }
                }
                else
                {
                    if (path.Count <= 0) path = pathFinder.RandomPath(currentTile, 10);
                    if (path.Count > 0)
                    {
                        targetTile = path.Dequeue();
                        state = EnemyState.MOVING;
                    }
                }
                break;

            case EnemyState.CHASE:
                velocity = targetTile.gameObject.transform.position - transform.position;
                transform.position += velocity.normalized * speed * Time.deltaTime;

                if (Vector3.Distance(transform.position, targetTile.gameObject.transform.position) <= 0.05f)
                {
                    currentTile = targetTile;

                    if (path.Count > 0)
                    {
                        targetTile = path.Dequeue();
                    }
                    else
                    {
                        state = EnemyState.DEFAULT;
                    }
                }
                break;

            case EnemyState.MOVING:
                velocity = targetTile.gameObject.transform.position - transform.position;
                transform.position += velocity.normalized * speed * Time.deltaTime;

                if (Vector3.Distance(transform.position, targetTile.gameObject.transform.position) <= 0.05f)
                {
                    currentTile = targetTile;
                    state = EnemyState.DEFAULT;
                }
                break;

            default:
                state = EnemyState.DEFAULT;
                break;
        }
    }

    // Enemy behavior 3: Patrols or guards an area
    private void HandleEnemyBehavior3()
    {
        switch (state)
        {
            case EnemyState.DEFAULT:
                material.color = Color.blue; // Color differentiation for behavior 3

                // Define a patrol route or target points
                if (path.Count <= 0) path = DefinePatrolPath();
                if (path.Count > 0)
                {
                    targetTile = path.Dequeue();
                    state = EnemyState.MOVING;
                }
                break;

            case EnemyState.MOVING:
                velocity = targetTile.gameObject.transform.position - transform.position;
                transform.position += velocity.normalized * speed * Time.deltaTime;

                if (Vector3.Distance(transform.position, targetTile.gameObject.transform.position) <= 0.05f)
                {
                    currentTile = targetTile;
                    state = EnemyState.DEFAULT;
                }
                break;

            default:
                state = EnemyState.DEFAULT;
                break;
        }
    }

    private Tile GetPlayerTile()
    {
        // Retrieve the tile the player is currently on
        return mapGenerator.GetTileAtPosition(playerGameObject.transform.position);
    }

    private Queue<Tile> DefinePatrolPath()
    {
        // Define a patrol route or points for behavior 3
        Queue<Tile> patrolPath = new Queue<Tile>();
        Tile firstPoint = FindWalkableTile();
        Tile secondPoint = FindWalkableTile();

        patrolPath.Enqueue(firstPoint);
        patrolPath.Enqueue(secondPoint);

        return patrolPath;
    }
}

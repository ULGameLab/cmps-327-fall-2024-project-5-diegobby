using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MapGen;

//FSM States for the Player
public enum PlayerState {DEFAULT, MOVING, EVADE, GOAL_REACHED, DEAD };

public class Player : MonoBehaviour
{
    PathFinder pathFinder;
    public GenerateMap mapGenerator;
    public Queue<Tile> path;
    public Tile currentTile;
    public Tile targetTile;
    Vector3 velocity;

    //properties
    public float slowSpeed = 1.0f;
    public float fastSpeed = 2.0f;
    float speed = 1.0f;
    int enemyCloseCounter = 0;
    public int maxCounter = 5;
    public float visionDistance = 4;
    Material material;
    Color playerColor;

    PlayerState state = PlayerState.DEFAULT;

    //environment
    List<Enemy> enemyList;
    Enemy closestEnemy;

    //Explosion Effect
    ParticleSystem explosion;
    bool explosionStarted = false;

    // Start is called before the first frame update
    void Start()
    {
        path = new Queue<Tile>();
        pathFinder = new PathFinder();
        enemyList = new List<Enemy>((Enemy[]) GameObject.FindObjectsByType(typeof(Enemy), FindObjectsSortMode.None));
        material = GetComponent<MeshRenderer>().material;
        playerColor = material.color;
        currentTile = mapGenerator.start;
        explosion = GameObject.Find("Explosion").GetComponent<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        if (mapGenerator.state == MapState.DESTROYED) return;

        HandlePlayerFSMStates();
    }

    public bool IsGoalReached()
    {
        if (state == PlayerState.GOAL_REACHED) return true;
        else return false;
    }

    public bool IsPlayerDead()
    {
        if (state == PlayerState.DEAD) return true;
        else return false;
    }

    private void HandlePlayerFSMStates()
{
    switch (state)
    {
        case PlayerState.DEFAULT:
            material.color = playerColor;
            speed = slowSpeed;

            if (path.Count <= 0) 
                path = pathFinder.FindPathAStar(currentTile, mapGenerator.goal);

            if (path.Count > 0)
            {
                targetTile = path.Dequeue();
                state = PlayerState.MOVING;
            }
            break;

        case PlayerState.EVADE:
            speed = fastSpeed;
            material.color = Color.yellow;

            if (path.Count <= 0)
            {
                // Pass the closest enemy tiles or enemy list to pathFinder
                path = pathFinder.FindPathAStarEvadeEnemy(currentTile, mapGenerator.goal, enemyList);
                enemyCloseCounter = 0;
            }

            if (path.Count > 0) 
                targetTile = path.Dequeue();
            else 
                targetTile = FindEvadeTile(closestEnemy.gameObject);
                
            state = PlayerState.MOVING;
            break;

        case PlayerState.MOVING:
            velocity = targetTile.transform.position - transform.position;
            transform.position = transform.position + (velocity.normalized * speed) * Time.deltaTime;

            foreach (Enemy enemy in enemyList)
            {
                if (Vector3.Distance(enemy.gameObject.transform.position, transform.position) < 0.5f)
                {
                    state = PlayerState.DEAD;
                }
            }

            if (Vector3.Distance(transform.position, targetTile.transform.position) <= 0.05f)
            {
                currentTile = targetTile;
                enemyCloseCounter--;

                if (currentTile == mapGenerator.goal)
                {
                    state = PlayerState.GOAL_REACHED;
                    break;
                }

                if (enemyCloseCounter <= 0)
                {
                    foreach (Enemy enemy in enemyList)
                    {
                        if (Vector3.Distance(enemy.gameObject.transform.position, transform.position) < visionDistance)
                        {
                            closestEnemy = enemy;
                            path.Clear();
                            enemyCloseCounter = maxCounter;
                            break;
                        }
                    }
                }
                if (enemyCloseCounter > 0) 
                    state = PlayerState.EVADE;
                else 
                    state = PlayerState.DEFAULT;
            }
            break;

        case PlayerState.GOAL_REACHED:
            material.color = playerColor;
            break;

        case PlayerState.DEAD:
            Debug.Log("Player Dead");
            transform.gameObject.GetComponent<Renderer>().enabled = false;
            StartExplosion();
            break;

        default:
            state = PlayerState.DEFAULT;
            break;
    }
}


    // Find a tile to evade from an incomming enemy
    // Lookahead time is a fixed value but could be estimated as well
    private Tile FindEvadeTile(GameObject enemy)
{
    Tile nextTile = null;

    Vector3 targetVelocity = enemy.GetComponent<Enemy>().velocity;
    float lookaheadTime = 100;
    Vector3 targetPredictedPosition = enemy.transform.position + targetVelocity * lookaheadTime;

    double maxAngle = 0;
    foreach (Tile adjacent in currentTile.Adjacents)
    {
        Vector3 adjacentDirection = adjacent.transform.position - transform.position;
        Vector3 targetDirection = targetPredictedPosition - transform.position;
        double angle = Mathf.Acos(Vector3.Dot(adjacentDirection.normalized, targetDirection.normalized));
        if (angle > maxAngle)
        {
            nextTile = adjacent;
            maxAngle = angle;
        }
    }
    return nextTile;
}


    private void StartExplosion()
    {
        if(explosionStarted == false)
        {
            explosion.Play();
            explosionStarted = true;
        }
        
    }
    private void StopExplosion()
    {
        explosionStarted = false;
        explosion.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
    }

   
    public void Reset(Tile tile)
    {
        Debug.Log("Player reset");
        path.Clear();
        transform.gameObject.GetComponent<Renderer>().enabled = true;
        StopExplosion();
        state = PlayerState.DEFAULT;
        currentTile = tile;
    }
}

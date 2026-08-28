using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance;

    [Header("Enemy Prefabs")]
    public GameObject enemyPrefab;          
    public GameObject hiddenEnemyPrefab;    

    [Header("Spawn Locations")]
    public Transform[] normalSpawnPoints;   
    public Transform hiddenSpawnPoint;      

    [Header("Spawn Settings")]
    public int totalEnemiesToSpawn = 6;     
    private List<GameObject> activeEnemies = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        SpawnStageEnemies();
    }

    public void SpawnStageEnemies()
    {
        ClearActiveEnemies();

        // 1. 일반 적 스폰
        if (enemyPrefab != null && normalSpawnPoints.Length > 0)
        {
            for (int i = 0; i < totalEnemiesToSpawn; i++)
            {
                Transform spawnPoint = normalSpawnPoints[i % normalSpawnPoints.Length];
                GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
                enemy.tag = "Enemy";
                activeEnemies.Add(enemy);
            }
        }

        // 2. 숨겨진 적 스폰
        if (hiddenEnemyPrefab != null && hiddenSpawnPoint != null)
        {
            GameObject hiddenEnemy = Instantiate(hiddenEnemyPrefab, hiddenSpawnPoint.position, hiddenSpawnPoint.rotation);
            hiddenEnemy.tag = "HiddenEnemy";

            EnemyTarget target = hiddenEnemy.GetComponent<EnemyTarget>();
            if (target != null)
            {
                target.isHiddenEnemy = true;
            }

            activeEnemies.Add(hiddenEnemy);
        }
    }

    public void ResetSpawnerForNextStage()
    {
        SpawnStageEnemies();
    }

    private void ClearActiveEnemies()
    {
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }
        activeEnemies.Clear();
    }
}
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스테이지 시작 시 일반 적 + 숨겨진 적 1명을 스폰한다.
/// 일반 적 수 = baseEnemyCount + (stage-1) * enemiesPerStage.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance;

    [Header("프리팹")]
    public GameObject enemyPrefab;
    public GameObject hiddenEnemyPrefab;

    [Header("스폰 위치")]
    public Transform[] normalSpawnPoints;
    public Transform hiddenSpawnPoint;

    [Header("스폰 수")]
    public int baseEnemyCount = 5;
    public int enemiesPerStage = 1;

    private readonly List<GameObject> active = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        SpawnStageEnemies(1);
    }

    public void SpawnStageEnemies(int stage)
    {
        ClearActive();

        int count = baseEnemyCount + Mathf.Max(0, stage - 1) * enemiesPerStage;

        if (enemyPrefab != null && normalSpawnPoints.Length > 0)
        {
            List<Transform> points = Shuffled(normalSpawnPoints);
            for (int i = 0; i < count; i++)
            {
                Transform p = points[i % points.Count];
                GameObject e = Instantiate(enemyPrefab, p.position, p.rotation);
                e.tag = "Enemy";
                active.Add(e);
            }
        }

        if (hiddenEnemyPrefab != null && hiddenSpawnPoint != null)
        {
            GameObject h = Instantiate(hiddenEnemyPrefab, hiddenSpawnPoint.position, hiddenSpawnPoint.rotation);
            h.tag = "HiddenEnemy";

            EnemyTarget t = h.GetComponent<EnemyTarget>();
            if (t != null) t.isHiddenEnemy = true;

            active.Add(h);
        }
    }

    // Fisher-Yates. 스폰 포인트가 적 수보다 적으면 순환하므로 겹칠 수 있다.
    private List<Transform> Shuffled(Transform[] src)
    {
        List<Transform> list = new List<Transform>(src);
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Transform tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
        return list;
    }

    private void ClearActive()
    {
        foreach (GameObject e in active)
            if (e != null) Destroy(e);
        active.Clear();
    }
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 스폰 관리(싱글턴). 두 가지 모드:
///  - Mission : 미션별 스폰(1: 일반 적, 2: 숨은 적 1명, 3: 헤드샷 수만큼 보충). 진행은 MissionManager가 담당.
///  - Waves   : 모두 처치하면 다음 웨이브. 수·체력이 계속 증가하는 무한 모드(부스 점수용).
/// GameManager가 있으면 StartRun() 시점에 BeginRun()이 호출될 때까지 대기한다.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance;

    public enum SpawnMode { Mission, Waves }

    [Header("모드")]
    public SpawnMode mode = SpawnMode.Mission;

    [Header("프리팹")]
    public GameObject enemyPrefab;
    public GameObject hiddenEnemyPrefab;
    [Tooltip("일반 적 대신 섞어 낼 추가 종류(탱커·자폭형 등). enemyPrefab과 같은 확률로 뽑는다.")]
    public GameObject[] extraEnemyPrefabs;

    [Header("스폰 위치")]
    public Transform[] normalSpawnPoints;
    public Transform hiddenSpawnPoint;

    [Header("미션 모드")]
    public int baseEnemyCount = 5;
    public int enemiesPerStage = 1;

    [Header("웨이브 모드")]
    public int firstWaveCount = 4;
    public int addPerWave = 2;
    public float timeBetweenWaves = 3f;
    public float hpPerWave = 8f;

    private readonly List<GameObject> active = new List<GameObject>();
    private int currentWave;
    private int aliveCount;
    private bool runActive;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        // GameManager가 흐름을 관리하면 BeginRun()을 기다린다. 없으면 단독 실행.
        if (GameManager.Instance == null) BeginRun();
    }

    /// <summary>런 시작. GameManager.StartRun()이 호출한다.</summary>
    public void BeginRun()
    {
        runActive = true;
        currentWave = 0;

        if (mode == SpawnMode.Waves)
        {
            NextWave();
        }
        else
        {
            int stage = MissionManager.Instance != null ? MissionManager.Instance.currentStage : 1;
            SpawnStageEnemies(stage);
        }
    }

    /// <summary>런 종료. GameManager.EndRun()이 호출한다. 예약된 다음 웨이브 스폰을 취소한다.</summary>
    public void EndRun()
    {
        runActive = false;
        CancelInvoke(nameof(NextWave));
    }

    // ---------- 미션 모드 ----------

    public void SpawnStageEnemies(int stage)
    {
        ClearActive();

        int count = baseEnemyCount + Mathf.Max(0, stage - 1) * enemiesPerStage;

        SpawnReinforcements(count);
    }

    /// <summary>미션 2 시작 시 숨은 적 1명 스폰. 숨은 적 프리팹이 없으면 일반 적 프리팹을 쓴다.</summary>
    public void SpawnHidden()
    {
        GameObject prefab = hiddenEnemyPrefab != null ? hiddenEnemyPrefab : enemyPrefab;
        if (prefab == null || hiddenSpawnPoint == null) return;

        GameObject h = Spawn(prefab, hiddenSpawnPoint);
        h.tag = "HiddenEnemy";
        EnemyTarget t = h.GetComponent<EnemyTarget>();
        if (t != null) t.isHiddenEnemy = true;
    }

    /// <summary>살아 있는 적이 count보다 적으면 그만큼 보충한다. 미션 3(헤드샷) 시작 시 사용.</summary>
    public void TopUp(int count)
    {
        if (mode == SpawnMode.Mission && runActive) SpawnReinforcements(count - aliveCount);
    }

    // ---------- 웨이브 모드 ----------

    private void NextWave()
    {
        if (!runActive || enemyPrefab == null || normalSpawnPoints.Length == 0) return;

        currentWave++;
        if (ScoreManager.Instance != null) ScoreManager.Instance.SetWave(currentWave);

        ClearActive();

        int count = firstWaveCount + (currentWave - 1) * addPerWave;
        float hpBonus = (currentWave - 1) * hpPerWave;

        List<Transform> points = Shuffled(normalSpawnPoints);
        for (int i = 0; i < count; i++)
        {
            GameObject e = Spawn(PickEnemy(), points[i % points.Count]);
            EnemyTarget t = e.GetComponent<EnemyTarget>();
            if (t != null) t.baseHp += hpBonus; // Start()에서 baseHp를 읽으므로 그 전에 세팅
        }

        Debug.Log($"[WAVE {currentWave}] 적 {count}, 체력 보너스 +{hpBonus}");
    }

    /// <summary>EnemyTarget이 사망 시 호출. 웨이브 모드에서 전멸하면 다음 웨이브 예약.</summary>
    public void NotifyEnemyKilled()
    {
        if (!runActive) return;

        aliveCount = Mathf.Max(0, aliveCount - 1);

        // 웨이브 모드는 전멸 시 다음 웨이브 예약. 미션 모드는 MissionManager가 보충 스폰을 판단한다.
        if (mode == SpawnMode.Waves)
        {
            if (aliveCount <= 0 && !IsInvoking(nameof(NextWave)))
                Invoke(nameof(NextWave), timeBetweenWaves);
        }
    }

    /// <summary>
    /// 미션 모드에서 적이 전멸했는데 스테이지가 아직 안 끝났으면 일반 적을 보충 스폰한다.
    /// (적 수가 부족해 미션을 못 깨고 진행이 멈추는 소프트락 방지)
    /// </summary>
    public void EnsureEnemiesAlive()
    {
        if (mode != SpawnMode.Mission || !runActive) return;
        if (aliveCount > 0) return;
        if (enemyPrefab == null || normalSpawnPoints.Length == 0) return;

        SpawnReinforcements(Mathf.Max(3, enemiesPerStage + 2));
    }

    /// <summary>일반 적 보충 스폰. Mission 모드에서 적이 부족할 때 사용한다.</summary>
    public void SpawnReinforcements(int count)
    {
        if (count <= 0 || enemyPrefab == null || normalSpawnPoints.Length == 0) return;

        List<Transform> points = Shuffled(normalSpawnPoints);
        for (int i = 0; i < count; i++)
            Spawn(PickEnemy(), points[i % points.Count]);
    }

    // ---------- 공용 ----------

    private GameObject Spawn(GameObject prefab, Transform p)
    {
        GameObject e = Instantiate(prefab, p.position, p.rotation);
        e.tag = "Enemy";
        active.Add(e);
        aliveCount++;
        return e;
    }

    private GameObject PickEnemy()
    {
        int n = extraEnemyPrefabs != null ? extraEnemyPrefabs.Length : 0;
        int i = Random.Range(0, n + 1);
        return i < n && extraEnemyPrefabs[i] != null ? extraEnemyPrefabs[i] : enemyPrefab;
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
        aliveCount = 0;
    }
}

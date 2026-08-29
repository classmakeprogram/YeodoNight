using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 스폰 관리(싱글턴). 두 가지 모드:
///  - Mission : 스테이지 시작 시 일반 적 + 숨은 적 1명. 진행은 MissionManager가 담당.
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

        aliveCount = active.Count;
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
            Transform p = points[i % points.Count];
            GameObject e = Instantiate(enemyPrefab, p.position, p.rotation);
            e.tag = "Enemy";
            EnemyTarget t = e.GetComponent<EnemyTarget>();
            if (t != null) t.baseHp += hpBonus; // Start()에서 baseHp를 읽으므로 그 전에 세팅
            active.Add(e);
        }

        aliveCount = active.Count;
        Debug.Log($"[WAVE {currentWave}] 적 {count}, 체력 보너스 +{hpBonus}");
    }

    /// <summary>EnemyTarget이 사망 시 호출. 웨이브 모드에서 전멸하면 다음 웨이브 예약.</summary>
    public void NotifyEnemyKilled()
    {
        if (mode != SpawnMode.Waves || !runActive) return;

        aliveCount--;
        if (aliveCount <= 0 && !IsInvoking(nameof(NextWave)))
            Invoke(nameof(NextWave), timeBetweenWaves);
    }

    // ---------- 공용 ----------

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

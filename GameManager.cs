using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 흐름 상태기: 타이틀 → 플레이 → (일시정지) → 게임오버 / 클리어 → 타이틀.
/// 부스 시연용: 결과 화면 자동 복귀, 플레이 중 장시간 무입력 시 타이틀 복귀(방치 방지).
/// 단일 씬 기준. UI 패널은 GameObject 활성/비활성으로만 전환한다.
/// 씬에 GameManager가 없으면 각 스크립트는 단독으로도 동작한다(폴백 경로).
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public enum GameState { Title, Playing, Paused, GameOver, Clear }

    [Header("UI 패널 (비워도 됨)")]
    public GameObject titlePanel;
    public GameObject hudPanel;
    public GameObject pausePanel;
    public GameObject gameOverPanel;
    public GameObject clearPanel;

    [Header("설정")]
    [Tooltip("게임오버/클리어 화면을 보여준 뒤 타이틀로 복귀하기까지의 시간(초).")]
    public float resultReturnDelay = 8f;
    [Tooltip("플레이 중 이 시간(초) 동안 입력이 없으면 타이틀로 복귀. 부스 방치 방지. 0이면 비활성.")]
    public float idleReturnSeconds = 90f;
    public bool startInTitle = true;

    public GameState State { get; private set; }
    public bool IsPlaying => State == GameState.Playing;

    private float lastInputTime;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        if (startInTitle) EnterTitle();
        else StartRun();
    }

    private void Update()
    {
        if (AnyInput()) lastInputTime = Time.unscaledTime;

        switch (State)
        {
            case GameState.Title:
                if (Input.anyKeyDown) StartRun();
                break;

            case GameState.Playing:
                if (Input.GetKeyDown(KeyCode.Escape)) SetPaused(true);
                else if (idleReturnSeconds > 0f && Time.unscaledTime - lastInputTime > idleReturnSeconds)
                    ReturnToTitle();
                break;

            case GameState.Paused:
                if (Input.GetKeyDown(KeyCode.Escape)) SetPaused(false);
                break;
        }
    }

    private bool AnyInput()
    {
        return Input.anyKey
            || Mathf.Abs(Input.GetAxisRaw("Mouse X")) > 0.01f
            || Mathf.Abs(Input.GetAxisRaw("Mouse Y")) > 0.01f;
    }

    private void EnterTitle()
    {
        State = GameState.Title;
        Time.timeScale = 1f;
        SetCursor(false);
        Show(titlePanel, true);
        Show(hudPanel, false);
        Show(pausePanel, false);
        Show(gameOverPanel, false);
        Show(clearPanel, false);
        lastInputTime = Time.unscaledTime;
    }

    /// <summary>타이틀에서 아무 키나 누르면 호출. 버튼에서 직접 호출해도 됨.</summary>
    public void StartRun()
    {
        State = GameState.Playing;
        Time.timeScale = 1f;
        SetCursor(true);
        Show(titlePanel, false);
        Show(hudPanel, true);
        Show(pausePanel, false);
        Show(gameOverPanel, false);
        Show(clearPanel, false);
        lastInputTime = Time.unscaledTime;

        if (ScoreManager.Instance != null) ScoreManager.Instance.ResetRun();
        if (EnemySpawner.Instance != null) EnemySpawner.Instance.BeginRun();
    }

    public void SetPaused(bool paused)
    {
        if (State != GameState.Playing && State != GameState.Paused) return;
        State = paused ? GameState.Paused : GameState.Playing;
        Time.timeScale = paused ? 0f : 1f;
        SetCursor(!paused);
        Show(pausePanel, paused);
    }

    public void OnPlayerDied()
    {
        if (State == GameState.Playing || State == GameState.Paused) EndRun(false);
    }

    public void OnGameCleared()
    {
        if (State == GameState.Playing || State == GameState.Paused) EndRun(true);
    }

    private void EndRun(bool cleared)
    {
        State = cleared ? GameState.Clear : GameState.GameOver;
        Time.timeScale = 1f;
        SetCursor(false);
        Show(hudPanel, false);
        Show(pausePanel, false);
        Show(cleared ? clearPanel : gameOverPanel, true);

        if (ScoreManager.Instance != null) ScoreManager.Instance.FinalizeRun();
        StartCoroutine(ReturnAfter(resultReturnDelay));
    }

    private IEnumerator ReturnAfter(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        ReturnToTitle();
    }

    /// <summary>결과 화면 "다시하기" 버튼 등에서 호출 가능. 씬을 리로드해 깨끗한 상태로 타이틀 진입.</summary>
    public void ReturnToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>일시정지 화면 "종료" 버튼용.</summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void SetCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private static void Show(GameObject go, bool on)
    {
        if (go != null) go.SetActive(on);
    }
}

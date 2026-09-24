using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임오버 / 클리어 패널에 부착. 점수가 랭킹에 들면 이름 입력 행을 띄우고,
/// 확정 시 저장 후 상위 목록을 렌더한다. (레거시 UI InputField / Text 사용)
/// </summary>
public class LeaderboardUI : MonoBehaviour
{
    [Header("이름 입력")]
    public GameObject nameEntryRow;   // InputField + 확인 버튼을 담은 컨테이너
    public InputField nameInput;      // 아케이드식 3글자
    public Button confirmButton;
    public int maxNameLength = 3;

    [Header("표시")]
    public Text listText;             // 여러 줄 순위 출력

    private bool submitted;

    private void OnEnable()
    {
        submitted = false;
        ScoreManager sm = ScoreManager.Instance;
        bool qualifies = sm != null && sm.Qualifies(sm.Score);

        if (nameEntryRow != null) nameEntryRow.SetActive(qualifies);

        if (qualifies && nameInput != null)
        {
            nameInput.characterLimit = maxNameLength;
            nameInput.text = "";
            nameInput.Select();
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(Submit);
            confirmButton.onClick.AddListener(Submit);
        }

        Render();
    }

    private void Update()
    {
        if (!submitted && nameEntryRow != null && nameEntryRow.activeSelf
            && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            Submit();
    }

    private void Submit()
    {
        if (submitted) return;
        ScoreManager sm = ScoreManager.Instance;
        if (sm == null) return;

        submitted = true;
        sm.SubmitScore(nameInput != null ? nameInput.text : "AAA", sm.Score);
        if (nameEntryRow != null) nameEntryRow.SetActive(false);
        Render();
    }

    private void Render()
    {
        if (listText == null || ScoreManager.Instance == null) return;

        var list = ScoreManager.Instance.GetLeaderboard();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("-- TOP SCORES --");
        for (int i = 0; i < list.Count; i++)
            sb.AppendLine($"{i + 1}. {list[i].name,-4} {list[i].score:N0}");
        if (list.Count == 0) sb.AppendLine("(기록 없음)");
        listText.text = sb.ToString();
    }
}

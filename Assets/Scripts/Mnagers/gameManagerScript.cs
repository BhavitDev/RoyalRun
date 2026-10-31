using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class gameManagerScript : MonoBehaviour
{
    
    [SerializeField] GameObject gameOverText;
    [SerializeField] TMP_Text timeText;
    [SerializeField] float initialTime = 10f;

    PlayerMovement playerMovement;
    float timeLeft;
    private Color timeTextDefaultColor;
    public bool isGameOver = false;

    private static readonly Color TimerWarningColor = new Color32(255, 70, 55, 255);

    public bool IsGameOver { get { return isGameOver; } }
    public int FinalScore { get; private set; }

    private void Awake()
    {

        
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 120;

        //Debug.Log(Application.targetFrameRate);
        
    }
    private void Start()
    {
        playerMovement = FindFirstObjectByType<PlayerMovement>();
        timeLeft = initialTime;
        if (timeText != null)
            timeTextDefaultColor = timeText.color;
    }

    private void Update()
    {
        TimeManager();
        //QuitGame();
    }
    public void IncreaseTimer(float timer)
    {
        timeLeft += timer;
    }

    private void TimeManager()
    {
        if (!isGameOver)
        {
            timeLeft -= Time.deltaTime;
            timeText.text = timeLeft.ToString("F1");
            UpdateTimerWarning();

            if (timeLeft <= 0)
            {
                GameOver();
            }
        }
    }

    private void UpdateTimerWarning()
    {
        if (timeText == null)
            return;

        if (timeLeft <= 3f && timeLeft > 0f)
        {
            float pulse = (Mathf.Sin(Time.unscaledTime * Mathf.PI * 6f) + 1f) * 0.5f;
            timeText.color = Color.Lerp(timeTextDefaultColor, TimerWarningColor, pulse);
        }
        else
        {
            timeText.color = timeTextDefaultColor;
        }
    }

    void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        FinalScore = FindFirstObjectByType<ScoreManagerScript>()?.FinalScore ?? 0;
        if (playerMovement != null) playerMovement.enabled = false;
        if (gameOverText != null) gameOverText.SetActive(false);
        Time.timeScale = 0f;
        EndGameLeaderboardUI.Show(FinalScore);
    }


    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("IndianMarketPreview");
    }
}

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
    public bool isGameOver = false;

    public bool IsGameOver { get { return isGameOver; } }

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

            if (timeLeft <= 0)
            {
                GameOver();
            }
        }
    }

    void GameOver()
    {
        isGameOver = true;
        playerMovement.enabled = false;
        gameOverText.SetActive(true);
        Time.timeScale = 0.1f;
    }


    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("IndianMarketPreview");
    }
}

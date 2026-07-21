using TMPro;
using UnityEngine;

public class gameManagerScript : MonoBehaviour
{
    
    [SerializeField] GameObject gameOverText;
    [SerializeField] TMP_Text timeText;
    [SerializeField] float initialTime = 5f;

    PlayerMovement playerMovement;
    float timeLeft;
    bool isGameOver = false;

    public bool IsGameOver { get { return isGameOver; } }

    private void Start()
    {
        playerMovement = FindFirstObjectByType<PlayerMovement>();
        timeLeft = initialTime;
    }

    private void Update()
    {
        TimeManager();
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
}

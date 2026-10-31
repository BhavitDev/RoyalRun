using TMPro;
using UnityEngine;

public class Coin : PickupManagerScript
{
    [SerializeField] float scoreByApple = 100f;
    ScoreManagerScript scoreManager;

    private void Start()
    {
        CacheScoreManager();
    }

    private void OnEnable() => CacheScoreManager();

    private void CacheScoreManager()
    {
        if (scoreManager == null)
            scoreManager = FindFirstObjectByType<ScoreManagerScript>();
    }

    protected override void OnPickup()
    {
        CacheScoreManager();
        if (scoreManager == null)
        {
            Debug.LogWarning("Coin pickup ignored because this scene has no active ScoreManagerScript.", this);
            return;
        }
        scoreManager.manageScore(scoreByApple);
    }
}

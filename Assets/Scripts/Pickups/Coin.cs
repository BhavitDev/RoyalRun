using TMPro;
using UnityEngine;

public class Coin : PickupManagerScript
{
    [SerializeField] float scoreByApple = 100f;
    ScoreManagerScript scoreManager;

    private void Start()
    {
        scoreManager = FindFirstObjectByType<ScoreManagerScript>();
    }
    protected override void OnPickup()
    {
        scoreManager.manageScore(scoreByApple);
    }
}

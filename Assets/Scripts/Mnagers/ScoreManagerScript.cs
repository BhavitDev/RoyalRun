using TMPro;
using UnityEngine;

public class ScoreManagerScript : MonoBehaviour
{
    [SerializeField] gameManagerScript gameManage;
    [SerializeField] TMP_Text scoreText;

    float finalScore = 0f;
    public int FinalScore => Mathf.Max(0, Mathf.FloorToInt(finalScore));

    public void manageScore(float score)
    {
        if (gameManage.IsGameOver) return;
        finalScore += score;
        if (scoreText != null) scoreText.text = FinalScore.ToString("N0");
    }
}

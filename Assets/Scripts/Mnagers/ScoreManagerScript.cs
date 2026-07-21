using TMPro;
using UnityEngine;

public class ScoreManagerScript : MonoBehaviour
{
    [SerializeField] gameManagerScript gameManage;
    [SerializeField] TMP_Text scoreText;

    float finalScore = 0f;

    public void manageScore(float score)
    {
        if (gameManage.IsGameOver) return;
        finalScore += score;
        scoreText.text = finalScore.ToString();
    }
}

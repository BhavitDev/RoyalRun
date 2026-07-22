using Unity.VisualScripting;
using UnityEngine;

public class CheckPointScript : MonoBehaviour
{
    [SerializeField] string playerTag = "Player";
    [SerializeField] float timerToAdd = 5f;

    gameManagerScript gameManageScript;

    private void Start()
    {
        gameManageScript = FindFirstObjectByType<gameManagerScript>();
    }
    private void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag(playerTag))
        {
            gameManageScript.IncreaseTimer(timerToAdd);
        }
    }


}

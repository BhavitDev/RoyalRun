using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class buttonsManager : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] bool isLeft;

    PlayerMovement playerMovement;
    gameManagerScript gameManager;
    bool isPressed = false;

    void Awake()
    {
        playerMovement = FindFirstObjectByType<PlayerMovement>();
        gameManager = FindFirstObjectByType<gameManagerScript>();
    }

    void Update()
    {
        if(isPressed)
        {
            if(isLeft)
            {
                playerMovement.transform.Translate(-playerMovement.playerMoveSpeed * Time.deltaTime, 0, 0);
            }
            else
            {
                playerMovement.transform.Translate(playerMovement.playerMoveSpeed * Time.deltaTime, 0, 0);
            }
        }

        if (gameManager.IsGameOver)
        {
            gameObject.SetActive(false);
        }
        if (!gameManager.isGameOver)
        {
            gameObject.SetActive(true);
        }
    }
    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        isPressed = true; 
    }

    void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
    {
        isPressed= false;
    }


}

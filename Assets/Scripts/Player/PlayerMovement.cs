
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float playerMoveSpeed = 5f;
    [SerializeField] float moveclamp = 3f;
    private float horizontalInput;

    private void Update()
    {
        if (horizontalInput != 0f)
        {
            transform.Translate(horizontalInput * playerMoveSpeed * Time.deltaTime, 0, 0);
        }

        MovementClamp();
    }

    public void Movement(InputAction.CallbackContext context)
    {
        horizontalInput = context.ReadValue<Vector2>().x;
    }

    public void MoveRightButton()
    {
        transform.Translate(playerMoveSpeed * Time.deltaTime, 0, 0);
    }

    public void MoveLeftButton()
    {
        transform.Translate(-playerMoveSpeed * Time.deltaTime, 0, 0);
    }

    private void MovementClamp()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, -moveclamp, moveclamp);
        transform.position = pos;
    }
}

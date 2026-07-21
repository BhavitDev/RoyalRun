
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float moveclamp = 3f;

    private void FixedUpdate()
    {
        Movement();
        MovementClamp();
    }

    private void Movement()
    {
        if (Input.GetKey(KeyCode.A))
        {
            transform.Translate(-moveSpeed * Time.deltaTime, 0, 0);
        }

        if (Input.GetKey(KeyCode.D))
        {
            transform.Translate(moveSpeed * Time.deltaTime, 0, 0);
        }
    }

    private void MovementClamp()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, -moveclamp, moveclamp);
        transform.position = pos;
    }
}

using UnityEngine;

public class ObjectDestroyManagerScript : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        Destroy(collision.gameObject);
    }
}

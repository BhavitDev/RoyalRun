
using UnityEngine;

public abstract class PickupManagerScript : MonoBehaviour
{
    [SerializeField] float rotationValue = 100f;
    private const string playerTag = "Player";

    void Update()
    {
        transform.Rotate(0f, rotationValue * Time.deltaTime, 0f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag(playerTag))
        {
            OnPickup();
            Destroy(gameObject);
        }
    }

    protected abstract void OnPickup();
}

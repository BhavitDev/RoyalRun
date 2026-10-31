using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class ObstaclePace : MonoBehaviour
{
    [SerializeField] float despawnBehindPlayer = 14f;
    [SerializeField, Min(1f)] float forwardSpeedMultiplier = 1.6f;
    [SerializeField, Min(0.2f)] float rollRadiusScale = 2f;

    Rigidbody obstacleBody;
    PlatformChunkLoader platformChunkLoader;
    Transform playerTransform;

    void Awake()
    {
        obstacleBody = GetComponent<Rigidbody>();
        platformChunkLoader = FindFirstObjectByType<PlatformChunkLoader>();
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
    }

    void FixedUpdate()
    {
        if (platformChunkLoader != null && obstacleBody != null)
        {
            var velocity = obstacleBody.linearVelocity;
            var towardRunner = -platformChunkLoader.transform.forward * platformChunkLoader.CurrentChunkMoveSpeed * forwardSpeedMultiplier;
            obstacleBody.linearVelocity = new Vector3(towardRunner.x, velocity.y, towardRunner.z);

            // Forward travel is along -Z; rotating around the cross-axis makes each appliance tumble down the track.
            var rollAxis = Vector3.Cross(Vector3.down, towardRunner.normalized);
            if (rollAxis.sqrMagnitude > 0.001f)
            {
                var collider = GetComponent<Collider>();
                var extents = collider != null ? collider.bounds.extents : Vector3.one * 0.5f;
                var rollRadius = Mathf.Max(0.25f, Mathf.Sqrt(extents.y * extents.y + extents.z * extents.z) * rollRadiusScale);
                obstacleBody.angularVelocity = rollAxis.normalized * (towardRunner.magnitude / rollRadius);
            }
        }

        if (playerTransform != null && transform.position.z < playerTransform.position.z - despawnBehindPlayer)
            Destroy(gameObject);
    }
}

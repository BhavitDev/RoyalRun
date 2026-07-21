using UnityEngine;

public class Apple : PickupManagerScript
{
    [SerializeField] float onPickupNewMoveSpeed = 3f;
    
    PlatformChunkLoader platformChunkLoader;

    private void Start()
    {
        platformChunkLoader = FindFirstObjectByType<PlatformChunkLoader>();
    }
    protected override void OnPickup()
    {
        platformChunkLoader.ChangeChunkMoveSpeed(onPickupNewMoveSpeed);
    }
}

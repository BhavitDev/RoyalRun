using Unity.VisualScripting;
using UnityEngine;

public class PlayerCollisionHandler : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] float onHitnewChunkSpeed = -2f;

    PlatformChunkLoader platformChunkLoader;


    private void Start()
    {
        platformChunkLoader = FindFirstObjectByType<PlatformChunkLoader>();
    }


    private const string hitTrigger = "Hit";
    float cooldownTime = 0f;

    private void Update()
    {
        cooldownTime += Time.deltaTime;
    }
    void OnCollisionEnter(Collision collision)
    {
        if(cooldownTime > 1f)
        {
            platformChunkLoader.ChangeChunkMoveSpeed(onHitnewChunkSpeed);
            cooldownTime = 0f;
            animator.SetTrigger(hitTrigger);
        }
    }
}

using Unity.VisualScripting;
using UnityEngine;

public class PlayerCollisionHandler : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] float onHitnewChunkSpeed = -2f;
    [SerializeField] private AudioClip onHitAudio;

    PlatformChunkLoader platformChunkLoader;
    private AudioSource onHitAudioSource;


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
            PlayOnHitAudio();
        }
    }

    private void PlayOnHitAudio()
    {
        if (onHitAudio == null)
            return;

        if (onHitAudioSource == null)
        {
            onHitAudioSource = gameObject.AddComponent<AudioSource>();
            onHitAudioSource.playOnAwake = false;
            onHitAudioSource.loop = false;
            onHitAudioSource.spatialBlend = 0f;
        }

        if (!onHitAudioSource.isPlaying)
            onHitAudioSource.PlayOneShot(onHitAudio);
    }
}

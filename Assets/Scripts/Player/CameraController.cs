using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] ParticleSystem speedUpParticle;
    [SerializeField] float zoomDuration = 1.0f;
    [SerializeField] float zoomMultipliyer = 3f;
    [SerializeField] float minZoom = 20f;
    [SerializeField] float maxZoom = 120f;
    CinemachineCamera cam;


    private void Awake()
    {
        cam = GetComponent<CinemachineCamera>();
    }


    public void ChangeCameraFOV(float newChunkSpeed)
    {
        StopAllCoroutines();
        StartCoroutine(ChangeCameraFOVRoutine(newChunkSpeed));
        if (newChunkSpeed > 0f)
        {
            speedUpParticle.Play();
        }
    }

    IEnumerator ChangeCameraFOVRoutine(float newChunkSpeed)
    {
        float initialFOV = cam.Lens.FieldOfView;
        float finalFOV = Mathf.Clamp(initialFOV + (newChunkSpeed * zoomMultipliyer), minZoom, maxZoom);

        float timePassed = 0f;

        while (timePassed < zoomDuration)
        {
            timePassed += Time.deltaTime;
            float t = timePassed / zoomDuration;

            cam.Lens.FieldOfView = Mathf.Lerp(initialFOV, finalFOV, t);
            yield return null;
        }
    }
}

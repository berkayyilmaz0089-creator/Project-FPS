using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [System.Serializable]
    public struct SurfaceAudioData
    {
        public string surfaceTag;
        public AudioClip walkClip;
        public AudioClip jumpClip;
        public AudioClip landClip;
    }
    
    [Header("Audio")]
    public SurfaceAudioData defaultAudio;
    public SurfaceAudioData[] taggedAudios;

    [Header("Audio Settings")]
    public float minPitch = 0.85f;
    public float maxPitch = 1.15f;

    [Header("Movement Intervals")]
    public float walkInterval = 0.5f;
    public float sprintInterval = 0.35f;
    public float crouchInterval = 0.8f;

    [Header("References")]
    public CharacterController controller;
    public PlayerMovement playerMovement;
    public AudioSource audioSource;
    public LayerMask groundMask;

    private float _stepTimer = 0.0f;

    private void Start()
    {
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }

        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (playerMovement != null)
        {
            playerMovement.OnJump += PlayJumpAudio;
            playerMovement.OnLand += PlayLandAudio;
        }
        
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    private void OnDestroy()
    {
        if (playerMovement != null)
        {
            playerMovement.OnJump -= PlayJumpAudio;
            playerMovement.OnLand -= PlayLandAudio;
        }
    }

    private void Update()
    {
        if (!controller.isGrounded)
        {
            _stepTimer = 0.0f;
            return;
        }

        Vector3 velocity = controller.velocity;
        float currentSpeedSqr = velocity.x * velocity.x + velocity.z * velocity.z;

        if (currentSpeedSqr < 0.01f)
        {
            _stepTimer = 0.0f;
            return;
        }

        _stepTimer += Time.deltaTime;
        float currentInterval = GetCurrentInterval(Mathf.Sqrt(currentSpeedSqr));

        if (_stepTimer >= currentInterval)
        {
            PlayWalkAudio();
            _stepTimer = 0.0f;
        }
    }

    private float GetCurrentInterval(float currentSpeed)
    {
        if (playerMovement != null)
        {
            if (currentSpeed <= playerMovement.crouchSpeed + 0.2f)
            {
                return crouchInterval;
            }
            
            if (currentSpeed >= playerMovement.sprintSpeed - 0.5f)
            {
                return sprintInterval;
            }
        }

        return walkInterval;
    }

    private SurfaceAudioData GetSurfaceData()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 2.0f, groundMask))
        {
            for (int i = 0; i < taggedAudios.Length; i++)
            {
                if (hit.collider.CompareTag(taggedAudios[i].surfaceTag))
                {
                    return taggedAudios[i];
                }
            }
        }

        return defaultAudio;
    }

    private void PlayWalkAudio()
    {
        SurfaceAudioData surface = GetSurfaceData();

        if (surface.walkClip != null)
        {
            PlayClipWithPitch(surface.walkClip);
        }
    }

    private void PlayJumpAudio()
    {
        SurfaceAudioData surface = GetSurfaceData();

        if (surface.jumpClip != null)
        {
            PlayClipWithPitch(surface.jumpClip);
        }
    }

    private void PlayLandAudio()
    {
        SurfaceAudioData surface = GetSurfaceData();

        if (surface.landClip != null)
        {
            PlayClipWithPitch(surface.landClip);
        }
    }

    private void PlayClipWithPitch(AudioClip clip)
    {
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(clip);
    }
}
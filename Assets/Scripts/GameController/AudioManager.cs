using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Source Principal (2D)")]
    [SerializeField] private AudioSource sfxSource;

    // ============================
    //   CONTROLES DE VOLUMEN
    // ============================

    [Header("Volúmenes (0–1)")]
    [Range(0f, 1f)][SerializeField] private float masterSfxVolume = 1f;

    [Header("Volumen individual por categoría")]
    [Range(0f, 1f)][SerializeField] private float footstepVolume = 0.6f;
    [Range(0f, 1f)][SerializeField] private float jumpVolume = 0.8f;
    [Range(0f, 1f)][SerializeField] private float landingVolume = 0.8f;

    // Puedes añadir más aquí cuando quieras:
    // public float gunshotVolume = 1f;
    // public float impactVolume = 1f;
    // public float uiVolume = 1f; etc.

    [Header("Pitch aleatorio ligero")]
    [Range(0.1f, 3f)] public float minPitch = 0.95f;
    [Range(0.1f, 3f)] public float maxPitch = 1.05f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f; // 2D
        }
    }

    // ============================
    //    MÉTODO GENÉRICO DE SFX
    // ============================

    private void Play(AudioClip clip, float volume, bool randomizePitch)
    {
        if (clip == null) return;

        sfxSource.pitch = randomizePitch ? Random.Range(minPitch, maxPitch) : 1f;

        float finalVol = Mathf.Clamp01(volume) * masterSfxVolume;
        if (finalVol <= 0f) return;

        sfxSource.PlayOneShot(clip, finalVol);
    }

    // ============================
    //   API DE SONIDOS ESPECÍFICOS
    // ============================

    public void PlaySFX(AudioClip clip, float vol = 1f, bool randomPitch = true)
        => Play(clip, vol, randomPitch);

    public void PlayFootstep(AudioClip clip)
        => Play(clip, footstepVolume, true);

    public void PlayJump(AudioClip clip)
        => Play(clip, jumpVolume, true);

    public void PlayLanding(AudioClip clip)
        => Play(clip, landingVolume, true);

    // ============================
    //   GETTERS Y SETTERS
    // ============================

    public void SetMasterSfxVolume(float v) => masterSfxVolume = Mathf.Clamp01(v);
    public void SetFootstepVolume(float v) => footstepVolume = Mathf.Clamp01(v);
    public void SetJumpVolume(float v) => jumpVolume = Mathf.Clamp01(v);
    public void SetLandingVolume(float v) => landingVolume = Mathf.Clamp01(v);

    public float GetMasterSfxVolume() => masterSfxVolume;
    public float GetFootstepVolume() => footstepVolume;
    public float GetJumpVolume() => jumpVolume;
    public float GetLandingVolume() => landingVolume;
}

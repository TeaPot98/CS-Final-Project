using UnityEngine;

public class EngineSound : MonoBehaviour
{
    [SerializeField] private AudioClip idleClip;
    [SerializeField] private AudioClip loadedClip;
    [SerializeField] private AudioClip unloadedClip;

    [SerializeField] private float minPitch = 0.65f;
    [SerializeField] private float maxPitch = 1.75f;
    [SerializeField] private float responseSpeed = 12f;

    private Car _car;
    private AudioSource _idleSource;
    private AudioSource _loadedSource;
    private AudioSource _unloadedSource;

    private float smoothedRpm;
    private float smoothedThrottle;

    private void Awake()
    {
        _car = GetComponent<Car>();

        _idleSource = CreateSource(idleClip);
        _loadedSource = CreateSource(loadedClip);
        _unloadedSource = CreateSource(unloadedClip);

        smoothedRpm = _car.engine.idleRpm;
    }

    private void Start()
    {
        _idleSource.Play();
        _loadedSource.Play();
        _unloadedSource.Play();

        _idleSource.volume = 0f;
        _loadedSource.volume = 0f;
        _unloadedSource.volume = 0f;
    }

    private void Update()
    {
        float blend = 1f - Mathf.Exp(-responseSpeed * Time.deltaTime);

        smoothedRpm = Mathf.Lerp(smoothedRpm, _car.EngineRpm, blend);
        smoothedThrottle = Mathf.Lerp(
            smoothedThrottle,
            Mathf.Clamp01(_car.Throttle),
            blend
        );

        float rpm01 = Mathf.InverseLerp(
            _car.engine.idleRpm,
            _car.engine.maxRpm,
            smoothedRpm
        );

        float runningPitch = Mathf.Lerp(minPitch, maxPitch, rpm01);

        _loadedSource.pitch = runningPitch;
        _unloadedSource.pitch = runningPitch;
        _idleSource.pitch = Mathf.Lerp(0.95f, 1.15f, rpm01);

        _idleSource.volume =
            0.55f * (1f - Mathf.SmoothStep(0.05f, 0.25f, rpm01));

        float runningVolume =
            Mathf.Lerp(0.25f, 1f, Mathf.SmoothStep(0.05f, 0.25f, rpm01));

        _loadedSource.volume =
            runningVolume * Mathf.Lerp(0.08f, 0.85f, smoothedThrottle);

        _unloadedSource.volume =
            runningVolume * Mathf.Lerp(0.65f, 0.05f, smoothedThrottle);
    }

    private AudioSource CreateSource(AudioClip clip)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();

        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;

        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = 2f;
        source.maxDistance = 60f;
        source.dopplerLevel = 0.25f;

        return source;
    }
}
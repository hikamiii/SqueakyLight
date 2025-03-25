using UnityEngine;

[RequireComponent(typeof(Light))]
public class CrankFlashlightReactiveAudio : MonoBehaviour
{
    [Header("Key Setup")]
    public KeyCode chargeKey = KeyCode.E;

    [Header("Light Settings")]
    public float maxIntensity = 8f;      // The absolute maximum brightness
    public float chargeChunk = 2f;       // How much intensity to gain each chunk
    public float chargeRate = 1f;        // How fast we charge toward the next chunk
    public float decayRate = 0.5f;       // How fast intensity decays when not charging

    [Header("Audio")]
    public AudioSource windUpAudio;      // Assign your wind-up AudioSource (with the long wind-up clip)
    public AudioSource clankAudio;       // Assign a separate AudioSource for the "clank"
    public AudioClip clankClip;          // Assign a short "clank" clip here

    // If true, we immediately stop (or fade out) the wind-up clip 
    // at the exact moment the chunk finishes charging.
    public bool stopAudioOnChunkFinish = true;

    private Light flashlight;
    private float currentIntensity = 0f;

    // The intensity we started charging *from* when we began this chunk
    private float chunkStartIntensity = 0f;

    // The target intensity for the *current* chunk
    private float targetIntensity = 0f;

    private bool isCharging = false;     // True while the user is actively holding the key
    private bool chunkFinished = false;  // Reached chunk target => must release before next chunk

    void Start()
    {
        flashlight = GetComponent<Light>();
        flashlight.intensity = currentIntensity;

        // Ensure these AudioSources aren't set to play on awake unless desired
        if (windUpAudio != null)
        {
            windUpAudio.playOnAwake = false;
            windUpAudio.loop = false; // Typically you won't loop a wind-up clip
        }

        if (clankAudio != null)
        {
            clankAudio.playOnAwake = false;
            // Usually "clank" is a short effect so no loop
            clankAudio.loop = false;
        }
    }

    void Update()
    {
        HandleCharging();
        HandleDecaying();
    }

    private void HandleCharging()
    {
        // 1) On key down (fresh press):
        //    - Only start if not already charging
        //    - and we haven't completed a chunk that hasn't been released yet
        if (Input.GetKeyDown(chargeKey) && !isCharging && !chunkFinished)
        {
            // Begin charging
            isCharging = true;

            // Store where we started this chunk
            chunkStartIntensity = currentIntensity;

            // New chunk target: current + chargeChunk, clamped by max
            targetIntensity = Mathf.Min(currentIntensity + chargeChunk, maxIntensity);

            // Start wind-up audio from the beginning
            if (windUpAudio != null)
            {
                windUpAudio.Stop();     // In case it was still playing
                windUpAudio.time = 0f;  // Reset to start
                windUpAudio.Play();     // Begin playing
            }
        }

        // 2) If we are currently charging:
        if (isCharging)
        {
            // If the user lets go mid-charge, we stop immediately
            if (!Input.GetKey(chargeKey))
            {
                StopChargingEarly();
                return;
            }

            // Move toward the chunk target
            currentIntensity = Mathf.MoveTowards(
                currentIntensity,
                targetIntensity,
                chargeRate * Time.deltaTime
            );
            flashlight.intensity = currentIntensity;

            // Update the wind-up audio’s playback position to match how far we've charged
            UpdateWindUpAudio();

            // Check if we’ve hit the chunk target (or max)
            if (Mathf.Approximately(currentIntensity, targetIntensity))
            {
                chunkFinished = true;
                isCharging = false;

                // Stop the wind-up audio if desired
                if (windUpAudio != null && stopAudioOnChunkFinish)
                {
                    windUpAudio.Stop();
                }

                // Play the "clank" sound to indicate the chunk is fully charged
                PlayClankSound();
            }
        }

        // 3) Once chunk is finished, the user must release to reset chunkFinished
        if (Input.GetKeyUp(chargeKey) && chunkFinished)
        {
            chunkFinished = false;
        }
    }

    private void HandleDecaying()
    {
        // If not charging and there's some brightness left, decay the intensity
        if (!isCharging && currentIntensity > 0f)
        {
            currentIntensity = Mathf.MoveTowards(
                currentIntensity,
                0f,
                decayRate * Time.deltaTime
            );
            flashlight.intensity = currentIntensity;
        }
    }

    private void StopChargingEarly()
    {
        isCharging = false;

        // Stop or fade out the wind-up sound
        if (windUpAudio != null && windUpAudio.isPlaying)
        {
            windUpAudio.Stop();
        }
    }

private void UpdateWindUpAudio()
{
    if (windUpAudio == null || windUpAudio.clip == null) return;

    float chunkSize = targetIntensity - chunkStartIntensity;
    float chunkProgress = 0f;
    if (!Mathf.Approximately(chunkSize, 0f))
    {
        chunkProgress = (currentIntensity - chunkStartIntensity) / chunkSize;
    }

    // Clamp the fraction strictly between 0 and 1
    chunkProgress = Mathf.Clamp01(chunkProgress);

    // Calculate newTime. Subtract a small epsilon so we never hit the clip’s exact length
    float newTime = chunkProgress * (windUpAudio.clip.length - 0.001f);

    // Only set the time if there's a big enough difference to avoid constant scrubbing
    if (Mathf.Abs(windUpAudio.time - newTime) > 0.05f)
    {
        // Additional clamp for extra safety
        newTime = Mathf.Clamp(newTime, 0f, windUpAudio.clip.length - 0.001f);

        windUpAudio.time = newTime;
    }
}


    private void PlayClankSound()
    {
        // If we have a valid AudioSource and clip
        if (clankAudio != null && clankClip != null)
        {
            clankAudio.Stop(); // Just to be safe, if something's playing
            clankAudio.clip = clankClip;
            clankAudio.Play();
        }
        // If you prefer using PlayOneShot:
        // clankAudio.PlayOneShot(clankClip);
    }
}

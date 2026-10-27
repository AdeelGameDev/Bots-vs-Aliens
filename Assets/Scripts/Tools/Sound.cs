using UnityEngine;

[System.Serializable]
public class Sound
{
    public string name;

    public AudioClip clip;

    [Range(0, 1)]
    public float volume = 1f;

    [Range(.1f, 3f)]
    public float pitch = 1f;

    public bool loop;

    [Header("Feel")]
    [Tooltip("Random +/- pitch wobble so repeated sounds do not sound machine-gunned. 0 = none.")]
    [Range(0f, 0.3f)]
    public float pitchJitter = 0.02f;

    [Tooltip("How many copies of this sound can overlap. More voices = smoother, less cut-off.")]
    [Range(1, 8)]
    public int voices = 2;

    [Tooltip("Minimum seconds between two plays of this sound. Stops same-frame stacking.")]
    [Range(0f, 0.5f)]
    public float minInterval = 0f;

    [Tooltip("Unity voice priority: 0 = most important, 256 = first to be culled.")]
    [Range(0, 256)]
    public int priority = 128;

    [Header("Music / stingers")]
    [Tooltip("Marks this entry as the background music track (faded, ducked, never pitch shifted).")]
    public bool music;

    [Tooltip("Dip the music while this sound plays, so the stinger reads clearly.")]
    public bool ducksMusic;

    [Range(0f, 3f)]
    public float duckDuration = 1f;

    [HideInInspector]
    public AudioSource source;
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central audio hub. Builds a small pool of AudioSources per sound so repeated
/// one-shots overlap instead of cutting each other off, keeps every effect well
/// below full scale (headroom is what stops the harsh clipping when several
/// sounds fire at once), adds light pitch variation so repeated sounds do not
/// fatigue the ear, and fades/ducks the background music.
/// </summary>
[DefaultExecutionOrder(-100)]
public class AudioManager : Singleton<AudioManager>
{
    [Header("Mix")]
    [Tooltip("Global trim applied to every sound. Use this for a master volume slider.")]
    [Range(0f, 1f)] public float masterVolume = 0.9f;

    [Tooltip("Music is multiplied by this on top of its own Sound volume. Music should sit under the effects.")]
    [Range(0f, 1f)] public float musicVolume = 0.35f;

    [Header("Music")]
    [Tooltip("Seconds to fade the music in when it starts.")]
    [Range(0f, 5f)] public float musicFadeIn = 2f;

    [Tooltip("How far the music drops while a big stinger plays (1 = no ducking).")]
    [Range(0f, 1f)] public float stingerDuckAmount = 0.45f;

    public Sound[] sounds;

    private sealed class Voice
    {
        public AudioSource source;
        public float startedAt;
    }

    private sealed class Channel
    {
        public Sound sound;
        public readonly List<Voice> voices = new List<Voice>();
        public float nextAllowedTime;
    }

    private readonly Dictionary<string, Channel> channels =
        new Dictionary<string, Channel>(StringComparer.OrdinalIgnoreCase);

    private Channel musicChannel;
    private Coroutine musicRoutine;
    private float musicTargetVolume;

    protected override void Awake()
    {
        base.Awake();
        BuildChannels();
    }

    private void Start()
    {
        PlayMusic();
    }

    // ---------------------------------------------------------------- setup

    private void BuildChannels()
    {
        channels.Clear();
        musicChannel = null;

        if (sounds == null) return;

        foreach (Sound s in sounds)
        {
            if (s == null) continue;

            if (string.IsNullOrWhiteSpace(s.name) || s.clip == null)
            {
                Debug.LogWarning("AudioManager: sound '" + s.name + "' has no clip and will be skipped.");
                continue;
            }

            if (channels.ContainsKey(s.name))
            {
                Debug.LogWarning("AudioManager: duplicate sound name '" + s.name + "'. Later entries are ignored.");
                continue;
            }

            bool isMusic = s.music;
            int voiceCount = isMusic ? 1 : Mathf.Clamp(s.voices, 1, 8);

            Channel channel = new Channel { sound = s };

            for (int i = 0; i < voiceCount; i++)
            {
                AudioSource source = CreateSource(s, isMusic);
                s.source = source;
                channel.voices.Add(new Voice { source = source });
            }

            channels.Add(s.name, channel);

            if (isMusic) musicChannel = channel;
        }
    }

    private AudioSource CreateSource(Sound s, bool isMusic)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = s.clip;
        source.playOnAwake = false;
        source.loop = isMusic || s.loop;
        source.pitch = Mathf.Max(0.1f, s.pitch);
        source.volume = isMusic
            ? s.volume * musicVolume * masterVolume
            : s.volume * masterVolume;
        source.spatialBlend = 0f;
        source.priority = Mathf.Clamp(s.priority, 0, 256);
        source.ignoreListenerPause = isMusic;
        return source;
    }

    // ---------------------------------------------------------------- playback

    public void Play(string name)
    {
        if (string.IsNullOrEmpty(name)) return;

        if (!channels.TryGetValue(name, out Channel channel))
        {
            Debug.LogWarning("Sound : " + name + " not found!");
            return;
        }

        if (channel.sound.music)
        {
            PlayMusic();
            return;
        }

        float now = Time.unscaledTime;

        // Frequency guard: many identical hits in the same frame read as one harsh
        // burst rather than as separate events, so let the first one land.
        if (channel.sound.minInterval > 0f && now < channel.nextAllowedTime) return;
        channel.nextAllowedTime = now + channel.sound.minInterval;

        Voice voice = PickVoice(channel);
        if (voice == null) return;

        float jitter = channel.sound.pitchJitter > 0f
            ? UnityEngine.Random.Range(-channel.sound.pitchJitter, channel.sound.pitchJitter)
            : 0f;

        AudioSource source = voice.source;
        source.pitch = Mathf.Max(0.1f, channel.sound.pitch * (1f + jitter));
        source.volume = channel.sound.volume * masterVolume;
        source.Play();
        voice.startedAt = now;

        if (channel.sound.ducksMusic) DuckMusic(channel.sound.duckDuration);
    }

    private Voice PickVoice(Channel channel)
    {
        if (channel.voices.Count == 0) return null;

        Voice oldest = channel.voices[0];

        for (int i = 0; i < channel.voices.Count; i++)
        {
            Voice voice = channel.voices[i];
            if (!voice.source.isPlaying) return voice;
            if (voice.startedAt < oldest.startedAt) oldest = voice;
        }

        return oldest;
    }

    // ---------------------------------------------------------------- music

    public void PlayMusic()
    {
        if (musicChannel == null) return;

        AudioSource source = musicChannel.voices[0].source;
        musicTargetVolume = musicChannel.sound.volume * musicVolume * masterVolume;

        if (!source.isPlaying)
        {
            source.volume = 0f;
            source.Play();
            RestartMusicRoutine(FadeMusic(source, musicTargetVolume, musicFadeIn));
        }
        else
        {
            RestartMusicRoutine(FadeMusic(source, musicTargetVolume, 0.4f));
        }
    }

    public void StopMusic(float fadeOut = 0.8f)
    {
        if (musicChannel == null) return;
        AudioSource source = musicChannel.voices[0].source;
        RestartMusicRoutine(FadeOutAndStop(source, fadeOut));
    }

    public void DuckMusic(float holdSeconds)
    {
        if (musicChannel == null) return;
        AudioSource source = musicChannel.voices[0].source;
        if (!source.isPlaying) return;

        RestartMusicRoutine(DuckRoutine(source, Mathf.Max(0.1f, holdSeconds)));
    }

    private void RestartMusicRoutine(IEnumerator routine)
    {
        if (musicRoutine != null) StopCoroutine(musicRoutine);
        musicRoutine = StartCoroutine(routine);
    }

    private IEnumerator DuckRoutine(AudioSource source, float holdSeconds)
    {
        yield return FadeMusic(source, musicTargetVolume * stingerDuckAmount, 0.12f);
        yield return new WaitForSecondsRealtime(holdSeconds);
        yield return FadeMusic(source, musicTargetVolume, 0.5f);
    }

    private IEnumerator FadeMusic(AudioSource source, float target, float duration)
    {
        float start = source.volume;

        if (duration <= 0f)
        {
            source.volume = target;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        source.volume = target;
    }

    private IEnumerator FadeOutAndStop(AudioSource source, float duration)
    {
        yield return FadeMusic(source, 0f, duration);
        source.Stop();
    }

    // ---------------------------------------------------------------- volume api

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);

        foreach (Channel channel in channels.Values)
        {
            if (channel.sound.music) continue;

            foreach (Voice voice in channel.voices)
            {
                voice.source.volume = channel.sound.volume * masterVolume;
            }
        }

        if (musicChannel != null)
        {
            AudioSource musicSource = musicChannel.voices[0].source;
            musicTargetVolume = musicChannel.sound.volume * musicVolume * masterVolume;
            musicSource.volume = musicTargetVolume;
        }
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);

        if (musicChannel == null) return;

        musicTargetVolume = musicChannel.sound.volume * musicVolume * masterVolume;
        musicChannel.voices[0].source.volume = musicTargetVolume;
    }
}

using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

[System.Serializable] public class Audio
{ 
    [Tooltip("Sets name of audio category.")] public string audioCategory;
    [Tooltip("Sets audio clips.")] public AudioClip[] audioClips;
    [Tooltip("Sets audio source mixer group.")] public AudioMixerGroup audioGroup;
    [Tooltip("Sets audio clip to play on awake or not.")] public bool playAwake;
    [Tooltip("Sets audio clip to loop or not.")] public bool loop;
    [Tooltip("Sets audio clip priority. Lower number = higher priority.")][Range(0f, 256f)] public int priority;
    [Tooltip("Sets audio volume.")][Range(0f, 1f)] public float volume;
    [Tooltip("Sets audio pitch/ frequency.")][Range(-3f, 3f)] public float pitch;
    [Tooltip("Sets audio spatial blend.")][Range(0f, 1f)] public float spatialBlend;
    [Tooltip("Sets audio doppler level.")][Range(0f, 5f)] public float dopplerLevel;
    [Tooltip("Sets audio min distance.")] public float minDistance;
    [Tooltip("Sets audio max distance.")] public float maxDistance;
    [Tooltip("Gets audio source component.")][HideInInspector] public AudioSource audioSource;
}
public class SC_AudioController : MonoBehaviour
{
    #region Variables
    [Tooltip("Gets audio mixer.")][SerializeField] private AudioMixer audioMixer;
    [Tooltip("List of audio categories.")][SerializeField] private Audio[] audioManager;

    [Header("Volume")]
    [Tooltip("Gets master volume amount.")] private float masterVolume;
    [Tooltip("Gets music volume amount.")] private float musicVolume;
    [Tooltip("Gets effects volume amount.")] private float effectsVolume;
    [Tooltip("Gets voice volume amount.")] private float voiceVolume;

    [Header("Components")]
    [Tooltip("Gets master volume slider UI.")] private Slider masterSlider;
    [Tooltip("Gets music volume slider UI.")] private Slider musicSlider;
    [Tooltip("Gets effects volume slider UI.")] private Slider effectsSlider;
    [Tooltip("Gets voice volume slider UI.")] private Slider voiceSlider;
    [Tooltip("Gets master volume number from TextMeshPro UI.")] private TextMeshProUGUI masterNumber;
    [Tooltip("Gets music volume number from TextMeshPro UI.")] private TextMeshProUGUI musicNumber;
    [Tooltip("Gets effects volume number from TextMeshPro UI.")] private TextMeshProUGUI effectsNumber;
    [Tooltip("Gets voice volume number from TextMeshPro UI.")] private TextMeshProUGUI voiceNumber;
    #endregion

    #region Call Functions
    void Awake() => Initialization();
    #endregion

    #region Class Functions
    /// <summary> Get components and set variables. </summary>
    private void Initialization()
    {
        // Loop through audio manager list, adding audio source components with specified variables.
        foreach (Audio audio in audioManager)
        {
            audio.audioSource = gameObject.AddComponent<AudioSource>();
            audio.audioSource.outputAudioMixerGroup = audio.audioGroup;
            audio.audioSource.playOnAwake = audio.playAwake;
            audio.audioSource.loop = audio.loop;
            audio.audioSource.priority = audio.priority;
            audio.audioSource.volume = audio.volume;
            audio.audioSource.pitch = audio.pitch;
            audio.audioSource.spatialBlend = audio.spatialBlend;
            audio.audioSource.dopplerLevel = audio.dopplerLevel;
            audio.audioSource.minDistance = audio.minDistance;
            audio.audioSource.maxDistance = audio.maxDistance;
        }

        if (gameObject == GameObject.Find("PR_UI"))
        {
            GameObject audioSettings = GameObject.Find("SettingsAudio");

            // Master.
            masterVolume = PlayerPrefs.GetFloat("masterVolume");
            masterSlider = GameObject.Find("MasterVolume").GetComponent<Slider>();
            masterNumber = GameObject.Find("MasterNumber").GetComponent<TextMeshProUGUI>();
            masterSlider.value = masterVolume;
            masterNumber.SetText($"{Mathf.Round(masterVolume * 100)}%");

            // Music.
            musicVolume = PlayerPrefs.GetFloat("musicVolume");
            musicSlider = GameObject.Find("MusicVolume").GetComponent<Slider>();
            musicNumber = GameObject.Find("MusicNumber").GetComponent<TextMeshProUGUI>();
            musicSlider.value = musicVolume;
            musicNumber.SetText($"{Mathf.Round(musicVolume * 100)}%");

            // Effects.
            effectsVolume = PlayerPrefs.GetFloat("effectsVolume");
            effectsSlider = GameObject.Find("EffectsVolume").GetComponent<Slider>();
            effectsNumber = GameObject.Find("EffectsNumber").GetComponent<TextMeshProUGUI>();
            effectsSlider.value = effectsVolume;
            effectsNumber.SetText($"{Mathf.Round(effectsVolume * 100)}%");

            // Voice.
            voiceVolume = PlayerPrefs.GetFloat("voiceVolume");
            voiceSlider = GameObject.Find("VoiceVolume").GetComponent<Slider>();
            voiceNumber = GameObject.Find("VoiceNumber").GetComponent<TextMeshProUGUI>();
            voiceSlider.value = voiceVolume;
            voiceNumber.SetText($"{Mathf.Round(voiceVolume * 100)}%");

            audioSettings.SetActive(false);
        }
    }
    /// <summary> Play or stop specified or random audio clip ("play", "stop"). </summary>
    public void PlayAudio(string type, string audioCategoryName, bool random, int index)
    {
        // Gets audio category name within the audioManager array and sets it to the audio local variable.
        Audio audio = Array.Find(audioManager, audio => audio.audioCategory == audioCategoryName);
        if (audio == null)
        {
            Debug.LogWarning($"Audio: {audioCategoryName} not found!");
            return;
        }

        if (random)
        {
            int randomIndex = UnityEngine.Random.Range(0, audio.audioClips.Length);
            index = randomIndex;
        }

        switch (type)
        {
            case "play":
                audio.audioSource.clip = audio.audioClips[index];
                audio.audioSource.Play();
                break;
            case "stop":
                if (audio.audioSource.isPlaying) { audio.audioSource.Stop(); }   
                break;
            default:
                Debug.LogWarning($"Play audio type: {type} not found!");
                break;
        }
    }
    /// <summary> Fade in or out specified or random audio clip ("fadeIn" = fade in, "fadeOut" = fade out). </summary>
    public IEnumerator FadeAudio(string type, string audioCategoryName, bool random, int index, float fadeSpeed)
    {
        // Gets audio category name within the audioManager array and sets it to the audio local variable.
        Audio audio = Array.Find(audioManager, audio => audio.audioCategory == audioCategoryName);
        if (audio == null)
        {
            Debug.LogWarning($"Audio: {audioCategoryName} not found!");
            yield return new WaitForSeconds(0.1f);
        }

        if (random)
        {
            int randomIndex = UnityEngine.Random.Range(0, audio.audioClips.Length);
            index = randomIndex;
        }

        switch (type)
        {
            case "fadeIn":
                audio.audioSource.clip = audio.audioClips[index];
                audio.audioSource.volume = 0f;
                audio.audioSource.Play();

                while (audio.audioSource.volume < audio.volume)
                {
                    audio.audioSource.volume += fadeSpeed * Time.deltaTime;
                    yield return new WaitForSeconds(0.1f);
                }
                break;
            case "fadeOut":
                while (audio.audioSource.volume > 0)
                {
                    audio.audioSource.volume -= fadeSpeed * Time.deltaTime;
                    yield return new WaitForSeconds(0.1f);
                }
                if (audio.audioSource.volume <= 0 && audio.audioSource.isPlaying)
                {
                    audio.audioSource.Stop();
                }
                break;
            default:
                Debug.LogWarning($"Fade audio type: {type} not found!");
                break;
        }
    }
    /// <summary> Sets master volume in audio mixer from menu slider UI. </summary>
    public void SetMasterVolume(float sliderVolume)
    {
        // Converts slider volume value to logarithmic of base 10 multipled by 20 because the volume is in decibels and 20 is the decibel unit ceiling in unity mixer group sliders.
        audioMixer.SetFloat("masterVolume", Mathf.Log10(sliderVolume) * 20);
        masterVolume = sliderVolume;
        PlayerPrefs.SetFloat("masterVolume", masterVolume);
        masterNumber.SetText($"{Mathf.Round(masterVolume * 100)}%");       
    }
    /// <summary> Sets music volume in audio mixer from menu slider UI. </summary>
    public void SetMusicVolume(float sliderVolume)
    {
        // Converts slider volume value to logarithmic of base 10 multipled by 20 because the volume is in decibels and 20 is the decibel unit ceiling in unity mixer group sliders.
        audioMixer.SetFloat("musicVolume", Mathf.Log10(sliderVolume) * 20);
        musicVolume = sliderVolume;
        PlayerPrefs.SetFloat("musicVolume", musicVolume);
        musicNumber.SetText($"{Mathf.Round(musicVolume * 100)}%");
    }
    /// <summary> Sets effects volume in audio mixer from menu slider UI. </summary>
    public void SetEffectsVolume(float sliderVolume)
    {
        // Converts slider volume value to logarithmic of base 10 multipled by 20 because the volume is in decibels and 20 is the decibel unit ceiling in unity mixer group sliders.
        audioMixer.SetFloat("effectsVolume", Mathf.Log10(sliderVolume) * 20);
        effectsVolume = sliderVolume;
        PlayerPrefs.SetFloat("effectsVolume", effectsVolume);
        effectsNumber.SetText($"{Mathf.Round(effectsVolume * 100)}%");
    }
    /// <summary> Sets voice volume in audio mixer from menu slider UI. </summary>
    public void SetVoiceVolume(float sliderVolume)
    {
        // Converts slider volume value to logarithmic of base 10 multipled by 20 because the volume is in decibels and 20 is the decibel unit ceiling in unity mixer group sliders.
        audioMixer.SetFloat("voiceVolume", Mathf.Log10(sliderVolume) * 20);
        voiceVolume = sliderVolume;
        PlayerPrefs.SetFloat("voiceVolume", voiceVolume);
        voiceNumber.SetText($"{Mathf.Round(voiceVolume * 100)}%");
    }
    #endregion
}
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Pooled audio manager. Handles music tracks and SFX.
/// Attach to a persistent GameObject alongside GameManager.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public enum Track { Menu, CityMap, Battle }
    public enum SFX   { ButtonClick, PooFound, EggLaid, EggHatched, BattleHit, BattleWin, BattleLoss,
                        DistrictCaptured, DistrictLost, BoosterActivated, CoinCollected }

    [Header("Music")]
    public AudioClip MusicMenu;
    public AudioClip MusicCityMap;
    public AudioClip MusicBattle;
    [Range(0f,1f)] public float MusicVolume = 0.6f;

    [Header("SFX clips — assign in order of SFX enum")]
    public AudioClip[] SfxClips;
    [Range(0f,1f)] public float SfxVolume = 0.8f;

    [Header("Pool size")]
    public int SfxPoolSize = 8;

    private AudioSource _musicSource;
    private Queue<AudioSource> _sfxPool = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildPool();
    }

    void BuildPool()
    {
        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.loop   = true;
        _musicSource.volume = MusicVolume;

        for (int i = 0; i < SfxPoolSize; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.volume = SfxVolume;
            _sfxPool.Enqueue(src);
        }
    }

    public void PlayMusic(Track track)
    {
        AudioClip clip = track switch
        {
            Track.Menu    => MusicMenu,
            Track.CityMap => MusicCityMap,
            Track.Battle  => MusicBattle,
            _             => null
        };
        if (clip == null || _musicSource.clip == clip) return;
        _musicSource.clip = clip;
        _musicSource.Play();
    }

    public void PlaySFX(SFX sfx)
    {
        int index = (int)sfx;
        if (SfxClips == null || index >= SfxClips.Length || SfxClips[index] == null) return;

        var src = _sfxPool.Dequeue();
        src.PlayOneShot(SfxClips[index]);
        _sfxPool.Enqueue(src);
    }

    public void SetMusicVolume(float v) { MusicVolume = v; _musicSource.volume = v; }
    public void SetSfxVolume(float v)   { SfxVolume   = v; }
}

using UnityEngine;
using System;

public class AudioManager : MonoBehaviour
{
    // Singleton Instance agar bisa dipanggil dari script lain
    public static AudioManager Instance { get; private set; }

    [Header("--- Audio Sources ---")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("--- Background Music (BGM) ---")]
    public AudioClip bgmMenu;
    public AudioClip bgmGameplay;
    public AudioClip bgmVictory;

    [Header("--- UI & Numpad SFX ---")]
    public AudioClip sfxButtonClick;
    public AudioClip sfxButtonClear;
    public AudioClip sfxButtonEnter;

    [Header("--- Logic & Gameplay SFX ---")]
    public AudioClip sfxCorrectAnswer;
    public AudioClip sfxWrongAnswer;
    public AudioClip sfxTimerTick;
    public AudioClip sfxRoundBell;

    [Header("--- Combat & Animation SFX ---")]
    public AudioClip sfxPunchHit;
    public AudioClip sfxPunchMiss;

    private void Awake()
    {
        // Setup Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Tetap aktif walau pindah Scene
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ==========================================
    // FUNGSI UNTUK MEMUTAR BGM
    // ==========================================
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    // ==========================================
    // FUNGSI UNTUK MEMUTAR SFX (TIDAK SALING BENTROK)
    // ==========================================
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;

        // PlayOneShot memungkinkan beberapa SFX berbunyi bersamaan tanpa memotong SFX sebelumnya
        sfxSource.PlayOneShot(clip);
    }
}
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioClip MoveSuccessClip;
    public AudioClip MoveFailedClip;
    public AudioClip ButtonPressClip;
    public AudioClip BackButtonPressClip;
    public AudioClip GameWonClip;
    public AudioClip GameLoseClip;
    public AudioClip ButtonFailedClip;
    public TextMeshProUGUI VolumeIndicator;

    private AudioSource m_AudioSource;
    public bool StaysOnScene = true;

    public static AudioManager instance;
    public void Awake()
    {
        AudioListener.volume = .3f;
        if (StaysOnScene == false)
        {
            //instance = this;//
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        m_AudioSource = GetComponent<AudioSource>();
    }

    public void MoveSuccessful()
    {
        m_AudioSource.PlayOneShot(MoveSuccessClip);
    }

    public void MoveFailed()
    {
        m_AudioSource.PlayOneShot(MoveFailedClip);
    }

    public void ButtonPress()
    {
        m_AudioSource.PlayOneShot(ButtonPressClip);
    }

    public void BackButtonPress()
    {
        m_AudioSource.PlayOneShot(BackButtonPressClip);
    }

    public void GameWon()
    {
        m_AudioSource.PlayOneShot(GameWonClip);
    }

    public void GameLose()
    {
        m_AudioSource.PlayOneShot(GameLoseClip);
    }

    public void ButtonFailed()
    {
        m_AudioSource.PlayOneShot(ButtonFailedClip);
    }

    public void SetVolume(float volume)
    {
        if (!StaysOnScene) return;
        AudioListener.volume = volume;
        VolumeIndicator.text = (volume * 100f).ToString("0.0");
    }
}

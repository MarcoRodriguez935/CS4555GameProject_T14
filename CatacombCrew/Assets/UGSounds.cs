using UnityEngine;
using System.Collections;

public class UGSounds : MonoBehaviour
{

    public AudioSource fxSource;
    public AudioSource walkSource;
    public AudioSource shuffleSource;

    public AudioClip attackClip;
    public AudioClip walkClip;
    public AudioClip shuffleClip;
    public AudioClip searchClip;
    public AudioClip alertClip;
    public AudioClip unsheatheClip;
    public AudioClip unsheathe2Clip;

    private float nextStep = 0;
    private float nextShuffle = 0;

    void Start()
    {
        StartWalk();
    }

    void Update()
    {
        if(Time.time >= nextStep){
            nextStep = Time.time + 0.60f;
            walkSource.Play();
        }

        if(Time.time >= nextShuffle){
            nextShuffle = Time.time + 1.75f;
            shuffleSource.Play();
        }
    }

    public void PlayAttack(){
        fxSource.PlayOneShot(attackClip);
    }

    public void PlayAlert(){
        fxSource.PlayOneShot(alertClip);
    }

    public void PlaySearch(){
        fxSource.PlayOneShot(searchClip);
    }

    public void PlayUnsheathe(){
        AudioClip use = (Random.value < 0.5f) ? unsheatheClip : unsheathe2Clip;
        fxSource.PlayOneShot(use);
    }

    public void StartWalk(){
        walkSource.volume = 0.15f;
        shuffleSource.volume = 0.15f;

        walkSource.clip = walkClip;
        shuffleSource.clip = shuffleClip;
    }
}

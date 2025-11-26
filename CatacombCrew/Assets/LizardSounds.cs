using UnityEngine;

public class LizardSounds : MonoBehaviour
{

    public AudioSource fxSource;
    public AudioSource snarlSource;
    public AudioSource walkSource;

    public AudioClip snarlClip;
    public AudioClip biteAttack;
    public AudioClip stalkingClip;
    public AudioClip huntHowl;
    public AudioClip walkClip;

    private float walkTime = 0;
    private float lastSnarl = 0;

    void Start()
    {
        fxSource.volume = 0.5f;
        walkSource.volume = 0.15f;
        snarlSource.volume = 0.05f;
    }

    void Update()
    {
        if(Time.time >= lastSnarl){
            snarlSource.PlayOneShot(snarlClip);
            lastSnarl = Time.time + Random.Range(10f, 20f);
        }   
        if(Time.time >= walkTime){
            walkSource.PlayOneShot(walkClip);
            walkTime = Time.time + 5f;
        }   
    }

    public void PlayStalk(){
        fxSource.PlayOneShot(stalkingClip);
    }

    public void PlayHunt(){
        fxSource.PlayOneShot(huntHowl);
    }

    public void PlayAttack(){
        fxSource.PlayOneShot(biteAttack);
    }

}

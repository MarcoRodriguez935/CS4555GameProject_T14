using UnityEngine;
using System.Collections;

public class CultistSounds : MonoBehaviour
{

    public AudioSource fx1Source;
    public AudioSource fx2Source;
    public AudioSource walkSource;

    public AudioClip surpriseClip;
    public AudioClip summonClip;
    public AudioClip walkClip;
    public AudioClip commune1Clip;
    public AudioClip commune2Clip;

    private float walkTime = 0;

    void Start()
    {
        fx1Source.volume = 0.25f;
        fx2Source.volume = 0.35f;
        walkSource.volume = 0.05f;
    }

    void Update()
    { 
        if(Time.time >= walkTime){
            walkSource.PlayOneShot(walkClip);
            walkTime = Time.time + 3.5f;
        }   
    }

    public void PlaySummon(){
        StartCoroutine(SummonSounds());
    }

    public IEnumerator SummonSounds(){
         fx1Source.PlayOneShot(surpriseClip);
         yield return new WaitForSeconds(0.15f);
         fx2Source.PlayOneShot(summonClip);
         yield return null;
    }

    public void PlayCommune(){
        fx1Source.volume = 0.05f;
        fx2Source.volume = 0.75f;
        fx2Source.loop = true;
        fx2Source.clip = commune2Clip;
        fx1Source.PlayOneShot(commune1Clip);
        fx2Source.Play();
        fx1Source.volume = 0.25f;
        fx2Source.volume = 0.35f;
        fx2Source.Stop();
    }

}


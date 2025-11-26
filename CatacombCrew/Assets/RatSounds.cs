using UnityEngine;

public class RatSounds : MonoBehaviour
{

    public AudioSource fxSource;
    public AudioSource ambientSource;

    public AudioClip biteAttack;
    public AudioClip squeaks1;
    public AudioClip squeaks2;

    private float lastSqueak = 0;

    void Start()
    {
        fxSource.volume = 0.5f;
        ambientSource.volume = 0.25f;
    }

    void Update()
    {
        if(Time.time >= lastSqueak){
            AudioClip use = (Random.value < 0.5f) ? squeaks1 : squeaks2;
            ambientSource.PlayOneShot(use);
            lastSqueak = Time.time + Random.Range(3f, 15f);
        }   
    }

    public void PlayAttack(){
        fxSource.PlayOneShot(biteAttack);
    }

}

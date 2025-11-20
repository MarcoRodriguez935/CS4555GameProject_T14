using UnityEngine;
using System.Collections;

public class PlayerSounds : MonoBehaviour
{

    public AudioSource loopSource;
    public AudioSource fxSource;
    public PlayerControl playerControl;

    public AudioClip walkClip;
    public AudioClip sprintClip;

    public AudioClip jumpDiveClip;
    public AudioClip hurtClip;
    public AudioClip deathClip;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        loopSource = GetComponent<AudioSource>();
        playerControl = GetComponent<PlayerControl>();

        loopSource.playOnAwake = false;
        loopSource.loop = false;
    }

    // Update is called once per frame
    void Update()
    {
        if(playerControl.rb.linearVelocity.sqrMagnitude > 0.1f){

            AudioClip target = playerControl.isSprinting ? sprintClip : walkClip;

            loopSource.volume = playerControl.isSneaking ? 0.55f : 1f;

            if(loopSource.clip != target || !loopSource.isPlaying){
                PlayLoop(target);
            }

        }
        else {
            if(loopSource.isPlaying && loopSource.loop){
                loopSource.Stop();
                loopSource.volume = 1f; 
            }   
        }
    }   

    public void PlayHurt(){
        fxSource.PlayOneShot(hurtClip);
    }

    public void PlayDeath(){
        fxSource.PlayOneShot(deathClip);
    }

    public void PlayJump(){
        StartCoroutine(delay());
    }

    public void PlayLoop(AudioClip clip){
        loopSource.clip = clip;
        loopSource.loop = true;
        loopSource.Play();
    }

    public IEnumerator delay(){
        yield return new WaitForSeconds(0.5f);
        fxSource.PlayOneShot(jumpDiveClip);
    }
}

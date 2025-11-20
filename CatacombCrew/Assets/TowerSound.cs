using UnityEngine;

public class TowerSound : MonoBehaviour
{

    public AudioSource source;
    public AudioClip sliding;
    public AudioClip alert;

    public Watchtower tower;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        source = GetComponent<AudioSource>();
        tower = GetComponent<Watchtower>();
        source.volume = 0.1f;
        source.loop = true;
        source.pitch = 0.5f;
    }

    // Update is called once per frame
    void Update()
    {

        AudioClip target = null;

        if(tower.alerting){
            target = alert;
        }

        else{
            target = sliding;
        }

        if(target != null && source.clip != target){
            source.clip = target;
            source.Play();
        }
    }

    public void PlayLoop(AudioClip clip){
        source.clip = clip;
        source.loop = true;
        source.Play();
    }

}

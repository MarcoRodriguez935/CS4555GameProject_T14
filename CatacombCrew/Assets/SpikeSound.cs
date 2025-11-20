using UnityEngine;

public class SpikeSound : MonoBehaviour
{

    public AudioSource source;

    public AudioClip clip;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        source = GetComponent<AudioSource>();
        StartCoroutine(spikes());
        source.volume = 0.25f;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public System.Collections.IEnumerator spikes(){
        while(true){
            yield return new WaitForSeconds(3f);
            source.PlayOneShot(clip);
        }
    }
}

using UnityEngine;

public class AudioTriggerC : MonoBehaviour
{
    public AudioSource aSource; // to Assign in Inspector
    private bool hasplayed = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) 
        {
            if (!aSource.isPlaying && !hasplayed) // Avoid overlapping audio
            {
                aSource.Play();
                hasplayed = true;
                GetComponent<Collider2D>().enabled = false; // Disable trigger after first play
            }
        }
    }

}

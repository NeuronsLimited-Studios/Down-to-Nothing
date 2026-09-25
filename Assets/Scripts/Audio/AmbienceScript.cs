using System.Collections;
using UnityEngine;

//Goes on an object of the menu: the sounds that play all the time in the background, as long as the scene lasts.
//If the music of the end of the game is still playing, it waits for it to finish first
public class AmbienceScript : MonoBehaviour
{
    public Sound[] loops;

    IEnumerator Start()
    {
        while (GameAudio.MusicPlaying)
            yield return null;

        foreach (Sound loop in loops)
            GameAudio.Loop(loop);
    }
}

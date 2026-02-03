using System;
using UnityEngine;
public class SoundManager : MonoBehaviour
{
    public Sound[] sounds;

	void Awake()
	{

		foreach (Sound s in sounds)
		{
			s.source = gameObject.AddComponent<AudioSource>();

			s.source.clip = s.clip;
			s.source.loop = s.loop;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
        }
	}

    private void Start()
    {
		Play("main menu");
    }

    private Sound Search(string sound)
    {
        Sound s = Array.Find(sounds, item => item.name == sound);
        if (s == null)
        {
            Debug.LogWarning("Sound: " + name + " not found!");
            return null;
        }

        return s;
    }

    public void Play(string sound)
	{
		Search(sound).source.Play();
	}

    public bool isPlaying(string sound)
    {

        return Search(sound).source.isPlaying;
    }

    public void Stop(string sound)
    {

        Search(sound).source.Stop();
    }

}

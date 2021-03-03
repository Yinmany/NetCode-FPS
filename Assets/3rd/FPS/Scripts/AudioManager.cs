using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public AudioMixer[] audioMixers;

    public AudioMixerGroup[] FindMatchingGroups(string subPath)
    {
        for (int i = 0; i < audioMixers.Length; i++)
        {
            AudioMixerGroup[] results = audioMixers[i].FindMatchingGroups(subPath);
            if (results != null && results.Length != 0)
            {
                return results;
            }
        }

        return null;
    }

    public void SetFloat(string name, float value)
    {
        for (int i = 0; i < audioMixers.Length; i++)
        {
            if (audioMixers[i] != null)
            {
                audioMixers[i].SetFloat(name, value);
            }
        }
    }

    public void GetFloat(string name, out float value)
    {
        value = 0f;
        for (int i = 0; i < audioMixers.Length; i++)
        {
            if (audioMixers[i] != null)
            {
                audioMixers[i].GetFloat(name, out value);
                break;
            }
        }
    }
}

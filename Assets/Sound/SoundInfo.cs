using UnityEngine;

[CreateAssetMenu]
public class SoundInfo : ScriptableObject
{
    public SoundType sound;
    public AudioClip clip;
    public bool loop;
    [Range(0, 1)] public float volume = 0.5f;
}
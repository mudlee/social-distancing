using UnityEngine;

public class Ambients : MonoBehaviour
{
    void Start()
    {
        var soundPlayer = FindObjectOfType<SoundPlayer>();
        soundPlayer?.Play(SoundType.BACKGROUND_AMBIENT);
        soundPlayer?.Play(SoundType.BACKGROUND_WIND);
    }
}

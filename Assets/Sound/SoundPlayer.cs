using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SoundPlayer : MonoBehaviour
{
    private static bool CREATED = false;

    [SerializeField] private SoundInfo[] _sounds;
    [SerializeField] [Range(1, 20)] private int _audioSourceQueueSize = 10;
    private readonly Dictionary<SoundType, SoundInfo> _map = new Dictionary<SoundType, SoundInfo>();
    private readonly Dictionary<int, AudioSource> _queue = new Dictionary<int, AudioSource>();
    private readonly Dictionary<int, int> _fadeOutQueue = new Dictionary<int, int>();
    private readonly Dictionary<int, FadeInProps> _fadeInQueue = new Dictionary<int, FadeInProps>();

    struct FadeInProps
    {
        public int NumOfUpdatesRequired;
        public float PortionPerUpdate;

        public FadeInProps(int NumOfUpdatesRequired, float PortionPerUpdate)
        {
            this.NumOfUpdatesRequired = NumOfUpdatesRequired;
            this.PortionPerUpdate = PortionPerUpdate;
        }
    };

    public int Play(SoundType sound)
    {
        return Play(sound, null);
    }

    public int Play(SoundType sound, int? fadeInTime)
    {
        SoundInfo info;

        if (!_map.TryGetValue(sound, out info))
        {
            Debug.LogError(string.Format("Sound {0} cannot be played. No SoundInfo found", sound.ToString()));
            return 0;
        }

        AudioSource source = _queue.Values.FirstOrDefault(AudioSource => !AudioSource.isPlaying);
        if (source == null)
        {
            Debug.LogError("No available AudioSource");
            return 0;
        }

        source.loop = info.loop;
        source.clip = info.clip;

        if (fadeInTime.HasValue)
        {
            int numOfUpdatesRequired = Mathf.RoundToInt((float)fadeInTime.Value / Time.deltaTime);
            float portionPerUpdate = info.volume / (float)numOfUpdatesRequired;
            _fadeInQueue.Add(source.GetInstanceID(), new FadeInProps(numOfUpdatesRequired, portionPerUpdate));
            source.volume = 0;
        }
        else
        {
            source.volume = info.volume;
        }

        source.Play();

        return source.GetInstanceID();
    }

    public void Stop(int audioSourceID)
    {
        if (audioSourceID == 0)
        {
            return;
        }

        Stop(audioSourceID, null);
    }

    public void Stop(int audioSourceID, int? fadeOutTime)
    {
        if (audioSourceID == 0)
        {
            return;
        }

        AudioSource source;

        if (!_queue.TryGetValue(audioSourceID, out source))
        {
            Debug.LogError(string.Format("AudioSource with ID:{0} not found", audioSourceID));
            return;
        }

        if (fadeOutTime.HasValue)
        {
            int numOfUpdatesRequired = Mathf.RoundToInt((float)fadeOutTime.Value / Time.deltaTime);
            _fadeOutQueue.Add(audioSourceID, numOfUpdatesRequired);
        }
        else
        {
            source.Stop();
        }
    }

    private void Awake()
    {
        if (!CREATED)
        {
            DontDestroyOnLoad(this.gameObject);
            CREATED = true;
        }

        foreach (SoundInfo info in _sounds)
        {
            _map.Add(info.sound, info);
        }

        for (int i = 0; i < _audioSourceQueueSize; i++)
        {
            AudioSource source = gameObject.AddComponent(typeof(AudioSource)) as AudioSource;
            _queue.Add(source.GetInstanceID(), source);
        }
    }

    private void Update()
    {
        foreach (KeyValuePair<int, int> entry in _fadeOutQueue.ToArray())
        {
            int audioSourceID = entry.Key;
            int remainingUpdates = entry.Value;

            AudioSource source;
            _queue.TryGetValue(audioSourceID, out source);

            if (source == null)
            {
                Debug.LogError(string.Format("AudioSource {0} not found", audioSourceID));
                continue;
            }

            float portion = source.volume / (float)remainingUpdates;
            source.volume -= portion;

            _fadeOutQueue[audioSourceID] = --remainingUpdates;

            if (remainingUpdates <= 0)
            {
                source.Stop();
                _fadeOutQueue.Remove(audioSourceID);
            }
        }

        foreach (KeyValuePair<int, FadeInProps> entry in _fadeInQueue.ToArray())
        {
            int audioSourceID = entry.Key;
            FadeInProps props = entry.Value;

            AudioSource source;
            _queue.TryGetValue(audioSourceID, out source);

            if (source == null)
            {
                Debug.LogError(string.Format("AudioSource {0} not found", audioSourceID));
                continue;
            }

            source.volume += props.PortionPerUpdate;
            props.NumOfUpdatesRequired -= 1;

            _fadeInQueue[audioSourceID] = props;

            if (props.NumOfUpdatesRequired <= 0)
            {
                _fadeInQueue.Remove(audioSourceID);
            }
        }
    }
}

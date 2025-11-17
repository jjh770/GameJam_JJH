using UnityEngine;
using System.Collections.Generic;

public class ParticleManager : MonoBehaviour
{
    public static ParticleManager Instance { get; private set; }

    [System.Serializable]
    public class ParticlePool
    {
        public string poolName;
        public ParticleSystem prefab;
        public int poolSize = 5;
        [HideInInspector] public Queue<ParticleSystem> pool = new Queue<ParticleSystem>();
    }

    [Header("Particle Pools")]
    [SerializeField] private ParticlePool[] _particlePools;

    private Dictionary<string, ParticlePool> _poolDictionary;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializePools();
    }

    private void InitializePools()
    {
        _poolDictionary = new Dictionary<string, ParticlePool>();

        foreach (var particlePool in _particlePools)
        {
            _poolDictionary.Add(particlePool.poolName, particlePool);

            for (int i = 0; i < particlePool.poolSize; i++)
            {
                ParticleSystem particle = Instantiate(particlePool.prefab, transform);
                particle.gameObject.SetActive(false);
                particlePool.pool.Enqueue(particle);
            }
        }
    }

    public void PlayParticle(string poolName, Vector3 position, Transform parent = null)
    {
        if (!_poolDictionary.ContainsKey(poolName))
        {
            Debug.LogWarning($"Particle pool '{poolName}' not found!");
            return;
        }

        ParticlePool particlePool = _poolDictionary[poolName];
        ParticleSystem particle;

        // 풀에서 파티클 가져오기
        if (particlePool.pool.Count > 0)
        {
            particle = particlePool.pool.Dequeue();
        }
        else
        {
            // 풀이 비어있으면 새로 생성
            particle = Instantiate(particlePool.prefab, transform);
        }

        // 파티클 설정 및 재생
        particle.transform.position = position;
        if (parent != null)
            particle.transform.SetParent(parent);

        particle.gameObject.SetActive(true);
        particle.Play();

        // 파티클 재생 후 자동 반환
        StartCoroutine(ReturnToPoolAfterPlay(particle, particlePool));
    }

    private System.Collections.IEnumerator ReturnToPoolAfterPlay(ParticleSystem particle, ParticlePool particlePool)
    {
        yield return new WaitWhile(() => particle.isPlaying);

        particle.gameObject.SetActive(false);
        particle.transform.SetParent(transform);
        particlePool.pool.Enqueue(particle);
    }
}

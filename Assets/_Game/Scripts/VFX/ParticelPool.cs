using UnityEngine;

public class ParticelPool : GameUnit
{
    ParticleSystem myParticleSystem;
    void Awake()
    {
        myParticleSystem = GetComponent<ParticleSystem>();
    }

    public void PlayVFX()
    {
        myParticleSystem.Play();
        float timeDespawn = myParticleSystem.main.duration;
        Invoke(nameof(DespawnVFX), timeDespawn);
    }

    public void DespawnVFX()
    {
        SimplePool.Despawn(this);
    }
}

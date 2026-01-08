using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : Singleton<SoundManager>
{
    [SerializeField] SoundAsset soundAsset;

    [Range(0, 1)]
    [SerializeField] float volumeMusic = 0.75f;

    private GameSetting gameSetting;

    private Sound musicprefab;
    void Awake()
    {
        gameSetting = Resources.Load<GameSetting>(Constants.KEY_LOAD_GAME_SETTING);
    }

    public void PlaySound(eAudioName name)
    {
        AudioClipAsset audioAsset = soundAsset.GetAudioClipAsset(name);
        if (audioAsset.type == eTypeSound.Music && musicprefab == null)
        {
            musicprefab = SimplePool.Spawn<Sound>(PoolType.Audio_Sources, Vector2.zero, Quaternion.identity);
            musicprefab.audioSource.clip = audioAsset.clip;
            musicprefab.audioSource.volume = gameSetting.isMusic ? volumeMusic : 0;
            musicprefab.audioSource.Play();
            musicprefab.audioSource.loop = true;
        }
        else if (audioAsset.type == eTypeSound.Sound)
        {
            if (gameSetting.isSound)
            {
                Sound prefab = SimplePool.Spawn<Sound>(PoolType.Audio_Sources, Vector2.zero, Quaternion.identity);
                prefab.audioSource.clip = audioAsset.clip;
                prefab.audioSource.volume = 1;
                prefab.audioSource.Play();
                StartCoroutine(WaitDespawnSound(prefab, audioAsset.lenght));
            }
        }
    }

    IEnumerator WaitDespawnSound(Sound sound, float time)
    {
        yield return new WaitForSeconds(time);
        SimplePool.Despawn(sound);
    }

    public void SetVolumnMusic(bool isActive)
    {
        if (musicprefab == null) return;
        musicprefab.audioSource.volume = isActive ? volumeMusic : 0;
    }
}
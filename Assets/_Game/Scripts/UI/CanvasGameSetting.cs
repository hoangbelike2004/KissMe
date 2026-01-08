using UnityEngine;
using UnityEngine.UI;

public class CanvasGameSetting : UICanvas
{
    [SerializeField] GameSetting gameSetting;

    [SerializeField] Button btnSound, btnVibrate, btnClose, btnMusic;


    private Image IconSoundActive, IconSoundDeactive, IconVibrateActive, IconVibrateDeactive, IconMusicActive, IconMusicDeactive;
    void Start()
    {
        IconSoundActive = btnSound.transform.GetChild(0).GetComponent<Image>();
        IconSoundDeactive = btnSound.transform.GetChild(1).GetComponent<Image>();
        IconVibrateActive = btnVibrate.transform.GetChild(0).GetComponent<Image>();
        IconVibrateDeactive = btnVibrate.transform.GetChild(1).GetComponent<Image>();
        IconMusicActive = btnMusic.transform.GetChild(0).GetComponent<Image>();
        IconMusicDeactive = btnMusic.transform.GetChild(1).GetComponent<Image>();
        btnSound.onClick.AddListener(OnClickSound);
        btnVibrate.onClick.AddListener(OnClickVibrate);
        btnMusic.onClick.AddListener(OnClickMusic);
        btnClose.onClick.AddListener(() =>
        {
            UIManager.Instance.CloseUI<CanvasGameSetting>(0);
        });
        InitUI();
    }

    public void InitUI()
    {
        IconSoundActive.gameObject.SetActive(gameSetting.isSound);
        IconSoundDeactive.gameObject.SetActive(!gameSetting.isSound);
        IconVibrateActive.gameObject.SetActive(gameSetting.isVibrate);
        IconVibrateDeactive.gameObject.SetActive(!gameSetting.isVibrate);
        IconMusicActive.gameObject.SetActive(gameSetting.isMusic);
        IconMusicDeactive.gameObject.SetActive(!gameSetting.isMusic);
    }

    public void OnClickSound()
    {
        gameSetting.isSound = !gameSetting.isSound;
        IconSoundActive.gameObject.SetActive(gameSetting.isSound);
        IconSoundDeactive.gameObject.SetActive(!gameSetting.isSound);
    }

    public void OnClickVibrate()
    {
        gameSetting.isVibrate = !gameSetting.isVibrate;
        IconVibrateActive.gameObject.SetActive(gameSetting.isVibrate);
        IconVibrateDeactive.gameObject.SetActive(!gameSetting.isVibrate);
    }

    public void OnClickMusic()
    {
        gameSetting.isMusic = !gameSetting.isMusic;
        IconMusicActive.gameObject.SetActive(gameSetting.isMusic);
        IconMusicDeactive.gameObject.SetActive(!gameSetting.isMusic);
        SoundManager.Instance.SetVolumnMusic(gameSetting.isMusic);
    }
}

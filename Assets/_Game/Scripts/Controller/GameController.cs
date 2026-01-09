using System.Collections;
using UnityEngine;
using CandyCoded.HapticFeedback;

public class GameController : Singleton<GameController>
{
    [SerializeField] float waitTimeLoadLevel = 2f;

    [SerializeField] private int currentLevel;

    [SerializeField] private CanvasGameplay canvasGameplay;

    [SerializeField] private CanvasTutorial canvasTutorial;

    private CameraFollow m_cameraFollow;
    private Level level;

    private GameSetting gameSetting;

    private int maxLevel;

    void Awake()
    {
        m_cameraFollow = Camera.main.GetComponent<CameraFollow>();
        gameSetting = Resources.Load<GameSetting>(Constants.KEY_LOAD_GAME_SETTING);
        GameObject[] gameObjects = Resources.LoadAll<GameObject>("Levels");
        maxLevel = gameObjects.Length;
    }
    void Start()
    {
        LoadLevel();
        ShowTutorial();
        SoundManager.Instance.PlaySound(eAudioName.Audio_Music);
    }

    public void GameComplete()
    {
        currentLevel++;
        StartCoroutine(WaitForLoadLevel());
    }


    //sang level khac ngay va luon

    public void NextLevel()
    {
        currentLevel++;
        m_cameraFollow.ClearWinZone();
        Destroy(level.gameObject);
        level = null;
        LoadLevel();
    }
    IEnumerator WaitForLoadLevel()
    {
        if (currentLevel > 1 && canvasTutorial != null) Destroy(canvasTutorial.gameObject);
        yield return new WaitForSeconds(waitTimeLoadLevel);
        m_cameraFollow.ClearWinZone();
        Destroy(level.gameObject);
        level = null;
        LoadLevel();
    }

    public void LoadLevel()
    {
        int tmp = 0;
        if (currentLevel <= maxLevel) tmp = currentLevel;
        else
        {
            tmp = currentLevel % maxLevel;
            if (tmp == 0) tmp = maxLevel;
            Debug.Log(tmp);
        }
        level = Resources.Load<Level>(Constants.KEY_LOAD_LEVEL + tmp);
        level = Instantiate(level);
        m_cameraFollow.SetDistanceCam(level.distanceCam);
        Observer.OnSetDragType?.Invoke(level.DragTypes);
        canvasGameplay.UpdateLevelText(currentLevel);
    }

    public void ShowTutorial()
    {
        canvasTutorial.SetTargets(level.target1, level.target2);
    }

    public void DestroyTutorial()
    {
        if (currentLevel == 1 && canvasTutorial != null) Destroy(canvasTutorial.gameObject);
    }

    public void Vibrate()
    {
        if (gameSetting.isVibrate)
        {
            HapticFeedback.LightFeedback();
        }
    }
}

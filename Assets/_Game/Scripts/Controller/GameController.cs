using System.Collections;
using UnityEngine;

public class GameController : Singleton<GameController>
{
    [SerializeField] float waitTimeLoadLevel = 2f;

    [SerializeField] private int currentLevel;

    [SerializeField] private CanvasGameplay canvasGameplay;

    [SerializeField] private CanvasTutorial canvasTutorial;

    private CameraFollow m_cameraFollow;
    private Level level;

    void Awake()
    {
        m_cameraFollow = Camera.main.GetComponent<CameraFollow>();
    }
    void Start()
    {
        LoadLevel();
        ShowTutorial();
    }

    public void GameComplete()
    {
        currentLevel++;
        StartCoroutine(WaitForLoadLevel());
    }

    IEnumerator WaitForLoadLevel()
    {
        yield return new WaitForSeconds(waitTimeLoadLevel);
        m_cameraFollow.ClearWinZone();
        Destroy(level.gameObject);
        level = null;
        LoadLevel();
    }

    public void LoadLevel()
    {
        level = Resources.Load<Level>(Constants.KEY_LOAD_LEVEL + currentLevel);
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
}

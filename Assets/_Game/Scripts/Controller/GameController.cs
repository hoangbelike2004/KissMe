using System.Collections;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [SerializeField] float waitTimeLoadLevel = 2f;
    private int currentLevel;

    private Level level;
    void Awake()
    {

    }
    void Start()
    {

    }

    public void GameComplete()
    {
        currentLevel++;
        StartCoroutine(WaitForLoadLevel());
    }

    IEnumerator WaitForLoadLevel()
    {
        yield return new WaitForSeconds(waitTimeLoadLevel);
        Destroy(level.gameObject);
        level = null;
        LoadLevel();
    }

    public void LoadLevel()
    {
        level = Resources.Load<Level>(Constants.KEY_LOAD_LEVEL + currentLevel);
        level = Instantiate(level);
        Observer.OnSetDragType?.Invoke(level.DragType);
    }
}

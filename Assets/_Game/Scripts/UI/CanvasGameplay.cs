using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasGameplay : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI txtLevel;

    [SerializeField] Button btnSetting, btnNextLevel;

    void Start()
    {
        btnSetting.onClick.AddListener(() =>
        {
            UIManager.Instance.OpenUI<CanvasGameSetting>();
        });

        btnNextLevel.onClick.AddListener(() =>
        {
            GameController.Instance.NextLevel();
        });
    }
    public void UpdateLevelText(int level)
    {
        txtLevel.text = "Level " + level;
    }
}

using TMPro;
using UnityEngine;

public class CanvasGameplay : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI txtLevel;

    public void UpdateLevelText(int level)
    {
        txtLevel.text = "Level " + level;
    }
}

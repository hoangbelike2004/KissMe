using UnityEngine;

[CreateAssetMenu(fileName = "GameSetting", menuName = "Scriptable Objects/GameSetting")]
public class GameSetting : ScriptableObject
{
    public bool isSound = true;
    public bool isMusic = true;
    public bool isVibrate = true;
}

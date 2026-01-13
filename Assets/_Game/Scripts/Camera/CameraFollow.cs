using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public List<Winzone> winzones = new List<Winzone>();

    [SerializeField] private float distance = 5;

    [SerializeField] private float speedMove = 10;

    [SerializeField] private float sizeDefault = 1;

    [SerializeField] private float fieldOfViewCam = 60;

    private float currentSize;

    public void UpdateCam()
    {
        if (winzones.Count == 0) return;

        if (winzones.Count == 1)
        {
            Vector3 pos = winzones[0].transform.position - transform.forward * distance;
            transform.position = Vector3.Lerp(transform.position, pos, Time.deltaTime * speedMove);
        }
        else
        {
            Vector3 centerPos = Vector3.zero;
            centerPos = winzones[0].transform.position + winzones[1].transform.position;
            centerPos /= 2;

            Vector3 desiredPosition = centerPos - transform.forward * distance;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * speedMove);

            currentSize = Vector3.Distance(winzones[0].transform.position, winzones[1].transform.position);
            if (currentSize <= sizeDefault) currentSize = sizeDefault;
            float clampSize = Mathf.Clamp(currentSize * currentSize * 2f + fieldOfViewCam, fieldOfViewCam, fieldOfViewCam + 30);
            Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, clampSize, Time.deltaTime * speedMove);
        }
    }
    public void SetDistanceCam(float dis)
    {
        this.distance = dis;
    }
    public void AddWinzone(Winzone wz)
    {
        if (!winzones.Contains(wz))
        {
            winzones.Add(wz);
        }
    }
    public void RemoveWinzone(Winzone wz)
    {
        if (winzones.Contains(wz))
        {
            winzones.Remove(wz);
        }
    }

    public void ClearWinZone()
    {
        winzones.Clear();
    }
}

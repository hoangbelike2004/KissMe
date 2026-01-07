using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CanvasTutorial : MonoBehaviour
{
    [SerializeField] RectTransform rectTutorial;

    [SerializeField] Vector3 offset;
    private Transform target1;
    private Transform target2;

    private Camera mainCam;

    private Vector3 startPos;

    private Vector3 endPos;

    private Image img;

    void Start()
    {
        mainCam = Camera.main; // Lấy Camera chính
        img = rectTutorial.GetChild(0).GetComponent<Image>();
        img.DOFade(0, 0);
    }

    public void SetTargets(Transform t1, Transform t2)
    {
        target1 = t1;
        target2 = t2;
        img.DOFade(1, 0.1f);
        if (target1 != null && target2 != null)
        {
            // Bước quan trọng: Chuyển toạ độ 3D -> Toạ độ màn hình 2D
            startPos = target1.position + offset;
            endPos = target2.position + offset;
            rectTutorial.position = startPos;
            rectTutorial.DOMove(endPos, 1.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }
    }
}

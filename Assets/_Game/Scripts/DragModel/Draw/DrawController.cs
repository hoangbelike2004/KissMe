using Unity.VisualScripting;
using UnityEngine;

public class DrawController : MonoBehaviour
{
    [SerializeField] RagdollPathDrawer ragdollPathDrawer;
    [SerializeField] RagdollPuppetMaster puppetMaster1, puppetMaster2;
    void Start()
    {
        ragdollPathDrawer.SetPuppetMaster(puppetMaster1);
    }

    public void SetPuppetMaster()
    {
        ragdollPathDrawer.ResetDrawing();
        ragdollPathDrawer.SetPuppetMaster(puppetMaster2);
    }
    void OnEnable()
    {
        Observer.OnDrawToTaget += SetPuppetMaster;
    }

    void OnDisable()
    {
        Observer.OnDrawToTaget -= SetPuppetMaster;
    }
}

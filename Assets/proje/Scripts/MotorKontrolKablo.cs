using UnityEngine;
public class MotorKontrolKablo : MonoBehaviour
{
    public Transform pervaneObjesi;
    public SnapTarget kablo1;
    public SnapTarget kablo2;
    public float donusHizi = 1000f;
    public Vector3 donusYonVektoru = new Vector3(0, 0, 1);
    private bool motorAcik = false;
    void Update()
    {
        bool kablolarBagli = kablo1 != null && kablo2 != null && kablo1.isConnected && kablo2.isConnected;
        if (motorAcik && kablolarBagli && pervaneObjesi != null)
        {
            pervaneObjesi.Rotate(donusYonVektoru * donusHizi * Time.deltaTime, Space.Self);
        }
    }
    public void DurumuDegistir()
    {
        motorAcik = !motorAcik;
    }
}
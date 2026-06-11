using UnityEngine;
using UnityEngine.InputSystem; 
using TMPro; 
public class MotorKontrol : MonoBehaviour
{
    public Transform pervaneObjesi; 
    public Renderer butonRenderer;           
    public float donusHizi = 1000f;
    public Vector3 donusYonVektoru = new Vector3(0, 0, 1);   
    private bool motorAcik = false;
    void Update()
    {
        if (motorAcik && pervaneObjesi != null)
        {
            pervaneObjesi.Rotate(donusYonVektoru * donusHizi * Time.deltaTime, Space.Self);
        }       
    }
    public void DurumuDegistir()
    {
        motorAcik = !motorAcik;           
    }        
}
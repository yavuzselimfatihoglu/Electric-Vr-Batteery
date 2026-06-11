using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
public class CanvasController : MonoBehaviour
{
    [SerializeField] string[] basliklar;
    [SerializeField] string[] aciklamalar;
    [SerializeField] VideoPlayer[] videoPlayers;
    [SerializeField] AudioSource audioSource;
    [SerializeField] TextMeshProUGUI baslikArea;
    [SerializeField] TextMeshProUGUI aciklamaArea;
    [SerializeField] VideoClip[] videolar;
    [SerializeField] AudioClip[] sesler;
    [SerializeField] Button[] butonlar;
    void Start()
    {
        baslikArea.text = basliklar[0];
        aciklamaArea.text = aciklamalar[0];
        videoPlayers[0].clip = videolar[0];
        audioSource.clip = sesler[0];
        for (int i = 0; i < butonlar.Length; i++)
        {
            int index = i; 
            butonlar[i].onClick.AddListener(() => ButonClick(index));
        }
    }
    private void ButonClick(int index)
    {
        baslikArea.text = basliklar[index];
        aciklamaArea.text = aciklamalar[index];
        videoPlayers[0].clip = videolar[index];
        audioSource.clip = sesler[index];
        audioSource.Play();
    }
}
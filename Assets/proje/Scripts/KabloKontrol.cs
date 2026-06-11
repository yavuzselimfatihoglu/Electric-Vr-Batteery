using UnityEngine;
using System.Collections.Generic;
[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class KabloKontrol : MonoBehaviour
{
    [Header("Kablo Noktaları")]   
    public List<Transform> noktalar = new List<Transform>();
    [Header("Görünüm Ayarları")]
    [Range(2, 50)]
    public int pürüzsüzlük = 10; 
    public float kabloGenisligi = 0.1f;
    public Material kabloMateryali;
    private LineRenderer lineRenderer;
    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        SetupLineRenderer();
    }
    void Update()
    {
        if (noktalar == null || noktalar.Count < 2)
        {
            lineRenderer.positionCount = 0;
            return;
        }
        CizimiGuncelle();
    }
    void SetupLineRenderer()
    {
        lineRenderer.startWidth = kabloGenisligi;
        lineRenderer.endWidth = kabloGenisligi;
        if (kabloMateryali != null) lineRenderer.material = kabloMateryali;        
        lineRenderer.numCornerVertices = 5;
        lineRenderer.numCapVertices = 5;
    }
    void CizimiGuncelle()
    {
        int toplamNokta = (noktalar.Count - 1) * pürüzsüzlük + 1;
        lineRenderer.positionCount = toplamNokta;
        int index = 0;
        for (int i = 0; i < noktalar.Count - 1; i++)
        {
            if (noktalar[i] == null || noktalar[i+1] == null) continue;
            for (int j = 0; j < pürüzsüzlük; j++)
            {
                float t = j / (float)pürüzsüzlük;
                Vector3 araPozisyon = Vector3.Lerp(noktalar[i].position, noktalar[i+1].position, t);
                lineRenderer.SetPosition(index, araPozisyon);
                index++;
            }
        }
        lineRenderer.SetPosition(toplamNokta - 1, noktalar[noktalar.Count - 1].position);       
        lineRenderer.startWidth = kabloGenisligi;
        lineRenderer.endWidth = kabloGenisligi;
    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        foreach (var nokta in noktalar)
        {
            if (nokta != null) Gizmos.DrawSphere(nokta.position, 0.05f);
        }
    }
}
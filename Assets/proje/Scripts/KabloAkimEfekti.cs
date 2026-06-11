using UnityEngine;
using GogoGaga.OptimizedRopesAndCables;
public class KabloAkimEfekti : MonoBehaviour
{
    public Rope rope;
    public SnapTarget baglanti;
    public float akimHizi = 0.8f;
    public float objeBoyutu = 0.03f;
    public Color akimRengi = new Color(1f, 0.9f, 0.2f);
    private Transform _akimObjesi;
    private float _t;
    private Material _mat;
    void Start()
    {
        if (rope == null)
            rope = GetComponent<Rope>();
        if (rope == null || baglanti == null) return;
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "AkimGosterge";
        go.transform.SetParent(transform);
        go.transform.localScale = Vector3.one * objeBoyutu;
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
        var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
        _mat = new Material(shader);
        _mat.color = akimRengi;
        go.GetComponent<Renderer>().sharedMaterial = _mat;
        _akimObjesi = go.transform;
        _akimObjesi.gameObject.SetActive(false);
    }
    void Update()
    {
        if (_akimObjesi == null || rope == null || baglanti == null) return;
        if (!baglanti.isConnected)
        {
            _akimObjesi.gameObject.SetActive(false);
            return;
        }
        _akimObjesi.gameObject.SetActive(true);
        _t += akimHizi * Time.deltaTime;
        if (_t > 1f) _t = 0f;
        _akimObjesi.position = rope.GetPointAt(_t);
    }
    void OnDestroy()
    {
        if (_mat != null) Destroy(_mat);
        if (_akimObjesi != null) Destroy(_akimObjesi.gameObject);
    }
}
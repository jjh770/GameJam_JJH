using UnityEngine;

public class BackgroundScroll : MonoBehaviour
{
    [SerializeField]
    private Vector2 _scrollDirection = Vector2.left;
    [SerializeField]
    private float _scrollSpeed = 0.1f;

    private Material _material;

    private void Awake()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogError("Renderer 컴포넌트를 찾을 수 없습니다.", this);
            enabled = false;
            return;
        }
        _material = renderer.material;
    }

    void Update()
    {
        if (_material == null) return;

        _material.mainTextureOffset += _scrollDirection.normalized * _scrollSpeed * Time.deltaTime;
    }
}

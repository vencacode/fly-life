using UnityEngine;

/// <summary>
/// Drag-to-pan + pinch-to-zoom camera for the city map.
/// </summary>
[RequireComponent(typeof(Camera))]
public class MapCamera : MonoBehaviour
{
    [Header("Pan")]
    public float DragSpeed = 1.5f;

    [Header("Zoom")]
    public float MinZoom = 3f;
    public float MaxZoom = 12f;
    public float ZoomSpeed = 0.5f;

    [Header("Bounds (match TerritoryManager grid)")]
    public float MinX = -2f;
    public float MaxX = 42f;
    public float MinY = -2f;
    public float MaxY = 42f;

    Camera _cam;
    Vector3 _dragOrigin;
    bool _dragging;

    void Awake() => _cam = GetComponent<Camera>();

    void Update()
    {
        HandleTouch();
        HandleMouse(); // editor testing
        ClampPosition();
    }

    void HandleTouch()
    {
        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                _dragOrigin = _cam.ScreenToWorldPoint(t.position);
                _dragging = true;
            }
            else if (t.phase == TouchPhase.Moved && _dragging)
            {
                Vector3 diff = _dragOrigin - _cam.ScreenToWorldPoint((Vector3)t.position);
                transform.position += diff;
            }
            else if (t.phase == TouchPhase.Ended) _dragging = false;
        }
        else if (Input.touchCount == 2)
        {
            _dragging = false;
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);
            float prevDist = (t0.position - t0.deltaPosition - (t1.position - t1.deltaPosition)).magnitude;
            float currDist = (t0.position - t1.position).magnitude;
            float delta = prevDist - currDist;
            _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize + delta * ZoomSpeed * Time.deltaTime, MinZoom, MaxZoom);
        }
    }

    void HandleMouse()
    {
        if (Input.GetMouseButtonDown(0))
            _dragOrigin = _cam.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButton(0))
        {
            Vector3 diff = _dragOrigin - _cam.ScreenToWorldPoint(Input.mousePosition);
            transform.position += diff;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
            _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize - scroll * 3f, MinZoom, MaxZoom);
    }

    void ClampPosition()
    {
        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, MinX, MaxX);
        p.y = Mathf.Clamp(p.y, MinY, MaxY);
        transform.position = p;
    }
}

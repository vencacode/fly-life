using UnityEngine;

/// <summary>
/// Handles map camera zoom (pinch on mobile, scroll wheel in editor) and pan (drag).
/// Attach to MainMap scene's Main Camera.
/// </summary>
public class MapCameraController : MonoBehaviour
{
    [Header("Zoom")]
    public float MinZoom = 3f;
    public float MaxZoom = 8f;
    public float ZoomSpeed = 0.5f;

    [Header("Pan Bounds")]
    public Vector2 MinBounds = new(-2f, -2f);
    public Vector2 MaxBounds = new(6f, 6f);

    Camera _cam;
    Vector3 _dragOrigin;
    bool _isDragging;

    void Start() => _cam = Camera.main;

    void Update()
    {
        if (_cam == null) return;

        // Pinch zoom (mobile)
        if (Input.touchCount == 2)
        {
            var t0 = Input.GetTouch(0);
            var t1 = Input.GetTouch(1);
            float prevDist = (t0.position - t0.deltaPosition - (t1.position - t1.deltaPosition)).magnitude;
            float currDist = (t0.position - t1.position).magnitude;
            float delta = prevDist - currDist;
            _cam.orthographicSize = Mathf.Clamp(
                _cam.orthographicSize + delta * ZoomSpeed * Time.deltaTime,
                MinZoom, MaxZoom);
            _isDragging = false;
            return;
        }

        // Scroll wheel zoom (editor)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
            _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize - scroll * 2f, MinZoom, MaxZoom);

        // Pan (single finger/mouse drag)
        if (Input.GetMouseButtonDown(0))
        {
            _dragOrigin = _cam.ScreenToWorldPoint(Input.mousePosition);
            _isDragging = true;
        }

        if (Input.GetMouseButton(0) && _isDragging)
        {
            Vector3 diff = _dragOrigin - _cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3 newPos = _cam.transform.position + diff;
            newPos.x = Mathf.Clamp(newPos.x, MinBounds.x, MaxBounds.x);
            newPos.y = Mathf.Clamp(newPos.y, MinBounds.y, MaxBounds.y);
            newPos.z = _cam.transform.position.z;
            _cam.transform.position = newPos;
        }

        if (Input.GetMouseButtonUp(0))
            _isDragging = false;
    }
}

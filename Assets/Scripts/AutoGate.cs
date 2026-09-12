using UnityEngine;

public class AutoGate : MonoBehaviour
{
    public float openAngle   = 90f;
    public float speed       = 80f;
    public float triggerDist = 4f;

    Transform _pivot;
    float     _currentAngle;
    bool      _open;
    Transform _player;

    void Start()
    {
        _pivot = transform;
        // Find XR rig or camera
        var cam = Camera.main;
        if (cam) _player = cam.transform;
    }

    void Update()
    {
        if (_player == null) return;

        float dist = Vector3.Distance(_pivot.position, _player.position);
        _open = dist < triggerDist;

        float target = _open ? openAngle : 0f;
        _currentAngle = Mathf.MoveTowards(_currentAngle, target, speed * Time.deltaTime);
        _pivot.localRotation = Quaternion.Euler(0f, _currentAngle, 0f);
    }
}

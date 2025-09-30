using UnityEngine;
using System.Collections;

public class DoorController : MonoBehaviour
{
    [Header("Rotation in degrees relative to starting rotation")]
    public Vector3 openRotationOffset = new Vector3(0f, 90f, 0f);
    public float openCloseDuration = 0.6f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0,0,1,1);

    Quaternion _closedRot;
    Quaternion _openRot;
    bool _isOpen;
    Coroutine _current;

    void Awake()
    {
        _closedRot = transform.rotation;
        _openRot = _closedRot * Quaternion.Euler(openRotationOffset);
    }

    public void ToggleDoor()
    {
        if (_current != null) StopCoroutine(_current);
        _current = StartCoroutine(RotateTo(_isOpen ? _closedRot : _openRot));
        _isOpen = !_isOpen;
    }

    IEnumerator RotateTo(Quaternion target)
    {
        Quaternion start = transform.rotation;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, openCloseDuration);
            float k = ease.Evaluate(Mathf.Clamp01(t));
            transform.rotation = Quaternion.Slerp(start, target, k);
            yield return null;
        }
        transform.rotation = target;
        _current = null;
    }
}
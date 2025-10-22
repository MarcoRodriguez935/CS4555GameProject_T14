using UnityEngine;

public interface SoundHeard 
{
    void OnSound(Vector3 soundOrigin, Vector3 soundDir, float magnitude, GameObject reason);
}

using UnityEngine;

public interface SoundHeard 
{
    void OnSound(Vector3 soundOrigin, float magnitude, GameObject reason);
}

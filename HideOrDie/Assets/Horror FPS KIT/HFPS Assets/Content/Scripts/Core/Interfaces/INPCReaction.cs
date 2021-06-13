using UnityEngine;

public interface INPCReaction
{
    void HitReaction();
    void SoundReaction(Vector3 pos, bool closeSound);
}

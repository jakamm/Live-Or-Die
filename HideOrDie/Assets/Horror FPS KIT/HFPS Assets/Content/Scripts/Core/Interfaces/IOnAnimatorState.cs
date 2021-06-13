using UnityEngine;

public interface IOnAnimatorState
{
    void OnStateEnter(AnimatorStateInfo state, string name);
}
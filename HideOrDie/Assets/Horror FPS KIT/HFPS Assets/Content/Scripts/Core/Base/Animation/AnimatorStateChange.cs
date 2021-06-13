using System.Linq;
using UnityEngine;

public class AnimatorStateChange : StateMachineBehaviour
{
    public string Name = "";

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        IOnAnimatorState[] scripts = (from script in animator.gameObject.GetComponents<MonoBehaviour>()
                                      where typeof(IOnAnimatorState).IsAssignableFrom(script.GetType())
                                      select script as IOnAnimatorState).ToArray();

        if(scripts.Length <= 0)
        {
            scripts = (from script in animator.transform.parent.gameObject.GetComponents<MonoBehaviour>()
                       where typeof(IOnAnimatorState).IsAssignableFrom(script.GetType())
                       select script as IOnAnimatorState).ToArray();
        }

        foreach (var istate in scripts)
        {
            istate.OnStateEnter(stateInfo, Name);
        }
    }
}

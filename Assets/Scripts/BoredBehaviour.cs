using UnityEngine;

public class BoredBehaviour : StateMachineBehaviour
{
    public float TimeUntillBored;

    public bool isBored;
    public float idleTime;
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        ResetIdle(animator);
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (isBored == false)
        {
            idleTime += Time.deltaTime;

            if (idleTime > TimeUntillBored && stateInfo.normalizedTime % 1 < 0.02f)
            {
                isBored = true; 

                animator.SetFloat("Bored", 1);
            }
        } else if (stateInfo.normalizedTime % 1 > 0.98)
        {
            ResetIdle(animator);
        }
    }

    private void ResetIdle(Animator animator)
    {
        isBored = false;
        idleTime = 0;

        animator.SetFloat("Bored", 0);
    }
    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    //override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateMove is called right after Animator.OnAnimatorMove()
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that processes and affects root motion
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}

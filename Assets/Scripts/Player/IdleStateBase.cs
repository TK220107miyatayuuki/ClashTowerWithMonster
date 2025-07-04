using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

namespace Game.Players
{
    public class IdleStateBase<T> : StateMachineBase<T> where T : PlayerControllerBase<T>
    {
       public IdleStateBase(T controller):base(controller) { }
        public override void OnEnter()
        {
            nextState = controller.MoveState;
            OnEnterProcess();
        }

        public override void OnUpdate()
        {
           if(InputManager.IsClickedMoveAndAutoAttack())
           {
                Debug.Log("押されました");
                controller.ChangeState(nextState);
                controller.MoveState.isPressedA = true;
           }
           else if (InputManager.IsCllikedMoveButton()) controller.ChangeState(nextState);
        }
        public override void OnExit()
        {
            Debug.Log($"{controller.name}のisIdleがfalseになりました");
            controller.animator.SetBool(controller.AnimatorPar.Idle, false);
        }
        protected virtual void OnEnterProcess()
        {
            AllResetBoolProparty();
           // nextState = controller.
        }

        void AllResetBoolProparty()
        {
            controller.animator.SetBool(controller.AnimatorPar.Idle, true);
            Debug.Log($"{controller.AnimatorPar.Move},{controller.AnimatorPar.Attack}");
           if(controller.animator.GetBool(controller.AnimatorPar.Move)) controller.animator.SetBool(controller.AnimatorPar.Move, false);
        }
    }
    
}



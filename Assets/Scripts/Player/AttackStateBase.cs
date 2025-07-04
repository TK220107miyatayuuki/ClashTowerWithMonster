using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
namespace Game.Players
{
    public class AttackStateBase<T> : StateMachineBase<T>,IUnitAttack where T : PlayerControllerBase<T>
    {
        public UnitBase target;
        public AttackStateBase(T controller) : base(controller) { }
        CancellationTokenSource cts = new CancellationTokenSource();
        int attackAmount = 0;
        bool isAttacking = false;
        protected float interval = 0f;
        public override void OnEnter()
        {
            attackAmount = controller.BuffStatus(BuffType.Power, controller.PlayerStatus.AttackAmount);
            cts = new CancellationTokenSource();
            if (clipLength == 0)
            {
                clipLength = controller.GetAnimClipLength();
            }
        }
        public override void OnUpdate()
        {
            attackAmount = controller.BuffStatus(BuffType.Power,controller.PlayerStatus.AttackAmount);
            if(target != null) LookEnemyDirection();
            Debug.Log(target);
            if(!isAttacking)
            {
                isAttacking = true;
                controller.OnAttackingPlayer?.Invoke(isAttacking);
                Attack();
            }
            if(target == null)
            {
                nextState = controller.IdleState;
                controller.ChangeState(nextState);
                Debug.Log("おあおあお");
            }
            else if (InputManager.IsClickedMoveWhenAttacking())//
            {
                nextState = controller.MoveState;
                controller.ChangeState(nextState);
            }

        }
        public override void OnExit()
        {
            cts.Cancel();
            cts.Dispose();
            isAttacking = false;
            controller.OnAttackingPlayer?.Invoke(isAttacking);
        }
        public void Attack()
        {
            controller.animator.SetTrigger(controller.AnimatorPar.Attack);
        }

        void LookEnemyDirection()
        {
            var direction = target.transform.position - controller.transform.position;
            var rotation = Quaternion.LookRotation(direction);
            controller.transform.rotation = rotation;
        }
        public void Attack_SimpleFromAnimEvent()
        {
            if (target != null && target.TryGetComponent<IUnitDamagable>(out var unitDamagable))
            {
                Debug.Log($"{controller.gameObject.name}のアタック");
                unitDamagable.Damage(attackAmount);
                EffectManager.Instance.hitEffect.GenerateHitEffect(target);
            }
            //controller.animator.speed = 0f;         
        }

        public async void WaitIntervalFromAnimEvent()
        {
            if (cts.IsCancellationRequested) return;
            try
            {
                //�@
                await UniTask.Delay(TimeSpan.FromSeconds(interval), cancellationToken: cts.Token);
            }
            catch(ObjectDisposedException)
            {
                //非同期のキモ挙動で�@の時にOnExit()が割り込んできた時用
                return;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            isAttacking = false;
            controller.OnAttackingPlayer?.Invoke(isAttacking);
        }
    }
}



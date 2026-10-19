using DG.Tweening;
using GameLib;
using UnityEngine;

namespace Game.Input
{
    public class AimingArrow : MonoBehaviour
    { 
        public enum State
        {
            Appearing,
            Disappearing,
            Enabled,
            Disabled
        }

        public GameObject Arrow;
        public Renderer[] Segments;
        public Material ActiveSegmentMaterial;
        public Material NonActiveSegmentMaterial;

        private StateMachine<State> _stateMachine;
        private Vector3 _originalScale;

        void Awake()
        {
            _stateMachine = new StateMachine<State>(this, State.Disabled);
            _originalScale = Arrow.transform.localScale;
            _stateMachine.GoTo(State.Disabled);
        }

        void Update()
        {
            _stateMachine.Update();
        }

        #region StateMachine

        void OnEnterDisabled()
        {
            // play disable animation
            Arrow.transform.DOKill();
            Arrow.transform.localScale = Vector3.zero;
        }

        void OnEnterEnabled()
        {
            // play enable animation
            Arrow.transform.DOScale(_originalScale, 0.1f).SetEase(Ease.InOutBack).SetUpdate(true);
        }

        void OnUpdateEnabled()
        {
            Arrow.SetActive(InputHandler.Instance.AimingLevel > 0);
            if (Arrow.activeSelf)
            {
                // Update scale
                for (int i = 0; i < Segments.Length; ++i)
                    Segments[i].transform.localScale = Vector3.one * (DotScale(i) * (1.0f + 0.025f * Mathf.Sin(-i + Time.unscaledTime * 7.0f)));

                // Update segment materials based on level of aiming
                SetArrowBasedOnLevel(InputHandler.Instance.AimingLevel);

                // Update rotation
                Arrow.transform.rotation = Quaternion.LookRotation(InputHandler.Instance.AimingDirection, Vector3.back);
            }
        }

        #endregion

        public void SetState(State state)
        {
            _stateMachine.GoToIfNotInState(state);
        }

        private static float DotScale(int idx)
        {
            return 0.8f - idx * 0.002f;
        }

        private void SetArrowBasedOnLevel(int level)
        {
            if (level == 0)
            {
                Segments[0].material = NonActiveSegmentMaterial;
                Segments[1].material = NonActiveSegmentMaterial;
                Segments[2].material = NonActiveSegmentMaterial;
            }
            else if (level == 1)
            {
                Segments[0].material = ActiveSegmentMaterial;
                Segments[1].material = NonActiveSegmentMaterial;
                Segments[2].material = NonActiveSegmentMaterial;
            }
            else if (level == 2)
            {
                Segments[0].material = ActiveSegmentMaterial;
                Segments[1].material = ActiveSegmentMaterial;
                Segments[2].material = NonActiveSegmentMaterial;
            }
            else if (level == 3)
            {
                Segments[0].material = ActiveSegmentMaterial;
                Segments[1].material = ActiveSegmentMaterial;
                Segments[2].material = ActiveSegmentMaterial;
            }
        }
    }
}
using Alg;
using DG.Tweening;
using Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class InputHandler : Singleton<InputHandler>
{
    public class EventInputFreezeStart
    {
    }

    public class EventInputFreezeRelease
    {
        public Vector2 AimingDirection;
        public int AimingLevel;
    }

    public class EventInputBoosterPress
    {
    }

    public class EventInputCloseBubble
    {
    }

    public PlayerInput PlayerInput;


    [Range(0f, 1f)]
    public float AimingLevel0;

    [Range(0f, 1f)]
    public float AimingLevel1;

    [Range(0f, 1f)]
    public float AimingLevel2;

    [Range(0f, 1f)]
    public float AimingLevel3;



    public float DragMax;
    public float DragMin;


    [Tooltip("Rects which is checked for the touch is didn't start inside them")]
    public RectTransform[] TouchDeniedRects;

    public Vector2 MoveVector { get; private set; }
    public Vector2 AimingDirection { get; private set; }
    public int AimingLevel { get; private set; }
    public Vector2 IntentionVector { get; private set; }


    // for touch support
    public Vector2 FirstTouchPoint { get; set; }
    public Vector2 LastTouchPoint { get; set; }
    public bool IsTouching { get; set; }

    private InputAction _move;
    private InputAction _intentionVector;
    private InputAction _pointer;
    private InputAction _stretch;

    private bool _isCharacterControlsEnabled;


    protected override void Awake()
    {
        base.Awake();

        _move = PlayerInput.actions["Move"];
        _pointer = PlayerInput.actions["Pointer"];
        _stretch = PlayerInput.actions["Stretch"];
        _intentionVector = PlayerInput.actions["IntentionVector"];
    }

    public void Update()
    {
        if(!_isCharacterControlsEnabled)
            return;
        
        MoveVector = _move.ReadValue<Vector2>();

        IntentionVector = _intentionVector.ReadValue<Vector2>();

        if (!UpdateTouchDrag())// update aiming using drag
        {
            // update aiming using gamepad stick
            var stretchVector = _stretch.ReadValue<Vector2>();
            float aiming01 = Mathf.Clamp01(stretchVector.magnitude);
            AimingDirection = stretchVector.normalized;
            AimingLevel = AimingVectorLength01ToLevel(aiming01);
        }
    }

    void OnDisable()
    {
        Gamepad.current?.SetMotorSpeeds(0.0f, 0.0f);
    }

    private bool UpdateTouchDrag()
    {
        if (!_isCharacterControlsEnabled)
            return false;
        if (!IsTouching)
            return false;
        if (DragControl.Instance == null)
            return false;
        
        LastTouchPoint = _pointer.ReadValue<Vector2>();
        var delta = DragControl.Instance.GetStartTouchPoint() - DragControl.Instance.GetEndTouchPoint();
        var onScreenDistance = delta.magnitude;
        AimingDirection = delta.normalized;

        if (onScreenDistance < DragMin)
            onScreenDistance = 0f;
        else if (onScreenDistance > DragMax)
            onScreenDistance = DragMax;

        float aiming01 = Mathf.Clamp01(onScreenDistance / DragMax);
        AimingLevel = AimingVectorLength01ToLevel(aiming01);
        return true;
    }

    #region callbacks from the input actions
    public void TimeStopHold(InputAction.CallbackContext context)
    {
        if (!_isCharacterControlsEnabled)
            return;
        if (context.started)
        {
            GlobalEventAggregator.EventAggregator.Publish(new EventInputFreezeStart());
        }
        else if (context.canceled)
        {
            GlobalEventAggregator.EventAggregator.Publish(new EventInputFreezeRelease { AimingDirection = AimingDirection, AimingLevel = AimingLevel });
        }
    }

    public void Booster(InputAction.CallbackContext context)
    {
        if (!_isCharacterControlsEnabled)
            return;
        if (context.started)
            GlobalEventAggregator.EventAggregator.Publish(new EventInputBoosterPress());
    }

    public void Stretch(InputAction.CallbackContext context)
    {
    }

    public void Move(InputAction.CallbackContext context)
    {
    }

    public void FirstTouch(InputAction.CallbackContext context)
    {
        if (!_isCharacterControlsEnabled)
            return;
        if (context.started)
        {
            if (IsTouching)
                return;

            var firstTouch = _pointer.ReadValue<Vector2>();
            if(firstTouch == Vector2.zero)
                return;

            foreach (var touchDeniedRect in TouchDeniedRects)
                if (RectTransformUtility.RectangleContainsScreenPoint(touchDeniedRect, firstTouch))
                    return;

            FirstTouchPoint = firstTouch;
            IsTouching = true;
            GlobalEventAggregator.EventAggregator.Publish(new EventInputFreezeStart());
        }
        else if (context.canceled)
        {
            UpdateTouchDrag();
            IsTouching = false;
            GlobalEventAggregator.EventAggregator.Publish(new EventInputFreezeRelease{AimingDirection = AimingDirection, AimingLevel = AimingLevel});
        }
    }

    public void Pointer(InputAction.CallbackContext context)
    {
    }

    public void CloseBubble(InputAction.CallbackContext context)
    {
        if (_isCharacterControlsEnabled) // only when char controls disabled disabled
            return;
        if (context.started)
            GlobalEventAggregator.EventAggregator.Publish(new EventInputCloseBubble());
    }
    #endregion

    #region API and implemetations

    public void PlayHitVibration(float power)
    {
        if (power < 0.9f)
            return;
        if (Gamepad.current == null)
            return;

        const float duration = 0.3f;
        Gamepad.current.SetMotorSpeeds(0.2f, 0.3f);
        DOVirtual.DelayedCall(duration, () => Gamepad.current.SetMotorSpeeds(0.0f, 0.0f));
    }

    public void EnablePlayerInputs(bool flag)
    {
        _isCharacterControlsEnabled = flag;
    }

    public void DisableActionMap()
    {
        PlayerInput.currentActionMap = null;
    }

    public void SwitchCurrentActionMap(string actionMapName)
    {
        PlayerInput.SwitchCurrentActionMap(actionMapName);
    }

    private int AimingVectorLength01ToLevel(float aiming01)
    {
        var res = 0;
        if (aiming01 < AimingLevel0)
        {
            res = 0;
            AimingDirection = Vector2.zero;
        }
        else if (aiming01 < AimingLevel1)
        {
            res = 1;
        }
        else if (aiming01 < AimingLevel2)
        {
            res = 2;
        }
        else if (aiming01 <= AimingLevel3)
        {
            res = 3;
        }
        return res;
    }
    #endregion







}

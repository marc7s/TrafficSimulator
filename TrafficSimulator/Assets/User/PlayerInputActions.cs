//------------------------------------------------------------------------------
// <auto-generated>
//     This code was auto-generated by com.unity.inputsystem:InputActionCodeGenerator
//     version 1.4.4
//     from Assets/User/PlayerInputActions.inputactions
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public partial class @PlayerInputActions : IInputActionCollection2, IDisposable
{
    public InputActionAsset asset { get; }
    public @PlayerInputActions()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""PlayerInputActions"",
    ""maps"": [
        {
            ""name"": ""Default"",
            ""id"": ""f13f64ef-05b6-4db9-9d32-a3a5599bcb13"",
            ""actions"": [
                {
                    ""name"": ""Move"",
                    ""type"": ""PassThrough"",
                    ""id"": ""bafa80d8-7147-46e9-972d-591e4ce839be"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""Rotate"",
                    ""type"": ""Value"",
                    ""id"": ""5a13b146-b221-442f-92da-820883220304"",
                    ""expectedControlType"": ""Axis"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""Point"",
                    ""type"": ""PassThrough"",
                    ""id"": ""e478f451-29d0-4519-8825-988835ad494d"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Zoom"",
                    ""type"": ""PassThrough"",
                    ""id"": ""7462e2e6-7754-4979-93a3-d32397374524"",
                    ""expectedControlType"": ""Axis"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""Click"",
                    ""type"": ""Button"",
                    ""id"": ""9e2e6297-8951-44d6-a212-a0b15ca19344"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""DebugButtonPressed"",
                    ""type"": ""Button"",
                    ""id"": ""b33ba533-3dbf-4ee6-80d6-496b40e47d62"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""DoubleClick"",
                    ""type"": ""Button"",
                    ""id"": ""18303f8e-2abb-4f18-80a7-d5d440aaab6e"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""MultiTap"",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Escape"",
                    ""type"": ""Button"",
                    ""id"": ""3b64df3f-5571-49e2-824f-63d4129796e3"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Space"",
                    ""type"": ""Button"",
                    ""id"": ""01b58cd3-6d2c-484e-9760-c8d336f0c331"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""MouseLookDirection"",
                    ""type"": ""PassThrough"",
                    ""id"": ""516fedfe-2107-42e3-97e9-b5bf29627b78"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                }
            ],
            ""bindings"": [
                {
                    ""name"": ""2D Vector"",
                    ""id"": ""65eb94cb-fef0-49ec-bbe2-0886913df82e"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""b889c2c0-65d0-4971-9eb3-8959c255df7c"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""bb0d79b2-2303-471d-91a3-914e6ce80fb1"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""14508c4d-f604-48ef-b940-414918f19434"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""dedb97df-3554-4468-82a1-f9c8bde77532"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""1D Axis"",
                    ""id"": ""ba3544ca-6cc5-4527-8be8-252aca2a216c"",
                    ""path"": ""1DAxis"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Rotate"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""negative"",
                    ""id"": ""95c4a0af-9672-4f78-9ea7-23bec748d7f4"",
                    ""path"": ""<Keyboard>/e"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Rotate"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""positive"",
                    ""id"": ""735a3008-2aee-46f3-8e92-e68cb9a74e71"",
                    ""path"": ""<Keyboard>/q"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Rotate"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""aea4a76d-49e9-4ba5-a583-9fa2117b4346"",
                    ""path"": ""<Mouse>/position"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Point"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""3af58f63-96f6-402e-9b61-e6a51c592048"",
                    ""path"": ""<Mouse>/scroll/y"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Zoom"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""d2f9f05d-406c-4666-87c0-f37d57d98ea0"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Click"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""1bfa7c39-1491-4e2c-b28c-19f926015c7e"",
                    ""path"": ""<Keyboard>/p"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""DebugButtonPressed"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""48fb0d6a-4c45-4975-95cd-c302891db2dc"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""DoubleClick"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""c48c7553-a8e1-4e1b-9f98-ed03c70f85c7"",
                    ""path"": ""<Keyboard>/escape"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Escape"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""03d273e8-9b1f-4a07-8e8f-f73c4a937fa1"",
                    ""path"": ""<Keyboard>/space"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Space"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""bee60d7a-a285-4a90-81af-305676ee4b19"",
                    ""path"": ""<Mouse>/delta"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MouseLookDirection"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""Car"",
            ""id"": ""f7f02c33-ef5d-42f2-8780-20a68fd5147b"",
            ""actions"": [
                {
                    ""name"": ""Acceleration"",
                    ""type"": ""PassThrough"",
                    ""id"": ""bd59c085-8a32-4c39-9ae0-2657db6827bf"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Braking"",
                    ""type"": ""PassThrough"",
                    ""id"": ""76364167-b9dc-41b9-a51c-697bc7c6c29a"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Steering"",
                    ""type"": ""PassThrough"",
                    ""id"": ""78c48eeb-a1f6-4f77-a5ca-4336263d5e02"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                }
            ],
            ""bindings"": [
                {
                    ""name"": ""1D Axis"",
                    ""id"": ""e5203caa-9944-4f63-b24b-165367320262"",
                    ""path"": ""1DAxis"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Acceleration"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""negative"",
                    ""id"": ""d54e8152-7b7a-4794-b7e4-a7c6b8c7e4ee"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Acceleration"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""positive"",
                    ""id"": ""fbde256d-af10-42a4-80c7-4e7c816d9c77"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Acceleration"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""8a842d97-c2f6-4191-8e3e-c36186c51fc7"",
                    ""path"": ""<Keyboard>/space"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Braking"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""1D Axis"",
                    ""id"": ""827c9094-8d2b-4493-bea7-93f8120a9f06"",
                    ""path"": ""1DAxis"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Steering"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""negative"",
                    ""id"": ""f290cc05-cc11-4b51-a7da-aa8950578ff5"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Steering"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""positive"",
                    ""id"": ""9bfebb10-5049-4b93-999f-9d42fa686147"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Steering"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");
        // Default
        m_Default = asset.FindActionMap("Default", throwIfNotFound: true);
        m_Default_Move = m_Default.FindAction("Move", throwIfNotFound: true);
        m_Default_Rotate = m_Default.FindAction("Rotate", throwIfNotFound: true);
        m_Default_Point = m_Default.FindAction("Point", throwIfNotFound: true);
        m_Default_Zoom = m_Default.FindAction("Zoom", throwIfNotFound: true);
        m_Default_Click = m_Default.FindAction("Click", throwIfNotFound: true);
        m_Default_DebugButtonPressed = m_Default.FindAction("DebugButtonPressed", throwIfNotFound: true);
        m_Default_DoubleClick = m_Default.FindAction("DoubleClick", throwIfNotFound: true);
        m_Default_Escape = m_Default.FindAction("Escape", throwIfNotFound: true);
        m_Default_Space = m_Default.FindAction("Space", throwIfNotFound: true);
        m_Default_MouseLookDirection = m_Default.FindAction("MouseLookDirection", throwIfNotFound: true);
        // Car
        m_Car = asset.FindActionMap("Car", throwIfNotFound: true);
        m_Car_Acceleration = m_Car.FindAction("Acceleration", throwIfNotFound: true);
        m_Car_Braking = m_Car.FindAction("Braking", throwIfNotFound: true);
        m_Car_Steering = m_Car.FindAction("Steering", throwIfNotFound: true);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }
    public IEnumerable<InputBinding> bindings => asset.bindings;

    public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
    {
        return asset.FindAction(actionNameOrId, throwIfNotFound);
    }
    public int FindBinding(InputBinding bindingMask, out InputAction action)
    {
        return asset.FindBinding(bindingMask, out action);
    }

    // Default
    private readonly InputActionMap m_Default;
    private IDefaultActions m_DefaultActionsCallbackInterface;
    private readonly InputAction m_Default_Move;
    private readonly InputAction m_Default_Rotate;
    private readonly InputAction m_Default_Point;
    private readonly InputAction m_Default_Zoom;
    private readonly InputAction m_Default_Click;
    private readonly InputAction m_Default_DebugButtonPressed;
    private readonly InputAction m_Default_DoubleClick;
    private readonly InputAction m_Default_Escape;
    private readonly InputAction m_Default_Space;
    private readonly InputAction m_Default_MouseLookDirection;
    public struct DefaultActions
    {
        private @PlayerInputActions m_Wrapper;
        public DefaultActions(@PlayerInputActions wrapper) { m_Wrapper = wrapper; }
        public InputAction @Move => m_Wrapper.m_Default_Move;
        public InputAction @Rotate => m_Wrapper.m_Default_Rotate;
        public InputAction @Point => m_Wrapper.m_Default_Point;
        public InputAction @Zoom => m_Wrapper.m_Default_Zoom;
        public InputAction @Click => m_Wrapper.m_Default_Click;
        public InputAction @DebugButtonPressed => m_Wrapper.m_Default_DebugButtonPressed;
        public InputAction @DoubleClick => m_Wrapper.m_Default_DoubleClick;
        public InputAction @Escape => m_Wrapper.m_Default_Escape;
        public InputAction @Space => m_Wrapper.m_Default_Space;
        public InputAction @MouseLookDirection => m_Wrapper.m_Default_MouseLookDirection;
        public InputActionMap Get() { return m_Wrapper.m_Default; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(DefaultActions set) { return set.Get(); }
        public void SetCallbacks(IDefaultActions instance)
        {
            if (m_Wrapper.m_DefaultActionsCallbackInterface != null)
            {
                @Move.started -= m_Wrapper.m_DefaultActionsCallbackInterface.OnMove;
                @Move.performed -= m_Wrapper.m_DefaultActionsCallbackInterface.OnMove;
                @Move.canceled -= m_Wrapper.m_DefaultActionsCallbackInterface.OnMove;
                @Rotate.started -= m_Wrapper.m_DefaultActionsCallbackInterface.OnRotate;
                @Rotate.performed -= m_Wrapper.m_DefaultActionsCallbackInterface.OnRotate;
                @Rotate.canceled -= m_Wrapper.m_DefaultActionsCallbackInterface.OnRotate;
                @Point.started -= m_Wrapper.m_DefaultActionsCallbackInterface.OnPoint;
                @Point.performed -= m_Wrapper.m_DefaultActionsCallbackInterface.OnPoint;
                @Point.canceled -= m_Wrapper.m_DefaultActionsCallbackInterface.OnPoint;
                @Zoom.started -= m_Wrapper.m_DefaultActionsCallbackInterface.OnZoom;
                @Zoom.performed -= m_Wrapper.m_DefaultActionsCallbackInterface.OnZoom;
                @Zoom.canceled -= m_Wrapper.m_DefaultActionsCallbackInterface.OnZoom;
                @Click.started -= m_Wrapper.m_DefaultActionsCallbackInterface.OnClick;
                @Click.performed -= m_Wrapper.m_DefaultActionsCallbackInterface.OnClick;
                @Click.canceled -= m_Wrapper.m_DefaultActionsCallbackInterface.OnClick;
                @DebugButtonPressed.started -= m_Wrapper.m_DefaultActionsCallbackInterface.OnDebugButtonPressed;
                @DebugButtonPressed.performed -= m_Wrapper.m_DefaultActionsCallbackInterface.OnDebugButtonPressed;
                @DebugButtonPressed.canceled -= m_Wrapper.m_DefaultActionsCallbackInterface.OnDebugButtonPressed;
                @DoubleClick.started -= m_Wrapper.m_DefaultActionsCallbackInterface.OnDoubleClick;
                @DoubleClick.performed -= m_Wrapper.m_DefaultActionsCallbackInterface.OnDoubleClick;
                @DoubleClick.canceled -= m_Wrapper.m_DefaultActionsCallbackInterface.OnDoubleClick;
                @Escape.started -= m_Wrapper.m_DefaultActionsCallbackInterface.OnEscape;
                @Escape.performed -= m_Wrapper.m_DefaultActionsCallbackInterface.OnEscape;
                @Escape.canceled -= m_Wrapper.m_DefaultActionsCallbackInterface.OnEscape;
                @Space.started -= m_Wrapper.m_DefaultActionsCallbackInterface.OnSpace;
                @Space.performed -= m_Wrapper.m_DefaultActionsCallbackInterface.OnSpace;
                @Space.canceled -= m_Wrapper.m_DefaultActionsCallbackInterface.OnSpace;
                @MouseLookDirection.started -= m_Wrapper.m_DefaultActionsCallbackInterface.OnMouseLookDirection;
                @MouseLookDirection.performed -= m_Wrapper.m_DefaultActionsCallbackInterface.OnMouseLookDirection;
                @MouseLookDirection.canceled -= m_Wrapper.m_DefaultActionsCallbackInterface.OnMouseLookDirection;
            }
            m_Wrapper.m_DefaultActionsCallbackInterface = instance;
            if (instance != null)
            {
                @Move.started += instance.OnMove;
                @Move.performed += instance.OnMove;
                @Move.canceled += instance.OnMove;
                @Rotate.started += instance.OnRotate;
                @Rotate.performed += instance.OnRotate;
                @Rotate.canceled += instance.OnRotate;
                @Point.started += instance.OnPoint;
                @Point.performed += instance.OnPoint;
                @Point.canceled += instance.OnPoint;
                @Zoom.started += instance.OnZoom;
                @Zoom.performed += instance.OnZoom;
                @Zoom.canceled += instance.OnZoom;
                @Click.started += instance.OnClick;
                @Click.performed += instance.OnClick;
                @Click.canceled += instance.OnClick;
                @DebugButtonPressed.started += instance.OnDebugButtonPressed;
                @DebugButtonPressed.performed += instance.OnDebugButtonPressed;
                @DebugButtonPressed.canceled += instance.OnDebugButtonPressed;
                @DoubleClick.started += instance.OnDoubleClick;
                @DoubleClick.performed += instance.OnDoubleClick;
                @DoubleClick.canceled += instance.OnDoubleClick;
                @Escape.started += instance.OnEscape;
                @Escape.performed += instance.OnEscape;
                @Escape.canceled += instance.OnEscape;
                @Space.started += instance.OnSpace;
                @Space.performed += instance.OnSpace;
                @Space.canceled += instance.OnSpace;
                @MouseLookDirection.started += instance.OnMouseLookDirection;
                @MouseLookDirection.performed += instance.OnMouseLookDirection;
                @MouseLookDirection.canceled += instance.OnMouseLookDirection;
            }
        }
    }
    public DefaultActions @Default => new DefaultActions(this);

    // Car
    private readonly InputActionMap m_Car;
    private ICarActions m_CarActionsCallbackInterface;
    private readonly InputAction m_Car_Acceleration;
    private readonly InputAction m_Car_Braking;
    private readonly InputAction m_Car_Steering;
    public struct CarActions
    {
        private @PlayerInputActions m_Wrapper;
        public CarActions(@PlayerInputActions wrapper) { m_Wrapper = wrapper; }
        public InputAction @Acceleration => m_Wrapper.m_Car_Acceleration;
        public InputAction @Braking => m_Wrapper.m_Car_Braking;
        public InputAction @Steering => m_Wrapper.m_Car_Steering;
        public InputActionMap Get() { return m_Wrapper.m_Car; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(CarActions set) { return set.Get(); }
        public void SetCallbacks(ICarActions instance)
        {
            if (m_Wrapper.m_CarActionsCallbackInterface != null)
            {
                @Acceleration.started -= m_Wrapper.m_CarActionsCallbackInterface.OnAcceleration;
                @Acceleration.performed -= m_Wrapper.m_CarActionsCallbackInterface.OnAcceleration;
                @Acceleration.canceled -= m_Wrapper.m_CarActionsCallbackInterface.OnAcceleration;
                @Braking.started -= m_Wrapper.m_CarActionsCallbackInterface.OnBraking;
                @Braking.performed -= m_Wrapper.m_CarActionsCallbackInterface.OnBraking;
                @Braking.canceled -= m_Wrapper.m_CarActionsCallbackInterface.OnBraking;
                @Steering.started -= m_Wrapper.m_CarActionsCallbackInterface.OnSteering;
                @Steering.performed -= m_Wrapper.m_CarActionsCallbackInterface.OnSteering;
                @Steering.canceled -= m_Wrapper.m_CarActionsCallbackInterface.OnSteering;
            }
            m_Wrapper.m_CarActionsCallbackInterface = instance;
            if (instance != null)
            {
                @Acceleration.started += instance.OnAcceleration;
                @Acceleration.performed += instance.OnAcceleration;
                @Acceleration.canceled += instance.OnAcceleration;
                @Braking.started += instance.OnBraking;
                @Braking.performed += instance.OnBraking;
                @Braking.canceled += instance.OnBraking;
                @Steering.started += instance.OnSteering;
                @Steering.performed += instance.OnSteering;
                @Steering.canceled += instance.OnSteering;
            }
        }
    }
    public CarActions @Car => new CarActions(this);
    public interface IDefaultActions
    {
        void OnMove(InputAction.CallbackContext context);
        void OnRotate(InputAction.CallbackContext context);
        void OnPoint(InputAction.CallbackContext context);
        void OnZoom(InputAction.CallbackContext context);
        void OnClick(InputAction.CallbackContext context);
        void OnDebugButtonPressed(InputAction.CallbackContext context);
        void OnDoubleClick(InputAction.CallbackContext context);
        void OnEscape(InputAction.CallbackContext context);
        void OnSpace(InputAction.CallbackContext context);
        void OnMouseLookDirection(InputAction.CallbackContext context);
    }
    public interface ICarActions
    {
        void OnAcceleration(InputAction.CallbackContext context);
        void OnBraking(InputAction.CallbackContext context);
        void OnSteering(InputAction.CallbackContext context);
    }
}

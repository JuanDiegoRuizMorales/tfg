using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nekalypse.Manager
{
    public class InputManager : MonoBehaviour
    {
        [SerializeField] private PlayerInput playerInput;

        public Vector2 move { get; private set; }
        public Vector2 look { get; private set; }
        public bool run { get; private set; }
        public bool jump { get; private set; }

        public bool shoot { get; private set; }
        public bool shootPressed { get; private set; }

        public bool interact { get; private set; }
        public bool dashPressed { get; private set; }
        public bool reloadPressed { get; private set; }

        public bool switchWeapon1 { get; private set; }
        public bool switchWeapon2 { get; private set; }


        private InputActionMap _currentMap;
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _runAction;
        private InputAction _jumpAction;
        private InputAction _shootAction;
        private InputAction _interactAction;
        private InputAction _dashAction;
        private InputAction _reloadAction;

        private InputAction _switchWeapon1Action;
        private InputAction _switchWeapon2Action;


        private void Awake()
        {
            if (playerInput == null)
                playerInput = GetComponent<PlayerInput>();

            HideCursor();
            InitializeActionsIfNeeded();
        }


        private void OnEnable()
        {
            InitializeActionsIfNeeded();
            _currentMap?.Enable();
        }


        private void Start()
        {
            InitializeActionsIfNeeded();
        }


        private void OnDisable()
        {
            _currentMap?.Disable();
        }


        private void InitializeActionsIfNeeded()
        {
            if (_currentMap != null) return;

            if (playerInput == null)
                playerInput = GetComponent<PlayerInput>();

            if (playerInput == null) return;

            _currentMap = playerInput.currentActionMap;
            if (_currentMap == null) return;

            _moveAction = _currentMap.FindAction("Move");
            _lookAction = _currentMap.FindAction("Look");
            _runAction = _currentMap.FindAction("Run");
            _jumpAction = _currentMap.FindAction("Jump");
            _shootAction = _currentMap.FindAction("Shoot");
            _interactAction = _currentMap.FindAction("Interact");
            _dashAction = _currentMap.FindAction("Dash");
            _reloadAction = _currentMap.FindAction("Reload");

            // NUEVO: Switch weapons
            _switchWeapon1Action = _currentMap.FindAction("SwitchWeapon1");
            _switchWeapon2Action = _currentMap.FindAction("SwitchWeapon2");

            if (_moveAction != null)
            {
                _moveAction.performed += onMove;
                _moveAction.canceled += onMove;
            }

            if (_lookAction != null)
            {
                _lookAction.performed += onLook;
                _lookAction.canceled += onLook;
            }

            if (_runAction != null)
            {
                _runAction.performed += onRun;
                _runAction.canceled += onRun;
            }

            if (_jumpAction != null)
            {
                _jumpAction.performed += onJump;
                _jumpAction.canceled += onJump;
            }

            if (_shootAction != null)
            {
                _shootAction.performed += onShoot;
                _shootAction.canceled += onShoot;
            }

            if (_interactAction != null)
            {
                _interactAction.performed += onInteract;
                _interactAction.canceled += onInteract;
            }
        }


        private void Update()
        {
            dashPressed = _dashAction != null && _dashAction.WasPressedThisFrame();
            reloadPressed = _reloadAction != null && _reloadAction.WasPressedThisFrame();
            shootPressed = _shootAction != null && _shootAction.WasPressedThisFrame();

            switchWeapon1 = _switchWeapon1Action != null && _switchWeapon1Action.WasPressedThisFrame();
            switchWeapon2 = _switchWeapon2Action != null && _switchWeapon2Action.WasPressedThisFrame();
        }


        private void HideCursor()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }


        private void onMove(InputAction.CallbackContext context) => move = context.ReadValue<Vector2>();
        private void onLook(InputAction.CallbackContext context) => look = context.ReadValue<Vector2>();
        private void onRun(InputAction.CallbackContext context) => run = context.ReadValueAsButton();
        private void onJump(InputAction.CallbackContext context) => jump = context.ReadValueAsButton();
        private void onShoot(InputAction.CallbackContext context) => shoot = context.ReadValueAsButton();
        private void onInteract(InputAction.CallbackContext context) => interact = context.ReadValueAsButton();
    }
}

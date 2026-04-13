using Nekalypse.Manager;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Nekalypse.PlayerControl
{
    public class PlayerController : MonoBehaviour
    {
        //Blend de animaciones del animator
        [SerializeField] private float _animationBlendSpeed = 4f;

        //Referencias del sistema de camara
        [SerializeField] private Transform _cameraRoot;
        [SerializeField] private Transform _camera;

        //Limites verticales de la camara
        [SerializeField] private float _upperLimit = -40f;
        [SerializeField] private float _bottomLimit = 70f;

        //sensibilidad del raton para la camara
        [Tooltip("Sensibilidad del raton")]
        [SerializeField] private float _mouseSensitivity;

        //Fuerza de salto en impulso
        [SerializeField] private float _jumpFactor = 260f;

        //Distancia minima al suelo para considerarse grounded
        [SerializeField] private float _distanceToGround = 0.8f;

        //Mascara de capas que se consideraran como suelo
        [SerializeField] private LayerMask _groundCheck;

        //Controla la reduccion de control en el aire
        [SerializeField] private float _airResistance = 0.8f;



        [Header("Stamina Settings")]
        [SerializeField] private float _maxStamina = 5f; // Cantidad maxima de stamina
        [SerializeField] private float _staminaConsumptionRate = 1f; // Consumo por segundo al sprintar
        [SerializeField] private float _staminaRegenRate = 0.8f; // Regeneracion por segundo
        [SerializeField] private float _staminaCooldownTime = 2f; // Penalizacion cuando se queda sin stamina
        [SerializeField] private float _currentStamina; // Stamina actual
        [SerializeField] private bool _staminaOnCooldown = false; // Si esta o no bloqueada la regeneracion
        [SerializeField] private float _staminaCooldownTimer = 0f; // Timer interno del cooldown

        // Cuenta las colisiones contra el suelo en triggers
        private int _groundContactCount = 0;

        // Componentes principales del jugador
        private Rigidbody _rb;
        private InputManager _inputManager;
        private Animator _animator;
        private bool _hasAnimator;

        public bool isGrounded;

        // Hash para animaciones
        private int _xVelHash;
        private int _yVelHash;
        private float _xRotation;
        private int _jumpHash;
        private int _groundHash;
        private int _fallingHash;

        //Velocidades basicas
        public float walkSpeed = 2f;
        public float runSpeed = 6f;

        // Multiplicador de gravedad para intensificar el peso
        [SerializeField] private float _gravityMultiplier = 2f;


        [Header("Salto")]
        [SerializeField] private float _postGroundedJumpDelay = 0.15f; // Tiempo minimo tras tocar suelo para poder saltar
        private float _groundedTimer = 0f;
        private bool _jumpReleasedSinceLast = true; // Evita saltos mantenidos

        // Variables relacionadas con la velocidad real y animaciones
        private Vector2 _currentVelocity;
        private Vector3 _localVelocity;
        private Vector3 _lastPosition;
        private Vector3 _realVelocity;

        // Aceleracion horizontal maxima por FixedUpdate
        [SerializeField] private float _maxHorizontalAccelPerFixed = 12f;



        [Header("Dash Settings")]
        [SerializeField] private float dashForce = 22f; //Fuerza del dash
        [SerializeField] private float dashDuration = 0.12f; //Tiempo impulsado
        [SerializeField] private float dashCooldown = 0.3f;//Cooldown mnimo
        [SerializeField] private float dashFOV = 115f; //FOV aumentado
        [SerializeField] private float dashFOVReturnSpeed = 8f;// Velocidad de retorno del FOV

        private bool isDashing = false;//Si esta dasheando
        private float dashCooldownTimer = 0f; //Timer de cooldown

        private Camera _playerCamera;// Camara del jugador
        private float baseFOV;//FOV original



        [Header("PlayerHealth Reference")]
        [SerializeField] private PlayerHealth playerHealth;



        [Header("Motion Blur")]
        [SerializeField] private Volume motionBlurVolume; //Volume de post-procesado
        private MotionBlur motionBlur;// MotionBlur dentro del profile
        [SerializeField] private float dashBlurIntensity = 0.65f; // Intensidad al hacer dash
        [SerializeField] private float dashBlurReturnSpeed = 3f;//Velocidad de desaparicion del blur

        [Header("Footstep Audio")]
        [SerializeField] private AudioClip footstepSFX;
        [SerializeField] private float stepDistance = 2f; // metros entre pasos
        private float stepDistanceCounter = 0f;




        void Start()
        {
            // Obtiene componentes principales
            _hasAnimator = TryGetComponent(out _animator);
            _rb = GetComponent<Rigidbody>();
            _inputManager = GetComponent<InputManager>();

            //Asigna hash de animaciones
            _xVelHash = Animator.StringToHash("X_Velocity");
            _yVelHash = Animator.StringToHash("Y_Velocity");
            _jumpHash = Animator.StringToHash("Jump");
            _groundHash = Animator.StringToHash("Grounded");
            _fallingHash = Animator.StringToHash("Falling");

            //Inicia stamina al maximo
            _currentStamina = _maxStamina;

            // Inicializa posicion previa para calculo de velocidad real
            _lastPosition = transform.position;

            // Suaviza fisicas
            if (_rb != null)
                _rb.interpolation = RigidbodyInterpolation.Interpolate;

            // Configura camara y guarda FOV original
            _playerCamera = _camera.GetComponent<Camera>();
            baseFOV = _playerCamera.fieldOfView;

            //Si no se asigna PlayerHealth por inspector, lo busca
            if (playerHealth == null)
                playerHealth = GetComponent<PlayerHealth>();


            //Obtiene Motion Blur, ya sea por volume asignado o encontrando uno en escena
            if (motionBlurVolume == null)
            {
                Volume vol = FindAnyObjectByType<Volume>();
                if (vol != null)
                    vol.profile.TryGet(out motionBlur);
            }
            else
            {
                motionBlurVolume.profile.TryGet(out motionBlur);
            }
        }

        private void FixedUpdate()
        {
            //Resta tiempo al cooldown del dash
            dashCooldownTimer -= Time.fixedDeltaTime;

            //Si no esta dasheando, ejecuta la logica normal de movimiento
            if (!isDashing)
            {
                UpdateStamina(Time.fixedDeltaTime);
                Move();

                //Si esta en el aire aplica gravedad intensificada
                if (!isGrounded)
                    _rb.AddForce(Physics.gravity * _gravityMultiplier, ForceMode.Acceleration);

                //Gestiona salto
                HandleJump();

                //Comprueba si esta tocando suelo
                SampleGround();
            }

            //Calcula velocidad real
            _realVelocity = (transform.position - _lastPosition) / Time.fixedDeltaTime;
            _lastPosition = transform.position;

            //Controla delay tras caer al suelo
            if (_groundedTimer > 0f)
                _groundedTimer -= Time.fixedDeltaTime;

            //Cuando suelta el boton de salto se puede volver a saltar
            if (!_inputManager.jump)
                _jumpReleasedSinceLast = true;
        }

        private void LateUpdate()
        {
            //Movimiento de camara
            CamMovements();

            //Ajuste del FOV animado del dash
            UpdateDashFOV();

            //Comprueba si se pulsa el dash
            if (_inputManager.dashPressed)
            TryDash();

            HandleFootsteps();

        }

        private void HandleFootsteps()
        {
            if (!isGrounded) return;                    // No sonar en el aire
            if (_inputManager.move == Vector2.zero) return; // No sonar si no hay input
            if (_realVelocity.magnitude < 1f) return;   // No sonar si casi no se mueve

            // Acumulamos distancia real recorrida
            stepDistanceCounter += _realVelocity.magnitude * Time.deltaTime;

            if (stepDistanceCounter >= stepDistance)
            {
                stepDistanceCounter = 0f;
                AudioManager.Instance.PlayFootstep(footstepSFX);
            }
        }


        private void TryDash()
        {
            //Requisitos para poder dashear
            if (isDashing) return;
            if (!isGrounded) return;
            if (dashCooldownTimer > 0f) return;

            StartCoroutine(DashRoutine());
        }

        private IEnumerator DashRoutine()
        {
            //Marca estado de dash
            isDashing = true;
            dashCooldownTimer = dashCooldown;

            //Calcula direccion del dash segun movimiento actual
            Vector3 moveDirection = new Vector3(_inputManager.move.x, 0f, _inputManager.move.y);

            Vector3 dashDirection =
                moveDirection.sqrMagnitude > 0.1f
                ? transform.TransformDirection(moveDirection.normalized)
                : transform.forward;

            //Activa invulnerabilidad temporal
            if (playerHealth != null)
                playerHealth.isInvulnerable = true;

            float timer = 0f;

            //Mientras dure el dash, aplica fuerza constante
            while (timer < dashDuration)
            {
                timer += Time.fixedDeltaTime;
                _rb.linearVelocity = dashDirection * dashForce;
                yield return new WaitForFixedUpdate();
            }

            //Fin del dash
            isDashing = false;

            //Desactiva invulnerabilidad
            if (playerHealth != null)
                playerHealth.isInvulnerable = false;
        }

        private void UpdateDashFOV()
        {
            //El FOV aumenta durante el dash y luego vuelve gradualmente al original
            float target = isDashing ? dashFOV : baseFOV;

            _playerCamera.fieldOfView =
                Mathf.Lerp(_playerCamera.fieldOfView, target, Time.deltaTime * dashFOVReturnSpeed);

            //Misma idea para el motion blur, basado en si esta dasheando o no
            if (motionBlur != null)
            {
                float targetBlur = isDashing ? dashBlurIntensity : 0f;

                motionBlur.intensity.value =
                    Mathf.Lerp(motionBlur.intensity.value, targetBlur, Time.deltaTime * dashBlurReturnSpeed);
            }
        }



        private void Move()
        {
            //Si falta cualquier componente esencial, no se mueve
            if (!_hasAnimator || _rb == null || _inputManager == null) return;

            //Determina si puede correr
            bool canRun = _inputManager.run && !_staminaOnCooldown && _currentStamina > 0f;
            float targetSpeed = canRun ? runSpeed : walkSpeed;

            //Sin input de movimiento no avanza
            if (_inputManager.move == Vector2.zero) targetSpeed = 0f;

            //Movimiento deseado en espacio local
            Vector3 desiredLocal = new Vector3(_inputManager.move.x * targetSpeed, 0f, _inputManager.move.y * targetSpeed);
            Vector3 desiredWorld = transform.TransformVector(desiredLocal);

            //Velocidades actuales
            Vector3 currentVel = _rb.linearVelocity;
            Vector3 currentHorizontal = new Vector3(currentVel.x, 0f, currentVel.z);

            //Capado de aceleracion por fixed update
            float maxDelta = _maxHorizontalAccelPerFixed * Time.fixedDeltaTime;

            if (isGrounded)
            {
                if (_inputManager.move == Vector2.zero)
                {
                    //Freno total en horizontal y compensacion de pendientes
                    if (Physics.Raycast(_rb.worldCenterOfMass, Vector3.down, out RaycastHit hit, _distanceToGround + 0.5f, _groundCheck))
                    {
                        Vector3 slopeNormal = hit.normal;
                        Vector3 gravityDirection = Vector3.ProjectOnPlane(Physics.gravity, slopeNormal);
                        _rb.AddForce(-gravityDirection, ForceMode.Acceleration);
                    }

                    _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
                }
                else
                {
                    //Movimiento en suelo con aceleracion controlada
                    Vector3 newHorizontal = Vector3.MoveTowards(currentHorizontal, desiredWorld, maxDelta);
                    _rb.linearVelocity = new Vector3(newHorizontal.x, _rb.linearVelocity.y, newHorizontal.z);
                }
            }
            else
            {
                //Movimiento reducido en el aire
                Vector3 airDesired = desiredWorld * _airResistance;
                Vector3 newHorizontal = Vector3.MoveTowards(currentHorizontal, airDesired, maxDelta * _airResistance);
                _rb.linearVelocity = new Vector3(newHorizontal.x, _rb.linearVelocity.y, newHorizontal.z);
            }

            //Velocidad local para animaciones
            _localVelocity = transform.InverseTransformDirection(_realVelocity);
            float horizontalSpeed = new Vector3(_realVelocity.x, 0f, _realVelocity.z).magnitude;
            bool isActuallyMoving = horizontalSpeed > 0.05f;

            //Actualiza blend tree de locomocion
            if (isActuallyMoving)
            {
                _animator.SetFloat(_xVelHash, _localVelocity.x, _animationBlendSpeed, Time.deltaTime);
                _animator.SetFloat(_yVelHash, _localVelocity.z, _animationBlendSpeed, Time.deltaTime);
            }
            else
            {
                _animator.SetFloat(_xVelHash, 0f, _animationBlendSpeed, Time.deltaTime);
                _animator.SetFloat(_yVelHash, 0f, _animationBlendSpeed, Time.deltaTime);
            }
        }


        private void CamMovements()
        {
            if (!_hasAnimator) return;

            var Mouse_X = _inputManager.look.x;
            var Mouse_Y = _inputManager.look.y;

            //La camara sigue el punto raiz
            _camera.position = _cameraRoot.position;

            //Inclinacion vertical
            _xRotation -= Mouse_Y * _mouseSensitivity * Time.smoothDeltaTime;
            _xRotation = Mathf.Clamp(_xRotation, _upperLimit, _bottomLimit);
            _camera.localRotation = Quaternion.Euler(_xRotation, 0, 0);

            //Rotacion horizontal del cuerpo
            _rb.MoveRotation(_rb.rotation * Quaternion.Euler(0, Mouse_X * _mouseSensitivity * Time.smoothDeltaTime, 0));
        }


        private void HandleJump()
        {
            if (!_hasAnimator) return;

            //Impide saltar si aun esta en delay
            if (_groundedTimer > 0f) return;

            //Obliga a soltar el boton antes de saltar otra vez
            if (!_jumpReleasedSinceLast) return;

            if (_inputManager.jump && isGrounded)
            {
                _animator.SetTrigger(_jumpHash);
                _jumpReleasedSinceLast = false;
            }
        }

        public void JumpAddForce()
        {
            //Cancela velocidad vertical previa y añade impulso
            _rb.AddForce(-_rb.linearVelocity.y * Vector3.up, ForceMode.VelocityChange);
            _rb.AddForce(Vector3.up * _jumpFactor, ForceMode.Impulse);
            _animator.ResetTrigger(_jumpHash);
        }

        public void SampleGround()
        {
            if (!_hasAnimator) return;

            bool wasGrounded = isGrounded;

            //Prefiere triggers de suelo si los hay
            if (_groundContactCount > 0)
            {
                isGrounded = true;
                SetAnimationGrounding();
            }
            else
            {
                //si no, usa raycast corto
                RaycastHit hitInfo;
                if (Physics.Raycast(_rb.worldCenterOfMass, Vector3.down, out hitInfo, _distanceToGround + 0.1f, _groundCheck))
                {
                    isGrounded = true;
                    SetAnimationGrounding();
                }
                else
                {
                    isGrounded = false;
                    SetAnimationGrounding();
                }
            }

            //Cuando toca suelo, aplica delay para permitir saltos consistentes
            if (isGrounded && !wasGrounded)
            {
                _animator.ResetTrigger(_jumpHash);
                _groundedTimer = _postGroundedJumpDelay;
            }
        }

        private void SetAnimationGrounding()
        {
            _animator.SetBool(_fallingHash, !isGrounded);
            _animator.SetBool(_groundHash, isGrounded);
        }


        private void OnTriggerEnter(Collider other)
        {
            if ((_groundCheck.value & (1 << other.gameObject.layer)) != 0)
            {
                _groundContactCount++;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if ((_groundCheck.value & (1 << other.gameObject.layer)) != 0)
            {
                _groundContactCount = Mathf.Max(0, _groundContactCount - 1);
            }
        }

        private void UpdateStamina(float deltaTime)
        {
            //si la stamina esta bloqueada por cooldown
            if (_staminaOnCooldown)
            {
                _staminaCooldownTimer -= deltaTime;
                if (_staminaCooldownTimer <= 0f)
                {
                    _staminaOnCooldown = false;
                    _staminaCooldownTimer = 0f;
                }

                //la stamina sigue regenerando lentamente incluso en cooldown
                _currentStamina = Mathf.Min(_maxStamina, _currentStamina + _staminaRegenRate * deltaTime);
                return;
            }

            //consumo activo si esta corriendo
            if (_inputManager != null && _inputManager.run && _currentStamina > 0f)
            {
                _currentStamina -= _staminaConsumptionRate * deltaTime;

                //si la stamina llega a 0, activa cooldown
                if (_currentStamina <= 0f)
                {
                    _currentStamina = 0f;
                    _staminaOnCooldown = true;
                    _staminaCooldownTimer = _staminaCooldownTime;
                }
            }
            else
            {
                //regeneracion normal
                _currentStamina = Mathf.Min(_maxStamina, _currentStamina + _staminaRegenRate * deltaTime);
            }
        }
    }
}

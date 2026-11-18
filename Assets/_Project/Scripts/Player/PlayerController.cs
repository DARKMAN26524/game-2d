using System;
using Scripts.Input;
using Scripts.States.Player;
using UnityEngine;


namespace Scripts.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerInputAdapter))]
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(PlayerJump))]
    [RequireComponent(typeof(PlayerAttack))]
    [RequireComponent(typeof(PlayerAnimation))]
    [RequireComponent(typeof(PlayerHealth))]
    public class PlayerController : MonoBehaviour
    {

        #region Player Events

       // public event Action<int> OnHealthChanged;

        #endregion

        #region Private Fields
        private PlayerInputAdapter _inputAdapter;
        private PlayerMovement _playerMovement;
        private PlayerJump _playerJump;
        private PlayerAttack _playerAttack;
        private PlayerAnimation _playerAnimation;
        private Rigidbody2D _playerRb;
        private PlayerHealth _playerHealth;
        private float _lookY;
        private bool _isCrouching;

        private const float SpeedYThreshold = 0.1f;
        private const float SpeedXThreshold = 0.01f;

        #endregion

        #region Unity Methods

        void Awake()
        {
            Init();
        }

        void OnEnable()
        {
            // safety check
            if (_inputAdapter == null)
            {
                return;
            }

            _inputAdapter.OnMoveX += _playerMovement.SetMove;
            _inputAdapter.OnMoveX += _playerMovement.UpdateFacingDirection;
            _inputAdapter.OnMoveX += _playerAttack.UpdatingFacingDirection;
            _inputAdapter.OnLookY += HandleLookDirection;
            _inputAdapter.OnJump += _playerJump.RequestJump;
            _inputAdapter.OnStartAttack += _playerAttack.StartAttacking;
            _inputAdapter.OnStopAttack += _playerAttack.StopAttacking;
        }

        void OnDisable()
        {
            // safety check
            if (_inputAdapter == null)
            {
                return;
            }

            _inputAdapter.OnMoveX -= _playerMovement.SetMove;
            _inputAdapter.OnMoveX -= _playerMovement.UpdateFacingDirection;
            _inputAdapter.OnMoveX -= _playerAttack.UpdatingFacingDirection;
            _inputAdapter.OnLookY -= HandleLookDirection;
            _inputAdapter.OnJump -= _playerJump.RequestJump;
            _inputAdapter.OnStartAttack -= _playerAttack.StartAttacking;
            _inputAdapter.OnStopAttack -= _playerAttack.StopAttacking;
        }

        void FixedUpdate()
        {
            // Centraliza a resolução de estado baseado nas condições atuais
            ResolveAndUpdatePlayerState();
            UpdateCrouching();
        }

        #endregion

        #region Script Methods

        void Init()
        {
            _inputAdapter = GetComponent<PlayerInputAdapter>();
            _playerMovement = GetComponent<PlayerMovement>();
            _playerJump = GetComponent<PlayerJump>();
            _playerAttack = GetComponent<PlayerAttack>();
            _playerAnimation = GetComponent<PlayerAnimation>();
            _playerRb = GetComponent<Rigidbody2D>();

            PlayerStates.CurrentState = PlayerStates.States.Idle;
        }

        private void ResolveAndUpdatePlayerState()
        {
            bool isAirborne = IsAirborne();
            bool isIdle = IsIdle();
            bool isAttacking = _playerAttack.IsAttacking;
            bool isAimingUpward = _playerAttack.IsAimingUpward;

            PlayerStates.States nextState;

            if (!isAirborne && isAttacking && isAimingUpward)
            {
                nextState = PlayerStates.States.Attack_Up;
            }
            // jumping / falling
            else if (isAirborne)
            {
                nextState = PlayerStates.States.Jump;
            }
            // idle / stand attack / attack up
            else if (isIdle)
            {
                if (isAttacking && isAimingUpward)
                {
                    nextState = PlayerStates.States.Attack_Up;
                }
                else
                {
                    nextState = isAttacking ? PlayerStates.States.Attack
                                            : PlayerStates.States.Idle;
                }
            }
            // running / run attack
            else
            {
                nextState = isAttacking ? PlayerStates.States.Run_Attack
                                        : PlayerStates.States.Run;
            }

            PlayerStates.CurrentState = nextState;
        }

        private bool IsAirborne()
        {
            return _playerRb.linearVelocityY > SpeedYThreshold ||
                   _playerRb.linearVelocityY < -SpeedYThreshold;
        }

        private bool IsIdle()
        {
            return Mathf.Abs(_playerRb.linearVelocityX) < SpeedXThreshold;
        }

        private void HandleLookDirection(float lookY)
        {
            _lookY = lookY;
            _playerAttack.UpdatingLookDirection(lookY);
        }

        private void UpdateCrouching()
        {
            bool crouch = _lookY < -0.5f && _playerJump.IsGrounded();
            if (_isCrouching == crouch)
            {
                return;
            }

            _isCrouching = crouch;
            _playerMovement.SetCrouching(crouch);
            _playerAttack.SetCrouching(crouch);
            _playerAnimation.SetCrouching(crouch);
        }

        #endregion
    }
}

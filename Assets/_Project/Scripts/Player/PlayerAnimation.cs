using UnityEngine;
using Scripts.States.Player;

public class PlayerAnimation : MonoBehaviour
{
    #region Private Fields
    private Animator _playerAnimator;
    private SpriteRenderer _playerSpriteRenderer;
    private Sprite _crouchSprite;
    private Sprite _standingSprite;
    private PlayerStates.States _currentState;
    private bool _isCrouching;
    private Vector3 _standingMeshPosition;

    #endregion

    #region Unity Methods

    void Awake()
    {
        _playerAnimator = GetComponentInChildren<Animator>();
        _playerSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _currentState = PlayerStates.CurrentState;
        if (_playerSpriteRenderer != null)
        {
            _standingMeshPosition = _playerSpriteRenderer.transform.localPosition;
            _standingSprite = _playerSpriteRenderer.sprite;
        }
    }

    private void FixedUpdate()
    {
        if (_isCrouching)
        {
            return;
        }

        // Observa mudanças de estado e sincroniza animações
        if (PlayerStates.CurrentState != _currentState)
        {
            _currentState = PlayerStates.CurrentState;
            PlayAnimation(_currentState);
        }
    }

    #endregion

    #region Script Methods

    public void SetCrouchSprite(Sprite crouchSprite)
    {
        _crouchSprite = crouchSprite;
    }

    public void SetCrouching(bool isCrouching)
    {
        if (_crouchSprite == null || _playerSpriteRenderer == null || _isCrouching == isCrouching)
        {
            return;
        }

        _isCrouching = isCrouching;
        if (_isCrouching)
        {
            _standingMeshPosition = _playerSpriteRenderer.transform.localPosition;
            _standingSprite = _playerSpriteRenderer.sprite;
            float standingBottom = _standingSprite != null ? _standingSprite.bounds.min.y : 0f;
            float crouchedBottom = _crouchSprite.bounds.min.y;
            float verticalScale = _playerSpriteRenderer.transform.localScale.y;
            _playerSpriteRenderer.sprite = _crouchSprite;
            _playerSpriteRenderer.transform.localPosition = _standingMeshPosition
                + Vector3.up * (standingBottom - crouchedBottom) * verticalScale;
            if (_playerAnimator != null)
            {
                _playerAnimator.enabled = false;
            }

            return;
        }

        _playerSpriteRenderer.transform.localPosition = _standingMeshPosition;
        if (_playerAnimator != null)
        {
            _playerAnimator.enabled = true;
            PlayAnimation(PlayerStates.CurrentState);
        }
    }

    private void PlayAnimation(PlayerStates.States state)
    {
        // safety check
        if (_playerAnimator == null)
        {
            Debug.LogWarning("Animator component is not assigned.");
            return;
        }

        _playerAnimator.Play(state.ToString());
    }

    #endregion
}

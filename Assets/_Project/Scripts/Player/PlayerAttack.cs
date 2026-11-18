using Scripts.Managers;
using UnityEngine;

namespace Scripts.Player
{
    [DisallowMultipleComponent]
    public class PlayerAttack : MonoBehaviour
    {
        private bool _isAttacking = false;
        [SerializeField] private float _attackDuration = 0.25f;
        private float _attackTimer;
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private Transform _gunBarrelRight;
        [SerializeField] private Transform _gunBarrelLeft;
        [SerializeField] private Transform _gunBarrelUpRight;
        [SerializeField] private Transform _gunBarrelUpLeft;
        private Transform _gunBarrel;
        private bool _isFacingRight = true;
        private bool _isAimingUpward = false;
        private bool _isCrouching;
        private float _lookY;
        private Vector2 _aimDirection = Vector2.right;
        private int _weaponUpgradeLevel;

        #region Public Properties
        public bool IsAttacking => _isAttacking;
        public bool IsAimingUpward => _isAimingUpward;
        #endregion

        #region Unity Methods
        void Start()
        {
            _gunBarrel = _gunBarrelRight;
        }

        void Update()
        {
            if (Time.timeScale == 0f)
            {
                return;
            }

            _attackTimer += Time.deltaTime;

            float attackDuration = Mathf.Max(0.1f, _attackDuration - _weaponUpgradeLevel * 0.04f);
            if (_isAttacking && _attackTimer >= attackDuration)
            {
                ShootProjectile();
                _attackTimer = 0f;
            }
        }

        #endregion

        #region Script Methods

        public void StartAttacking()
        {
            _isAttacking = true;
        }

        public void StopAttacking()
        {
            _isAttacking = false;
        }

        public void SetCrouching(bool isCrouching)
        {
            _isCrouching = isCrouching;
            UpdateAimDirection();
        }

        public void SetWeaponUpgrade(int level)
        {
            _weaponUpgradeLevel = Mathf.Clamp(level, 0, 3);
        }

        public void ResetWeaponUpgrade()
        {
            _weaponUpgradeLevel = 0;
        }

        public void ShootProjectile()
        {
            if (_projectilePrefab == null)
            {
                Debug.LogWarning("Projectile Prefab is not assigned in the inspector.");
                return;
            }

            if (_gunBarrel == null)
            {
                Debug.LogError("No gun barrel is assigned for the current aim direction.", this);
                return;
            }

            Vector3 spawnOffset = _isCrouching ? Vector3.down * 0.8f : (_aimDirection.y < 0f ? Vector3.down * 0.1f : Vector3.zero);
            GameObject projectile = Instantiate(_projectilePrefab, _gunBarrel.position + spawnOffset, _gunBarrel.rotation);
            Projectile projScript = projectile.GetComponent<Projectile>();
            if (projScript == null)
            {
                Debug.LogError("Projectile Prefab is missing its Projectile component.", _projectilePrefab);
                Destroy(projectile);
                return;
            }

            projScript.OnChangeDirection(_aimDirection);
            projScript.SetDamage(1 + _weaponUpgradeLevel);
            AudioManager.Instance.PlayAttackSFX();
        }



        public void UpdatingFacingDirection(float moveX)
        {

            if (moveX > 0f)
            {
                _isFacingRight = true;
            }
            else if (moveX < 0f)
            {
                _isFacingRight = false;
            }

            UpdateAimDirection();
        }

        public void UpdatingLookDirection(float lookY)
        {
            _lookY = lookY;
            UpdateAimDirection();
        }

        private void UpdateAimDirection()
        {
            float horizontal = _isFacingRight ? 1f : -1f;
            if (_lookY > 0.5f)
            {
                _isAimingUpward = true;
                _aimDirection = new Vector2(horizontal, 1f).normalized;
                _gunBarrel = _isFacingRight ? _gunBarrelUpRight : _gunBarrelUpLeft;
                return;
            }

            _isAimingUpward = false;
            if (_lookY < -0.5f && !_isCrouching)
            {
                _aimDirection = new Vector2(horizontal, -1f).normalized;
                _gunBarrel = _isFacingRight ? _gunBarrelRight : _gunBarrelLeft;
                return;
            }

            _aimDirection = new Vector2(horizontal, 0f);
            _gunBarrel = _isFacingRight ? _gunBarrelRight : _gunBarrelLeft;
        }
        

        #endregion
    }
}
using UnityEngine;
using Scripts.Audio;
using Scripts.Managers;

public class Projectile : MonoBehaviour
{

    private Rigidbody2D _projectileRb;
    [SerializeField] private float _speed = 10f;
    private Vector2 _direction = Vector2.right;
    private int _damage = 1;
    private bool _hasCollided = false;

    private Animator _projectileAnimator;

    void Awake()
    {
        _projectileRb = GetComponent<Rigidbody2D>();
        _projectileAnimator = GetComponent<Animator>();
    }


    void Update()
    {
        if (_projectileRb == null || _hasCollided)
        {
            return;
        }

        _projectileRb.linearVelocity = _direction * _speed;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasCollided)
        {
            return;
        }

        if (other.GetComponent<Projectile>() != null)
        {
            return;
        }

        CaveEnemy enemy = other.GetComponentInParent<CaveEnemy>();
        if (enemy != null && enemy.IsProjectile)
        {
            return;
        }

        CaveHazard hazard = other.GetComponentInParent<CaveHazard>();
        if (hazard != null && hazard.IsScoreCrystal)
        {
            return;
        }

        if (enemy != null)
        {
            enemy.TakeDamage(_damage);
        }

        _hasCollided = true;
        if (_projectileRb != null)
        {
            _projectileRb.linearVelocity = Vector2.zero;
        }

        if (_projectileAnimator != null)
        {
            _projectileAnimator.Play("Collision");
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBurstSFX();
        }

        Destroy(gameObject, 0.25f);
    }

    public void OnChangeDirection(Vector2 direction)
    {
        _direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        transform.right = _direction;
        if (_direction.y > 0f)
        {
            int upperPlatformLayer = LayerMask.NameToLayer("UpperPlatform");
            Collider2D projectileCollider = GetComponent<Collider2D>();
            if (upperPlatformLayer >= 0 && projectileCollider != null)
            {
                projectileCollider.excludeLayers |= 1 << upperPlatformLayer;
            }
        }
    }

    public void SetDamage(int damage)
    {
        _damage = Mathf.Max(1, damage);
    }

    void OnBecameInvisible()
    {
        Destroy(gameObject);
    }

}

using UnityEngine;

public class CaveEnemy : MonoBehaviour
{
    private CaveRunController _run;
    private Transform _player;
    private SpriteRenderer _renderer;
    private Sprite[] _animationFrames;
    private float _left;
    private float _right;
    private float _speed;
    private float _nextShot;
    private float _lifeTime;
    private float _animationTimer;
    private int _health;
    private int _maxHealth;
    private int _bossPhase = 1;
    private float _telegraphEndsAt;
    private bool _isBoss;
    private bool _isProjectile;
    private bool _isTelegraphing;
    private Vector2 _projectileDirection;
    private Rigidbody2D _projectileBody;

    public bool IsProjectile => _isProjectile;
    public bool IsBoss => _isBoss;
    public int Health => _health;
    public int MaxHealth => _maxHealth;
    public int BossPhase => _bossPhase;

    public void Configure(CaveRunController run, Transform player, float left, float right, int health, bool isBoss, Sprite[] animationFrames)
    {
        _run = run;
        _player = player;
        _left = left;
        _right = right;
        _health = health;
        _maxHealth = health;
        _isBoss = isBoss;
        _isProjectile = false;
        _isTelegraphing = false;
        _bossPhase = 1;
        _speed = isBoss ? 1.5f : 1.1f;
        _nextShot = Time.time + Random.Range(isBoss ? 1.5f : 0.8f, isBoss ? 2.2f : 1.8f);
        _renderer = GetComponent<SpriteRenderer>();
        _animationFrames = animationFrames;
        if (_renderer != null)
        {
            _renderer.color = Color.white;
        }

        if (_isBoss)
        {
            _run.RegisterBoss(this);
        }
    }

    public void ConfigureProjectile(CaveRunController run, Vector2 direction)
    {
        _run = run;
        _projectileDirection = direction.normalized;
        _isProjectile = true;
        _lifeTime = 5f;
        _projectileBody = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (_isProjectile)
        {
            return;
        }

        if (_run == null || _run.IsRunOver || _player == null)
        {
            return;
        }

        float direction = Mathf.PingPong(Time.time * _speed, _right - _left) + _left - transform.position.x;
        float move = Mathf.Clamp(direction, -_speed * Time.deltaTime, _speed * Time.deltaTime);
        transform.position += Vector3.right * move;
        _animationTimer += Time.deltaTime;
        if (_renderer != null && _animationFrames != null && _animationFrames.Length > 0 && _animationTimer >= 0.16f)
        {
            _animationTimer = 0f;
            int frame = Mathf.FloorToInt(Time.time * 7f) % _animationFrames.Length;
            _renderer.sprite = _animationFrames[frame];
            _renderer.flipX = _player.position.x < transform.position.x;
        }

        if (_isBoss)
        {
            UpdateBossAttack();
            return;
        }

        if (Mathf.Abs(_player.position.x - transform.position.x) < 11f && Time.time >= _nextShot)
        {
            FireAtPlayer();
        }
    }

    private void UpdateBossAttack()
    {
        if (_isTelegraphing)
        {
            if (_renderer != null)
            {
                _renderer.color = Mathf.FloorToInt(Time.time * 12f) % 2 == 0
                    ? new Color(1f, 0.9f, 0.25f)
                    : Color.white;
            }

            if (Time.time >= _telegraphEndsAt)
            {
                _isTelegraphing = false;
                FireBossVolley();
            }

            return;
        }

        if (_renderer != null)
        {
            _renderer.color = _bossPhase switch
            {
                1 => Color.white,
                2 => new Color(1f, 0.78f, 0.95f),
                _ => new Color(1f, 0.45f, 0.7f)
            };
        }

        float horizontalDistance = Mathf.Abs(_player.position.x - transform.position.x);
        float verticalDistance = Mathf.Abs(_player.position.y - transform.position.y);
        if (horizontalDistance <= 24f && verticalDistance <= 12f && Time.time >= _nextShot)
        {
            _isTelegraphing = true;
            _telegraphEndsAt = Time.time + 0.8f;
            float cooldown = _bossPhase switch
            {
                1 => 2.4f,
                2 => 1.9f,
                _ => 1.55f
            };
            _nextShot = _telegraphEndsAt + cooldown;
            _run.SetBossWarning(_bossPhase);
        }
    }

    private void FixedUpdate()
    {
        if (!_isProjectile || _projectileBody == null)
        {
            return;
        }

        _projectileBody.MovePosition(_projectileBody.position + _projectileDirection * (Time.fixedDeltaTime * 8f));
        _lifeTime -= Time.fixedDeltaTime;
        if (_lifeTime <= 0f)
        {
            Destroy(gameObject);
        }
    }

    public void TakeDamage(int damage)
    {
        if (_isProjectile)
        {
            Destroy(gameObject);
            return;
        }

        _health -= damage;
        if (_isBoss)
        {
            int phase = _health > _maxHealth * 2 / 3 ? 1 : _health > _maxHealth / 3 ? 2 : 3;
            if (phase != _bossPhase)
            {
                _bossPhase = phase;
                _isTelegraphing = false;
                if (_renderer != null)
                {
                    _renderer.color = Color.white;
                }

                _nextShot = Time.time + 0.75f;
                _run?.SetBossPhase(_bossPhase);
            }
        }

        _run?.UpdateBossHealth(this);
        if (_health > 0)
        {
            if (_renderer != null)
            {
                _renderer.color = Color.white;
                CancelInvoke(nameof(RestoreColor));
                Invoke(nameof(RestoreColor), 0.12f);
            }

            return;
        }

        if (_run != null)
        {
            _run.EnemyDefeated(this, _isBoss ? 1000 : 250);
        }

        if (_isBoss)
        {
            gameObject.SetActive(false);
            return;
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isProjectile)
        {
            if (other.GetComponentInParent<PlayerHealth>() != null)
            {
                _run?.TakeDamage();
                Destroy(gameObject);
                return;
            }

            if (!other.isTrigger && other.GetComponent<CaveEnemy>() == null)
            {
                Destroy(gameObject);
            }

            return;
        }

        if (other.GetComponentInParent<PlayerHealth>() != null)
        {
            _run?.TakeDamage();
        }
    }

    private void FireAtPlayer()
    {
        Vector2 direction = (_player.position - transform.position).normalized;
        FireProjectile(direction, new Color(1f, 0.72f, 0.4f));
        _nextShot = Time.time + Random.Range(1.8f, 2.8f);
    }

    private void FireBossVolley()
    {
        Vector2 direction = (_player.position - transform.position).normalized;
        int count = _bossPhase == 1 ? 1 : _bossPhase == 2 ? 3 : 5;
        float spread = _bossPhase == 2 ? 16f : 14f;
        for (int i = 0; i < count; i++)
        {
            float angle = (i - (count - 1) * 0.5f) * spread;
            Vector2 shotDirection = Quaternion.Euler(0f, 0f, angle) * direction;
            FireProjectile(shotDirection, new Color(1f, 0.35f + 0.16f * _bossPhase, 0.9f));
        }
    }

    private void FireProjectile(Vector2 direction, Color color)
    {
        Vector2 origin = (Vector2)transform.position + direction * 0.7f;
        Transform parent = transform.parent;
        Vector3 localOrigin = parent != null ? parent.InverseTransformPoint(origin) : origin;
        GameObject shot = CaveRunController.CreateBlock(
            parent,
            "Enemy energy bolt",
            localOrigin,
            new Vector2(0.3f, 0.3f),
            Color.white,
            3,
            false,
            true);
        SpriteRenderer shotRenderer = shot.GetComponent<SpriteRenderer>();
        if (_run != null && _run.ProjectileSprite != null)
        {
            shotRenderer.sprite = _run.ProjectileSprite;
            Vector2 spriteSize = _run.ProjectileSprite.bounds.size;
            if (spriteSize.x > 0f && spriteSize.y > 0f)
            {
                float scale = Mathf.Min(0.3f / spriteSize.x, 0.3f / spriteSize.y);
                shot.transform.localScale = Vector3.one * scale;
                shot.GetComponent<BoxCollider2D>().size = Vector2.one * (0.18f / scale);
            }
        }

        shotRenderer.color = color;
        if (_run == null || _run.ProjectileSprite == null)
        {
            shot.GetComponent<BoxCollider2D>().size = new Vector2(0.62f, 0.62f);
        }
        shot.transform.rotation = Quaternion.FromToRotation(Vector3.right, direction);
        Rigidbody2D body = shot.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.bodyType = RigidbodyType2D.Kinematic;
        body.useFullKinematicContacts = true;
        CaveEnemy projectile = shot.AddComponent<CaveEnemy>();
        projectile.ConfigureProjectile(_run, direction);
    }

    private void RestoreColor()
    {
        if (_renderer != null)
        {
            _renderer.color = Color.white;
        }
    }
}

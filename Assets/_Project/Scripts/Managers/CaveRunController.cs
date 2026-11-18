using System.Collections.Generic;
using Scripts.Player;
using UnityEngine;
using UnityEngine.InputSystem;

public class CaveRunController : MonoBehaviour
{
    private enum RunState
    {
        Playing,
        Paused,
        Won,
        Lost
    }

    private const int WeaponUpgradeMilestone = 500;
    private const int DeathScorePenalty = WeaponUpgradeMilestone / 2;
    private const int MaximumWeaponUpgrades = 3;

    private static Sprite _pixelSprite;
    private static Texture2D _pixelTexture;
    private readonly List<CaveEnemy> _enemies = new();
    private CaveEnemy _boss;
    private CaveEnemy _sceneBoss;
    private bool _buildingScenePreview;
    private int _bossHealth;
    private int _bossMaxHealth;
    private int _bossPhase;
    private SpriteRenderer[] _playerSpriteRenderers = System.Array.Empty<SpriteRenderer>();
    private Color[] _playerSpriteColors = System.Array.Empty<Color>();
    [SerializeField] private Sprite[] _crabFrames;
    [SerializeField] private Sprite[] _jumperFrames;
    [SerializeField] private Sprite[] _octopusFrames;
    [SerializeField] private Sprite _playerCrouchSprite;
    [SerializeField] private Sprite _backgroundSprite;
    [SerializeField] private Sprite _middleGroundSprite;
    [SerializeField] private Sprite _stoneSprite;
    [SerializeField] private Sprite _stalactiteSprite;
    [SerializeField] private Sprite _plantBigSprite;
    [SerializeField] private Sprite _plantSmallSprite;
    [SerializeField] private Sprite _stoneHeadSprite;
    [SerializeField] private Sprite _gateSprite;
    [SerializeField] private Sprite _crystalSprite;
    [SerializeField] private Sprite _lifeCrystalSprite;
    [SerializeField] private Sprite _shotSprite;
    private GameObject _player;
    private Transform _checkpoint;
    private Transform _levelRoot;
    private Vector2 _startPosition;
    private RunState _state;
    private int _lives = 3;
    private int _score;
    private int _weaponUpgradeLevel;
    private float _elapsedTime;
    private float _invulnerableUntil;
    private float _nextDamageBlink;
    private float _messageUntil;
    private string _message = "Clear the cavern and defeat the guardian to open the gate.";
    private GUIStyle _hudStyle;
    private GUIStyle _messageStyle;
    private GUIStyle _titleStyle;

    public static CaveRunController Instance { get; private set; }
    public GameObject Player => _player;
    public bool IsRunOver => _state == RunState.Won || _state == RunState.Lost;
    public bool IsExitUnlocked => _enemies.Count == 0;
    public Sprite ProjectileSprite => _shotSprite;
    public int WeaponUpgradeLevel => _weaponUpgradeLevel;
    public bool IsBossFightActive => _boss != null;

    public void RegisterBoss(CaveEnemy boss)
    {
        _boss = boss;
        _bossHealth = boss.Health;
        _bossMaxHealth = boss.MaxHealth;
        _bossPhase = boss.BossPhase;
    }

    public void UpdateBossHealth(CaveEnemy boss)
    {
        if (boss == _boss)
        {
            _bossHealth = boss.Health;
        }
    }

    public void SetBossPhase(int phase)
    {
        _bossPhase = phase;
        SetMessage(phase == 2
            ? "The Crystal Guardian enrages! It is firing a three-shot spread!"
            : "The Guardian is desperate! Watch for a five-shot volley!");
    }

    public void SetBossWarning(int phase)
    {
        SetMessage(phase == 1
            ? "Guardian is targeting you! Move to dodge!"
            : "Guardian is charging a spread volley! Jump or move!");
    }

    public void Initialize(GameObject player, Transform spawnPoint)
    {
        if (player == null || spawnPoint == null)
        {
            Debug.LogError("CaveRunController requires a player and a spawn point.");
            enabled = false;
            return;
        }

        Instance = this;
        _player = player;
        GameObject bossObject = GameObject.Find("Crystal Guardian");
        _sceneBoss = bossObject != null ? bossObject.GetComponent<CaveEnemy>() : null;
        _playerSpriteRenderers = player.GetComponentsInChildren<SpriteRenderer>(true);
        _playerSpriteColors = new Color[_playerSpriteRenderers.Length];
        for (int i = 0; i < _playerSpriteRenderers.Length; i++)
        {
            _playerSpriteColors[i] = _playerSpriteRenderers[i].color;
        }
        _startPosition = spawnPoint.position;
        GameObject oldLevel = GameObject.Find("Level");
        if (oldLevel != null)
        {
            oldLevel.SetActive(false);
        }

        GameObject scenePreview = GameObject.Find("Crystal Cavern - Gauntlet");
        if (scenePreview != null)
        {
            scenePreview.SetActive(false);
            Destroy(scenePreview);
        }

        _enemies.Clear();
        _boss = null;
        _checkpoint = new GameObject("Checkpoint - Entrance").transform;
        _checkpoint.position = _startPosition;
        _state = RunState.Playing;
        Time.timeScale = 1f;
        PlayerAnimation playerAnimation = player.GetComponent<PlayerAnimation>();
        if (playerAnimation != null)
        {
            playerAnimation.SetCrouchSprite(_playerCrouchSprite);
        }

        BuildLevel();
    }

    public Transform GenerateScenePreview(GameObject player)
    {
        if (Application.isPlaying)
        {
            Debug.LogError("The cave scene preview can only be generated in Edit Mode.", this);
            return null;
        }

        if (player == null)
        {
            Debug.LogError("A player object is required to generate the cave scene preview.", this);
            return null;
        }

        _player = player;
        GameObject bossObject = GameObject.Find("Crystal Guardian");
        _sceneBoss = bossObject != null ? bossObject.GetComponent<CaveEnemy>() : null;
        _enemies.Clear();
        _boss = null;
        Instance = this;
        _buildingScenePreview = true;
        try
        {
            BuildLevel();
        }
        finally
        {
            _buildingScenePreview = false;
        }

        return _levelRoot;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        Time.timeScale = 1f;
    }

    private void Update()
    {
        UpdateDamageBlink();

        if (_state == RunState.Playing)
        {
            _elapsedTime += Time.deltaTime;
        }

        if (Keyboard.current == null)
        {
            return;
        }

        if (IsRunOver)
        {
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                RestartRun();
            }

            return;
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    public static Sprite PixelSprite
    {
        get
        {
            if (_pixelSprite != null)
            {
                return _pixelSprite;
            }

            _pixelTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            _pixelTexture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            _pixelTexture.Apply();
            _pixelSprite = Sprite.Create(_pixelTexture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
            _pixelSprite.hideFlags = HideFlags.HideAndDontSave;
            return _pixelSprite;
        }
    }

    public static GameObject CreateBlock(
        Transform parent,
        string objectName,
        Vector2 position,
        Vector2 size,
        Color color,
        int sortingOrder,
        bool solid,
        bool trigger)
    {
        var block = new GameObject(objectName);
        block.transform.SetParent(parent, false);
        block.transform.localPosition = new Vector3(position.x, position.y, 0f);
        block.transform.localScale = new Vector3(size.x, size.y, 1f);

        var renderer = block.AddComponent<SpriteRenderer>();
        renderer.sprite = PixelSprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;

        if (solid || trigger)
        {
            var collider = block.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            collider.isTrigger = trigger;
        }

        if (solid)
        {
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0)
            {
                block.layer = groundLayer;
            }
        }

        return block;
    }

    public void EnemyDefeated(CaveEnemy enemy, int points)
    {
        _enemies.Remove(enemy);
        if (enemy == _boss)
        {
            _boss = null;
            _bossHealth = 0;
        }

        AddScore(points);
        if (IsExitUnlocked)
        {
            SetMessage("All creatures are defeated. The exit is open!");
            return;
        }

        SetMessage(enemy.IsBoss
            ? "Guardian defeated. Clear the remaining creatures to open the gate."
            : "Cave creature defeated!");
    }

    public void AddScore(int points)
    {
        _score += Mathf.Max(0, points);
        UpdateWeaponUpgradeFromScore();
    }

    public void TakeDamage()
    {
        if (_state != RunState.Playing || Time.time < _invulnerableUntil)
        {
            return;
        }

        _lives--;
        _invulnerableUntil = Time.time + 1.25f;
        _score = Mathf.Max(0, _score - DeathScorePenalty);
        UpdateWeaponUpgradeFromScore();

        if (_lives <= 0)
        {
            _lives = 0;
            _state = RunState.Lost;
            RestorePlayerSpriteColors();
            Time.timeScale = 0f;
            UnlockCursor();
            return;
        }

        SetMessage("Hit! " + _lives + " lives left. -" + DeathScorePenalty + " points.");
    }

    private void UpdateWeaponUpgradeFromScore()
    {
        int upgradeLevel = Mathf.Min(_score / WeaponUpgradeMilestone, MaximumWeaponUpgrades);
        if (upgradeLevel == _weaponUpgradeLevel)
        {
            return;
        }

        _weaponUpgradeLevel = upgradeLevel;
        PlayerAttack playerAttack = _player.GetComponent<PlayerAttack>();
        if (playerAttack == null)
        {
            Debug.LogError("Player is missing the PlayerAttack component; score-based weapon upgrade could not be applied.", _player);
            return;
        }

        playerAttack.SetWeaponUpgrade(_weaponUpgradeLevel);
        if (_weaponUpgradeLevel > 0)
        {
            SetMessage("Score milestone reached! Shot level " + (_weaponUpgradeLevel + 1) + ".");
        }
    }

    public void PlayerFell()
    {
        if (_state != RunState.Playing)
        {
            return;
        }

        TakeDamage();
        if (_state == RunState.Playing)
        {
            RespawnAtCheckpoint();
        }
    }

    public void SetCheckpoint(Vector2 position)
    {
        _checkpoint.position = position;
        SetMessage("Checkpoint reached.");
    }

    public void GrantLife()
    {
        if (_lives >= 3)
        {
            AddScore(100);
            SetMessage("Life crystal collected: +100 bonus points.");
            return;
        }

        _lives++;
        SetMessage("Life crystal collected: +1 life.");
    }

    public void TryFinish()
    {
        if (_state != RunState.Playing)
        {
            return;
        }

        if (_enemies.Count > 0)
        {
            SetMessage("Defeat every creature to unlock the gate!");
            return;
        }

        _score += 1000 + Mathf.Max(0, 300 - Mathf.FloorToInt(_elapsedTime)) * 10;
        _state = RunState.Won;
        Time.timeScale = 0f;
        UnlockCursor();
    }

    private void RespawnAtCheckpoint()
    {
        _player.transform.position = _checkpoint.position;
        Rigidbody2D body = _player.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }
    }

    private void TogglePause()
    {
        if (_state == RunState.Playing)
        {
            _state = RunState.Paused;
            Time.timeScale = 0f;
            UnlockCursor();
            return;
        }

        if (_state == RunState.Paused)
        {
            _state = RunState.Playing;
            Time.timeScale = 1f;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private static void UnlockCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void RestartRun()
    {
        Time.timeScale = 1f;
        _state = RunState.Playing;
        _lives = 3;
        _score = 0;
        _weaponUpgradeLevel = 0;
        PlayerAttack playerAttack = _player.GetComponent<PlayerAttack>();
        if (playerAttack != null)
        {
            playerAttack.ResetWeaponUpgrade();
        }
        _elapsedTime = 0f;
        _invulnerableUntil = 0f;
        RestorePlayerSpriteColors();
        _checkpoint.position = _startPosition;
        _player.transform.position = _startPosition;
        Rigidbody2D body = _player.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }

        foreach (Projectile projectile in FindObjectsByType<Projectile>())
        {
            Destroy(projectile.gameObject);
        }

        if (_levelRoot != null)
        {
            _levelRoot.gameObject.SetActive(false);
            Destroy(_levelRoot.gameObject);
        }

        _enemies.Clear();
        _boss = null;
        _bossHealth = 0;
        _bossMaxHealth = 0;
        _bossPhase = 0;
        BuildLevel();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        SetMessage("Clear the cavern and defeat the guardian to open the gate.");
    }

    private void SetMessage(string message)
    {
        _message = message;
        _messageUntil = Time.unscaledTime + 2.5f;
    }

    private void UpdateDamageBlink()
    {
        if (_state != RunState.Playing || Time.time >= _invulnerableUntil)
        {
            RestorePlayerSpriteColors();
            return;
        }

        if (Time.unscaledTime < _nextDamageBlink)
        {
            return;
        }

        _nextDamageBlink = Time.unscaledTime + 0.08f;
        bool visible = Mathf.FloorToInt(Time.unscaledTime / 0.08f) % 2 == 0;
        for (int i = 0; i < _playerSpriteRenderers.Length; i++)
        {
            if (_playerSpriteRenderers[i] == null)
            {
                continue;
            }

            Color color = _playerSpriteColors[i];
            color.a *= visible ? 1f : 0.2f;
            _playerSpriteRenderers[i].color = color;
        }
    }

    private void RestorePlayerSpriteColors()
    {
        for (int i = 0; i < _playerSpriteRenderers.Length; i++)
        {
            if (_playerSpriteRenderers[i] != null)
            {
                _playerSpriteRenderers[i].color = _playerSpriteColors[i];
            }
        }
    }

    private void BuildLevel()
    {
        _levelRoot = new GameObject("Crystal Cavern - Gauntlet").transform;
        _levelRoot.position = Vector3.left * 14f;
        Transform level = _levelRoot;
        Color rock = Color.white;
        Color platformColor = Color.white;
        Color hazardColor = new(0.95f, 0.22f, 0.22f);
        Color crystalColor = new(0.3f, 0.95f, 0.88f);

        for (int i = 0; i < 7; i++)
        {
            float x = -2f + i * 18f;
            CreateScenery(level, "Deep cavern background " + i, _backgroundSprite, new Vector2(x, -1f), new Vector2(19f, 15f), 4f, -20);
            CreateScenery(level, "Cavern middle ground " + i, _middleGroundSprite, new Vector2(x, -1f), new Vector2(19f, 13f), 2f, -12);
        }

        for (int i = 0; i < 19; i++)
        {
            float x = -4f + i * 6f;
            Sprite decoration = i % 3 == 0 ? _plantBigSprite : (i % 2 == 0 ? _plantSmallSprite : _stalactiteSprite);
            Vector2 position = new(x, i % 2 == 0 ? -2.8f : 4.6f);
            Vector2 size = i % 2 == 0 ? new Vector2(1.1f, 1.2f) : new Vector2(0.75f, 1.7f);
            CreateScenery(level, "Cave decoration " + i, decoration, position, size, 0f, -1);
        }

        CreateGround(level, "Cavern path - west", new Vector2(16f, -4f), new Vector2(12f, 1f), rock);
        CreateGround(level, "Cavern path - mid", new Vector2(31f, -4f), new Vector2(10f, 1f), rock);
        CreateGround(level, "Cavern path - deep", new Vector2(47.5f, -4f), new Vector2(13f, 1f), rock);
        CreateGround(level, "Cavern path - guardian", new Vector2(68.5f, -4f), new Vector2(17f, 1f), rock);
        CreateGround(level, "Cavern path - final approach", new Vector2(89f, -4f), new Vector2(18f, 1f), rock);

        CreateGround(level, "Upper route - west ledge", new Vector2(17f, -0.65f), new Vector2(4.8f, 0.55f), rock, true);
        CreateGround(level, "Upper route - west span", new Vector2(25.2f, 1.25f), new Vector2(4.8f, 0.55f), rock, true);
        CreateGround(level, "Upper route - mid descent", new Vector2(33.4f, -0.65f), new Vector2(4.8f, 0.55f), rock, true);
        CreateGround(level, "Upper route - checkpoint ridge", new Vector2(41.6f, 1.25f), new Vector2(4.8f, 0.55f), rock, true);
        CreateGround(level, "Upper route - deep ledge", new Vector2(49.8f, -0.65f), new Vector2(4.8f, 0.55f), rock, true);
        CreateGround(level, "Upper route - deep span", new Vector2(58f, 1.25f), new Vector2(4.8f, 0.55f), rock, true);
        CreateGround(level, "Upper route - hanging descent", new Vector2(66.2f, -0.65f), new Vector2(4.8f, 0.55f), rock, true);
        CreateGround(level, "Upper route - guardian descent", new Vector2(74.4f, 1.25f), new Vector2(4.8f, 0.55f), rock, true);
        CreateGround(level, "Upper route - final descent", new Vector2(82.6f, -0.65f), new Vector2(4.8f, 0.55f), rock, true);

        CreateMovingPlatform(level, "Drifting bridge - first chasm", new Vector2(24f, -2.65f), 2f, 0.45f, 2.1f, platformColor);
        CreateMovingPlatform(level, "Drifting bridge - second chasm", new Vector2(38.5f, -2.65f), 2f, 0.45f, 1.8f, platformColor);
        CreateMovingPlatform(level, "Drifting bridge - third chasm", new Vector2(57f, -2.65f), 2f, 0.45f, 2.2f, platformColor);
        CreateMovingPlatform(level, "Drifting bridge - final approach", new Vector2(78.5f, -2.65f), 2f, 0.45f, 0.7f, platformColor);

        CreateHazard(level, "Spike bed - west", new Vector2(14f, -3.32f), new Vector2(2.2f, 0.36f), hazardColor, CaveHazard.Kind.Spikes);
        CreateHazard(level, "Spike bed - mid", new Vector2(32f, -3.32f), new Vector2(1.8f, 0.36f), hazardColor, CaveHazard.Kind.Spikes);
        CreateHazard(level, "Spike bed - deep", new Vector2(49f, -3.32f), new Vector2(2.4f, 0.36f), hazardColor, CaveHazard.Kind.Spikes);
        CreateHazard(level, "Spike bed - final approach", new Vector2(78.5f, -3.32f), new Vector2(2.4f, 0.36f), hazardColor, CaveHazard.Kind.Spikes);
        CreateHazard(level, "Pulse laser", new Vector2(43.5f, -3.05f), new Vector2(0.22f, 0.9f), new Color(1f, 0.1f, 0.55f), CaveHazard.Kind.TimedLaser);

        CreateEnemy(level, "Cave crab - west", new Vector2(19f, -3.1f), 16f, 21f, 2, false, _crabFrames);
        CreateEnemy(level, "Cave jumper - mid", new Vector2(34f, -3.1f), 30f, 35f, 2, false, _jumperFrames);
        CreateEnemy(level, "Cave crab - deep", new Vector2(50.5f, -3.1f), 45f, 52f, 2, false, _crabFrames);
        CreateEnemy(level, "Cave jumper - gate", new Vector2(63.5f, -3.1f), 61f, 66f, 3, false, _jumperFrames);
        CreateBoss(level);
        CreateEnemy(level, "High ledge sentry - west", new Vector2(18f, 0f), 16f, 19f, 2, false, _jumperFrames);
        CreateEnemy(level, "High ledge sentry - span", new Vector2(25.2f, 1.925f), 23.8f, 26.6f, 2, false, _crabFrames);
        CreateEnemy(level, "High ledge sentry - deep", new Vector2(49.8f, 0f), 48.2f, 51.4f, 2, false, _jumperFrames);
        CreateEnemy(level, "High ledge sentry - guardian", new Vector2(58f, 1.925f), 56.6f, 59.4f, 2, false, _crabFrames);
        CreateEnemy(level, "Ridge sentry - checkpoint", new Vector2(41.6f, 1.9f), 40.2f, 43f, 2, false, _crabFrames);
        CreateEnemy(level, "Ridge sentry - descent", new Vector2(66.2f, 0f), 64.8f, 67.6f, 2, false, _jumperFrames);
        CreateEnemy(level, "Final route sentry - ridge", new Vector2(82.6f, 0f), 81.3f, 83.9f, 2, false, _jumperFrames);
        CreateEnemy(level, "Final route sentry - ground", new Vector2(88f, -3.1f), 84f, 92f, 2, false, _crabFrames);

        CreateHazard(level, "Checkpoint beacon", new Vector2(41.2f, -2.45f), new Vector2(0.6f, 1.7f), new Color(1f, 0.76f, 0.2f), CaveHazard.Kind.Checkpoint);
        CreateHazard(level, "Extra life crystal", new Vector2(27.5f, -1.1f), new Vector2(0.8f, 0.8f), crystalColor, CaveHazard.Kind.LifeCrystal);
        CreateHazard(level, "Score crystal - west", new Vector2(8f, -1.2f), new Vector2(0.65f, 0.65f), crystalColor, CaveHazard.Kind.ScoreCrystal);
        CreateHazard(level, "Score crystal - mid", new Vector2(29f, 0.15f), new Vector2(0.65f, 0.65f), crystalColor, CaveHazard.Kind.ScoreCrystal);
        CreateHazard(level, "Score crystal - deep", new Vector2(54f, -1.05f), new Vector2(0.65f, 0.65f), crystalColor, CaveHazard.Kind.ScoreCrystal);
        CreateHazard(level, "Upper route crystal - west", new Vector2(25.2f, 2.15f), new Vector2(0.65f, 0.65f), crystalColor, CaveHazard.Kind.ScoreCrystal);
        CreateHazard(level, "Upper route crystal - deep", new Vector2(58f, 2.15f), new Vector2(0.65f, 0.65f), crystalColor, CaveHazard.Kind.ScoreCrystal);
        CreateHazard(level, "Guardian gate", new Vector2(96f, -2.25f), new Vector2(0.8f, 2.5f), new Color(0.85f, 0.22f, 0.95f), CaveHazard.Kind.Exit);

    }

    private void CreateMovingPlatform(Transform parent, string objectName, Vector2 position, float width, float height, float distance, Color color)
    {
        GameObject platform = CreateBlock(parent, objectName, position, new Vector2(width, height), color, 0, true, false);
        SpriteRenderer platformRenderer = platform.GetComponent<SpriteRenderer>();
        if (_stoneSprite != null)
        {
            platformRenderer.sprite = _stoneSprite;
            platformRenderer.drawMode = SpriteDrawMode.Tiled;
            platformRenderer.size = new Vector2(width, height);
            platform.transform.localScale = Vector3.one;
            platform.GetComponent<BoxCollider2D>().size = new Vector2(width, height);
        }

        Rigidbody2D body = platform.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        CaveMovingPlatform mover = platform.AddComponent<CaveMovingPlatform>();
        if (!_buildingScenePreview)
        {
            mover.Configure(platform.transform.position, distance, 1.1f + distance * 0.15f);
        }
    }

    private CaveHazard CreateHazard(Transform parent, string objectName, Vector2 position, Vector2 size, Color color, CaveHazard.Kind kind)
    {
        GameObject hazard = CreateBlock(parent, objectName, position, size, color, 1, false, true);
        if (kind == CaveHazard.Kind.Spikes)
        {
            hazard.GetComponent<SpriteRenderer>().enabled = false;
            CreateSpikeVisuals(hazard.transform, size.x);
        }
        else if (kind == CaveHazard.Kind.ScoreCrystal || kind == CaveHazard.Kind.LifeCrystal)
        {
            hazard.GetComponent<SpriteRenderer>().enabled = false;
            BoxCollider2D collider = hazard.GetComponent<BoxCollider2D>();
            collider.size = new Vector2(0.62f, 0.62f);
            Sprite sprite = kind == CaveHazard.Kind.LifeCrystal ? _lifeCrystalSprite : _crystalSprite;
            CreateHazardSprite(hazard.transform, sprite, new Vector2(0.55f, 0.55f), 1);
            SpriteRenderer pickupRenderer = hazard.GetComponentInChildren<SpriteRenderer>();
            if (pickupRenderer != null)
            {
                pickupRenderer.color = color;
            }
        }
        else if (kind == CaveHazard.Kind.Checkpoint)
        {
            hazard.GetComponent<SpriteRenderer>().enabled = false;
            BoxCollider2D collider = hazard.GetComponent<BoxCollider2D>();
            collider.size = new Vector2(1.05f, 0.6f);
            CreateHazardSprite(hazard.transform, _stoneHeadSprite, new Vector2(1f, 1.5f), 1);
        }
        else if (kind == CaveHazard.Kind.Exit)
        {
            hazard.GetComponent<SpriteRenderer>().enabled = false;
            BoxCollider2D collider = hazard.GetComponent<BoxCollider2D>();
            collider.size = new Vector2(0.8f, 0.64f);
            CreateHazardSprite(hazard.transform, _gateSprite, new Vector2(1.4f, 2.1f), 1);
        }

        CaveHazard behaviour = hazard.AddComponent<CaveHazard>();
        if (!_buildingScenePreview)
        {
            behaviour.Configure(this, kind, color);
        }

        return behaviour;
    }

    private void CreateEnemy(Transform parent, string objectName, Vector2 position, float patrolLeft, float patrolRight, int health, bool isBoss, Sprite[] animationFrames)
    {
        GameObject enemyObject = CreateBlock(
            parent,
            objectName,
            position,
            isBoss ? new Vector2(1.6f, 1.7f) : new Vector2(1.1f, 1.15f),
            Color.white,
            2,
            false,
            true);
        SpriteRenderer enemyRenderer = enemyObject.GetComponent<SpriteRenderer>();
        Sprite firstFrame = animationFrames != null && animationFrames.Length > 0 ? animationFrames[0] : null;
        enemyRenderer.sprite = firstFrame;
        float spriteScale = firstFrame != null
            ? Mathf.Max(
                (isBoss ? 1.6f : 1.1f) / firstFrame.bounds.size.x,
                (isBoss ? 1.7f : 1.15f) / firstFrame.bounds.size.y)
            : 1f;
        enemyObject.transform.localScale = Vector3.one * spriteScale;
        BoxCollider2D enemyCollider = enemyObject.GetComponent<BoxCollider2D>();
        enemyCollider.size = new Vector2(
            isBoss ? 1.05f : 0.68f,
            isBoss ? 1.2f : 0.6f) / spriteScale;

        if (animationFrames == null || animationFrames.Length == 0)
        {
            Debug.LogError("Enemy sprite frames are missing for " + objectName + ". Assign them on the Game Manager prefab.");
        }

        CaveEnemy enemy = enemyObject.AddComponent<CaveEnemy>();
        Vector3 left = parent.TransformPoint(new Vector3(patrolLeft, position.y, 0f));
        Vector3 right = parent.TransformPoint(new Vector3(patrolRight, position.y, 0f));
        if (!_buildingScenePreview)
        {
            enemy.Configure(this, _player.transform, left.x, right.x, health, isBoss, animationFrames);
            _enemies.Add(enemy);
        }
    }

    private void CreateBoss(Transform parent)
    {
        if (_sceneBoss == null)
        {
            CreateEnemy(parent, "The Crystal Guardian", new Vector2(70f, -2.45f), 67f, 72f, 12, true, _octopusFrames);
            return;
        }

        Vector2 bossPosition = parent.TransformPoint(new Vector3(70f, -2.45f, 0f));
        _sceneBoss.transform.position = bossPosition;
        _sceneBoss.gameObject.SetActive(true);

        SpriteRenderer bossRenderer = _sceneBoss.GetComponent<SpriteRenderer>();
        Sprite firstFrame = _octopusFrames != null && _octopusFrames.Length > 0 ? _octopusFrames[0] : null;
        if (bossRenderer != null)
        {
            bossRenderer.sprite = firstFrame;
            bossRenderer.sortingOrder = 2;
        }

        float spriteScale = firstFrame != null
            ? Mathf.Max(1.6f / firstFrame.bounds.size.x, 1.7f / firstFrame.bounds.size.y)
            : 1f;
        _sceneBoss.transform.localScale = Vector3.one * spriteScale;
        BoxCollider2D bossCollider = _sceneBoss.GetComponent<BoxCollider2D>();
        if (bossCollider == null)
        {
            Debug.LogError("The placed Crystal Guardian is missing its BoxCollider2D.", _sceneBoss);
            return;
        }

        bossCollider.size = new Vector2(1.05f, 1.2f) / spriteScale;
        if (!_buildingScenePreview)
        {
            Vector3 patrolLeft = parent.TransformPoint(new Vector3(67f, -2.45f, 0f));
            Vector3 patrolRight = parent.TransformPoint(new Vector3(72f, -2.45f, 0f));
            _sceneBoss.Configure(this, _player.transform, patrolLeft.x, patrolRight.x, 12, true, _octopusFrames);
            _enemies.Add(_sceneBoss);
        }
    }

    private void CreateGround(Transform parent, string objectName, Vector2 position, Vector2 size, Color color, bool upperPlatform = false)
    {
        GameObject ground = CreateBlock(parent, objectName, position, size, color, -2, true, false);
        int platformLayer = LayerMask.NameToLayer(upperPlatform ? "UpperPlatform" : "Ground");
        if (platformLayer < 0)
        {
            Debug.LogError("The required platform layer is missing: " + (upperPlatform ? "UpperPlatform" : "Ground"));
        }
        else
        {
            ground.layer = platformLayer;
        }

        SpriteRenderer renderer = ground.GetComponent<SpriteRenderer>();
        if (_stoneSprite != null)
        {
            renderer.sprite = _stoneSprite;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = size;
            ground.transform.localScale = Vector3.one;
            ground.GetComponent<BoxCollider2D>().size = size;
        }
    }

    private void CreateScenery(Transform parent, string objectName, Sprite sprite, Vector2 position, Vector2 size, float z, int sortingOrder)
    {
        if (sprite == null)
        {
            Debug.LogWarning("Missing cave scenery sprite for " + objectName + ".");
            return;
        }

        GameObject scenery = new(objectName);
        scenery.transform.SetParent(parent, false);
        scenery.transform.localPosition = new Vector3(position.x, position.y, z);
        SpriteRenderer renderer = scenery.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
        Vector2 spriteSize = sprite.bounds.size;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
        {
            return;
        }

        float scale = Mathf.Min(size.x / spriteSize.x, size.y / spriteSize.y);
        scenery.transform.localScale = Vector3.one * scale;
        if (sprite == _stalactiteSprite)
        {
            renderer.flipY = position.y < 0f;
        }
    }

    private void CreateSpikeVisuals(Transform parent, float width)
    {
        if (_stalactiteSprite == null)
        {
            Debug.LogWarning("Missing stalactite sprite for spike visuals.");
            return;
        }

        int spikeCount = Mathf.Max(2, Mathf.FloorToInt(width / 0.42f));
        float spacing = width / spikeCount;
        for (int i = 0; i < spikeCount; i++)
        {
            GameObject spike = new("Spike " + i);
            spike.transform.SetParent(parent, false);
            spike.transform.localPosition = new Vector3(
                (-width * 0.5f + spacing * (i + 0.5f)) / parent.localScale.x,
                0.16f / parent.localScale.y,
                -0.1f);
            SpriteRenderer renderer = spike.AddComponent<SpriteRenderer>();
            renderer.sprite = _stalactiteSprite;
            renderer.flipY = true;
            renderer.sortingOrder = 1;
            Vector2 spriteSize = _stalactiteSprite.bounds.size;
            if (spriteSize.x > 0f && spriteSize.y > 0f)
            {
                float scale = Mathf.Min(spacing * 0.9f / spriteSize.x, 0.5f / spriteSize.y);
                spike.transform.localScale = new Vector3(
                    scale / parent.localScale.x,
                    scale / parent.localScale.y,
                    1f);
            }
        }
    }

    private void CreateHazardSprite(Transform parent, Sprite sprite, Vector2 worldSize, int sortingOrder)
    {
        if (sprite == null)
        {
            Debug.LogWarning("Missing sprite for hazard visual under " + parent.name + ".");
            return;
        }

        Vector2 spriteSize = sprite.bounds.size;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
        {
            return;
        }

        float scale = Mathf.Min(worldSize.x / spriteSize.x, worldSize.y / spriteSize.y);
        GameObject visual = new("Visual");
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = new Vector3(0f, 0f, -0.1f);
        visual.transform.localScale = new Vector3(
            scale / parent.localScale.x,
            scale / parent.localScale.y,
            1f);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
    }

    private void OnGUI()
    {
        if (_hudStyle == null)
        {
            _hudStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            _messageStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                normal = { textColor = Color.white }
            };
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 34,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        _hudStyle.fontSize = Mathf.Clamp(Screen.width / 42, 12, 18);
        float hudWidth = Mathf.Min(700f, Screen.width - 32f);
        string nextUpgrade = _weaponUpgradeLevel >= MaximumWeaponUpgrades
            ? "MAX"
            : (_score % WeaponUpgradeMilestone) + "/" + WeaponUpgradeMilestone;
        GUI.Box(new Rect(16f, 14f, hudWidth, 42f), "LIVES: " + _lives + "/3      SHOT LVL: " + (_weaponUpgradeLevel + 1) + " (" + nextUpgrade + ")      SCORE: " + _score + "      TIME: " + FormatTime(_elapsedTime), _hudStyle);
        GUI.Box(new Rect(16f, 62f, 760f, 32f), "A/D MOVE   UP AIM HIGH   DOWN CROUCH / AIM DOWN IN AIR   SPACE JUMP   HOLD LMB FIRE   ESC PAUSE", _messageStyle);

        if (_boss != null && _bossMaxHealth > 0)
        {
            const float barWidth = 360f;
            float barX = (Screen.width - barWidth) * 0.5f;
            GUI.Box(new Rect(barX, 104f, barWidth, 28f), "CRYSTAL GUARDIAN - PHASE " + _bossPhase, _messageStyle);
            float healthWidth = barWidth * Mathf.Clamp01((float)_bossHealth / _bossMaxHealth);
            GUI.color = Color.Lerp(new Color(0.95f, 0.2f, 0.45f), new Color(0.35f, 0.95f, 0.85f), (float)_bossHealth / _bossMaxHealth);
            GUI.DrawTexture(new Rect(barX + 4f, 134f, healthWidth - Mathf.Min(healthWidth, 8f), 12f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        if (Time.unscaledTime < _messageUntil && _state == RunState.Playing)
        {
            float messageY = _boss != null ? 154f : 104f;
            GUI.Box(new Rect(Screen.width * 0.5f - 220f, messageY, 440f, 36f), _message, _messageStyle);
        }

        if (_state == RunState.Playing)
        {
            return;
        }

        string title = _state switch
        {
            RunState.Paused => "PAUSED",
            RunState.Won => "CAVERN CLEARED!",
            RunState.Lost => "RUN OVER",
            _ => string.Empty
        };
        GUI.Box(new Rect(Screen.width * 0.5f - 190f, Screen.height * 0.5f - 120f, 380f, 240f), GUIContent.none);
        GUI.Label(new Rect(Screen.width * 0.5f - 180f, Screen.height * 0.5f - 105f, 360f, 55f), title, _titleStyle);
        if (_state == RunState.Won)
        {
            GUI.Label(new Rect(Screen.width * 0.5f - 170f, Screen.height * 0.5f - 45f, 340f, 35f), "Score: " + _score + "    Time: " + FormatTime(_elapsedTime), _messageStyle);
        }

        string buttonText = _state == RunState.Paused ? "RESUME" : "PLAY AGAIN";
        if (GUI.Button(new Rect(Screen.width * 0.5f - 95f, Screen.height * 0.5f + 30f, 190f, 48f), buttonText))
        {
            if (_state == RunState.Paused)
            {
                TogglePause();
            }
            else
            {
                RestartRun();
            }
        }
    }

    private static string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.FloorToInt(seconds);
        return (totalSeconds / 60).ToString("00") + ":" + (totalSeconds % 60).ToString("00");
    }
}

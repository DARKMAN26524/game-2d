using System.Collections.Generic;
using Scripts.Utilities.Singletons;
using UnityEngine;

public class GameManager : NonPersistentSingleton<GameManager>
{

    [SerializeField] private Transform _playerSpawnPoint;
    [SerializeField] private GameObject _playerInScene;
    [SerializeField] private float _deathYLevel = -25f;
    private CaveRunController _runController;

    protected override void Awake()
    {
        base.Awake();
    }

    void Start()
    {

        CheckErrors();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        _playerInScene.transform.position = _playerSpawnPoint.position;
        _runController = GetComponent<CaveRunController>();
        if (_runController == null)
        {
            _runController = gameObject.AddComponent<CaveRunController>();
        }

        _runController.Initialize(_playerInScene, _playerSpawnPoint);
    }

    void Update()
    {

        if (_playerInScene == null || _runController == null || _runController.IsRunOver)
        {
            return;
        }

        if (_playerInScene.transform.position.y < _deathYLevel)
        {
            _runController.PlayerFell();
        }
    }

    private void CheckErrors()
    {
        List<string> errors = new();

        if (_playerSpawnPoint == null)
        {
            errors.Add("GameManager: Player Spawn Point is not assigned.");
        }

        if (_playerInScene == null)
        {
            errors.Add("GameManager: Player In Scene is not assigned.");
        }
        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                Debug.LogError(error);
            }
            throw new System.Exception("GameManager initialization failed due to errors.");
        }

    }
}

using UnityEngine;

public class CaveHazard : MonoBehaviour
{
    public enum Kind
    {
        Spikes,
        TimedLaser,
        ScoreCrystal,
        LifeCrystal,
        Checkpoint,
        Exit
    }

    private CaveRunController _run;
    private SpriteRenderer _renderer;
    private Kind _kind;
    private Color _baseColor;
    private float _phase;
    private bool _consumed;
    public bool IsScoreCrystal => _kind == Kind.ScoreCrystal;

    public void Configure(CaveRunController run, Kind kind, Color color)
    {
        _run = run;
        _kind = kind;
        _baseColor = color;
        _renderer = GetComponentInChildren<SpriteRenderer>();
        _phase = Random.Range(0f, 2f);
    }

    private void Update()
    {
        if (_kind == Kind.Exit && _run != null && _run.IsExitUnlocked)
        {
            _renderer.color = new Color(0.2f, 0.95f, 0.55f);
        }

        if (_kind != Kind.TimedLaser || _consumed)
        {
            return;
        }

        _phase += Time.deltaTime;
        bool active = _phase % 3.2f < 1.65f;
        Collider2D trigger = GetComponent<Collider2D>();
        trigger.enabled = active;
        _renderer.enabled = active;
        if (active)
        {
            _renderer.color = Color.Lerp(_baseColor, Color.white, Mathf.PingPong(Time.time * 5f, 0.4f));
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_run == null || _consumed || other.GetComponentInParent<PlayerHealth>() == null)
        {
            return;
        }

        switch (_kind)
        {
            case Kind.Spikes:
            case Kind.TimedLaser:
                _run.TakeDamage();
                break;
            case Kind.ScoreCrystal:
                _run.AddScore(100);
                _consumed = true;
                Destroy(gameObject);
                break;
            case Kind.LifeCrystal:
                _run.GrantLife();
                _consumed = true;
                Destroy(gameObject);
                break;
            case Kind.Checkpoint:
                _run.SetCheckpoint(transform.position + Vector3.up * 0.9f);
                _consumed = true;
                _renderer.color = Color.green;
                break;
            case Kind.Exit:
                _run.TryFinish();
                break;
        }
    }
}

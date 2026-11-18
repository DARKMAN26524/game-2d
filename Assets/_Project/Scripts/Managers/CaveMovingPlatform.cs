using UnityEngine;

public class CaveMovingPlatform : MonoBehaviour
{
    private Vector3 _start;
    private float _distance;
    private float _speed;
    private float _offset;
    private Rigidbody2D _body;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
    }

    public void Configure(Vector2 start, float distance, float speed)
    {
        _start = new Vector3(start.x, start.y, 0f);
        _distance = distance;
        _speed = speed;
        _offset = Random.Range(0f, 4f);
    }

    private void FixedUpdate()
    {
        float travel = Mathf.PingPong((Time.fixedTime + _offset) * _speed, _distance) - _distance * 0.5f;
        _body.MovePosition(_start + Vector3.right * travel);
    }
}

using UnityEngine;
/// <summary>
/// Movimiento flotante y caótico como una mosca.
/// </summary>
public class FlyEnemy : HuntEnemyBase
{
    private float amplitude;
    private float frequency;
    private float speed;
    private Vector2 startPos;
    protected override void Awake()
    {
        base.Awake();
        startPos = rect.anchoredPosition;
        amplitude = Random.Range(20f, 50f);
        frequency = Random.Range(2f, 5f);
        speed = Random.Range(30f, 60f);
    }
    protected override void Move()
    {
        float x = startPos.x + Mathf.Sin(Time.time * frequency) * amplitude;
        float y = rect.anchoredPosition.y + speed * Time.deltaTime;
        rect.anchoredPosition = new Vector2(x, y);
    }
}

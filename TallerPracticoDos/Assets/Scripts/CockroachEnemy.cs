using UnityEngine;
/// <summary>
/// Movimiento errático y en zigzag, imita a una cucaracha.
/// </summary>
public class CockroachEnemy : HuntEnemyBase
{
    private Vector2 direction;
    private float speed;
    protected override void Awake()
    {
        base.Awake();
        direction = new Vector2(Random.Range(-1f, 1f), Random.Range(-0.2f, 0.2f)).normalized;
        speed = Random.Range(100f, 150f);
    }
    protected override void Move()
    {
        rect.anchoredPosition += direction * speed * Time.deltaTime;
    }
}

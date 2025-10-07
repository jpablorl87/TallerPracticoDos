using UnityEngine;
/// <summary>
/// Movimiento recto y rápido de un lado a otro.
/// </summary>
public class MouseEnemy : HuntEnemyBase
{
    private Vector2 direction;
    private float speed;
    protected override void Awake()
    {
        base.Awake();
        direction = Vector2.right * (Random.value > 0.5f ? 1 : -1);
        speed = Random.Range(120f, 180f);
    }
    protected override void Move()
    {
        rect.anchoredPosition += direction * speed * Time.deltaTime;
    }
}

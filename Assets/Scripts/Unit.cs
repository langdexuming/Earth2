using UnityEngine;

public class Unit : SelectableEntity
{
    [Header("Unit")]
    public string unitName = "Unit";
    public UnitType unitType = UnitType.Infantry;
    public float moveSpeed = 5f;
    public int attackDamage = 10;
    public float attackRange = 3f;
    public float attackCooldown = 1.2f;
    public float turnSpeed = 8f;
    public float stoppingDistance = 0.15f;

    [Header("Air Unit")]
    public float flightHeight = 7f;

    private Vector3 moveTarget;
    private bool hasMoveTarget = false;
    private SelectableEntity attackTarget;
    private float attackCooldownTimer = 0f;

    private void Update()
    {
        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.deltaTime;
        }

        if (attackTarget != null)
        {
            HandleAttackBehaviour();
            return;
        }

        if (hasMoveTarget)
        {
            HandleMovement(moveTarget);
        }
    }

    public void Configure(UnitType type, int teamId)
    {
        unitType = type;
        team = teamId;

        switch (unitType)
        {
            case UnitType.Infantry:
                unitName = "Infantry";
                maxHealth = 90;
                moveSpeed = 5.6f;
                attackDamage = 12;
                attackRange = 4f;
                attackCooldown = 1.2f;
                break;
            case UnitType.Tank:
                unitName = "Tank";
                maxHealth = 240;
                moveSpeed = 3.3f;
                attackDamage = 36;
                attackRange = 8f;
                attackCooldown = 2.2f;
                break;
            case UnitType.Aircraft:
                unitName = "Aircraft";
                maxHealth = 160;
                moveSpeed = 7.8f;
                attackDamage = 28;
                attackRange = 9f;
                attackCooldown = 1.6f;
                break;
        }

        health = maxHealth;

        Color tint = team == 0 ? new Color(0.18f, 0.55f, 1f) : new Color(1f, 0.35f, 0.3f);
        if (unitType == UnitType.Tank)
        {
            tint = Color.Lerp(tint, Color.gray, 0.35f);
        }
        if (unitType == UnitType.Aircraft)
        {
            tint = Color.Lerp(tint, Color.white, 0.2f);
        }
        SetTeamTint(tint);

        Vector3 position = transform.position;
        position.y = unitType == UnitType.Aircraft ? flightHeight : 0.55f;
        transform.position = position;
    }

    public bool CanAttack()
    {
        return attackDamage > 0;
    }

    public void MoveTo(Vector3 destination)
    {
        attackTarget = null;
        hasMoveTarget = true;
        moveTarget = destination;
        moveTarget.y = unitType == UnitType.Aircraft ? flightHeight : 0.55f;
    }

    public void Attack(SelectableEntity target)
    {
        if (target == null || target == this || target.team == team)
        {
            return;
        }

        attackTarget = target;
        hasMoveTarget = false;
    }

    private void HandleAttackBehaviour()
    {
        if (attackTarget == null)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, attackTarget.transform.position);
        if (distance > attackRange)
        {
            HandleMovement(attackTarget.transform.position);
            return;
        }

        FaceTowards(attackTarget.transform.position - transform.position);

        if (attackCooldownTimer <= 0f)
        {
            attackTarget.TakeDamage(attackDamage);
            attackCooldownTimer = attackCooldown;
        }
    }

    private void HandleMovement(Vector3 destination)
    {
        Vector3 desired = destination;
        desired.y = unitType == UnitType.Aircraft ? flightHeight : 0.55f;

        transform.position = Vector3.MoveTowards(transform.position, desired, moveSpeed * Time.deltaTime);
        FaceTowards(desired - transform.position);

        if (Vector3.Distance(transform.position, desired) <= stoppingDistance)
        {
            hasMoveTarget = false;
        }
    }

    private void FaceTowards(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }
}

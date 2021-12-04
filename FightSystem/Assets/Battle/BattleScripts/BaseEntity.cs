using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BaseEntity : MonoBehaviour
{

    public HealthBar barPrefab;
    public SpriteRenderer spriteRender;
    public Animator animator;

    public int baseDamage = 1;
    public int baseHealth = 3;
    [Range(1, 5)]
    public int range = 1;
    public float attackSpeed = 1f; //Attacks per second
    public float movementSpeed = 1f; //Attacks per second

    protected Team myTeam;
    protected BaseEntity currentTarget = null;
    protected Node currentNode;

    public Node CurrentNode => currentNode;
     
    protected bool HasEnemy => currentTarget != null;
    protected bool IsInRange => currentTarget != null && Vector3.Distance(this.transform.position, currentTarget.transform.position) <= range;
    protected bool moving;
    protected Node destination;
    protected HealthBar healthbar;

    protected bool dead = false;
    protected bool canAttack = true;
    protected float waitBetweenAttack;

    protected Vector2 walkStartPos;
    protected Vector2 walkEndPos;

    protected static float roundEndWalkDuration = 2f;
    protected float walkDuration = 0;

    protected Node roundStartNode;

    public void Setup(Team team, Node currentNode)
    {
        myTeam = team;

        spriteRender.flipX = myTeam == Team.Team2;

        this.currentNode = currentNode;
        transform.position = currentNode.worldPosition;
        currentNode.SetOccupied(true);

        healthbar = Instantiate(barPrefab, this.transform);
        healthbar.Setup(this.transform, baseHealth);
    }

    protected void Start()
    {
        GameManager.Instance.OnRoundStart += OnRoundStart;
        GameManager.Instance.OnRoundEnd += OnRoundEnd;
        GameManager.Instance.OnUnitDied += OnUnitDied;
        GameManager.Instance.OnLevelChanged += OnLevelChanged;
    }

    public void RemoveListeners(){
        GameManager.Instance.OnRoundStart -= OnRoundStart;
        GameManager.Instance.OnRoundEnd -= OnRoundEnd;
        GameManager.Instance.OnUnitDied -= OnUnitDied;
        GameManager.Instance.OnLevelChanged -= OnLevelChanged;
    }

    protected void WalkToStart()
    {
        if(!GameManager.Instance.IsGameStarted){
            if(walkDuration > 0)
            {
                walkDuration -= Time.deltaTime * 1 / roundEndWalkDuration;
                transform.position = Vector3.Lerp(walkEndPos, walkStartPos, walkDuration / 2);

                spriteRender.flipX = walkEndPos.x - walkStartPos.x > 0 ? false : true;
            }
            else
            {
                spriteRender.flipX = myTeam == Team.Team2;
            }
        }
    }

    protected virtual void OnLevelChanged(int prelevel, int currlevel){
        walkStartPos = this.transform.position;
        var nextNode = GridManager.Instance.Graphs[currlevel].Nodes[currentNode.index];
        walkEndPos = nextNode.worldPosition;
        nextNode.SetOccupied(true);
        currentNode = nextNode;
        walkDuration = 2;
    }

    protected virtual void OnRoundStart() 
    { 
        roundStartNode = currentNode;
        FindTarget();
    }

    protected virtual void OnRoundEnd(Team team) 
    { 
        moving = false;
        currentNode = roundStartNode;
        destination = null;
        walkEndPos = roundStartNode.worldPosition;
        walkStartPos = this.transform.position;
        walkDuration = roundEndWalkDuration;
        currentNode.SetOccupied(true);
    }

    protected virtual void OnUnitDied(BaseEntity diedUnity) { }



    protected void FindTarget()
    {
        var allEnemies = GameManager.Instance.GetEntitiesAgainst(myTeam);
        float minDistance = Mathf.Infinity;
        BaseEntity entity = null;
        foreach (BaseEntity e in allEnemies)
        {
            if (Vector3.Distance(e.transform.position, this.transform.position) <= minDistance)
            {
                minDistance = Vector3.Distance(e.transform.position, this.transform.position);
                entity = e;
            }
        }

        currentTarget = entity;
    }

    protected bool MoveTowards(Node nextNode)
    {
        Vector3 direction = (nextNode.worldPosition - this.transform.position);
        if(direction.sqrMagnitude <= 0.005f)
        {
            transform.position = nextNode.worldPosition;
            animator.SetBool("walking", false);
            return true;
        }
        animator.SetBool("walking", true);

        this.transform.position += direction.normalized * movementSpeed * Time.deltaTime;
        return false;
    }

    protected void GetInRange()
    {
        if (currentTarget == null)
            return;

        if(!moving)
        {
            destination = null;
            List<Node> candidates = GridManager.Instance.GetNodesCloseTo(currentTarget.CurrentNode);
            candidates = candidates.OrderBy(x => Vector3.Distance(x.worldPosition, this.transform.position)).ToList();
            for(int i = 0; i < candidates.Count;i++)
            {
                if (!candidates[i].IsOccupied)
                {
                    destination = candidates[i];
                    break;
                }
            }
            if (destination == null)
                return;

            var path = GridManager.Instance.GetPath(currentNode, destination);
            if (path == null && path.Count >= 1)
                return;

            if (path[1].IsOccupied)
                return;

            path[1].SetOccupied(true);
            destination = path[1];            
        }
        
        moving = !MoveTowards(destination);
        if(!moving)
        {
            //Free previous node
            currentNode.SetOccupied(false);
            SetCurrentNode(destination);
        }
    }

    public void SetCurrentNode(Node node)
    {
        currentNode = node;
    }
    
    public void TakeDamage(int amount)
    {
        baseHealth -= amount;
        healthbar.UpdateBar(baseHealth);

        if(baseHealth <= 0 && !dead)
        {
            dead = true;
            currentNode.SetOccupied(false);
            GameManager.Instance.UnitDead(this);
            RemoveListeners();
        }
    }

    protected virtual void Attack()
    {
        if (!canAttack)
            return;

        animator.SetTrigger("attack");

        waitBetweenAttack = 1 / attackSpeed;
        StartCoroutine(WaitCoroutine());
    }

    IEnumerator WaitCoroutine()
    {
        canAttack = false;
        yield return null;
        animator.ResetTrigger("attack");
        yield return new WaitForSeconds(waitBetweenAttack);
        canAttack = true;
    }
}

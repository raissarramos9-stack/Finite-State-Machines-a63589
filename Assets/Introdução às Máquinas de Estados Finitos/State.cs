
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class State
{
    public enum STATE
    {
        IDLE,
        PATROL,
        PURSUE,
        ATTACK,
        SLEEP,
        RUNAWAY
    }

    public enum EVENT
    {
        ENTER,
        UPDATE,
        EXIT
    }

    public STATE name;
    protected EVENT stage;
    protected GameObject npc;
    protected NavMeshAgent agent;
    protected Animator anim;
    protected Transform player;
    protected State nextState;

    float visDist = 10.0f;
    float visAngle = 30.0f;
    float shootDist = 7.0f;

    public State(GameObject _npc, NavMeshAgent _agent,
        Animator _anim, Transform _player)
    {
        npc = _npc;
        agent = _agent;
        anim = _anim;
        player = _player;
        stage = EVENT.ENTER;
    }

    public virtual void Enter()
    {
        stage = EVENT.UPDATE;
    }

    public virtual void Update()
    {
        stage = EVENT.UPDATE;
    }

    public virtual void Exit()
    {
        stage = EVENT.EXIT;
    }

    public State Process()
    {
        if (stage == EVENT.ENTER)
            Enter();

        if (stage == EVENT.UPDATE)
            Update();

        if (stage == EVENT.EXIT)
        {
            Exit();
            return nextState;
        }

        return this;
    }

    public bool CanSeePlayer()
    {
        if (player == null || npc == null)
            return false;

        Vector3 direction =
            player.position - npc.transform.position;

        float angle =
            Vector3.Angle(direction, npc.transform.forward);

        return direction.magnitude < visDist &&
               angle < visAngle;
    }

    public bool IsPlayerBehind()
    {
        if (player == null || npc == null)
            return false;

        Vector3 direction =
            npc.transform.position - player.position;

        float angle =
            Vector3.Angle(direction, npc.transform.forward);

        return direction.magnitude < 2.0f &&
               angle < visAngle;
    }

    public bool CanAttackPlayer()
    {
        if (player == null || npc == null)
            return false;

        Vector3 direction =
            player.position - npc.transform.position;

        return direction.magnitude < shootDist;
    }

    protected bool AgentIsReady()
    {
        return agent != null &&
               agent.enabled &&
               agent.isOnNavMesh;
    }
}

public class Idle : State
{
    public Idle(GameObject _npc, NavMeshAgent _agent,
        Animator _anim, Transform _player)
        : base(_npc, _agent, _anim, _player)
    {
        name = STATE.IDLE;
    }

    public override void Enter()
    {
        if (anim != null)
            anim.SetTrigger("isIdle");

        base.Enter();
    }

    public override void Update()
    {
        if (CanSeePlayer())
        {
            nextState = new Pursue(npc, agent, anim, player);
            stage = EVENT.EXIT;
        }
        else if (Random.Range(0, 100) < 10)
        {
            nextState = new Patrol(npc, agent, anim, player);
            stage = EVENT.EXIT;
        }
    }

    public override void Exit()
    {
        if (anim != null)
            anim.ResetTrigger("isIdle");

        base.Exit();
    }
}

public class Patrol : State
{
    int currentIndex = -1;

    public Patrol(GameObject _npc, NavMeshAgent _agent,
        Animator _anim, Transform _player)
        : base(_npc, _agent, _anim, _player)
    {
        name = STATE.PATROL;

        if (AgentIsReady())
        {
            agent.speed = 2.0f;
            agent.isStopped = false;
        }
    }

    public override void Enter()
    {
        currentIndex = -1;

        if (anim != null)
            anim.SetTrigger("isWalking");

        base.Enter();
    }

    public override void Update()
    {
        if (AgentIsReady())
        {
            if (!agent.pathPending &&
                (!agent.hasPath || agent.remainingDistance < 1.0f))
            {
                if (GameEnvironment.Singleton != null &&
                    GameEnvironment.Singleton.Checkpoints != null &&
                    GameEnvironment.Singleton.Checkpoints.Count > 0)
                {
                    currentIndex++;

                    if (currentIndex >=
                        GameEnvironment.Singleton.Checkpoints.Count)
                    {
                        currentIndex = 0;
                    }

                    Transform checkpoint =
                        GameEnvironment.Singleton
                        .Checkpoints[currentIndex].transform;

                    if (checkpoint != null)
                    {
                        agent.SetDestination(checkpoint.position);
                    }
                }
            }
        }

        if (CanSeePlayer())
        {
            nextState = new Pursue(npc, agent, anim, player);
            stage = EVENT.EXIT;
        }
        else if (IsPlayerBehind())
        {
            nextState = new RunAway(npc, agent, anim, player);
            stage = EVENT.EXIT;
        }
    }

    public override void Exit()
    {
        if (anim != null)
            anim.ResetTrigger("isWalking");

        base.Exit();
    }
}

public class Pursue : State
{
    public Pursue(GameObject _npc, NavMeshAgent _agent,
        Animator _anim, Transform _player)
        : base(_npc, _agent, _anim, _player)
    {
        name = STATE.PURSUE;

        if (AgentIsReady())
        {
            agent.speed = 5.0f;
            agent.isStopped = false;
        }
    }

    public override void Enter()
    {
        if (anim != null)
            anim.SetTrigger("isRunning");

        base.Enter();
    }

    public override void Update()
    {
        if (AgentIsReady() && player != null)
        {
            agent.SetDestination(player.position);

            if (agent.hasPath)
            {
                if (CanAttackPlayer())
                {
                    nextState = new Attack(npc, agent, anim, player);
                    stage = EVENT.EXIT;
                }
                else if (!CanSeePlayer())
                {
                    nextState = new Patrol(npc, agent, anim, player);
                    stage = EVENT.EXIT;
                }
            }
        }
    }

    public override void Exit()
    {
        if (anim != null)
            anim.ResetTrigger("isRunning");

        base.Exit();
    }
}

public class Attack : State
{
    float rotationSpeed = 2.0f;
    AudioSource shoot;

    public Attack(GameObject _npc, NavMeshAgent _agent,
        Animator _anim, Transform _player)
        : base(_npc, _agent, _anim, _player)
    {
        name = STATE.ATTACK;

        if (npc != null)
            shoot = npc.GetComponent<AudioSource>();
    }

    public override void Enter()
    {
        if (anim != null)
            anim.SetTrigger("isShooting");

        if (AgentIsReady())
            agent.isStopped = true;

        if (shoot != null)
            shoot.Play();

        base.Enter();
    }

    public override void Update()
    {
        if (npc == null || player == null)
            return;

        Vector3 direction =
            player.position - npc.transform.position;

        direction.y = 0.0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            npc.transform.rotation = Quaternion.Slerp(
                npc.transform.rotation,
                Quaternion.LookRotation(direction),
                Time.deltaTime * rotationSpeed
            );
        }

        if (!CanAttackPlayer())
        {
            if (AgentIsReady())
                agent.isStopped = false;

            nextState = new Pursue(npc, agent, anim, player);
            stage = EVENT.EXIT;
        }
    }

    public override void Exit()
    {
        if (anim != null)
            anim.ResetTrigger("isShooting");

        if (shoot != null && shoot.isPlaying)
            shoot.Stop();

        base.Exit();
    }
}

public class RunAway : State
{
    GameObject safeLocation;

    public RunAway(GameObject _npc, NavMeshAgent _agent,
        Animator _anim, Transform _player)
        : base(_npc, _agent, _anim, _player)
    {
        name = STATE.RUNAWAY;
        safeLocation = GameObject.FindGameObjectWithTag("Safe");
    }

    public override void Enter()
    {
        if (anim != null)
            anim.SetTrigger("isRunning");

        if (AgentIsReady() && safeLocation != null)
        {
            agent.isStopped = false;
            agent.speed = 6.0f;
            agent.SetDestination(safeLocation.transform.position);
        }
        else if (safeLocation == null)
        {
            Debug.LogError(
                "Não foi encontrado nenhum objeto com a tag Safe."
            );
        }

        base.Enter();
    }

    public override void Update()
    {
        if (!AgentIsReady())
            return;

        if (!agent.pathPending &&
            agent.hasPath &&
            agent.remainingDistance < 1.0f)
        {
            nextState = new Idle(npc, agent, anim, player);
            stage = EVENT.EXIT;
        }
    }

    public override void Exit()
    {
        if (anim != null)
            anim.ResetTrigger("isRunning");

        base.Exit();
    }
}

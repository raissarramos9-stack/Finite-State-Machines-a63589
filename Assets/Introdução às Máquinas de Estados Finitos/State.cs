
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class State
{
    public enum STATE
    {
        IDLE, PATROL, PURSUE, ATTACK, SLEEP
    }

    public enum EVENT
    {
        ENTER, UPDATE, EXIT
    }

    public STATE name;
    protected EVENT stage;
    protected GameObject npc;
    protected Animator anim;
    protected Transform player;
    protected State nextState;
    protected NavMeshAgent agent;

    float visDist = 10.0f;
    float visAngle = 30.0f;
    float shootDist = 7.0f;

    public State(GameObject _npc, NavMeshAgent _agent,
                 Animator _anim, Transform _player)
    {
        npc = _npc;
        agent = _agent;
        anim = _anim;
        stage = EVENT.ENTER;
        player = _player;
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
        anim.SetTrigger("isIdle");
        base.Enter();
    }

    public override void Update()
    {
        if (Random.Range(0, 100) < 10)
        {
            nextState = new Patrol(npc, agent, anim, player);
            stage = EVENT.EXIT;
        }

       
    }

    public override void Exit()
    {
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
        agent.speed = 2;
        if (agent != null && agent.isOnNavMesh)
{
    agent.isStopped = false;
}
    }

    
public override void Enter()
{
    currentIndex = 0;

    if (agent != null && agent.isOnNavMesh &&
        GameEnvironment.Singleton != null &&
        GameEnvironment.Singleton.Checkpoints != null &&
        GameEnvironment.Singleton.Checkpoints.Count > 0)
    {
        agent.isStopped = false;

        agent.SetDestination(
            GameEnvironment.Singleton.Checkpoints[currentIndex].transform.position
        );

        currentIndex++;
    }

    anim.SetTrigger("isWalking");
    base.Enter();
}


   
public override void Update()
{
    if (agent == null || !agent.isOnNavMesh)
        return;

    if (GameEnvironment.Singleton == null ||
        GameEnvironment.Singleton.Checkpoints == null ||
        GameEnvironment.Singleton.Checkpoints.Count == 0)
        return;

    if (!agent.pathPending && agent.remainingDistance < 1)
    {
        if (currentIndex >= GameEnvironment.Singleton.Checkpoints.Count)
        {
            currentIndex = 0;
        }

        agent.SetDestination(
            GameEnvironment.Singleton.Checkpoints[currentIndex].transform.position
        );

        currentIndex++;
    }
}


    public override void Exit()
    {
        anim.ResetTrigger("isWalking");
        base.Exit();
    }
}

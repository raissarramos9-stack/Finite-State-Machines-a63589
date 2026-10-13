
using UnityEngine;
using UnityEngine.AI;

public class AI : MonoBehaviour
{
    NavMeshAgent agent;
    Animator anim;
    State currentState;

    public Transform player;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        if (agent == null)
        {
            Debug.LogError("O Ugg não tem NavMeshAgent!");
            enabled = false;
            return;
        }

        if (anim == null)
        {
            Debug.LogError("O Ugg não tem Animator!");
            enabled = false;
            return;
        }

        if (player == null)
        {
            Debug.LogError("O jogador não está atribuído no campo Player!");
            enabled = false;
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError(
                "O Ugg não está sobre o NavMesh!"
            );
            enabled = false;
            return;
        }

        currentState = new Idle(
            gameObject, agent, anim, player
        );
    }

    void Update()
    {
        if (currentState == null)
            return;

        currentState = currentState.Process();

        if (currentState == null)
        {
            Debug.LogError("O estado atual do Ugg é null!");
            enabled = false;
            return;
        }

        if (agent.isOnNavMesh)
        {
            Debug.Log(
                "Estado: " + currentState.name +
                " | Velocidade: " + agent.velocity.magnitude
            );
        }
    }
}

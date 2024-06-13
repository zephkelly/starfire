public interface IState
{
    void Enter();
    void Execute();
    void FixedUpdate();
    void Exit();
}

public class StateMachine
{
    public IState currentState { get; private set; }

    public void ChangeState(IState newState)
    {
        if(currentState != null)
        {
            currentState.Exit();
        }
        
        currentState = newState;
        currentState.Enter();
    }

    public void Update()
    {
        if(currentState != null)
        {
            currentState.Execute();
        }
    }

    public void FixedUpdate()
    {
        if(currentState != null)
        {
            currentState.FixedUpdate();
        }
    }
}
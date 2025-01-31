namespace BlazorState;

public abstract class ActionHandler<TAction> : IRequestHandler<TAction>
where TAction : IAction
{
  public ActionHandler(IStore aStore)
  {
    Store = aStore;
  }

  protected IStore Store { get; set; }

  public abstract Task Handle(TAction aAction, CancellationToken aCancellationToken);
}

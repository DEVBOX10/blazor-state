namespace BlazorState.Pipeline.State;

using AnyClone;
using BlazorState;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

internal class CloneStateBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
  private readonly ILogger Logger;
  private readonly IMediator Mediator;
  private readonly BlazorStateOptions BlazorStateOptions;
  private readonly IStore Store;
  private bool IsClientSide;

  public CloneStateBehavior
  (
    ILogger<CloneStateBehavior<TRequest, TResponse>> aLogger,
    BlazorStateOptions aBlazorStateOptions,
    IStore aStore,
    IMediator aMediator)
  {
    Logger = aLogger;
    Logger.LogDebug($"{GetType().Name} constructor");
    BlazorStateOptions = aBlazorStateOptions;
    Store = aStore;
    Mediator = aMediator;
  }

  public async Task<TResponse> Handle
  (
    TRequest aRequest,
    CancellationToken aCancellationToken,
    RequestHandlerDelegate<TResponse> aNext
  )
  {
    Type declaringType = typeof(TRequest).DeclaringType;
    // logging variables
    string className = GetType().Name;
    className = className.Remove(className.IndexOf('`'));
    string requestId = $"{aRequest.GetType().FullName}:{aRequest.GetHashCode()}";

    Logger.LogDebug($"Pipeline Start: {requestId}");
    Logger.LogDebug($"{className}: Start");

    IState originalState = default;
    // Constrain here if not IState then ignore.
    if (typeof(IState).IsAssignableFrom(declaringType))
    {
      IsClientSide = true;
      Logger.LogDebug($"{className}: Clone State of type {declaringType}");
      originalState = Store.GetState(declaringType) as IState;
      Logger.LogDebug($"{className}: originalState.Guid:{originalState.Guid}");
      IState newState = (originalState is ICloneable clonable) ? (IState)clonable.Clone() : originalState.Clone();
      Logger.LogDebug($"{className}: newState.Guid:{newState.Guid}");
      Store.SetState(newState as IState);
    }
    else
    {
      Logger.LogDebug($"{className}: Not cloning State because {declaringType} is not an IState");
    }

    try
    {
      Logger.LogDebug($"{className}: Call next");
      TResponse response = await aNext();
      Logger.LogDebug($"{className}: Start Post Processing");
      Logger.LogDebug($"{className}: End Post Processing");
      Logger.LogDebug($"Pipeline End: {requestId}");
      return response;
    }
    catch (Exception aException)
    {
      // If something fails we restore system to previous state.
      // One may consider extension point here for error handling.
      // Update some error state so the user knows of the failure.
      // But as a rule if this is an exception it should be unexpected.
      Logger.LogWarning($"{className}: Error: {aException.Message}");
      Logger.LogWarning($"{className}: InnerError: {aException?.InnerException?.Message}");
      Logger.LogWarning($"{className}: Restoring State of type: {declaringType}");

      if (IsClientSide && originalState != null)
      {
        Store.SetState(originalState as IState);

        var exceptionNotification = new ExceptionNotification
        {
          //Request = aRequest,
          RequestName = className,
          Exception = aException
        };

        await Mediator.Publish(exceptionNotification).ConfigureAwait(false);
        return default;
      }

      throw;
    }
  }
}

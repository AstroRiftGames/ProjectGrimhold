/// <summary>
/// Interface representing an entity that can be interacted with.
/// </summary>
public interface IInteractable : IEntity
{
    /// <summary>
    /// Checks whether the interaction can proceed under the given request conditions.
    /// </summary>
    bool CanInteract(in InteractionRequest request);

    /// <summary>
    /// Executes the interaction and returns the outcome.
    /// </summary>
    InteractionResult Interact(in InteractionRequest request);
}

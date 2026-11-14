/// <summary>Object an agent can focus and interact with.</summary>
public interface IInteractable
{
    /// <summary>Runs the interaction.</summary>
    void Interact();

    /// <summary>Marks the object as focused.</summary>
    void Focus();

    /// <summary>Clears the focus of the object.</summary>
    void Unfocus();

    /// <summary>Returns true when an agent with viewRadius can interact with the object.</summary>
    /// <param name="priority">Priority of the object among the interactable objects.</param>
    bool CanInteract(float viewRadius, out float priority);
}

namespace Blackout.Interaction
{
    /// <summary>
    /// Something the player can look at and use with the Interact action.
    /// Implement on a component that sits on (or above) the collider the player aims at.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Short action text for a future on-screen prompt, e.g. "Pick up Cable".</summary>
        string Prompt { get; }

        void Interact(PlayerInteractor interactor);
    }
}

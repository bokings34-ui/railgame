namespace Railgame.Hansol.ShoulderView
{
    public interface IShoulderInteractable
    {
        string InteractionPrompt { get; }
        bool CanInteract { get; }
        void Interact(ShoulderInteractor interactor);
    }
}

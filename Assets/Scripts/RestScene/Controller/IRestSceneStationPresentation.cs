/// <summary>Connects the station sign to the controller's existing sprite outline.</summary>
public interface IRestSceneStationPresentation
{
    bool HasInteractionFocus { get; }
    void SetPresentationFocus(bool focused);
}

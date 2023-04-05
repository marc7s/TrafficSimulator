namespace DataModel
{
    /// <summary> Determines what to do when the end of a road is reached </summary>
    public enum RoadEndBehaviour
    {
        Loop,
        Stop
    }
    public enum TurnDirection
    {
        Left = -1,
        Straight = 0,
        Right = 1
    }
}
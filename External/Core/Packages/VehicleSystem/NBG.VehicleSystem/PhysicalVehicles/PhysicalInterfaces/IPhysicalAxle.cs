namespace NBG.VehicleSystem
{
    public interface IPhysicalAxle : IAxle
    {
        ref readonly PhysicalAxleSettings Settings { get; }
    }
}

namespace NBG.VehicleSystem
{
    public interface IEngineAttachment
    {
        void TransmitDriveShaftPower(float speedRads, float torqueNm);
    }
}

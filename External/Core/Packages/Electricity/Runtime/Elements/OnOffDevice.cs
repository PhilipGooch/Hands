namespace NBG.Electricity
{
    public class OnOffDevice : ElectricityReceiver
    {
        public float requiredAmperes;
        public bool isOn;
        public override void Tick()
        {
            isOn = Input >= requiredAmperes;
        }
    }
}

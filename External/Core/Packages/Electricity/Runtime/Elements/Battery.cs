namespace NBG.Electricity
{
    public class Battery : ElectricityProvider
    {
        public ElectricityReceiver inputSocket;

        public float outputAmperes;
        public float maxStorage;
        public float storage;
        protected override void Start()
        {
            base.Start();
            Electricity.CreateDependency(this, inputSocket);
        }
        public override void Tick()
        {
            storage += inputSocket.Input;
            if (storage > maxStorage)
                storage = maxStorage;

            if (Electricity.IsConnected(this))
            {
                if (storage >= outputAmperes)
                {
                    Output = outputAmperes;
                    storage -= outputAmperes;
                }
                else
                {
                    Output = 0;
                }
            }
        }
    }
}

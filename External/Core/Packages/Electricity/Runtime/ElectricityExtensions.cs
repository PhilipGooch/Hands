namespace NBG.Electricity
{
    public static class ElectricityExtensions
    {
        public static void Register(this IProvider provider)
        {
            Electricity.Instance.Register(provider);
        }

        public static void Register(this IReceiver receiver)
        {
            Electricity.Instance.Register(receiver);
        }
    }
}

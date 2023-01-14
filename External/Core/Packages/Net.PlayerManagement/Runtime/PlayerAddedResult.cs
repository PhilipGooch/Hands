namespace NBG.Net.PlayerManagement
{
    public struct PlayerAddedResultData
    {
        public PlayerAddResult result;
        public int localID;
        public int globalID;

        public override string ToString()
        {
            return $"PlayerAddedResult: {result}, localID {localID}, globalID {globalID}";
        }
    }
}
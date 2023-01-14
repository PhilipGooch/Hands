namespace Noodles.Animation
{
    public struct SelectedKey
    {
        public int track;
        public int time;

        public SelectedKey(int track, int time)
        {
            this.track = track;
            this.time = time;
        }
    }
}

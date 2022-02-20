using GameKit.Ads;

namespace GameKit.AdMob
{
    public struct AdMobUnitInfo: IAdInfo
    {
        public AdMobUnitInfo(string name, int floor)
        {
            Name = name;
            Floor = floor;
        }

        public string Name { get; }
        public int Floor { get; }
    }
}
using System;
using GameKit.Ads;
using GameKit.Ads.Units;
using GoogleMobileAds.Api;

namespace GameKit.AdMob
{
    [Serializable]
    internal class RewardedUnit : AdmobUnit<RewardedAd>, IRewardedVideoAdUnit
    {
        public override void Show()
        {
            if (Logger<AdMobNetwork>.IsDebugAllowed) Logger<AdMobNetwork>.Debug($"{Name} is showing");
            Instance.Show();
        }

        protected override void Initialize()
        {
            Instance.OnAdClosed += OnAdClosed;
            Instance.OnAdLoaded += OnAdLoaded;
            Instance.OnAdFailedToLoad += OnAdFailedToLoad;
            Instance.OnAdFailedToShow += OnAdFailedToShow;
            Instance.OnUserEarnedReward += OnEarnedReward;
            Instance.OnAdOpening += OnAdDisplayed;
        }

        private void OnEarnedReward(object sender, Reward e)
        {
            if (Logger<AdMobNetwork>.IsDebugAllowed) Logger<AdMobNetwork>.Debug($"{Name} is earned");
            IsEarned = true;
            if (Reward is null) Reward = new DefaultRewardAdInfo((int)e.Amount, e.Type);
        }

        public override void Release()
        {
            base.Release();
            if (Instance == null) return;
            Instance.OnAdClosed -= OnAdClosed;
            Instance.OnAdLoaded -= OnAdLoaded;
            Instance.OnAdFailedToLoad -= OnAdFailedToLoad;
            Instance.OnAdFailedToShow -= OnAdFailedToShow;
            Instance.OnUserEarnedReward -= OnEarnedReward;
            Instance.OnAdOpening -= OnAdDisplayed;
            Instance = null;
        }

        public override bool Load(AdRequest request)
        {
            Instance = new RewardedAd(Key);
            if (Logger<AdMobNetwork>.IsDebugAllowed) Logger<AdMobNetwork>.Debug($"{Name} is loading");
            State = AdUnitState.Loading;
            Instance.LoadAd(request);
            return true;
        }

        public RewardedUnit(AdUnitConfig config) : base(config) { }
        public bool IsEarned { get; private set; }
        public IRewardAdInfo Reward { get; set; }
    }
}
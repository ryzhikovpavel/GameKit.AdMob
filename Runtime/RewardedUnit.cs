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
            base.Show();
            Instance.Show(OnEarnedReward);
        }

        protected override void Initialize()
        {
            Instance.OnAdFullScreenContentClosed += OnAdClosed;
            Instance.OnAdFullScreenContentFailed += OnAdFailedToShow; 
        }

        public override void Release()
        {
            if (Instance != null)
            {
                Instance.OnAdFullScreenContentClosed += OnAdClosed;
                Instance.OnAdFullScreenContentFailed += OnAdFailedToShow;
                Instance = null;
            }

            base.Release();
        }

        private void OnEarnedReward(Reward e)
        {
            if (Logger<AdMobNetwork>.IsDebugAllowed) Logger<AdMobNetwork>.Debug($"{Name} is earned");
            IsEarned = true;
            if (Reward is null) Reward = new DefaultRewardAdInfo((int)e.Amount, e.Type);
        }

        public override bool Load(AdRequest request)
        {
            if (Logger<AdMobNetwork>.IsDebugAllowed) Logger<AdMobNetwork>.Debug($"{Name} is loading");
            State = AdUnitState.Loading;
            RewardedAd.Load(Key,request, OnLoadCompleted);
            return true;
        }

        public RewardedUnit(AdUnitConfig config) : base(config) { }

        public bool IsEarned { get; private set; }

        public IRewardAdInfo Reward { get; set; }
    }
}
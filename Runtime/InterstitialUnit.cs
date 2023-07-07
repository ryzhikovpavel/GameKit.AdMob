using System;
using GameKit.Ads.Units;
using GoogleMobileAds.Api;

namespace GameKit.AdMob
{
    [Serializable]
    internal class InterstitialUnit : AdmobUnit<InterstitialAd>, IInterstitialAdUnit
    {
        protected override void Initialize()
        {
            Instance.OnAdFullScreenContentClosed += OnAdClosed;
            Instance.OnAdFullScreenContentFailed += OnAdFailedToShow;
        }
        
        public override void Release()
        {
            if (Instance != null)
            {
                Instance.OnAdFullScreenContentClosed -= OnAdClosed;
                Instance.OnAdFullScreenContentFailed -= OnAdFailedToShow;
                Instance.Destroy();
                Instance = null;
            }

            base.Release();
        }

        public override bool Load(AdRequest request)
        {
            if (Logger<AdMobNetwork>.IsDebugAllowed) Logger<AdMobNetwork>.Debug($"{Name} is loading");
            State = AdUnitState.Loading;
            InterstitialAd.Load(Key, request, OnLoadCompleted);
            return true;
        }

        public override void Show()
        {
            base.Show();
            Instance.Show();
        }

        public InterstitialUnit(AdUnitConfig config) : base(config) { }
    }
}
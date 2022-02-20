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
            Instance.OnAdClosed += OnAdClosed;
            Instance.OnAdLoaded += OnAdLoaded;
            Instance.OnAdFailedToLoad += OnAdFailedToLoad;
            Instance.OnAdFailedToShow += OnAdFailedToShow;
            Instance.OnAdOpening += OnAdDisplayed;
        }
        
        public override void Release()
        {
            base.Release();
            if (Instance == null) return;
            Instance.OnAdClosed -= OnAdClosed;
            Instance.OnAdLoaded -= OnAdLoaded;
            Instance.OnAdFailedToLoad -= OnAdFailedToLoad;
            Instance.OnAdFailedToShow -= OnAdFailedToShow;
            Instance.OnAdOpening -= OnAdDisplayed;
            Instance.Destroy();
            Instance = null;
        }

        public override bool Load(AdRequest request)
        {
            Instance = new InterstitialAd(Key);
            if (Logger<AdMobNetwork>.IsDebugAllowed) Logger<AdMobNetwork>.Debug($"{Name} is loading");
            State = AdUnitState.Loading;
            Instance.LoadAd(request);
            return true;
        }
        
        public override void Show()
        {
            if (Logger<AdMobNetwork>.IsDebugAllowed) Logger<AdMobNetwork>.Debug($"{Name} is showing");
            Instance.Show();
        }

        public InterstitialUnit(AdUnitConfig config) : base(config) { }
    }
}
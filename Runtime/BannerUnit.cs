using System;
using GameKit.Ads;
using GameKit.Ads.Units;
using GoogleMobileAds.Api;

namespace GameKit.AdMob
{
    [Serializable]
    internal class BannerUnit : AdmobUnit<BannerView>, ITopSmartBannerAdUnit, IBottomSmartBannerAdUnit
    {
        private readonly AdPosition _position;
        public event Action EventClicked;
        
        protected override void Initialize()
        {
            Instance.OnAdFullScreenContentClosed += OnAdClosed;
            Instance.OnBannerAdLoaded += OnAdLoaded;
            Instance.OnBannerAdLoadFailed += OnAdFailedToLoad;
            Instance.OnAdClicked += OnAdClicked;
        }

        public override void Release()
        {
            if (Instance is not null)
            {
                Instance.OnAdFullScreenContentClosed -= OnAdClosed;
                Instance.OnBannerAdLoaded -= OnAdLoaded;
                Instance.OnBannerAdLoadFailed -= OnAdFailedToLoad;
                Instance.OnAdClicked -= OnAdClicked;
                Instance.Destroy();
                Instance = null;
            }

            base.Release();
        }

        protected void OnAdClicked()
        {
            State = AdUnitState.Clicked;
            EventClicked?.Invoke();
        }

        public override bool Load(AdRequest request)
        {
            if (Instance != null) Release();
            if (Logger<AdMobNetwork>.IsDebugAllowed) Logger<AdMobNetwork>.Debug($"{Name} is loading");
            Instance = new BannerView(Key, AdSize.SmartBanner, _position);
            //Instance.SetPosition(_position);
            State = AdUnitState.Loading;
            Instance.LoadAd(request);
            Instance.Hide();
            return true;
        }

        public override void Show()
        {
            base.Show();
            Instance.Show();
        }

        public void Hide()
        {
            Instance?.Hide();
            State = AdUnitState.Closed;
        }

        public BannerUnit(AdUnitConfig config, AdPosition position) : base(config)
        {
            _position = position;
        }
    }
}
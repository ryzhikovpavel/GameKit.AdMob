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
            Instance.OnAdClosed += OnAdClosed;
            Instance.OnAdLoaded += OnAdLoaded;
            Instance.OnAdFailedToLoad += OnAdFailedToLoad;
            Instance.OnAdOpening += OnAdClicked;
        }
        
        protected override void OnAdClicked(object sender, EventArgs eventArgs)
        {
            base.OnAdClicked(sender, eventArgs);
            EventClicked?.Invoke();
        }
        
        public override void Release()
        {
            base.Release();
            if (Instance is null) return;
            Instance.OnAdClosed -= OnAdClosed;
            Instance.OnAdLoaded -= OnAdLoaded;
            Instance.OnAdFailedToLoad -= OnAdFailedToLoad;
            Instance.OnAdOpening -= OnAdClicked;
            Instance.Destroy();
            Instance = null;
        }

        public override bool Load(AdRequest request)
        {
            Instance = new BannerView(Key, AdSize.SmartBanner, AdPosition.Bottom);
            Instance.SetPosition(_position);
            if (Logger<AdMobNetwork>.IsDebugAllowed) Logger<AdMobNetwork>.Debug($"{Name} is loading");
            State = AdUnitState.Loading;
            Instance.LoadAd(request);
            Instance.Hide();
            return true;
        }

        public override void Show()
        {
            Instance.Show();
            base.Show();
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
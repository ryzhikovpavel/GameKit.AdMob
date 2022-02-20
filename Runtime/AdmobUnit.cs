using System;
using GameKit.Ads;
using GameKit.Ads.Units;
using GoogleMobileAds.Api;

namespace GameKit.AdMob
{
    [Serializable]
    internal abstract class AdmobUnit : IAdUnit
    {
        public readonly AdUnitConfig Config;
        public string Name => Config.name;
        public string Key => Config.unitKey;
        public int Attempt { get; set; }
        public abstract bool Load(AdRequest request);
        public DateTime PauseUntilTime;
        public DateTime BestBeforeDate;
        
        public AdUnitState State { get; set; }
        public string Error { get; protected set;}
        public IAdInfo Info { get; }

        public virtual void Show()
        {
            if (Logger<AdMobNetwork>.IsDebugAllowed) Logger<AdMobNetwork>.Debug($"{Name} is Displayed");
            State = AdUnitState.Displayed;
        }

        public virtual void Release()
        {
            if (Logger<AdMobNetwork>.IsDebugAllowed) Logger<AdMobNetwork>.Debug($"{Name} is Released");
            State = AdUnitState.Empty;
        }
        
        public AdmobUnit(AdUnitConfig config)
        {
            this.Config = config;
            Info = new AdMobUnitInfo(config.name, config.priceFloor);
        }
    }
    
    [Serializable]
    internal abstract class AdmobUnit<T> : AdmobUnit
    {
        public T Instance
        {
            get { return _instance; }
            set
            {
                _instance = value;
                if (_instance is null == false) Initialize();
            }
        }

        private T _instance;

        protected abstract void Initialize();
        
        protected virtual void OnAdClosed(object sender, EventArgs eventArgs)
        {
            State = AdUnitState.Closed;
            if (Logger<AdMobNetwork>.IsDebugAllowed) Logger<AdMobNetwork>.Debug($"{Name} is closed");
        }

        protected virtual void OnAdLoaded(object sender, EventArgs eventArgs)
        {
            State = AdUnitState.Loaded;
            Attempt = -1;
            if (Logger<AdMobNetwork>.IsDebugAllowed) Logger<AdMobNetwork>.Debug($"{Name} is loaded");
        }

        protected virtual void OnAdClicked(object sender, EventArgs eventArgs)
        {
            State = AdUnitState.Clicked;
            if (Logger<AdMobNetwork>.IsDebugAllowed) Logger<AdMobNetwork>.Debug($"{Name} is clicked");
        }
        
        protected virtual void OnAdDisplayed(object sender, EventArgs eventArgs)
        {
            State = AdUnitState.Displayed;
            if (Logger<AdMobNetwork>.IsDebugAllowed) Logger<AdMobNetwork>.Debug($"{Name} is Displayed");
        }
        
        protected virtual void OnAdFailedToLoad(object sender, AdFailedToLoadEventArgs e)
        {
            Error = e.LoadAdError.GetMessage();
            State = AdUnitState.Error;
            PauseUntilTime = DateTime.Now.AddSeconds(AdMobNetwork.PauseDelay);
            if (Logger<AdMobNetwork>.IsDebugAllowed) Logger<AdMobNetwork>.Debug($"{Name} load failed");
        }
        
        protected virtual void OnAdFailedToShow(object sender, AdErrorEventArgs e)
        {
            Error = e.AdError.GetMessage();
            State = AdUnitState.Error;
            if (Logger<AdMobNetwork>.IsDebugAllowed) Logger<AdMobNetwork>.Debug($"{Name} is show failed");
        }

        protected AdmobUnit(AdUnitConfig config) : base(config) { }
    }
}
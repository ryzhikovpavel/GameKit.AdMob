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
                if (_instance != null) Release();
                _instance = value;
                if (_instance is null == false) Initialize();
            }
        }

        private T _instance;

        protected abstract void Initialize();
        protected virtual void OnAdClosed()
        {
            State = AdUnitState.Closed;
            if (Logger<AdMobNetwork>.IsDebugAllowed) Logger<AdMobNetwork>.Debug($"{Name} is closed");
        }

        protected virtual void OnAdLoaded()
        {
            State = AdUnitState.Loaded;
            Attempt = -1;
            if (Logger<AdMobNetwork>.IsDebugAllowed) Logger<AdMobNetwork>.Debug($"{Name} is loaded");
        }

        protected virtual void OnAdFailedToLoad(LoadAdError error)
        {
            Error = error.GetMessage();
            State = AdUnitState.Error;
            PauseUntilTime = DateTime.Now.AddSeconds(AdMobNetwork.PauseDelay);
            if (Logger<AdMobNetwork>.IsDebugAllowed) Logger<AdMobNetwork>.Debug($"{Name} load failed with error: {Error}");
        }

        protected virtual void OnAdFailedToShow(AdError error)
        {
            Error = error.GetMessage();
            State = AdUnitState.Error;
            if (Logger<AdMobNetwork>.IsDebugAllowed) Logger<AdMobNetwork>.Debug($"{Name} is show failed with error: {Error}");

        }
        
        protected AdmobUnit(AdUnitConfig config) : base(config) { }

        protected void OnLoadCompleted(T instance, LoadAdError error)
        {
            if (instance == null || error != null)
            {
                OnAdFailedToLoad(error);
            }
            else
            {
                Instance = instance;
                OnAdLoaded();
            }
        }
    }
}
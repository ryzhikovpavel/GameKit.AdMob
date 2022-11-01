using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameKit.Ads;
using GameKit.Ads.Networks;
using GameKit.Ads.Units;
using GoogleMobileAds.Api;
using UnityEngine;
// ReSharper disable RedundantExplicitArrayCreation

namespace GameKit.AdMob
{
    [CreateAssetMenu(fileName = "AdMobConfig", menuName = "GameKit/Ads/AdMob")]
    public class AdMobNetwork: ScriptableObject, IAdsNetwork
    {
        private static ILogger Logger => Logger<AdMobNetwork>.Instance;
        
        internal static int PauseDelay;

        private enum ContentFiltering
        {
            Unspecified,
            G,
            Pg,
            T,
            Ma,
        }

        [SerializeField] 
        private bool autoRegister = true;

        [SerializeField] 
        private UnityEngine.LogType debugLevel = UnityEngine.LogType.Warning;
        
        [SerializeField, Range(1, 3)]
        [Tooltip("Required number of simultaneously loaded banners")]
        private int targetBannerLoaded = 2;

        [SerializeField, Range(2, 10)]
        [Tooltip("Delay between two request load ad banners")]
        private int delayBetweenRequest = 2;

        [SerializeField, Range(15, 300)]
        [Tooltip("Pause between two request load ad instance")]
        private int pauseAfterFailedRequest = 30;
        
        [SerializeField, Range(15, 300)]
        [Tooltip("Pause after click to banner")]
        private int pauseAfterBannerClicked = 30;
        
        //[SerializeField, Range(15, 300)]
        //private int bestBeforeDateTime = 30;
        
        [Header("Extras")]
        [SerializeField]
        private bool testMode;
        [SerializeField]
        private bool initializeOnEditor;
        [SerializeField] 
        private bool enableInterstitial = true;
        [SerializeField] 
        private bool enableRewarded = true;
        [SerializeField] 
        private bool enableBannersTopPosition;
        [SerializeField]
        private bool enableBannersBottomPosition = true;
        
        [Header("Content ratings")] 
        [SerializeField] private TagForUnderAgeOfConsent tagForUnderAgeOfConsent;
        [SerializeField] private TagForChildDirectedTreatment tagForChildDirectedTreatment;
        [SerializeField] private bool isDesignedForFamilies;
        
        [Tooltip("G:3+; PG:7+; T:12; MA:16+" +
                 "More: https://support.google.com/admob/answer/7562142")]
        [SerializeField]
        private ContentFiltering contentRating = ContentFiltering.Unspecified;

        [SerializeField]
        private string[] keywords;

        [Header("Platforms")]
        [SerializeField]
        // ReSharper disable once NotAccessedField.Local
        private PlatformConfig android;
        [SerializeField] 
        // ReSharper disable once NotAccessedField.Local
        private PlatformConfig iOS;
        
        private readonly Dictionary<Type, IAdUnit[]> _units = new Dictionary<Type,  IAdUnit[]>();
        private bool _trackingConsent;
        

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Registration()
        {
            var config = Resources.Load<AdMobNetwork>("AdMobConfig");
            if (config != null && config.autoRegister)
            {
                if (Application.isEditor && config.initializeOnEditor == false) return;
                
                Service<AdsMediator>.Instance.RegisterNetwork(config);
                switch (config.debugLevel)
                {
                    case UnityEngine.LogType.Log:
                        Logger.SetAllowed(LogType.Normal);
                        break;
                    case UnityEngine.LogType.Warning:
                        Logger.SetAllowed(LogType.Important);
                        break;
                    case UnityEngine.LogType.Assert:
                    case UnityEngine.LogType.Error:
                    case UnityEngine.LogType.Exception:
                        Logger.SetAllowed(LogType.Error);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                Logger.Info("Registered");
            }
        }
        
        public TaskRoutine Initialize(bool trackingConsent, bool intrusiveAdUnits)
        {
            _trackingConsent = trackingConsent;
            PauseDelay = pauseAfterFailedRequest;
            return TaskRoutine.Run(Initialize(intrusiveAdUnits));
        }
        
        private IEnumerator Initialize(bool intrusiveAdUnits)
        {
            bool waitAdmob = true;
            MobileAds.Initialize((_)=> waitAdmob = false);
            while (waitAdmob) yield return null;

            var requestConfiguration = MobileAds.GetRequestConfiguration()?.ToBuilder() ?? new RequestConfiguration.Builder();
            requestConfiguration.SetTagForUnderAgeOfConsent(tagForUnderAgeOfConsent);
            requestConfiguration.SetTagForChildDirectedTreatment(tagForChildDirectedTreatment);
            
            switch (contentRating)
            {
                case ContentFiltering.Unspecified:
                    requestConfiguration.SetMaxAdContentRating(MaxAdContentRating.Unspecified);
                    break;
                case ContentFiltering.G:
                    requestConfiguration.SetMaxAdContentRating(MaxAdContentRating.G);
                    break;
                case ContentFiltering.Pg:
                    requestConfiguration.SetMaxAdContentRating(MaxAdContentRating.PG);
                    break;
                case ContentFiltering.T:
                    requestConfiguration.SetMaxAdContentRating(MaxAdContentRating.T);
                    break;
                case ContentFiltering.Ma:
                    requestConfiguration.SetMaxAdContentRating(MaxAdContentRating.MA);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            MobileAds.SetRequestConfiguration(requestConfiguration.build());
            
            PlatformConfig units;
            switch (Application.platform)
            {
                case RuntimePlatform.Android: units = android; break;
                case RuntimePlatform.IPhonePlayer: units = iOS; break;
                default: units = null; testMode = true; break;
            }

            if (testMode) units = GetTestPlatform();

            if (units is null)
            {
                if (Logger.IsErrorAllowed) Logger.Error("Units is null");
                yield break;
            }

            if (units.interstitialUnits.Length > 0 && enableInterstitial && intrusiveAdUnits)
                _units.Add(typeof(IInterstitialAdUnit), InitializeUnits<InterstitialUnit>(units.interstitialUnits));

            if (units.bannerUnits.Length > 0 && intrusiveAdUnits)
            {
                if (enableBannersTopPosition)
                    _units.Add(typeof(ITopSmartBannerAdUnit), InitializeUnits<BannerUnit>(units.bannerUnits, AdPosition.Center));
                if (enableBannersBottomPosition)
                    _units.Add(typeof(IBottomSmartBannerAdUnit), InitializeUnits<BannerUnit>(units.bannerUnits, AdPosition.Bottom));
            }
                
            if (units.rewardedUnits.Length > 0 && enableRewarded)
                _units.Add(typeof(IRewardedVideoAdUnit), InitializeUnits<RewardedUnit>(units.rewardedUnits));
            

            foreach (var banners in _units.Values)
            {
                DownloadHandler(new List<AdmobUnit>(banners.Cast<AdmobUnit>()));
            }
            
            if (_units.TryGetValue(typeof(ITopSmartBannerAdUnit), out var bannerUnits))
                // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
                foreach (BannerUnit u in bannerUnits) u.EventClicked += StartPauseAfterClick;
            if (_units.TryGetValue(typeof(IBottomSmartBannerAdUnit), out bannerUnits))
                // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
                foreach (BannerUnit u in bannerUnits) u.EventClicked += StartPauseAfterClick;
        }
        
        private void StartPauseAfterClick()
        {
            void AppendPause(IAdUnit[] units)
            {
                // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
                foreach (BannerUnit u in units)
                {
                    u.PauseUntilTime = DateTime.Now.AddSeconds(pauseAfterBannerClicked);
                    if (u.State is AdUnitState.Loaded or AdUnitState.Loading)
                        u.Release();
                }
            }
            
            if (_units.TryGetValue(typeof(ITopSmartBannerAdUnit), out var banners)) AppendPause(banners);
            if (_units.TryGetValue(typeof(IBottomSmartBannerAdUnit), out banners)) AppendPause(banners);
        }
        
        private PlatformConfig GetTestPlatform()
        {
            var config = new PlatformConfig();

            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    config.bannerUnits = new AdUnitConfig[]
                    {
                        new AdUnitConfig() { name = "Test Banner 1", unitKey = "ca-app-pub-3940256099942544/6300978111" },
                        new AdUnitConfig() { name = "Test Banner 2", unitKey = "ca-app-pub-3940256099942544/6300978111" }
                    };

                    config.interstitialUnits = new AdUnitConfig[]
                    {
                        new AdUnitConfig() { name = "Test Interstitial", unitKey = "ca-app-pub-3940256099942544/1033173712" }
                    };

                    config.rewardedUnits = new AdUnitConfig[]
                    {
                        new AdUnitConfig() { name = "Test Rewarded", unitKey = "ca-app-pub-3940256099942544/5224354917" }
                    };    
                    break;
                case RuntimePlatform.IPhonePlayer:
                    config.bannerUnits = new AdUnitConfig[]
                    {
                        new AdUnitConfig() { name = "Test Banner 1", unitKey = "ca-app-pub-3940256099942544/2934735716" },
                        new AdUnitConfig() { name = "Test Banner 2", unitKey = "ca-app-pub-3940256099942544/2934735716" }
                    };

                    config.interstitialUnits = new AdUnitConfig[]
                    {
                        new AdUnitConfig() { name = "Test Interstitial", unitKey = "ca-app-pub-3940256099942544/4411468910" }
                    };

                    config.rewardedUnits = new AdUnitConfig[]
                    {
                        new AdUnitConfig() { name = "Test Rewarded", unitKey = "ca-app-pub-3940256099942544/1712485313" }
                    };  
                    break;
                default:
                    // ReSharper disable UseArrayEmptyMethod
                    config.bannerUnits = new AdUnitConfig[0];
                    config.interstitialUnits = new AdUnitConfig[0];
                    config.rewardedUnits = new AdUnitConfig[0];
                    // ReSharper restore UseArrayEmptyMethod
                    break;
            }

            return config;
        }
        
        private IAdUnit[] InitializeUnits<TUnit>(AdUnitConfig[] configs) where TUnit: AdmobUnit
        {
            List<IAdUnit> units = new List<IAdUnit>();
            foreach (var config in configs)
            {
                if (string.IsNullOrEmpty(config.name)) config.name = typeof(TUnit).Name;
                var unit = (TUnit)Activator.CreateInstance(typeof(TUnit), config);
                units.Add(unit);
            }
            
            return units.ToArray();
        }
        
        private IAdUnit[] InitializeUnits<TUnit>(AdUnitConfig[] configs, AdPosition position) where TUnit: BannerUnit
        {
            List<IAdUnit> units = new List<IAdUnit>();
            foreach (var config in configs)
            {
                if (string.IsNullOrEmpty(config.name)) config.name = typeof(TUnit).Name;
                var unit = (TUnit)Activator.CreateInstance(typeof(TUnit), config, position);
                unit.EventClicked += StartPauseAfterClick;
                units.Add(unit);
            }
            
            return units.ToArray();
        }
        
        public bool IsSupported(Type type) => _units.ContainsKey(type);
        public IAdUnit[] GetUnits(Type type) => _units[type];
        
        private AdRequest GetRequest()
        {
            var b = new AdRequest.Builder();
            
            if (isDesignedForFamilies) b.AddExtra("is_designed_for_families", "true");
            if (_trackingConsent == false) b.AddExtra("npa", "1");
            
            if (keywords is null == false && keywords.Length > 0)
                foreach (var k in keywords) b.AddKeyword(k);

            return b.Build();
        }
        
        private async void DownloadHandler(List<AdmobUnit> units)
        {
            if (Logger.IsDebugAllowed) Logger.Debug("Start download handler");
            units.Sort((a,b)=>a.Config.priceFloor.CompareTo(b.Config.priceFloor));
            
            int attempt = 0;

            var request = GetRequest();

            var last = units.Last();
            last.Load(request);

            while (Application.isPlaying)
            {
                var count = 0;
                foreach (var u in units)
                {
                    if (u.State == AdUnitState.Loaded) count++;
                    if (count >= targetBannerLoaded)
                    {
                        attempt++;
                        break;
                    }
                    
                    if (u.State == AdUnitState.Empty && attempt > u.Attempt && u.PauseUntilTime < DateTime.Now)
                    {
                        u.Load(request);
                        u.Attempt = attempt;
                        break;
                    }

                    if (last == u) attempt++;
                }

                await Task.Delay(delayBetweenRequest * 1000);
            }
        }
    }
}
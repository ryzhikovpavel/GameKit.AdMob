using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameKit.Ads;
using GameKit.Ads.Networks;
using GameKit.Ads.Units;
using GoogleMobileAds.Api;
using UnityEngine;

namespace GameKit.AdMob
{
    [CreateAssetMenu(fileName = "AdMobConfig", menuName = "GameKit/Ads/AdMob")]
    public class AdMobNetwork: ScriptableObject, IAdsNetwork
    {
        internal static int PauseDelay;

        private enum ContentFiltering
        {
            Unspecified,
            G,
            PG,
            T,
            MA,
        }

        [SerializeField] 
        private bool autoRegister = true;
        
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
        private bool _purchasedDisableUnits;

        public bool IsInitialized { get; private set; }
        public bool IsValid { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Registration()
        {
            var config = Resources.Load<AdMobNetwork>("AdMobConfig");
            if (config.autoRegister)
            {
                Service<AdsMediator>.Instance.RegisterNetwork(config);
                Logger<AdMobNetwork>.Info("Registered");
            }
        }
        
        public void Initialize(bool trackingConsent, bool purchasedDisableUnits)
        {
            _trackingConsent = trackingConsent;
            _purchasedDisableUnits = purchasedDisableUnits;
            PauseDelay = pauseAfterFailedRequest;
            Loop.StartCoroutine(Initialize());
        }
        
        private IEnumerator Initialize()
        {
            IsInitialized = false;
            bool waitAdmob = true;
            MobileAds.Initialize((_)=> waitAdmob = false);
            while (waitAdmob) yield return null;

            var requestConfiguration = MobileAds.GetRequestConfiguration()?.ToBuilder() ?? new RequestConfiguration.Builder();
            requestConfiguration.SetMaxAdContentRating(MaxAdContentRating.G);
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
                case ContentFiltering.PG:
                    requestConfiguration.SetMaxAdContentRating(MaxAdContentRating.PG);
                    break;
                case ContentFiltering.T:
                    requestConfiguration.SetMaxAdContentRating(MaxAdContentRating.T);
                    break;
                case ContentFiltering.MA:
                    requestConfiguration.SetMaxAdContentRating(MaxAdContentRating.MA);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            MobileAds.SetRequestConfiguration(requestConfiguration.build());
            
#if UNITY_ANDROID
            PlatformConfig units = android;
#elif UNITY_IOS
            PlatformConfig units = ios;
#else
            PlatformConfig units = null;
#endif
            
            if (testMode)
            {
                InitializeTestUnits();
            }
            else
            {
                if (units is null) yield break;

                if (units.interstitialUnits.Length > 0)
                    _units.Add(typeof(IInterstitialAdUnit), InitializeUnits<InterstitialUnit>(units.interstitialUnits));

                if (units.bannerUnits.Length > 0)
                    _units.Add(typeof(IAnchoredBannerAdUnit), InitializeUnits<BannerUnit>(units.bannerUnits));
                
                if (units.rewardedUnits.Length > 0)
                    _units.Add(typeof(IRewardedVideoAdUnit), InitializeUnits<RewardedUnit>(units.rewardedUnits));
            }

            foreach (var banners in _units.Values)
            {
                Loop.StartCoroutine(DownloadHandler(new List<AdmobUnit>(banners.Cast<AdmobUnit>())));
            }
            
            if (_units.TryGetValue(typeof(IAnchoredBannerAdUnit), out var bannerUnits))
                // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
                foreach (BannerUnit u in bannerUnits) u.EventClicked += StartPauseAfterClick;
            
            IsInitialized = true;
            IsValid = true;
        }
        
        private void StartPauseAfterClick()
        {
            if (_units.TryGetValue(typeof(IAnchoredBannerAdUnit), out var units))
            {
                if (Logger<AdMobNetwork>.IsDebugAllowed) Logger<AdMobNetwork>.Debug($"All banners paused after click on {pauseAfterBannerClicked} sec");
                
                // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
                foreach (BannerUnit u in units)
                {
                    u.PauseUntilTime = DateTime.Now.AddSeconds(pauseAfterBannerClicked);
                    if (u.State is AdUnitState.Loaded or AdUnitState.Loading)
                        u.Release();
                }
            }
        }
        
        private void InitializeTestUnits()
        {
            #if UNITY_ANDROID
            _units.Add(typeof(IAnchoredBannerAdUnit), new IAdUnit[]{
                new BannerUnit(new AdUnitConfig() { name = "Test Banner 1", unitKey = "ca-app-pub-3940256099942544/6300978111" }),
                new BannerUnit(new AdUnitConfig() { name = "Test Banner 2", unitKey = "ca-app-pub-3940256099942544/6300978111" })
            });
            
            _units.Add(typeof(IInterstitialAdUnit), new IAdUnit[]{new InterstitialUnit(new AdUnitConfig()
            {
                name = "Test Interstitial",
                unitKey = "ca-app-pub-3940256099942544/1033173712"
            })});
            
            _units.Add(typeof(IRewardedVideoAdUnit), new IAdUnit[]{new RewardedUnit(new AdUnitConfig()
            {
                name = "Test Rewarded",
                unitKey = "ca-app-pub-3940256099942544/5224354917"
            })});
            
            #endif
            
            #if UNITY_IOS

            _units.Add(typeof(IAnchoredBannerAdUnit), new IAdUnit[]{
                new BannerUnit(new AdUnitConfig() { name = "Test Banner 1", unitKey = "ca-app-pub-3940256099942544/2934735716" }),
                new BannerUnit(new AdUnitConfig() { name = "Test Banner 2", unitKey = "ca-app-pub-3940256099942544/2934735716" })
            });
            
            _units.Add(typeof(IInterstitialAdUnit), new IAdUnit[]{new BannerUnit(new AdUnitConfig()
            {
                name = "Test Interstitial",
                unitKey = "ca-app-pub-3940256099942544/4411468910"
            })});
            
            _units.Add(typeof(IRewardedVideoAdUnit), new IAdUnit[]{new RewardedUnit(new AdUnitConfig()
            {
                name = "Test Rewarded",
                unitKey = "ca-app-pub-3940256099942544/1712485313"
            })});
            
            #endif
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
        
        public void DisableUnits()
        {

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
        
        private IEnumerator DownloadHandler(List<AdmobUnit> units)
        {
            units.Sort((a,b)=>a.Config.priceFloor.CompareTo(b.Config.priceFloor));
            
            int attempt = 0;

            var request = GetRequest();
            var delay = new WaitForSecondsRealtime(delayBetweenRequest);

            var last = units.Last();
            last.Load(request);

            yield return null;

            while (Loop.IsQuitting == false)
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

                yield return delay;
            }
        }
    }
}
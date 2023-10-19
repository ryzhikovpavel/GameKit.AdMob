using System.Threading;
using System.Threading.Tasks;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace GameKit.AdMob
{
    public class UmpProvider
    {
        private readonly UmpConsentOperation _operationRequest;

        public UmpProvider(bool tagForUnderAgeOfConsent, DebugGeography debugSettings = DebugGeography.Disabled)
        {
            var request = new ConsentRequestParameters();
            request.ConsentDebugSettings.DebugGeography = debugSettings;
            request.ConsentDebugSettings.TestDeviceHashedIds.Add(SystemInfo.deviceUniqueIdentifier.ToUpper());
            request.TagForUnderAgeOfConsent = tagForUnderAgeOfConsent;

            _operationRequest = new UmpConsentOperation(request);
        }

        public async Task<bool> AskConsentAsync(CancellationToken cancellationToken)
        {
            while (_operationRequest.keepWaiting)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
            }
            
            return _operationRequest.State == UmpConsentOperation.Status.Obtained;;
        }

        public UmpConsentOperation AskConsent()
        {
            return _operationRequest;
        }

        public void ResetConsent()
        {
            ConsentInformation.Reset();
        }
    }
}
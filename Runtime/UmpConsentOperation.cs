using System;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace GameKit.AdMob
{
    public class UmpConsentOperation : CustomYieldInstruction
    {
        public enum Status
        {
            Created,
            Ready,
            Requested,
            Obtained,
            Failed
        }

        public Status State { get; private set; }
        public FormError Error { get; private set; }
        
        internal UmpConsentOperation(ConsentRequestParameters request)
        {
            State = Status.Created;
            Debug.Log($"[Consent] Check the current consent information status.");
            ConsentInformation.Update(request, OnConsentStateUpdated);
        }
        
        private void OnConsentStateUpdated(FormError error)
        {
            if (error != null)
            {
                Failed(error);
                return;
            }
            
            Debug.Log($"[Consent] Check the current consent information status.");
            State = Status.Ready;
        }

        public override bool keepWaiting
        {
            get
            {
                switch (State)
                {
                    case Status.Ready:
                        State = Status.Requested;
                        Debug.Log($"[Consent] LoadAndShowConsentFormIfRequired");
                        ConsentForm.LoadAndShowConsentFormIfRequired(OnConsentRequestFinished);
                        return true;
                    case Status.Created:
                    case Status.Requested:
                        return true;
                    case Status.Obtained:
                    case Status.Failed:
                        return false;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void OnConsentRequestFinished(FormError error)
        {
            if (error != null)
            {
                Failed(error);
                return;
            }

            Debug.Log($"[Consent] Consent obtained");
            State = Status.Obtained;
        }

        private void Failed(FormError error)
        {
            Debug.LogError($"[Consent] Error: {error.ErrorCode} - {error.Message}");
            State = Status.Failed;
            Error = error;
            if (ConsentInformation.CanRequestAds())
            {
                State = Status.Obtained;
            }
        }
    }
}
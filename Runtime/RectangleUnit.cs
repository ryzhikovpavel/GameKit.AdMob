namespace GameKit.AdMob
{
    // [Serializable]
    // public class RectangleUnit : AdmobUnit<BannerView>, IPositionAdUnit
    // {
    //     protected override void Initialize()
    //     {
    //         Instance.OnAdClosed += OnAdClosed;
    //         Instance.OnAdLoaded += OnAdLoaded;
    //         Instance.OnAdFailedToLoad += OnAdFailedToLoad;
    //     }
    //
    //     public override void Dispose()
    //     {
    //         if (Instance == null) return;
    //         Instance.OnAdClosed -= OnAdClosed;
    //         Instance.OnAdLoaded -= OnAdLoaded;
    //         Instance.OnAdFailedToLoad -= OnAdFailedToLoad;
    //         Instance.Destroy();
    //     }
    //
    //     public override bool Create(AdRequest request)
    //     {
    //         if (string.IsNullOrEmpty(Key.value)) return false;
    //
    //         Instance = new BannerView(Key.value, AdSize.MediumRectangle, AdPosition.Center);
    //         Instance.LoadAd(request);
    //         Instance.Hide();
    //         Initialized = true;
    //         return true;
    //     }
    //
    //     public override void Show()
    //     {
    //         Instance?.Show();
    //         base.Show();
    //     }
    //
    //     public void SetPosition(Vector2 position)
    //     {
    //         position = position / Mathf.RoundToInt(Screen.dpi / 160);
    //         position.x -= AdSize.MediumRectangle.Width / 2f;
    //         position.y -= AdSize.MediumRectangle.Height / 2f;
    //         Debug.Log("AdMob Set position: " + position);
    //         Instance?.SetPosition((int)position.x, (int)position.y);
    //     }
    //
    //     public void Hide()
    //     {
    //         Instance?.Hide();
    //         Dispose();
    //         Instance = null;
    //     }
    // }
}
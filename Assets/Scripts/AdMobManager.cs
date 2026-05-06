using UnityEngine;
using GoogleMobileAds.Api;

public class AdMobManager : MonoBehaviour
{
    public static AdMobManager Instance { get; private set; }

    [Header("AdMob Settings")]
    [SerializeField] private bool useTestAds = true;

    [Header("Interstitial Control")]
    [SerializeField] private int gamesBetweenAds = 5;
    [SerializeField] private float minSecondsBetweenAds = 300f; // 5분
    [SerializeField] private int noAdsFirstGames = 3;

    private InterstitialAd interstitialAd;

    private int finishedGameCount = 0;
    private float lastAdShownTime = -99999f;

#if UNITY_ANDROID
    private string interstitialAdUnitId => useTestAds
        ? "ca-app-pub-3940256099942544/1033173712"
        : "ca-app-pub-8502618733998421/4759705022";
#elif UNITY_IOS
    private string interstitialAdUnitId => useTestAds
        ? "ca-app-pub-3940256099942544/4411468910"
        : "ca-app-pub-8502618733998421/5689643310";
#else
    private string interstitialAdUnitId => "unused";
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        MobileAds.Initialize(initStatus =>
        {
            Debug.Log("[AdMob] Initialized");
            LoadInterstitial();
        });
    }

    private void LoadInterstitial()
    {
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }

        AdRequest request = new AdRequest();

        InterstitialAd.Load(interstitialAdUnitId, request, (InterstitialAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning("[AdMob] Interstitial load failed: " + error);
                return;
            }

            interstitialAd = ad;

            interstitialAd.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("[AdMob] Interstitial closed");
                LoadInterstitial();
            };

            interstitialAd.OnAdFullScreenContentFailed += adError =>
            {
                Debug.LogWarning("[AdMob] Interstitial show failed: " + adError);
                LoadInterstitial();
            };

            Debug.Log("[AdMob] Interstitial loaded");
        });
    }

    public void OnGameFinished()
    {
        finishedGameCount++;

        Debug.Log("[AdMob] Finished Game Count: " + finishedGameCount);

        if (CanShowInterstitial())
        {
            ShowInterstitial();
        }
    }

    private bool CanShowInterstitial()
    {
        if (finishedGameCount <= noAdsFirstGames)
            return false;

        if (finishedGameCount % gamesBetweenAds != 0)
            return false;

        if (Time.realtimeSinceStartup - lastAdShownTime < minSecondsBetweenAds)
            return false;

        if (interstitialAd == null)
            return false;

        if (!interstitialAd.CanShowAd())
            return false;

        return true;
    }

    private void ShowInterstitial()
    {
        lastAdShownTime = Time.realtimeSinceStartup;
        interstitialAd.Show();
    }
}
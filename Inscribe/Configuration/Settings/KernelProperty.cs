﻿
namespace Inscribe.Configuration.Settings
{
    public class KernelProperty
    {
        public int ImageGCInitialDelay = 1000 * 60 * 3;

        public int ImageGCInterval = 1000 * 60;

        public int ImageLifetime = 1000 * 60 * 10;

        public int TweetCacheMaxCount = 2000;

        public double TweetCacheSurviveDensity = 0.5;

        public int UserCacheMaxCount = 500;

        public double UserCacheSurviveDensity = 0.5;

        public int ImageCacheMaxCount = 500;

        public double ImageCacheSurviveDensity = 0.5;

        public bool TweetPerpetuation = true;

        public int TweetPerpetuationMaxCount = 20000;
    }
}

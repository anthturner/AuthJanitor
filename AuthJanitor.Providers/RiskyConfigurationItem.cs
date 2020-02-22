using System;

namespace AuthJanitor.Providers
{
    public class RiskyConfigurationItem
    {
        public double Score { get; set; } = 1.0d;
        public string Risk { get; set; }
        public string Recommendation { get; set; }

        public string Summarize()
        {
            return $"[{(int)Math.Round(Score * 100, 0)}]  {Risk}";
        }

        public string WithRecommendation()
        {
            return $"[{(int)Math.Round(Score * 100, 0)}]  {Risk}" + Environment.NewLine + $"{Recommendation}";
        }
    }
}

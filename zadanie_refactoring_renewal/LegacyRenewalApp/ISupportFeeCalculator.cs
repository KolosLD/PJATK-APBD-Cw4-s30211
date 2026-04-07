namespace LegacyRenewalApp
{
    public interface ISupportFeeCalculator
    {
        decimal CalculateSupportFee(string planCode);
    }

    public class SupportFeeCalculator : ISupportFeeCalculator
    {
        public decimal CalculateSupportFee(string planCode)
        {
            return planCode switch
            {
                "START" => 250m,
                "PRO" => 400m,
                "ENTERPRISE" => 700m,
                _ => 0m
            };
        }
    }
}
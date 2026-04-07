using System;

namespace LegacyRenewalApp
{
    public interface IFeeCalculator
    {
        (decimal amount, string note) CalculatePaymentFee(decimal totalAmount, string paymentMethod);
    }

    public class FeeCalculator : IFeeCalculator
    {
        public (decimal amount, string note) CalculatePaymentFee(decimal totalAmount, string paymentMethod)
        {
            return paymentMethod switch
            {
                "CARD" => (totalAmount * 0.02m, "card payment fee; "),
                "BANK_TRANSFER" => (totalAmount * 0.01m, "bank transfer fee; "),
                "PAYPAL" => (totalAmount * 0.035m, "paypal fee; "),
                "INVOICE" => (0m, "invoice payment; "),
                _ => throw new ArgumentException("Unsupported payment method")
            };
        }
    }
}
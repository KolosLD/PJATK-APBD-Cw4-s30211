using System;

namespace LegacyRenewalApp
{
    public class SubscriptionRenewalService
    {
        private readonly CustomerRepository _customerRepo;
        private readonly SubscriptionPlanRepository _planRepo;
        private readonly IBillingService _billing;
        private readonly IDiscountCalculator _discountCalc;
        private readonly ITaxProvider _taxProvider;
        private readonly IFeeCalculator _feeCalculator;
        private readonly ISupportFeeCalculator _supportFeeCalc;
        
        public SubscriptionRenewalService(
            CustomerRepository customerRepo,
            SubscriptionPlanRepository planRepo,
            IBillingService billing,
            IDiscountCalculator discountCalc,
            ITaxProvider taxProvider,
            IFeeCalculator feeCalculator,
            ISupportFeeCalculator supportFeeCalc)
        {
            _customerRepo = customerRepo;
            _planRepo = planRepo;
            _billing = billing;
            _discountCalc = discountCalc;
            _taxProvider = taxProvider;
            _feeCalculator = feeCalculator;
            _supportFeeCalc = supportFeeCalc;
        }

        
        public SubscriptionRenewalService() : this(
            new CustomerRepository(),
            new SubscriptionPlanRepository(),
            new BillingServiceAdapter(),
            new DiscountCalculator(),
            new TaxProvider(),
            new FeeCalculator(),
            new SupportFeeCalculator())
        {
        }

        public RenewalInvoice CreateRenewalInvoice(
            int customerId,
            string planCode,
            int seatCount,
            string paymentMethod,
            bool includePremiumSupport,
            bool useLoyaltyPoints)
        {
            ValidateInput(customerId, planCode, seatCount, paymentMethod);

            string normalizedPlanCode = planCode.Trim().ToUpperInvariant();
            string normalizedPaymentMethod = paymentMethod.Trim().ToUpperInvariant();
            
            var customer = _customerRepo.GetById(customerId);
            var plan = _planRepo.GetByCode(normalizedPlanCode);

            if (!customer.IsActive)
            {
                throw new InvalidOperationException("Inactive customers cannot renew subscriptions");
            }
            
            decimal baseAmount = (plan.MonthlyPricePerSeat * seatCount * 12m) + plan.SetupFee;
            
            var (discountAmount, discountNotes) = _discountCalc.CalculateDiscount(
                customer, plan, seatCount, baseAmount, useLoyaltyPoints);

            decimal subtotalAfterDiscount = baseAmount - discountAmount;
            
            decimal supportFee = includePremiumSupport ? _supportFeeCalc.CalculateSupportFee(normalizedPlanCode) : 0m;
            var (paymentFee, paymentNote) = _feeCalculator.CalculatePaymentFee(subtotalAfterDiscount + supportFee, normalizedPaymentMethod);

            string totalNotes = (discountNotes + (includePremiumSupport ? "premium support included; " : "") + paymentNote).Trim();
            
            decimal taxRate = _taxProvider.GetTaxRate(customer.Country);
            decimal taxBase = subtotalAfterDiscount + supportFee + paymentFee;
            decimal taxAmount = taxBase * taxRate;
            decimal finalAmount = taxBase + taxAmount;
            
            if (finalAmount < 500m)
            {
                finalAmount = 500m;
                totalNotes += " minimum invoice amount applied;";
            }
            
            var invoice = BuildInvoice(customerId, customer.FullName, normalizedPlanCode, normalizedPaymentMethod, 
                                       seatCount, baseAmount, discountAmount, supportFee, paymentFee, taxAmount, 
                                       finalAmount, totalNotes.Trim());
            
            _billing.SaveInvoice(invoice);
            SendNotification(customer, normalizedPlanCode, invoice.FinalAmount);

            return invoice;
        }

        private void ValidateInput(int customerId, string planCode, int seatCount, string paymentMethod)
        {
            if (customerId <= 0) throw new ArgumentException("Customer id must be positive");
            if (string.IsNullOrWhiteSpace(planCode)) throw new ArgumentException("Plan code is required");
            if (seatCount <= 0) throw new ArgumentException("Seat count must be positive");
            if (string.IsNullOrWhiteSpace(paymentMethod)) throw new ArgumentException("Payment method is required");
        }

        private RenewalInvoice BuildInvoice(int customerId, string name, string plan, string method, int seats, 
            decimal baseAmt, decimal discAmt, decimal suppFee, decimal payFee, decimal taxAmt, decimal finalAmt, string notes)
        {
            return new RenewalInvoice
            {
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{customerId}-{plan}",
                CustomerName = name,
                PlanCode = plan,
                PaymentMethod = method,
                SeatCount = seats,
                BaseAmount = Math.Round(baseAmt, 2, MidpointRounding.AwayFromZero),
                DiscountAmount = Math.Round(discAmt, 2, MidpointRounding.AwayFromZero),
                SupportFee = Math.Round(suppFee, 2, MidpointRounding.AwayFromZero),
                PaymentFee = Math.Round(payFee, 2, MidpointRounding.AwayFromZero), 
                TaxAmount = Math.Round(taxAmt, 2, MidpointRounding.AwayFromZero),
                FinalAmount = Math.Round(finalAmt, 2, MidpointRounding.AwayFromZero),
                Notes = notes,
                GeneratedAt = DateTime.UtcNow
            };
        }

        private void SendNotification(Customer customer, string planCode, decimal amount)
        {
            if (!string.IsNullOrWhiteSpace(customer.Email))
            {
                _billing.SendEmail(customer.Email, "Subscription renewal invoice", 
                    $"Hello {customer.FullName}, your renewal for plan {planCode} has been prepared. Final amount: {amount:F2}.");
            }
        }
    }
}
using System;
using System.Text;

namespace LegacyRenewalApp
{
    public interface IDiscountCalculator
    {
        (decimal amount, string notes) CalculateDiscount(
            Customer customer, 
            SubscriptionPlan plan, 
            int seatCount, 
            decimal baseAmount, 
            bool useLoyaltyPoints);
    }

    public class DiscountCalculator : IDiscountCalculator
    {
        public (decimal amount, string notes) CalculateDiscount(
            Customer customer, 
            SubscriptionPlan plan, 
            int seatCount, 
            decimal baseAmount, 
            bool useLoyaltyPoints)
        {
            decimal discountAmount = 0m;
            var notesBuilder = new StringBuilder();

           
            if (customer.Segment == "Silver")
            {
                discountAmount += baseAmount * 0.05m;
                notesBuilder.Append("silver discount; ");
            }
            else if (customer.Segment == "Gold")
            {
                discountAmount += baseAmount * 0.10m;
                notesBuilder.Append("gold discount; ");
            }
            else if (customer.Segment == "Platinum")
            {
                discountAmount += baseAmount * 0.15m;
                notesBuilder.Append("platinum discount; ");
            }
            else if (customer.Segment == "Education" && plan.IsEducationEligible)
            {
                discountAmount += baseAmount * 0.20m;
                notesBuilder.Append("education discount; ");
            }
            
            
            if (customer.YearsWithCompany >= 5)
            {
                discountAmount += baseAmount * 0.07m;
                notesBuilder.Append("long-term loyalty discount; ");
            }
            else if (customer.YearsWithCompany >= 2)
            {
                discountAmount += baseAmount * 0.03m;
                notesBuilder.Append("basic loyalty discount; ");
            }
            
            
            if (seatCount >= 50)
            {
                discountAmount += baseAmount * 0.12m;
                notesBuilder.Append("large team discount; ");
            }
            else if (seatCount >= 20)
            {
                discountAmount += baseAmount * 0.08m;
                notesBuilder.Append("medium team discount; ");
            }
            else if (seatCount >= 10)
            {
                discountAmount += baseAmount * 0.04m;
                notesBuilder.Append("small team discount; ");
            }

            
            if (useLoyaltyPoints && customer.LoyaltyPoints > 0)
            {
                int pointsToUse = customer.LoyaltyPoints > 200 ? 200 : customer.LoyaltyPoints;
                discountAmount += pointsToUse;
                notesBuilder.Append($"loyalty points used: {pointsToUse}; ");
            }

           
            decimal subtotalAfterDiscount = baseAmount - discountAmount;
            if (subtotalAfterDiscount < 300m)
            {
               
                discountAmount = baseAmount - 300m;
                notesBuilder.Append("minimum discounted subtotal applied; ");
            }

            return (discountAmount, notesBuilder.ToString());
        }
    }
}
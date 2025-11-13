using FinancialGoals.Models;
using System;
using System.Collections.Generic;

namespace FinancialGoals.Services
{
    public static class CalculationService
    {
        public static decimal MonthlyRate(decimal annualPercent) => annualPercent / 100m / 12m;

       
        public static decimal CalculateMonthlyPayment(decimal targetFV, decimal pv, decimal annualPercent, int months)
        {
            var r = MonthlyRate(annualPercent);
            if (months <= 0) throw new ArgumentException("months must be > 0");
            if (r == 0m) return (targetFV - pv) / months;
            var factor = (decimal)Math.Pow((double)(1 + r), months);
            return (targetFV - pv * factor) * r / (factor - 1);
        }

      
        public static (List<SimulationRow> rows, decimal finalBalance) Simulate(decimal pv, decimal monthlyContribution, decimal annualPercent, int months, DateTime startDate)
        {
            var r = MonthlyRate(annualPercent);
            var rows = new List<SimulationRow>();
            decimal balance = pv;
            for (int m = 1; m <= months; m++)
            {
                var interest = balance * r;
                balance += interest + monthlyContribution;
                rows.Add(new SimulationRow
                {
                    MonthIndex = m,
                    Date = startDate.AddMonths(m),
                    Contribution = Math.Round(monthlyContribution, 2),
                    Interest = Math.Round(interest, 2),
                    Balance = Math.Round(balance, 2)
                });
            }
            return (rows, Math.Round(balance, 2));
        }

      
        public static int EstimateMonthsToTarget(decimal targetFV, decimal pv, decimal monthlyContribution, decimal annualPercent, int maxMonths = 600)
        {
            if (monthlyContribution <= 0) return int.MaxValue;
            int low = 1, high = maxMonths;
            while (low < high)
            {
                int mid = (low + high) / 2;
                var (_, final) = Simulate(pv, monthlyContribution, annualPercent, mid, DateTime.Today);
                if (final >= targetFV) high = mid;
                else low = mid + 1;
            }
            return low;
        }
    }
}
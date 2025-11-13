using System;

namespace FinancialGoals.Models
{
    public class FinancialGoal
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = "";
        public decimal TargetAmount { get; set; }
        public decimal CurrentAmount { get; set; }
        public decimal AnnualRatePercent { get; set; }
        public decimal MonthlyContribution { get; set; }
        public DateTime? TargetDate { get; set; }

    
        public int EstimatedMonthsToTarget { get; set; }

        
        public double PercentComplete
        {
            get
            {
                if (TargetAmount <= 0) return 0;
                double percent = (double)(CurrentAmount / TargetAmount * 100m);
                return Math.Min(percent, 100.0);
            }
        }

        public string ProgressSummary
        {
            get
            {
                return $"{CurrentAmount:F0}/{TargetAmount:F0} ({PercentComplete:F1}%)";
            }
        }

        public int? DueDateMonthsLeft
        {
            get
            {
                if (!TargetDate.HasValue) return null;
                int monthsLeft = ((TargetDate.Value.Year - DateTime.Today.Year) * 12)
                                 + (TargetDate.Value.Month - DateTime.Today.Month);
                return monthsLeft;
            }
        }
    }
}

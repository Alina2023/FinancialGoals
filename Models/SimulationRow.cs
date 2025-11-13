using System;

namespace FinancialGoals.Models
{
    public class SimulationRow
    {
        public int MonthIndex { get; set; }
        public DateTime Date { get; set; }
        public decimal Contribution { get; set; }
        public decimal Interest { get; set; }
        public decimal Balance { get; set; }
    }
}
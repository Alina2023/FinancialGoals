using FinancialGoals.Models;
using FinancialGoals.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FinancialGoals.Views
{
    public partial class ReportWindow : Window
    {
        private FinancialGoal goal;
        private List<SimulationRow> rows = new List<SimulationRow>();

        public ReportWindow(FinancialGoal goal)
        {
            InitializeComponent();
            this.goal = goal;
            txtGoalName.Text = goal.Name;
        }

        private void BtnSimulateAll_Click(object sender, RoutedEventArgs e)
        {
            int months;
            if (goal.TargetDate.HasValue)
            {
                var monthsSpan = ((goal.TargetDate.Value.Year - DateTime.Today.Year) * 12) + (goal.TargetDate.Value.Month - DateTime.Today.Month);
                months = Math.Max(1, monthsSpan);
            }
            else if (goal.MonthlyContribution > 0)
                months = CalculationService.EstimateMonthsToTarget(goal.TargetAmount, goal.CurrentAmount, goal.MonthlyContribution, goal.AnnualRatePercent);
            else { MessageBox.Show("Set TargetDate or MonthlyContribution"); return; }

            var result = CalculationService.Simulate(goal.CurrentAmount, goal.MonthlyContribution, goal.AnnualRatePercent, months, DateTime.Today);
            rows = result.rows;
            dgRows.ItemsSource = rows;

            DrawChart(rows);
        }

        private void DrawChart(List<SimulationRow> rows)
        {
            chartCanvas.Children.Clear();
            if (rows == null || rows.Count == 0) return;

           
            double width = Math.Max(600, rows.Count * 6);
            chartCanvas.Width = width;
            double height = chartCanvas.Height;
            decimal maxBalance = rows.Max(r => r.Balance);
            if (maxBalance <= 0) maxBalance = 1;

            PointCollection points = new PointCollection();
            for (int i = 0; i < rows.Count; i++)
            {
                double x = i * (width / rows.Count);
                double y = (double)(1m - (rows[i].Balance / maxBalance)) * (height - 10) + 5;
                points.Add(new Point(x, y));
            }

            var poly = new Polyline
            {
                Stroke = Brushes.Blue,
                StrokeThickness = 2,
                Points = points
            };
            chartCanvas.Children.Add(poly);

            
            for (int i = 0; i < points.Count; i += Math.Max(1, points.Count / 50))
            {
                var ellipse = new Ellipse { Width = 3, Height = 3, Fill = Brushes.DarkBlue };
                Canvas.SetLeft(ellipse, points[i].X - 1.5);
                Canvas.SetTop(ellipse, points[i].Y - 1.5);
                chartCanvas.Children.Add(ellipse);
            }
        }
    }
}


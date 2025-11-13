using FinancialGoals.Models;
using FinancialGoals.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace FinancialGoals
{
    public partial class MainWindow : Window
    {
        private List<FinancialGoal> goals = new List<FinancialGoal>();
        private FinancialGoal selected = null;

        // представление после фильтра/сортировки
        private List<FinancialGoal> currentView = new List<FinancialGoal>();

        public MainWindow()
        {
            InitializeComponent();

          
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            
            this.Loaded -= MainWindow_Loaded;

            // Инициализация контролов (защита на случай, если XAML изменялся)
            try
            {
                if (cmbSort != null) cmbSort.SelectedIndex = 0;
            }
            catch { /* игнорируем */ }

            RefreshGoalsUI();
        }

        // ---------------- UI обновление / фильтра / сортировка ----------------
        private void RefreshGoalsUI()
        {
            // Защита: если UI ещё не построен, выходим
            if (lbGoals == null || cmbSort == null || chkNearDeadline == null || tbDeadlineMonths == null)
                return;

            ApplyFilterAndSort();

            // обновляем источник данных
            lbGoals.ItemsSource = null;
            lbGoals.ItemsSource = currentView;

            // если выбран элемент — восстановим выбор, если он всё ещё в view
            if (selected != null)
            {
                var found = currentView.FirstOrDefault(g => g.Id == selected.Id);
                lbGoals.SelectedItem = found;
            }
        }

        private void ApplyFilterAndSort()
        {
            IEnumerable<FinancialGoal> view = goals;

            // Фильтрация: близкие к дедлайну
            bool filterNear = false;
            int monthsThreshold = 0;
            try
            {
                filterNear = chkNearDeadline.IsChecked == true;
                int.TryParse(tbDeadlineMonths.Text, out monthsThreshold);
            }
            catch
            {
                filterNear = false;
                monthsThreshold = 0;
            }

            if (filterNear)
            {
                if (monthsThreshold < 0) monthsThreshold = 0;
                view = view.Where(g =>
                {
                    int? monthsLeft = g.DueDateMonthsLeft;
                    return monthsLeft.HasValue && monthsLeft.Value <= monthsThreshold;
                });
            }

            // Сортировка
            int sortIndex = 0;
            try { sortIndex = cmbSort.SelectedIndex; } catch { sortIndex = 0; }

            if (sortIndex == 1)
            {
                view = view.OrderByDescending(g => g.PercentComplete);
            }
            else if (sortIndex == 2)
            {
                // цели без даты ставим в конец
                view = view.OrderBy(g => g.TargetDate ?? DateTime.MaxValue);
            }

            currentView = view.ToList();
        }

        // ---------------- обработчики событий сортировки/фильтра ----------------
        private void CmbSort_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (lbGoals == null) return;
            ApplyFilterAndSort();
            lbGoals.ItemsSource = null;
            lbGoals.ItemsSource = currentView;
        }

        private void FilterControls_Changed(object sender, RoutedEventArgs e)
        {
            if (lbGoals == null) return;
            ApplyFilterAndSort();
            lbGoals.ItemsSource = null;
            lbGoals.ItemsSource = currentView;
        }

        private void FilterControls_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (lbGoals == null) return;
            ApplyFilterAndSort();
            lbGoals.ItemsSource = null;
            lbGoals.ItemsSource = currentView;
        }

        // ---------------- выбор элемента / показ полей ----------------
        private void LbGoals_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // защита
            if (lbGoals == null) return;
            selected = lbGoals.SelectedItem as FinancialGoal;
            ShowSelected();
        }

        private void ShowSelected()
        {
            if (selected == null)
            {
                txtName.Text = "";
                txtTargetAmount.Text = "";
                txtCurrentAmount.Text = "";
                txtAnnualRate.Text = "";
                txtMonthlyContribution.Text = "";
                dpTargetDate.SelectedDate = null;
                txtResultSummary.Text = "";
                dgPreview.ItemsSource = null;
                return;
            }

            txtName.Text = selected.Name;
            txtTargetAmount.Text = selected.TargetAmount.ToString("F2");
            txtCurrentAmount.Text = selected.CurrentAmount.ToString("F2");
            txtAnnualRate.Text = selected.AnnualRatePercent.ToString("F2");
            txtMonthlyContribution.Text = selected.MonthlyContribution.ToString("F2");
            dpTargetDate.SelectedDate = selected.TargetDate;
        }

        // ---------------- для целей ----------------
        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            selected = new FinancialGoal { Name = "New goal" };
            goals.Add(selected);
            RefreshGoalsUI();
            ShowSelected();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (selected == null) { MessageBox.Show("Select a goal first"); return; }
            // Поля уже доступны для редактирования
            txtName.Focus();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (selected == null) return;
            goals.Remove(selected);
            selected = null;
            RefreshGoalsUI();
            ShowSelected();
        }

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            if (selected == null) { MessageBox.Show("Select or create a goal"); return; }

            // Разбор полей
            selected.Name = txtName.Text.Trim();
            decimal target;
            if (!decimal.TryParse(txtTargetAmount.Text, out target)) { MessageBox.Show("Invalid target amount"); return; }
            decimal current;
            if (!decimal.TryParse(txtCurrentAmount.Text, out current)) { MessageBox.Show("Invalid current amount"); return; }
            decimal rate;
            if (!decimal.TryParse(txtAnnualRate.Text, out rate)) { MessageBox.Show("Invalid annual rate"); return; }
            decimal monthly;
            if (!decimal.TryParse(txtMonthlyContribution.Text, out monthly)) monthly = 0m;

            selected.TargetAmount = Math.Max(0, target);
            selected.CurrentAmount = Math.Max(0, current);
            selected.AnnualRatePercent = rate;
            selected.MonthlyContribution = monthly;
            selected.TargetDate = dpTargetDate.SelectedDate;

            // Обновляем представление (проценты и прогресс)
            RefreshGoalsUI();
            MessageBox.Show("Goal saved");
        }

        // ---------------- Сохранение / загрузка ----------------
        private void BtnSaveList_Click(object sender, RoutedEventArgs e)
        {
            if (lbGoals == null) return;
            var dlg = new SaveFileDialog { DefaultExt = ".json", Filter = "JSON files (*.json)|*.json" };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    PersistenceService.SaveGoals(dlg.FileName, goals);
                    MessageBox.Show("Saved");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error saving: " + ex.Message);
                }
            }
        }

        private void BtnLoadList_Click(object sender, RoutedEventArgs e)
        {
            if (lbGoals == null) return;
            var dlg = new OpenFileDialog { DefaultExt = ".json", Filter = "JSON files (*.json)|*.json" };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    goals = PersistenceService.LoadGoals(dlg.FileName);
                    selected = goals.FirstOrDefault();
                    RefreshGoalsUI();
                    ShowSelected();
                    MessageBox.Show("Loaded " + goals.Count + " goals");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading: " + ex.Message);
                }
            }
        }

        // ---------------- Расчёты и симуляция ----------------
        private void BtnCalculate_Click(object sender, RoutedEventArgs e)
        {
            if (selected == null) { MessageBox.Show("Select a goal"); return; }

            int months = 0;
            if (selected.TargetDate.HasValue)
            {
                var monthsSpan = ((selected.TargetDate.Value.Year - DateTime.Today.Year) * 12) + (selected.TargetDate.Value.Month - DateTime.Today.Month);
                months = Math.Max(1, monthsSpan);
            }
            else if (selected.MonthlyContribution > 0)
            {
                months = CalculationService.EstimateMonthsToTarget(
                    selected.TargetAmount,
                    selected.CurrentAmount,
                    selected.MonthlyContribution,
                    selected.AnnualRatePercent);
            }
            else
            {
                MessageBox.Show("Either set TargetDate or MonthlyContribution");
                return;
            }

            decimal pmt = CalculationService.CalculateMonthlyPayment(
                selected.TargetAmount,
                selected.CurrentAmount,
                selected.AnnualRatePercent,
                months);

            selected.EstimatedMonthsToTarget = months;
            txtResultSummary.Text = $"Months: {months}; Required monthly payment: {pmt:F2}";
        }

        private void BtnSimulate_Click(object sender, RoutedEventArgs e)
        {
            if (selected == null) { MessageBox.Show("Select a goal"); return; }
            int months = selected.EstimatedMonthsToTarget;
            if (months <= 0)
            {
                MessageBox.Show("Calculate first");
                return;
            }

            var (rows, final) = CalculationService.Simulate(
                selected.CurrentAmount,
                selected.MonthlyContribution,
                selected.AnnualRatePercent,
                months,
                DateTime.Today);

            dgPreview.ItemsSource = rows.Count > 6 ? rows.GetRange(0, 6) : rows;
            MessageBox.Show($"Simulation finished. Final balance: {final:F2}");
        }

        // ---------------- Экспорт CSV ----------------
        private void BtnExportCsv_Click(object sender, RoutedEventArgs e)
        {
            if (selected == null) return;
            if (selected.EstimatedMonthsToTarget <= 0) { MessageBox.Show("Calculate and simulate first"); return; }

            var (rows, final) = CalculationService.Simulate(
                selected.CurrentAmount,
                selected.MonthlyContribution,
                selected.AnnualRatePercent,
                selected.EstimatedMonthsToTarget,
                DateTime.Today);

            var dlg = new SaveFileDialog { DefaultExt = ".csv", Filter = "CSV files (*.csv)|*.csv" };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    using (var sw = new StreamWriter(dlg.FileName, false, new System.Text.UTF8Encoding(true)))
                    {
                        sw.WriteLine("Month,Date,Contribution,Interest,Balance");
                        foreach (var r in rows)
                        {
                            sw.WriteLine(string.Format("{0},{1},{2},{3},{4}",
                                r.MonthIndex,
                                r.Date.ToString("yyyy-MM-dd"),
                                r.Contribution,
                                r.Interest,
                                r.Balance));
                        }
                    }
                    MessageBox.Show("Exported CSV");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error exporting CSV: " + ex.Message);
                }
            }
        }

        // ---------------- Открыть окно отчёта ----------------
        private void BtnOpenReport_Click(object sender, RoutedEventArgs e)
        {
            if (selected == null) { MessageBox.Show("Select a goal"); return; }
            var report = new Views.ReportWindow(selected);
            report.Owner = this;
            report.Show();
        }
    }
}

using FinancialGoals.Models;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FinancialGoals.Services
{
    public static class PersistenceService
    {
        public static void SaveGoals(string path, List<FinancialGoal> goals)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(goals, options);
            File.WriteAllText(path, json);
        }

        public static List<FinancialGoal> LoadGoals(string path)
        {
            if (!File.Exists(path)) return new List<FinancialGoal>();
            string text = File.ReadAllText(path);
            try
            {
                var list = JsonSerializer.Deserialize<List<FinancialGoal>>(text);
                return list ?? new List<FinancialGoal>();
            }
            catch
            {
                return new List<FinancialGoal>();
            }
        }
    }
}

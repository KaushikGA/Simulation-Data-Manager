using System.Diagnostics;
using System.Text.Json;       // Parsing JSON
using Microsoft.Data.Sqlite;  // Database handling
using System.Reflection;

namespace SimulationManager
{
    // 1. Define a class that matches Python JSON output exactly
    public class SimulationResult
    {
        public string? file { get; set; }        
        public double s11_max { get; set; }
        public string? status { get; set; }      
    }

    class Program
    {
        static void Main()
        {
            Console.WriteLine("--- Starting Simulation Data Manager ---");

            // 2. Setup Database
            string dbFile = "simulation_results.db";
            InitializeDatabase(dbFile);

            string logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            string pythonScript = ExtractPythonScript();

            if (!File.Exists(pythonScript)) { Console.WriteLine("Error: parser.py not found!"); return; }

            string[] files = Directory.GetFiles(logDirectory, "*.log");

            foreach (string file in files)
            {
                
                string jsonOutput = RunPythonParser(pythonScript, file);

                if (!string.IsNullOrEmpty(jsonOutput))  // 3. Parse JSON output
                {
                    try
                    {
                            SimulationResult? data = JsonSerializer.Deserialize<SimulationResult>(jsonOutput);

                        if (data != null && data.status == "Success")
                        {
                            // 4. Save to SQL
                            SaveToDatabase(dbFile, data);
                            Console.WriteLine($"[SAVED] {Path.GetFileName(file)} -> Stress: {data.s11_max} MPa");
                        }
                        else
                        {
                            Console.WriteLine($"[SKIP] {Path.GetFileName(file)} (Value not found)");
                        }
                    }
                    catch (Exception ex)
                    {
                        
                        Console.WriteLine($"[JSON ERROR] {ex.Message}");
                    }
                }
            }
            Console.WriteLine("--- All Data Saved to SQL Database ---");
            Console.ReadLine();
            if (File.Exists(pythonScript)) File.Delete(pythonScript);
        }
        static string ExtractPythonScript()
        {            
            var assembly = Assembly.GetExecutingAssembly();          
            string resourceName = "Simulation_Data_Manager.parser.py";

            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new Exception($"Could not find embedded resource '{resourceName}'. Did you add it to .csproj?");
            
            string tempPath = Path.Combine(Path.GetTempPath(), "parser.py");

                        using (var fileStream = File.Create(tempPath))
            {
                stream.CopyTo(fileStream);
            }

            return tempPath; 
        }
        
        static void InitializeDatabase(string dbFile)
        {
            using (var connection = new SqliteConnection($"Data Source={dbFile}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                // Create a table if it doesn't exist
                command.CommandText =
                @"
                    CREATE TABLE IF NOT EXISTS StressResults (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Filename TEXT,
                        MaxStress REAL,
                        ProcessedDate TEXT
                    );
                ";
                command.ExecuteNonQuery();
            }
        }

        static void SaveToDatabase(string dbFile, SimulationResult data)
        {
            using (var connection = new SqliteConnection($"Data Source={dbFile}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                // Insert data using parameters 
                command.CommandText =
                @"
                    INSERT INTO StressResults (Filename, MaxStress, ProcessedDate)
                    VALUES ($filename, $stress, $date);
                ";
                command.Parameters.AddWithValue("$filename", Path.GetFileName(data.file));
                command.Parameters.AddWithValue("$stress", data.s11_max);
                command.Parameters.AddWithValue("$date", DateTime.Now.ToString());
                command.ExecuteNonQuery();
            }
        }

        
        static string RunPythonParser(string scriptPath, string logPath)
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = "py"; 
            start.Arguments = $"\"{scriptPath}\" \"{logPath}\"";
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.CreateNoWindow = true;

            using (Process? process = Process.Start(start))
            {
                if (process == null)
                    throw new Exception("Failed to start Python process");

                using (StreamReader reader = process.StandardOutput)
                {
                    return reader.ReadToEnd().Trim();
                }
            }
        }
    }
}
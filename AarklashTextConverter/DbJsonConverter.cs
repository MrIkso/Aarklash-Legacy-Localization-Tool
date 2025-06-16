using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AarklashTextConverter2
{
    public class DbJsonConverter
    {
        private class JsonEntry
        {
            public int Id { get; set; }
            public string Text { get; set; }
        }

        /// <summary>
        /// Exports all text records from a DB file to a JSON file.
        /// </summary>
        /// <param name="dbFilePath">Path to the input DB file.</param>
        /// <param name="jsonFilePath">Path to the output JSON file.</param>
        public void ExportToJson(string dbFilePath, string jsonFilePath)
        {
            Console.WriteLine($"Starting export from '{dbFilePath}' to '{jsonFilePath}'...");
            
            LocalizationFile dbData = LocalizationLoader.Load(dbFilePath);
            var jsonData = new List<JsonEntry>();
            foreach (int id in dbData.OrderedIds)
            {
                jsonData.Add(new JsonEntry
                {
                    Id = id,
                    Text = dbData.Strings.ContainsKey(id) ? dbData.Strings[id] : string.Empty
                });
            }
           
            string jsonString = JsonConvert.SerializeObject(jsonData, Formatting.Indented);

            File.WriteAllText(jsonFilePath, jsonString, Encoding.UTF8);

            Console.WriteLine($"Export completed successfully. {jsonData.Count} records saved.");
        }

        /// <summary>
        /// Imports text records from a JSON file and writes them to a new DB file,
        /// preserving the structure of the original DB file.
        /// </summary>
        /// <param name="jsonFilePath">Path to the input JSON file.</param>
        /// <param name="originalDbFilePath">Path to the original DB file (needed to copy the header).</param>
        /// <param name="outputDbFilePath">Path to the output DB file.</param>
        public void ImportFromJson(string jsonFilePath, string originalDbFilePath, string outputDbFilePath)
        {
            Console.WriteLine($"Starting import from '{jsonFilePath}' to '{outputDbFilePath}'...");
            string json = File.ReadAllText(jsonFilePath, Encoding.UTF8);
            var jsonData = JsonConvert.DeserializeObject<List<JsonEntry>>(json);
            if (jsonData == null)
            {
                throw new InvalidDataException("JSON file could not be deserialized or is empty.");
            }
           
            var modifications = jsonData.ToDictionary(entry => entry.Id, entry => entry.Text);
           
            LocalizationFile originalData = LocalizationLoader.Load(originalDbFilePath);

            var writer = new LocalizationWriter();
            writer.Save(originalDbFilePath, outputDbFilePath, originalData, modifications);

            Console.WriteLine($"Import completed successfully.");
        }
    }
}

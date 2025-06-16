using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AarklashTextConverter2
{
    public class LocalizationWriter
    {
        private readonly Encoding _encoding = Encoding.UTF8;

        /// <summary>
        /// Writes modifications to a new localization file.
        /// </summary>
        /// <param name="originalFilePath">Path to the original file.</param>
        /// <param name="newFilePath">Path to save the new file.</param>
        /// <param name="originalData">Original data loaded via LocalizationLoader.</param>
        /// <param name="modifications">Dictionary with row IDs and new text.</param>
        public void Save(string originalFilePath, string newFilePath, LocalizationFile originalData, Dictionary<int, string> modifications)
        {
            var finalStrings = new Dictionary<int, string>(originalData.Strings);
            foreach (var modification in modifications)
            {
                if (finalStrings.ContainsKey(modification.Key))
                {
                    finalStrings[modification.Key] = modification.Value;
                }
            }

            var newStringLengths = new List<int>();
            var newContentBlockStream = new MemoryStream();

            foreach (int id in originalData.OrderedIds)
            {
                string text = finalStrings[id];
                if (string.IsNullOrEmpty(text))
                {
                    newStringLengths.Add(0);
                }
                else
                {
                    byte[] stringBytes = _encoding.GetBytes(text);
                    newStringLengths.Add(stringBytes.Length + 1); // +1 for the null terminator

                    newContentBlockStream.Write(stringBytes, 0, stringBytes.Length);
                    newContentBlockStream.WriteByte(0); // Adding a null terminator
                }
            }

            byte[] newContentBlock = newContentBlockStream.ToArray();
            int newContentBlockLength = newContentBlock.Length;

            byte[] originalBytes = File.ReadAllBytes(originalFilePath);

            using (var fileStream = File.Create(newFilePath))
            using (var writer = new BinaryWriter(fileStream, _encoding))
            {
                const int stringLengthsTableOffset = 22365;
                writer.Write(originalBytes, 0, stringLengthsTableOffset);
                foreach (int length in newStringLengths)
                {
                    writer.Write(length);
                }
              
                writer.Write(newContentBlockLength);              
                writer.Write(newContentBlock);
            }

            Console.WriteLine($"[SUCCESS] New file saved to: {newFilePath}");
            Console.WriteLine($"Original content length: {originalData.Strings.Count} strings.");
            Console.WriteLine($"New content length: {newContentBlockLength} bytes.");
        }
    }
}

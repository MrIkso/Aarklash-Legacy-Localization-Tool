using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AarklashTextConverter2
{
    public class LocalizationFile
    {
        public Dictionary<int, string> Strings { get; set; } = new Dictionary<int, string>();
        public List<int> OrderedIds { get; set; } = new List<int>();
    }

    public class LocalizationLoader
    {
        private const string EXPECTED_MAGIC = "GAMENAME_DSMGR2010100801";

        public static LocalizationFile Load(string filePath)
        {
            var result = new LocalizationFile();

            using (var stream = File.OpenRead(filePath))
            using (var reader = new BinaryReader(stream, Encoding.UTF8))
            {
                // read header
                byte[] magicBytes = reader.ReadBytes(24);
                string magicString = Encoding.ASCII.GetString(magicBytes);
                Console.WriteLine($"[DEBUG] Magic String: {magicString}");
                if (magicString != EXPECTED_MAGIC)
                {
                    throw new InvalidDataException("File has incorrect magic number.");
                }

                // read table indexes lenght
                // Offset 0x51
                reader.BaseStream.Seek(0x51, SeekOrigin.Begin);
                Console.WriteLine($"[DEBUG] Current stream position before reading length: {reader.BaseStream.Position}");
                int indexTableLength = reader.ReadInt32(); // Offset 0x51: 0xF8560000 -> 22264
                int recordCount = indexTableLength / 8;
                Console.WriteLine($"[DEBUG] Index Table Length: {indexTableLength} bytes");
                Console.WriteLine($"[DEBUG] Calculated Record Count: {recordCount}");

                if (recordCount <= 0)
                {
                    throw new InvalidDataException("Record count is zero or less. Something is wrong with the file structure or reading logic.");
                }

                // skipping some bytes to reach the next section
                reader.ReadBytes(8); // Offset 85

                // read table indexes

                var idMap = new List<Tuple<int, int>>();
                for (int i = 0; i < recordCount; i++)
                {
                    int id = reader.ReadInt32();
                    int orderIndex = reader.ReadInt32();
                    idMap.Add(new Tuple<int, int>(id, orderIndex));
                }

                result.OrderedIds = idMap.Select(tuple => tuple.Item1).ToList();
                // read table len index 

                Console.WriteLine($"[DEBUG] Current stream position before reading table index: {reader.BaseStream.Position}");
                // Offset 22365
                var stringLengths = new List<int>();
                for (int i = 0; i < recordCount; i++)
                {
                    int currentStringLength = reader.ReadInt32();
                    stringLengths.Add(currentStringLength);
                }

                // read text blobs
                Console.WriteLine($"[DEBUG] Current stream position before reading table enrties lenght: {reader.BaseStream.Position}");
                int textBlockLength = reader.ReadInt32(); // Offset 33501
                Console.WriteLine($"[DEBUG] Text Block Length: {textBlockLength}");

                byte[] textBlockBytes = reader.ReadBytes(textBlockLength);

                int currentOffsetInBlock = 0;
                for (int i = 0; i < recordCount; i++)
                {
                    int id = result.OrderedIds[i];
                    int length = stringLengths[i];

                    if (length > 0)
                    {
                        string text = Encoding.UTF8.GetString(textBlockBytes, currentOffsetInBlock, length - 1);
                        result.Strings[id] = text;
                        currentOffsetInBlock += length;
                    }
                    else
                    {
                        if (!result.Strings.ContainsKey(id))
                        {
                            result.Strings[id] = string.Empty;
                        }
                    }
                }
            }

            return result;
        }
    }
}
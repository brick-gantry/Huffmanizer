using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Huffman
{
    static class Huffmanizer
    {
        class Node
        {
            [JsonIgnore]
            public int Count { get; set; }
            [JsonProperty("v", NullValueHandling = NullValueHandling.Ignore)]
            public char? Value { get; set; }
            [JsonProperty("b", NullValueHandling = NullValueHandling.Ignore)]
            public Node[] Branch { get; set; }
        }

        static string Filter(string input)
        {
            string acceptable = "abcdefghijklmnopqrstuvwxyz ,.!\n";
            return new string(input.ToLower().ToList().SelectMany(ch => acceptable.Contains(ch) ? new char[] { ch } : new char[] { }).ToArray());
        }

        static void Increment(this Dictionary<char, int> counts, char ch)
        {
            if (counts.ContainsKey(ch))
                counts[ch]++;
            else
                counts.Add(ch, 1);
        }

        static Dictionary<char, int> GetFrequency(string input)
        {
            Dictionary<char, int> counts = new Dictionary<char, int>();
            input.ToList().ForEach(ch => counts.Increment(ch));
            return counts;
        }

        static int Compare(dynamic a, dynamic b)
        {
            return a.Count - b.Count;
        }

        static Node ConstructMapping(Dictionary<char, int> frequency)
        {
            List<Node> toProcess = new List<Node>();
            foreach (var p in frequency)
            {
                toProcess.Add(new Node { Value = p.Key, Count = p.Value });
            }
            while (true)
            {
                toProcess.Sort(Compare);
                var a = toProcess[0];
                var b = toProcess[1];
                Node nextRoot = new Node { Count = a.Count + b.Count, Branch = new Node[] { a, b } };
                if (toProcess.Count == 2)
                    return nextRoot;
                toProcess.Add(nextRoot);
                toProcess.RemoveAt(0);
                toProcess.RemoveAt(0);
            }
        }

        static Dictionary<char, string> ConstructCodes(Node mapping, Dictionary<char, string> codes = null, string code = "")
        {
            if (codes == null)
            {
                codes = new Dictionary<char, string>();
            }
            if (mapping.Value.HasValue)
            {
                codes.Add(mapping.Value.Value, code);
            }
            else
            {
                ConstructCodes(mapping.Branch[0], codes, string.Format("{0}0", code));
                ConstructCodes(mapping.Branch[1], codes, string.Format("{0}1", code));
            }
            return codes;
        }

        public class EncodingResult
        {
            public string Mapping { get; set; }
            public int ExtraBits { get; set; }
            public byte[] Message { get; set; }
        }

        public static EncodingResult Encode(string input, bool filter = true)
        {
            EncodingResult result = new EncodingResult();

            var filteredInput = filter ? Filter(input) : input;
            var frequency = GetFrequency(filteredInput);
            var mapping = ConstructMapping(frequency);
            var codes = ConstructCodes(mapping);

            List<byte> byteArray = new List<byte>();
            string storage = string.Empty;

            int byteSize = 8;

            foreach (var ch in filteredInput)
            {
                storage += codes[ch];
                if (storage.Length >= byteSize)
                {
                    byteArray.Add(Convert.ToByte(storage.Substring(0, byteSize), 2));
                    storage = storage.Substring(byteSize);
                }
            }
            result.ExtraBits = byteSize - storage.Length;
            if (result.ExtraBits > 0)
            {
                for (int i = 0; i < result.ExtraBits; i++)
                {
                    storage += '0';
                }
                byteArray.Add(Convert.ToByte(storage.Substring(0, byteSize), 2));
            }

            result.Mapping = JsonConvert.SerializeObject(mapping);
            result.Message = byteArray.ToArray();
            return result;
        }

        public static string Decode(byte[] message, int extraBits, string serialMapping)
        {
            var mapping = JsonConvert.DeserializeObject<Node>(serialMapping);

            string output = string.Empty;
            var currNode = mapping;

            for (int i = 0; i < message.Length; i++)
            {
                var s = Convert.ToString(message[i], 2).PadLeft(8, '0');

                if (i == message.Length - 1)
                {
                    s = s.Substring(0, 8 - extraBits);
                }

                foreach(var ch in s)
                {
                    currNode = currNode.Branch[ch - '0'];
                    if (currNode.Value.HasValue)
                    {
                        output += currNode.Value;
                        currNode = mapping;
                    }
                }
            }

            return output;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var input = 
@"According to the chief technology officer from the company, the robots were controlled from a single mobile device and were built with a special encryption technology that would prevent interference from other devices in the area.

This event took place at the Qingdao Beer Festival in Shandong, China, which is known as the Asian Oktoberfest, because if there’s one thing you want to show a very drunk person, it’s this many robots dancing.";
            var output = Huffmanizer.Encode(input, false);
            var decoded = Huffmanizer.Decode(output.Message, output.ExtraBits, output.Mapping);
            Console.WriteLine(decoded);
            Console.WriteLine("Original: {0}", input.Length);
            Console.WriteLine("Encoded: {0}", output.Message.Length);
            Console.ReadKey();
        }
    }
}

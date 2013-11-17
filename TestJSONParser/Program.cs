//#define WITH_HUGE_TEST
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Text.Json;

namespace TestJSONParser
{
    class Program
    {
        // Note: fathers.json.txt was generated using:
        // http://experiments.mennovanslooten.nl/2010/mockjson/tryit.html
        // avg: file size ~ exec time (on Lenovo Win7 PC, i5, 2.50GHz, 6Gb)
        const string FATHERS_TEST_FILE_PATH = @"..\..\fathers.json.txt"; // avg: 12mb ~ 1sec
        const string SMALL_TEST_FILE_PATH = @"..\..\small.json.txt"; // avg: 4kb ~ 1ms
#if WITH_HUGE_TEST
        const string HUGE_TEST_FILE_PATH = @"..\..\huge.json.txt"; // avg: 180mb ~ 20sec
#endif
        static Parser parser = new Parser (new ParserSettings { LiteralsBuffer = 1024 });

#if WITH_HUGE_TEST
        static void HugeTest()
        {
            string json = System.IO.File.ReadAllText(HUGE_TEST_FILE_PATH);
            object obj;
            Console.WriteLine("Huge Test - JSON parse... {0} kb ({1} mb)", (int)(json.Length / 1024), (int)(json.Length / (1024 * 1024)));
            Console.WriteLine();

            /*var serializer = new System.Web.Script.Serialization.JavaScriptSerializer
            {
                MaxJsonLength = int.MaxValue
            };
            Console.WriteLine("\tParsed by {0} in...", serializer.GetType().FullName);
            DateTime start1 = DateTime.Now;
            obj = serializer.DeserializeObject(json);
            Console.WriteLine("\t\t{0} ms", (int)DateTime.Now.Subtract(start1).TotalMilliseconds);
            Console.WriteLine();*/

            Console.WriteLine("\tParsed by {0} in...", parser.GetType().FullName);
            DateTime start2 = DateTime.Now;
            obj = parser.Parse(json);
            Console.WriteLine("\t\t{0} ms", (int)DateTime.Now.Subtract(start2).TotalMilliseconds);
            Console.WriteLine("Press a key...");
            Console.WriteLine();
            Console.ReadKey();
        }
#endif

        static void Top10Youtube2013Test()
        {
            var JSON_DATE =
                new
                {
                    Year = 0,
                    Month = 0,
                    Day = 0
                };

            Reviver ToDate =
                (type, key, value) =>
                    (type == typeof(double)) ?
                        (Func<object>)(() => Convert.ToInt32(value)) :
                        ((type == JSON_DATE.GetType()) && (key == null)) ?
                            (Func<object>)
                            (
                                () => new DateTime
                                (
                                    value.As(JSON_DATE).Year,
                                    value.As(JSON_DATE).Month,
                                    value.As(JSON_DATE).Day
                                )
                            ) :
                            null;

            DateTime dateTime =
                JSON_DATE.
                FromJson
                (
                    @" { ""Year"": 1970, ""Month"": 5, ""Day"": 10 }",
                    ToDate
                ).
                As<DateTime>();

            Console.WriteLine(dateTime);
            Console.WriteLine();

            Console.WriteLine("Top 10 Youtube 2013 Test - JSON parse...");
            Console.WriteLine();
            System.Net.WebRequest www = System.Net.WebRequest.Create("https://gdata.youtube.com/feeds/api/videos?q=2013&max-results=10&v=2&alt=jsonc");
            using (System.IO.Stream stream = www.GetResponse().GetResponseStream())
            {
                // Yup, as simple as this, step #1:
                var YOUTUBE_SCHEMA = new
                {
                    Data = new
                    {
                        Items = new[]
                        {
                            new
                            {
                                Title = "",
                                Category = "",
                                Uploaded = DateTime.Now,
                                Updated = DateTime.Now,
                                Player = new
                                {
                                    @Default = ""
                                }
                            }
                        }
                    }
                };

                // And as easy as that, step #2:
                var parsed = parser.Parse
                    (
                        stream,
                        YOUTUBE_SCHEMA,
                        (type, key, value) =>
                            // maps: "data" => "Data", "items" => "Items", "title" => "Title", ...etc
                            (key == Parser.DOT) ?
                                (Func<object>)(() => String.Concat((char)(value.ToString()[0] - 32), value.ToString().Substring(1))) :
                                null,
                        (type, key, value) =>
                            ((type == YOUTUBE_SCHEMA.Data.Items[0].GetType()) && (key == "Uploaded") || (key == "Updated")) ?
                                (Func<object>)(() => DateTime.Parse((string)value)) :
                                null
                    ).As(YOUTUBE_SCHEMA);

                Console.WriteLine();
                foreach (var item in parsed.Data.Items)
                {
                    var title = item.Title;
                    var category = item.Category;
                    var uploaded = item.Uploaded;
                    var player = item.Player;
                    var link = player.@Default;
                    Console.WriteLine("\t\"{0}\" (category: {1}, uploaded: {2})", title, category, uploaded);
                    Console.WriteLine("\t\tURL: {0}", link);
                    Console.WriteLine();
                }
                Console.WriteLine("Press a key...");
                Console.WriteLine();
                Console.ReadKey();
            }
        }

        static void Main(string[] args)
        {
            Top10Youtube2013Test();
#if WITH_HUGE_TEST
            HugeTest();
#endif

            string small = System.IO.File.ReadAllText(SMALL_TEST_FILE_PATH);
            Console.WriteLine("Small Test - JSON parse... {0} bytes ({1} kb)", small.Length, ((decimal)small.Length / (decimal)1024));
            Console.WriteLine();

            Console.WriteLine("\tParsed by {0} in...", parser.GetType().FullName);
            DateTime start = DateTime.Now;
            var obj = parser.Parse(small);
            Console.WriteLine("\t\t{0} ms", (int)DateTime.Now.Subtract(start).TotalMilliseconds);
            Console.WriteLine();

            string json = System.IO.File.ReadAllText(FATHERS_TEST_FILE_PATH);
            Console.WriteLine("Fathers Test - JSON parse... {0} kb ({1} mb)", (int)(json.Length / 1024), (int)(json.Length / (1024 * 1024)));
            Console.WriteLine();

            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer
            {
                MaxJsonLength = int.MaxValue
            };
            Console.WriteLine("\tParsed by {0} in...", serializer.GetType().FullName);
            DateTime start1 = DateTime.Now;
            var msObj = serializer.DeserializeObject(json);
            Console.WriteLine("\t\t{0} ms", (int)DateTime.Now.Subtract(start1).TotalMilliseconds);
            Console.WriteLine();

            Console.WriteLine("\tParsed by {0} in...", parser.GetType().FullName);
            DateTime start2 = DateTime.Now;
            var myObj = parser.Parse(json);
            Console.WriteLine("\t\t{0} ms", (int)DateTime.Now.Subtract(start2).TotalMilliseconds);
            Console.WriteLine();

            Console.WriteLine("Press '1' to inspect our result object,\r\nany other key to inspect Microsoft's JS serializer result object...");
            var parsed = ((Console.ReadKey().KeyChar == '1') ? myObj : msObj);

            IList<object> items = (IList<object>)((IDictionary<string, object>)parsed)["fathers"];
            Console.WriteLine();
            Console.WriteLine("Found : {0} fathers", items.Count);
            Console.WriteLine();
            Console.WriteLine("Press a key to list them...");
            Console.WriteLine();
            Console.ReadKey();
            Console.WriteLine();
            foreach (object item in items)
            {
                var father = item.JsonObject();
                var name = (string)father["name"];
                var sons = father["sons"].JsonArray();
                var daughters = father["daughters"].JsonArray();
                Console.WriteLine("{0}", name);
                Console.WriteLine("\thas {0} son(s), and {1} daughter(s)", sons.Count, daughters.Count);
                Console.WriteLine();
            }
            Console.WriteLine();
            Console.WriteLine("The end... Press a key...");

            Console.ReadKey();
        }
    }
}
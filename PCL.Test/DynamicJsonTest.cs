using System;
using System.Dynamic;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.Core.Utils;

namespace PCL.Test
{
    [TestClass]
    public class DynamicJsonTest
    {
        [TestMethod]
        public void TestDynamicJson()
        {
            dynamic obj = new { a = 123, b = new { c = this, d = "text" } };
            var options = new JsonSerializerOptions
            {
                WriteIndented = true, 
                Converters = { new ExpandoObjectConverter() },
                AllowTrailingCommas = true
            };
            string json = JsonSerializer.Serialize(obj, options);
            Console.WriteLine(json);
            dynamic obj2 = JsonSerializer.Deserialize<ExpandoObject>(json, options);
            Console.WriteLine(obj2.ToString());
        }
    }
}

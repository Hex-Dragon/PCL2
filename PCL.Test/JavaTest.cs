using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using PCL.Core.Java;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PCL.Test
{
    [TestClass]
    public class JavaTest
    {
        [TestMethod]
        public async Task TestJavaSearch()
        {
            var res = await JavaModel.ScanJava();
            //Assert.IsTrue(res.Count > 0, "No Java successfully found.");
            foreach (var ja in res)
            {
                Assert.IsTrue(ja.Version.Major > 0, "Java version is not valid: " + ja.Path);
                Assert.IsTrue(!string.IsNullOrWhiteSpace(ja.Path));
            }
            Logger.LogMessage("Got result: {0}", res.Select(x => x.Path).ToList());
        }
    }
}

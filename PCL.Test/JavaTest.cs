using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using PCL.Core.Helper;

namespace PCL.Test
{
    [TestClass]
    public class JavaTest
    {
        [TestMethod]
        public async Task TestJavaSearch()
        {
            var jas = new JavaManage();
            await jas.ScanJava();
            //Assert.IsTrue(res.Count > 0, "No Java successfully found.");
            foreach (var ja in jas.JavaList)
            {
                Assert.IsTrue(ja.Version.Major > 0, "Java version is not valid: " + ja.JavaFolder);
                Assert.IsTrue(!string.IsNullOrWhiteSpace(ja.JavaFolder));
            }
        }
    }
}

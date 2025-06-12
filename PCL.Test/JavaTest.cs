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
            // Java 搜索是否稳定
            var jas = new JavaManage();
            await jas.ScanJava();
            var firstScanedCount = jas.JavaList.Count;
            foreach (var ja in jas.JavaList)
            {
                Console.WriteLine(ja.ToString());
                Assert.IsTrue(ja.Version.Major > 0, "Java version is not valid: " + ja.JavaFolder);
                Assert.IsTrue(!string.IsNullOrWhiteSpace(ja.JavaFolder));
            }
            await jas.ScanJava();
            var secondScanedCount = jas.JavaList.Count;
            Assert.IsTrue(firstScanedCount == secondScanedCount);
            // Java 搜索是否能够正确选择
            Assert.IsTrue(jas.JavaList.Count == 0 || (jas.JavaList.Count > 0 && (await jas.SelectSuitableJava(new Version(1, 8, 0), new Version(30, 0, 0))).Count > 0));
            // Java 是否有重复
            Assert.IsFalse(jas.JavaList.GroupBy(x => x.JavawExePath).Any(x => x.Count() > 1));
        }
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PCL.Core.Helper;

namespace PCL.Test
{
    [TestClass]
    public class SemVerTest
    {
        [TestMethod]
        public void TestParse()
        {
            var t1 = SemVer.Parse("2.3.4");
            Assert.IsTrue(
                t1.Major == 2
                && t1.Minor == 3
                && t1.Patch == 4
                );
            var t2 = SemVer.Parse("2.3.4-beta.1");
            Assert.IsTrue(
                t2.Major == 2
                && t2.Minor == 3
                && t2.Patch == 4
                && t2.Prerelease == "beta.1"
                );
            var t3 = SemVer.Parse("2.3.4-beta.1+11451aq");
            Assert.IsTrue(
                t3.Major == 2
                && t3.Minor == 3
                && t3.Patch == 4
                && t3.Prerelease == "beta.1"
                && t3.BuildMetadata == "11451aq"
                );
        }
    }
}

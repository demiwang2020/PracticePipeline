using NETCoreMUStaticLib.UpdateHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCoreMUStaticLib.Testcases
{
    class TestcaseVerifyDuplicatedPackage : TestcaseBase
    {
        public TestcaseVerifyDuplicatedPackage(InnerData inputData, Update expectUpdate, Update actualUpdate)
            : base(inputData, expectUpdate, actualUpdate, "Verify update does not carry duplicated packages")
        {
        }

        protected override void RunTest()
        {
            _result.LogMessage("Verifying duplicated packages from child update title...");

            List<string> lst = new List<string>();
            foreach (var child in _actualUpdate.ChildUpdates)
            {
                if (lst.Contains(child.Title.ToLower()))
                {
                    GenerateFailResult("Duplicated packages detected", "Duplicated Package detected from update title", "No duplicated package", child.Title);
                }
                else
                {
                    lst.Add(child.Title.ToLower());
                }
            }

            _result.LogMessage("Verifying duplicated packages from package name...");

            lst.Clear();
            foreach (var child in _actualUpdate.ChildUpdates)
            {
                if (child.Title.Contains(" (app-host pack) ") || child.Title.Contains(" Hosting "))
                    continue;

                string packageName = System.IO.Path.GetFileName(child.PackagePath).ToLower();

                if (lst.Contains(packageName))
                {
                    GenerateFailResult("Duplicated packages detected", "Duplicated Packages detected from package name", "No duplicated package", packageName);
                }
                else
                {
                    lst.Add(packageName);
                }
            }
        }
    }
}

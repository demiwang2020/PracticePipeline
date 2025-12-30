using PubsuiteStaticTestLib.UpdateHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubsuiteStaticTestLib.Testcases
{
    class TestcaseVerifyPackage : TestcaseBase
    {
        public TestcaseVerifyPackage(InputData inputData, Update expectUpdate, Update actualUpdate)
            : base(inputData, expectUpdate, actualUpdate, "Package Verification (Verify package on pubsuite is same as TFS)")
        {
        }

        protected override void RunTest()
        {
            foreach (Update expectChildUpdate in _expectedUpdate.ChildUpdates)
            {
                Update actualChildUpdate = _actualUpdate.GetMatchedChildUpdate(expectChildUpdate);

                string path1 = expectChildUpdate.PackagePath;
                string path2 = actualChildUpdate.PackagePath;

                _result.LogMessage("Verifying " + actualChildUpdate.Title + "...");
                _result.LogMessage("TFS package location = " + path1);
                _result.LogMessage("Pubsuite package location = " + path2);

                bool bSame = true;
                if (!path1.Equals(path2, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!File.Exists(path1))
                    {
                        _result.LogMessage(path1 + " does not exist or you do not have permission to access");
                        bSame = false;
                    }
                    else if (!File.Exists(path2))
                    {
                        _result.LogMessage(path2 + " does not exist or you do not have permission to access");
                        bSame = false;
                    }
                    else
                    {
                        //bSame = FileAttrCompare(path1, path2);
                        bSame = HashCompare(path1, path2);
                    }
                }

                if (bSame)
                {
                    _result.LogMessage("Pubsuite package is same as TFS");
                }
                else
                {
                    _result.LogError("Pubsuite package is different from TFS");
                }

                _result.Result &= bSame;
            }
        }

        private bool HashCompare(string path1, string path2)
        {
            bool bSame = true;
            //Get CRC value of each patch
            var hash = System.Security.Cryptography.HashAlgorithm.Create();
            var stream_1 = new System.IO.FileStream(path1, System.IO.FileMode.Open, FileAccess.Read);
            byte[] hashByte_1 = hash.ComputeHash(stream_1);
            stream_1.Close();

            var stream_2 = new System.IO.FileStream(path2, System.IO.FileMode.Open, FileAccess.Read);
            byte[] hashByte_2 = hash.ComputeHash(stream_2);
            stream_2.Close();

            if (hashByte_1.Length != hashByte_2.Length)
            {
                bSame = false;
            }
            else
            {
                for (int i = 0; i < hashByte_2.Length; ++i)
                {
                    if (hashByte_1[i] != hashByte_2[i])
                    {
                        bSame = false;
                        break;
                    }
                }
            }

            return bSame;
        }

        private bool FileAttrCompare(string path1, string path2)
        {
            FileInfo fileInfo1 = new FileInfo(path1);
            FileInfo fileInfo2 = new FileInfo(path2);

            //Name
            if (!fileInfo1.Name.Equals(fileInfo2.Name, StringComparison.InvariantCultureIgnoreCase))
                return false;

            //Size
            if (fileInfo1.Length != fileInfo2.Length)
                return false;

            //Modified date
            if (fileInfo1.LastWriteTimeUtc != fileInfo2.LastWriteTimeUtc)
                return false;

            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Reflection;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace ScorpionDAL
{
    partial class TTestProdAttribute
    {
        public Context ContextObject { get; set; }
    }

    //TP TestCase support. Test case requires manifest (payload) file in the context. This datamember will store manifest file path.
    partial class TRun
    {
        public string PayLoadFilex86 { get; set; }
        public string SupersedingPatch { get; set; }
        public string SupersedingMSP { get; set; }

        public string Status { get; set; }
        
    }

    partial class TTestMatrix:ICloneable
    {
        //TTestMatrix Clone()
        //{
        //    //return (TTestMatrix)  Clone();
        //    MemoryStream ms = new MemoryStream();
        //    BinaryFormatter bf = new BinaryFormatter();
        //    bf.Serialize(ms, this);
        //    ms.Position = 0;
        //    TTestMatrix obj = (TTestMatrix)bf.Deserialize(ms);
        //    ms.Close();
        //    return obj;

        //    //TTestMatrix objTestMatrix = new TTestMatrix();
        //    //PropertyInfo[] arrProperties = this.GetType().GetProperties();
        //    //foreach (PropertyInfo objPropertyInfo in arrProperties)
        //    //{
        //    //     objPropertyInfo.Name
        //    //}

        //    //objTestMatrix.Active = Active;
        //    //return objTestMatrix;
        //}

        #region ICloneable Members

        //public AnotherClass deepRef;

        public object Clone()
        {
            //TTestMatrix mc = (TTestMatrix) this.MemberwiseClone();
            //mc.deepRef = new AnotherClass();
            //mc.deepRef = (AnotherClass) this.deepRef.Clone(); 
            //return mc;
            
            //MemoryStream ms = new MemoryStream();
            //BinaryFormatter bf = new BinaryFormatter();
            //TCPU objCPU = new TCPU();
            //bf.Serialize(ms, objCPU);
            //ms.Position = 0;
            //object obj = bf.Deserialize(ms);
            //ms.Close();
            //return obj;

            //TTestMatrix objTestMatrix = new TTestMatrix();
            //objTestMatrix = (TTestMatrix) this.MemberwiseClone();
            //return objTestMatrix;

            TTestMatrix objTestMatrix = new TTestMatrix();
            objTestMatrix.TestMatrixID = -1;
            objTestMatrix.MDOSID = MDOSID;
            objTestMatrix.OSImageID = OSImageID;
            objTestMatrix.ProductID = ProductID;
            objTestMatrix.ProductSKUID = ProductSKUID;
            objTestMatrix.ProductCPUID = ProductCPUID;
            objTestMatrix.ProductLanguageID = ProductLanguageID;
            objTestMatrix.ProductSPLevel = ProductSPLevel;
            objTestMatrix.TestCaseID = TestCaseID;
            objTestMatrix.TestCaseSpecificData = TestCaseSpecificData;
            objTestMatrix.TestMatrixPriority = TestMatrixPriority;
            objTestMatrix.KBNumber = KBNumber;
            objTestMatrix.TestMatrixName = TestMatrixName;
            objTestMatrix.CreatedBy = CreatedBy;
            objTestMatrix.CreatedDate = CreatedDate;
            objTestMatrix.LastModifiedBy = LastModifiedBy;
            objTestMatrix.LastModifiedDate = LastModifiedDate;
            objTestMatrix.Active = Active;
            objTestMatrix.ApplicationType = ApplicationType;
            objTestMatrix.MaddogDBID = MaddogDBID; 
            return objTestMatrix;
        }

        #endregion

        public static string CreateCopyTestMatrix(string strNewTestMatrixName, string strExistingTestMatrixName, string strCreatedBy)
        {
            string strResult;
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();

            var vTestMatrixCheck = from t in db.TTestMatrixes
                              where t.TestMatrixName == strNewTestMatrixName
                              select t;

            if (vTestMatrixCheck.Count() > 0)
            {
                strResult = "Test matrix '" + strNewTestMatrixName + "' already exists. Please select another name";
                return strResult;
            }

            var vTestMatrix = from t in db.TTestMatrixes
                              where t.TestMatrixName == strExistingTestMatrixName
                              select t;

            
            List<TTestMatrix> lstTestMatrix = new List<TTestMatrix>();
            foreach (TTestMatrix objTestMatrixExisting in vTestMatrix)
            {
                TTestMatrix objTestMatrixNew = (TTestMatrix) objTestMatrixExisting.Clone();

                objTestMatrixNew.TestMatrixName = strNewTestMatrixName;
                objTestMatrixNew.CreatedBy = strCreatedBy;
                objTestMatrixNew.CreatedDate = DateTime.Now;
                objTestMatrixNew.LastModifiedBy = null;
                objTestMatrixNew.LastModifiedDate = null;

                lstTestMatrix.Add(objTestMatrixNew);
            }

            db.TTestMatrixes.InsertAllOnSubmit(lstTestMatrix);
            db.SubmitChanges();

            strResult = "Success";
            return strResult;
        }

        public int SaveToDB()
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            db.TTestMatrixes.InsertOnSubmit(this);
            db.SubmitChanges();
            return TestMatrixID;
        }
    }

    partial class TO
    {
        public string SaveToDB()
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            try
            {
                db.TOs.InsertOnSubmit(this);
                db.SubmitChanges();
                return "Success";
            }
            catch (Exception ex)
            {
                return "Insertion Failed: Error Message - " + ex.Message;
            }
        }
    }

    partial class TOSImage
    {
        public string SaveToDB()
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            try
            {
                db.TOSImages.InsertOnSubmit(this);
                db.SubmitChanges();
                return "Success";
            }
            catch (Exception ex)
            {
                return "Insertion Failed: Error Message - " + ex.Message;
            }
        }
    }

    partial class TContextAttribute
    {
        public string SaveToDB()
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            try
            {
                db.TContextAttributes.InsertOnSubmit(this);
                db.SubmitChanges();
                return "Success";
            }
            catch (Exception ex)
            {
                return "Insertion Failed: Error Message - " + ex.Message;
            }
        }
    }

    partial class TContextAttributeProductSpecificValue
    {
        public string SaveToDB()
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            try
            {
                db.TContextAttributeProductSpecificValues.InsertOnSubmit(this);
                db.SubmitChanges();
                return "Success";
            }
            catch (Exception ex)
            {
                return "Insertion Failed: Error Message - " + ex.Message;
            }
        }
    }

    partial class TTestCaseWithPriority
    {
        public string SaveToDB()
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            try
            {
                db.TTestCaseWithPriorities.InsertOnSubmit(this);
                db.SubmitChanges();
                return "Success";
            }
            catch (Exception ex)
            {
                return "Insertion Failed: Error Message - " + ex.Message;
            }
        }
    }
    
    partial class TTestCaseContextAttributeMapping
    {
        public string SaveToDB()
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            try
            {
                if(ActivateRecord() == true)
                    return "Success";

                db.TTestCaseContextAttributeMappings.InsertOnSubmit(this);
                db.SubmitChanges();
                return "Success";
            }
            catch (Exception ex)
            {
                return "Insertion Failed: Error Message - " + ex.Message;
            }
        }

        private bool ActivateRecord()
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            try
            {
                TTestCaseContextAttributeMapping objTestCaseContextAttributeMapping = db.TTestCaseContextAttributeMappings.Single(testcontext => (testcontext.TestCaseID == this.TestCaseID && testcontext.ContextAttributeID == this.ContextAttributeID));
                objTestCaseContextAttributeMapping.Active = true;
                objTestCaseContextAttributeMapping.LastModifiedBy = this.CreatedBy;
                objTestCaseContextAttributeMapping.LastModifiedDate = this.CreatedDate;
                db.SubmitChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string InactivateRecord(int intTestCaseID, short intContextAttributeID, string strUserName)
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            try
            {
                TTestCaseContextAttributeMapping objTestCaseContextAttributeMapping = db.TTestCaseContextAttributeMappings.Single(testcontext => (testcontext.TestCaseID == intTestCaseID && testcontext.ContextAttributeID == intContextAttributeID));
                objTestCaseContextAttributeMapping.Active = false;
                objTestCaseContextAttributeMapping.LastModifiedBy = strUserName;
                objTestCaseContextAttributeMapping.LastModifiedDate = DateTime.Now;
                db.SubmitChanges();
                return "Success";
            }
            catch (Exception ex)
            {
                return "Insertion Failed: Error Message - " + ex.Message;
            }
        }
    }

    //public class AnotherClass:ICloneable
    //{
    //    #region ICloneable Members

    //    public object Clone()
    //    {
    //        return this;
    //    }

    //    #endregion
    //}
}

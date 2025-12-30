using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

using RMIntegration.RMService;
using LoggerLibrary;


namespace RMIntegration
{
    public class RMSvcMethods
    {
        #region Data Members
        private readonly ReleaseDataClient _releaseManClient;
        private readonly int _workItemId;
        
        /// <summary>
        /// Raw private patch object, contains all patch related meta-data
        /// </summary>
        public Patch PPatch { get; private set; }

        public String RMServiceErrorMessage { get; private set; }

        #endregion

        public string GetCustom1Data()
        {
            return _releaseManClient.GetCustom1DataById(_workItemId);
        }

        public string GetBaseBuildNumber()
        {
            return _releaseManClient.GetBaseBuildNumberById(_workItemId);
        }

        public int GetLCUKBNumber()
        {
            return _releaseManClient.GetLCUKBNumberById(_workItemId);
        }
        /// <summary>
        /// private constructor for internal processing
        /// </summary>
        private RMSvcMethods()
        {
            _releaseManClient = new ReleaseDataClient();
            PPatch = null;
        }

        /// <summary>
        /// Constructor initiate connection with ReleaseMan data service
        /// </summary>
        /// <param name="workItemId"></param>
        public RMSvcMethods(int workItemId): this()
        {            
            _workItemId = workItemId;            
        }

        /// <summary>
        /// Fetches specific Patch details for work item id.
        /// </summary>
        public void Populate()
        {
            try
            {
                _releaseManClient.Open();
                PPatch = _releaseManClient.GetPatchById(_workItemId);
                _releaseManClient.Close();
            }
            catch (Exception eX)
            {                
                RMServiceErrorMessage = eX.Message;
                //Logger.Instance.AddLogMessage("ReleaseMan Intergration", LogHelper.LogLevel.ERROR,  , eX);
                Console.WriteLine("ReleaseMan Integration: {0}", eX.Message);
                throw new Exception("RMSvcMethods.Populate had an issue with the connection.", eX);
            }
            finally
            {
                if (_releaseManClient.State == CommunicationState.Opened)
                    _releaseManClient.Close();
            }
        }

        private void PopualateAllActiveReleases()
        {
            _releaseManClient.Open();
            int[] releasesIds = _releaseManClient.GetActiveReleaseIds();
            //Release[] rell = client.GetActiveReleases();

            foreach (int rIndex in releasesIds)
            {
                try
                {
                    Release rel = _releaseManClient.GetRelease(rIndex);

                    if (rel == null || rel.Patches == null) continue;
                    foreach (Patch p in rel.Patches)
                    {
                        //TODO: Create list of patches and add to private data member
                    }
                }
                catch (Exception eX)
                {
                    //TODO: add logger call here
                    RMServiceErrorMessage += eX.Message + "\n";
                    Console.WriteLine(eX.Message);
                    continue;
                }
            }

            _releaseManClient.Close();
        }
    }
}

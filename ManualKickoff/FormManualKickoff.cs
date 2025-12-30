using System;
using System.Windows.Forms;
using THTestLib;

namespace ManualKickoff
{
    public partial class FormKickoff : Form
    {
        public FormKickoff()
        {
            InitializeComponent();
        }

        //private void buttonKickoff_Click(object sender, EventArgs e)
        //{
        //    int jobID = 0;
        //    var inputID = textBoxJobID.Text.Trim();
        //    if (!string.IsNullOrWhiteSpace(inputID) && !int.TryParse(inputID, out jobID))
        //    {
        //        MessageBox.Show("Please input a valid job id");
        //        return;
        //    }

        //    using (ScorpionDAL.PatchTestDataClassDataContext dataContext = new ScorpionDAL.PatchTestDataClassDataContext())
        //    {
        //        var objTJob = dataContext.TJobs.SingleOrDefault(c => c.JobID == jobID && c.Active == true);
        //        if (objTJob != null)
        //        {
        //            Job job = new Job(jobID);
        //            job.StartJob();
        //        }
        //        else
        //        {
        //            MessageBox.Show("There is no data in tjob datatable, please double check!");
        //            return;
        //        }
        //    }

        //    MessageBox.Show("Kickoff success!");
        //}

        private void BtnWin10Test_Click(object sender, EventArgs e)
        {
            THTestProcess.StartTest();
            MessageBox.Show("test finished");
        }

        /// <summary>
        /// For testing the local static runs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void button1_Click(object sender, EventArgs e)
        //{
        //    int workitemid = 0;
        //    if (!int.TryParse(textBox1.Text, out workitemid))
        //    {
        //        throw new Exception("Please enter a valid work item id to test");
        //    }
        //    BtnWin10Test.Enabled = false;
        //    THTestProcess.StartTest(workitemid);
        //    BtnWin10Test.Enabled = true;
        //}
    }
}

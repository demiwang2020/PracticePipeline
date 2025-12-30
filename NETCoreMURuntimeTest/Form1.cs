using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NETCoreMURuntimeTest
{
    public partial class NETCoreMUForm : Form
    {
        public NETCoreMUForm()
        {
            InitializeComponent();
        }

        private void button_Cancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button_Start_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(textBox_Release.Text) ||
                String.IsNullOrEmpty(textBox_Title.Text) ||
                String.IsNullOrEmpty(textBox_GUID.Text))
            {
                MessageBox.Show("Please input Release, title and GUID");
                return;
            }

            NETCoreMURuntimeLib.UpdateInfo updateInfo = new NETCoreMURuntimeLib.UpdateInfo()
            {
                ReleaseName = textBox_Release.Text.Trim(),
                Title = textBox_Title.Text.Trim(),
                BundleGUID = textBox_GUID.Text.Trim()
            };

            List<int> runIDs = NETCoreMURuntimeLib.NETCoreMU.KickoffRuntimeTest(updateInfo);

            if (runIDs != null && runIDs.Count > 0)
            {
                MessageBox.Show(String.Format("Done, {0} runs were created", runIDs.Count));
            }
        }
    }
}

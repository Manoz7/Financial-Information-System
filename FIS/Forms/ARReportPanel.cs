using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FIS.Forms
{
    public partial class ARReportPanel : UserControl
    {
        public ARReportPanel()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(240, 243, 248);

            BuildUI();
        }

        private void BuildUI()
        {
            // Temporary placeholder — replace when building this module
            var lbl = new Label
            {
                Text = "AR Report",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 190, 210),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lbl);
        }
    }
}

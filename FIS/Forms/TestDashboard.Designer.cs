namespace FIS.Forms
{
    partial class TestDashboard
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Button btnLoadVendors;
        private System.Windows.Forms.Button btnLoadInvoices;
        private System.Windows.Forms.Button btnLoadCustomers;
        private System.Windows.Forms.DataGridView dataGridView1;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.btnLoadVendors = new System.Windows.Forms.Button();
            this.btnLoadInvoices = new System.Windows.Forms.Button();
            this.btnLoadCustomers = new System.Windows.Forms.Button();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // btnLoadVendors
            // 
            this.btnLoadVendors.Location = new System.Drawing.Point(223, 20);
            this.btnLoadVendors.Name = "btnLoadVendors";
            this.btnLoadVendors.Size = new System.Drawing.Size(75, 23);
            this.btnLoadVendors.TabIndex = 0;
            this.btnLoadVendors.Text = "Load Vendors";
            this.btnLoadVendors.Click += new System.EventHandler(this.btnLoadVendors_Click);
            // 
            // btnLoadInvoices
            // 
            this.btnLoadInvoices.Location = new System.Drawing.Point(20, 20);
            this.btnLoadInvoices.Name = "btnLoadInvoices";
            this.btnLoadInvoices.Size = new System.Drawing.Size(75, 23);
            this.btnLoadInvoices.TabIndex = 1;
            this.btnLoadInvoices.Text = "Load Invoices";
            this.btnLoadInvoices.Click += new System.EventHandler(this.btnLoadInvoices_Click);
            // 
            // btnLoadCustomers
            // 
            this.btnLoadCustomers.Location = new System.Drawing.Point(128, 20);
            this.btnLoadCustomers.Name = "btnLoadCustomers";
            this.btnLoadCustomers.Size = new System.Drawing.Size(75, 23);
            this.btnLoadCustomers.TabIndex = 2;
            this.btnLoadCustomers.Text = "Load Customers";
            this.btnLoadCustomers.Click += new System.EventHandler(this.btnLoadCustomers_Click);
            // 
            // dataGridView1
            // 
            this.dataGridView1.ColumnHeadersHeight = 29;
            this.dataGridView1.Location = new System.Drawing.Point(20, 70);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersWidth = 51;
            this.dataGridView1.Size = new System.Drawing.Size(760, 350);
            this.dataGridView1.TabIndex = 3;
            // 
            // TestDashboard
            // 
            this.ClientSize = new System.Drawing.Size(863, 523);
            this.Controls.Add(this.btnLoadVendors);
            this.Controls.Add(this.btnLoadInvoices);
            this.Controls.Add(this.btnLoadCustomers);
            this.Controls.Add(this.dataGridView1);
            this.Name = "TestDashboard";
            this.Text = "Test Dashboard";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
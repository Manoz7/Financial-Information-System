using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FIS.Services;

namespace FIS.Forms
{
    public partial class TestDashboard : Form
    {
        public TestDashboard()
        {
            InitializeComponent();
        }

        private void btnLoadVendors_Click(object sender, EventArgs e)
        {
            try
            {
                var service = new VendorService();
                dataGridView1.DataSource = service.GetAllVendors();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading vendors: " + ex.Message);
            }
        }

        private void btnLoadInvoices_Click(object sender, EventArgs e)
        {
            try
            {
                var service = new InvoiceAPService();
                dataGridView1.DataSource = service.GetAllInvoices();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading invoices: " + ex.Message);
            }
        }

        private void btnLoadCustomers_Click(object sender, EventArgs e)
        {
            try
            {
                var service = new CustomerOrderService();
                dataGridView1.DataSource = service.GetAllOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading customers: " + ex.Message);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Procurement.Views
{
    public partial class frmSpreadSheetControl : Form
    {
        private static frmSpreadSheetControl instance = null;
        public frmSpreadSheetControl()
        {
            InitializeComponent();
        }
        public static frmSpreadSheetControl Instance
        {
            get
            {
                if (instance == null || instance.IsDisposed)
                {

                    instance = new frmSpreadSheetControl();
                }


                return instance;
            }
        }
    }
}

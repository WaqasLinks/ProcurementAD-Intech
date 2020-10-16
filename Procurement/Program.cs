﻿using Procurement.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Procurement
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(FrmMDI.Instance);
            //Application.Run(new FrmMDI());
            //Application.Run(new FrmMain());
            //Application.Run(new FrmLogin());
            //Application.Run(new FrmEmployee());

        }
    }
}

﻿using StaticClasses;
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
    public partial class FrmMDI : Form
    {
        private static FrmMDI instance = null;
        private FrmMDI()
        {
            InitializeComponent();
        }
        public static FrmMDI Instance
        {
            get
            {
                if (instance == null || instance.IsDisposed)
                {

                    instance = new FrmMDI();
                }


                return instance;
            }
        }
        private void FrmMDI_Load(object sender, EventArgs e)
        {
            OnFormLoad();
            FrmProjects_Show();
            //contextMenuStrip1.Show();
         
        }
        private void FrmMDI_Activated(object sender, EventArgs e)
        {
            //contextMenuStrip1.Show(this, 600, 600);
            //contextMenuStrip1.AutoClose = false;
        }
        private void button1_Click(object sender, EventArgs e)
        {

        }
        private void OnFormLoad()
        {
            this.Visible = false;
            FrmLogin frmLogin = new FrmLogin();
            frmLogin.ShowDialog();
            userToolStripMenuItem.Text = LoginInfo.LoginEmployee.EmployeeName;
        }
        private void projectsListToolStripMenuItem_Click(object sender, EventArgs e)
        {

            FrmProjects_Show();
        }
        private void FrmProjects_Show()
        {
            FrmProjects.Instance.MdiParent = FrmMDI.Instance; //this;
            if (!FrmProjects.Instance.Visible)
            {
                FrmProjects.Instance.Show();
            }
            else
            {
                if (FrmProjects.Instance.WindowState == FormWindowState.Minimized)
                {
                    FrmProjects.Instance.WindowState = FormWindowState.Normal;
                }
                else
                {
                    FrmProjects.Instance.BringToFront();
                }

            }
            // FrmBOM_Show();

            //FrmBOM frmBOM = FrmBOM.Instance;
            //frmBOM.Show();
            // FrmBOM.Instance.Show();

        }

        private void newProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmNewProject_Show();


        }
        private void FrmNewProject_Show()
        {
            FrmNewProject.Instance.MdiParent = FrmMDI.Instance; //this;
            FrmNewProject.Instance._newMode = true;
            if (!FrmNewProject.Instance.Visible)
            {
                FrmNewProject.Instance.Show();
            }
            else
            {
                if (FrmNewProject.Instance.WindowState == FormWindowState.Minimized)
                {
                    FrmNewProject.Instance.WindowState = FormWindowState.Normal;
                }
                else
                {
                    FrmNewProject.Instance.BringToFront();
                }

            }

        }

        private void openBOMsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentOpenProject.CurrentProject == null)
            {
                MessageBox.Show("Please Open Project First");
                return;
            }
            FrmBOM_Show();
        }
        private void FrmBOM_Show()
        {
            FrmBOM.Instance.MdiParent = FrmMDI.Instance; //this;
            if (!FrmBOM.Instance.Visible)
            {
                FrmBOM.Instance.Show();
            }
            else
            {
                if (FrmBOM.Instance.WindowState == FormWindowState.Minimized)
                {
                    FrmBOM.Instance.WindowState = FormWindowState.Normal;
                }
                else
                {
                    FrmBOM.Instance.BringToFront();
                }

            }

        }

        private void openMaterialRequestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentOpenProject.CurrentProject == null)
            {
                MessageBox.Show("Please Open Project First");
                return;
            }
            FrmMR_Show();
        }
        private void FrmMR_Show()
        {
            FrmMR.Instance.MdiParent = FrmMDI.Instance; //this;
            if (!FrmMR.Instance.Visible)
            {
                FrmMR.Instance.Show();
            }
            else
            {
                if (FrmMR.Instance.WindowState == FormWindowState.Minimized)
                {
                    FrmMR.Instance.WindowState = FormWindowState.Normal;
                }
                else
                {
                    FrmMR.Instance.BringToFront();
                }

            }

        }

        private void mRListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentOpenProject.CurrentProject == null)
            {
                MessageBox.Show("Please Open Project First");
                return;
            }
            FrmMRs_Show();
        }
        private void FrmMRs_Show()
        {
            FrmMRs.Instance.MdiParent = FrmMDI.Instance; //this;
            if (!FrmMRs.Instance.Visible)
            {
                FrmMRs.Instance.Show();
            }
            else
            {
                if (FrmMRs.Instance.WindowState == FormWindowState.Minimized)
                {
                    FrmMRs.Instance.WindowState = FormWindowState.Normal;
                }
                else
                {
                    FrmMRs.Instance.BringToFront();
                }

            }

        }

        private void modifyMaterialRequestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentOpenProject.CurrentProject == null)
            {
                MessageBox.Show("Please Open Project First");
                return;
            }
            FrmModifyMR_Show();
        }
        private void FrmModifyMR_Show()
        {
            FrmModifyMR.Instance.MdiParent = FrmMDI.Instance; //this;
            if (!FrmModifyMR.Instance.Visible)
            {
                FrmModifyMR.Instance.Show();
            }
            else
            {
                if (FrmModifyMR.Instance.WindowState == FormWindowState.Minimized)
                {
                    FrmModifyMR.Instance.WindowState = FormWindowState.Normal;
                }
                else
                {
                    FrmModifyMR.Instance.BringToFront();
                }

            }

        }

        private void employeesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmEmployees_Show();
        }
        private void FrmEmployees_Show()
        {
            FrmEmployeeAD.Instance.MdiParent = FrmMDI.Instance; //this;
            
            if (!FrmEmployeeAD.Instance.Visible)
            {
                FrmEmployeeAD.Instance.Show();
            }
            else
            {
                if (FrmEmployeeAD.Instance.WindowState == FormWindowState.Minimized)
                {
                    FrmEmployeeAD.Instance.WindowState = FormWindowState.Normal;
                }
                else
                {
                    FrmEmployeeAD.Instance.BringToFront();
                }

            }
        }

        private void logoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            for (int i = Application.OpenForms.Count - 1; i >= 0; i--)
            {
                if (Application.OpenForms[i].Name != "FrmMDI")
                {
                    Application.OpenForms[i].Close();
                }
            }
            OnFormLoad();
        }

        private void userToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmEmployeeAD.Instance._EmployeeCode = LoginInfo.LoginEmployee.EmployeeCode;
            FrmEmployees_Show();
            
        }

        private void FrmMDI_Click(object sender, EventArgs e)
        {
            
        }

        private void linkLabel5_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {

        }

        private void FrmMDI_KeyDown(object sender, KeyEventArgs e)
        {
            //if (e.Button == System.Windows.Forms.MouseButtons.Right)
            //{
            //    MenuStripDelete.Show(Cursor.Position);

            //}
        }
    }
}

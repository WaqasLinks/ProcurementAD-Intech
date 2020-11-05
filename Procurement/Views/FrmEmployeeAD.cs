using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Data.OleDb;
using Repository.DAL;
using Procurement.Controllers;
using System.Reflection;
using StaticClasses;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices;

namespace Procurement
{
    public partial class FrmEmployeeAD : Form
    {
        EmployeeController _ec;
        EmployeeTypeController _etc;
        ProjectController _pc;
        ProjectEmployeeDetailController _pedc;

        List<Employee> _LstEmployees;
        List<EmployeeType> _LstEmployeeTypes;
        List<Project> _LstProjects;
        DataTable _dtEmployees;
        DataTable _dtProjects;
        public decimal _EmployeeCode;
        decimal _maxEmpCode;
        bool _newMode;
        Employee _currentLoadedEmployee;


        private static FrmEmployeeAD instance = null;
        private FrmEmployeeAD()
        {
            InitializeComponent();
        }

        public static FrmEmployeeAD Instance
        {
            get
            {
                if (instance == null || instance.IsDisposed)
                {

                    instance = new FrmEmployeeAD();
                }


                return instance;
            }
        }




        #region "Load On Start"
        //public FrmEmployeeAD(decimal pEmployeeCode)
        //{
        //    _EmployeeCode = pEmployeeCode;
        //}


        private void FrmEmployees_Load(object sender, EventArgs e)
        {
            try
            {
                _ec = new EmployeeController();
                _etc = new EmployeeTypeController();
                _pc = new ProjectController();
                Employee emp = new Employee();
                if (_EmployeeCode > 0)
                {// if employee viewing then this code will run... _EmployeeCode value is comming from FrmMain.
                    emp = _ec.GetModelByID(_EmployeeCode);
                    _LstEmployees = new List<Employee>();
                    _LstEmployees.Add(emp);
                    //disable controls for employee
                    btnNewEmployee.Visible = false;
                    btnResize.Visible = false;
                    splitContainer1.SplitterDistance = 0;
                    dataGridViewEmployees.Visible = false;
                    cmbEmployeeType.Enabled = false;
                    dataGridViewSelectProjects.Enabled = false;
                    this.Text = emp.EmployeeName;
                }
                else
                {//for manager view
                    //_LstEmployees = _ec.GetModelsByCreatedBy();
                    _LstEmployees = _ec.GetModels();

                }

                _LstEmployeeTypes = _etc.GetModels();

                EmployeeType removeET;
                switch (LoginInfo.LoginEmployee.EmployeeTypeCode)
                {
                    case 1://Manager
                        //remove admin
                        removeET = _LstEmployeeTypes.Where(x => x.EmployeeTypeCode == 3).FirstOrDefault();
                        _LstEmployeeTypes.Remove(removeET);
                        break;

                    case 2://Employee
                        //remove admin, manager
                        removeET = _LstEmployeeTypes.Where(x => x.EmployeeTypeCode == 1).FirstOrDefault();
                        _LstEmployeeTypes.Remove(removeET);
                        removeET = _LstEmployeeTypes.Where(x => x.EmployeeTypeCode == 3).FirstOrDefault();
                        _LstEmployeeTypes.Remove(removeET);
                        break;

                    //case 3://Admin
                    //    Console.WriteLine("case 9");
                    //    break;


                }





                _LstProjects = _pc.GetModelsByCreatedByLoginedEmp();//_pc.GetModels();




                _dtEmployees = ToDataTable<Employee>(_LstEmployees);
                _dtProjects = ToDataTable<Project>(_LstProjects);

                //var bindingSource1 = new BindingSource();
                //bindingSource1.DataSource = _LstProjects.OrderByDescending(x=>x.ProjectCode).ToList();
                //cmbProjects.DataSource = bindingSource1.DataSource;
                //cmbProjects.DisplayMember = "ProjectName";
                //cmbProjects.ValueMember = "ProjectCode";

                //FillCmbManagers();


                //_dtProjects.Columns.Remove("BOMs");
                //_dtProjects.Columns.Remove("ProjectEmployeeDetails");



                _dtProjects.Columns.Remove("CreatedBy");
                _dtProjects.Columns.Remove("UpdatedBy");


                _dtProjects.Columns.Add("Select Projects", typeof(Boolean)).SetOrdinal(0);

                dataGridViewSelectProjects.DataSource = _dtProjects;
                //dataGridViewSelectProjects.Columns["Select Projects"].Width = 30;

                var bindingSource3 = new BindingSource();
                bindingSource3.DataSource = _LstEmployeeTypes;
                cmbEmployeeType.DataSource = bindingSource3.DataSource;
                cmbEmployeeType.DisplayMember = "EmployeeType1";
                cmbEmployeeType.ValueMember = "EmployeeTypeCode";
                cmbEmployeeType.SelectedIndex = 0;



                /////////////////
                //_dtEmployees.Columns.Add("Employee Type");
                //_dtEmployees.Columns.Add("Project Name");
                //foreach (DataRow dr in _dtEmployees.Rows) // search whole table
                //{
                //    if (dr["Manager"] == null) dr["Manager"] = DBNull.Value;
                //    if (dr["ProjectCode"] == null) dr["ProjectCode"] = DBNull.Value;
                //    EmployeeType et = (EmployeeType) dr["EmployeeType"];
                //    dr["Employee Type"] = et.EmployeeType1.ToString();

                //    if (dr["Project"] != DBNull.Value)
                //    {
                //        Project proj = (Project)dr["Project"];
                //        dr["Project Name"] = proj.ProjectName.ToString();
                //    }
                //}

                _dtEmployees.Columns.Remove("EmployeeTypeCode");
                //_dtEmployees.Columns.Remove("EmployeeType");
                _dtEmployees.Columns.Remove("ProjectCode");
                _dtEmployees.Columns.Remove("Manager");
                _dtEmployees.Columns.Remove("Password");
                _dtEmployees.Columns.Remove("CreatedBy");
                _dtEmployees.Columns.Remove("UpdateDate");
                _dtEmployees.Columns.Remove("UpdatedBy");
                //_dtEmployees.Columns.Remove("Active");

                //_dtEmployees.Columns.Remove("ProjectEmployeeDetails");

                ////////////////////
                DataView dv = _dtEmployees.DefaultView;
                dv.Sort = "EmployeeCode desc";
                _dtEmployees = dv.ToTable();

                dataGridViewEmployees.DataSource = _dtEmployees;
                dataGridViewEmployees.Columns["EmployeeCode"].Visible = false;


                if (_LstEmployees.Count == 0)
                {

                    _newMode = true;
                    _ec.ReseedPk();
                    //txtProjectCode.Text = (1).ToString();
                    SetForNew();
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }
        //private void FillCmbManagers()
        //{
        //    var bindingSource2 = new BindingSource();
        //    bindingSource2.DataSource = _LstEmployees.Where(x => x.EmployeeTypeCode == 1).OrderBy(y => y.EmployeeName).ToList();
        //    cmbManagers.DataSource = bindingSource2.DataSource;
        //    cmbManagers.DisplayMember = "EmployeeName";
        //    cmbManagers.ValueMember = "EmployeeCode";
        //}
        private void cmbEmployeeType_SelectedIndexChanged(object sender, EventArgs e)
        {
            //if (cmbEmployeeType.SelectedValue.ToString() == 1.ToString())
            //{
            //    cmbManagers.Enabled = false;
            //    lblManager.Enabled = false;
            //}
            //else
            //{
            //    cmbManagers.Enabled = true;
            //    lblManager.Enabled = true;
            //}
        }

        #endregion "Load On Start"



        private void btnSave_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Do you want to save?", "Confirmation", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No) return;
            this.Enabled = false;
            Employee empModel;
            List<ProjectEmployeeDetail> LstPed;
            if (_newMode == true)
            {
                //SaveData();
                empModel = FillEmployeeModelFromUI();
                empModel.CreatedBy = LoginInfo.LoginEmployee.EmployeeCode;
                empModel.CreatedDate = DateTime.Now;

                _ec = new EmployeeController(empModel);
                _ec.InsertnSave();
                _EmployeeCode = empModel.EmployeeCode;
                //------------------
                LstPed = FillProjectEmployeeDetailModel();
                _pedc = new ProjectEmployeeDetailController(LstPed);
                _pedc.SaveList(empModel.EmployeeCode);
                //-------------------
                DataRow NewRow = _dtEmployees.NewRow();
                NewRow[0] = empModel.EmployeeCode;
                NewRow[1] = empModel.EmployeeName;
                //NewRow[2] = empModel.EmployeeTypeCode;
                //NewRow[3] = empModel.ProjectCode;
                //if (empModel.ProjectCode == null) { NewRow[3] = DBNull.Value; } else { NewRow[3] = empModel.ProjectCode; }
                //if (empModel.Manager == null){ NewRow[4] = DBNull.Value; } else { NewRow[4] = empModel.Manager; }

                //TO DO
                //if (empModel.ProjectCode == null) { NewRow[5] = DBNull.Value; } else { NewRow[5] = empModel.EmployeeType.EmployeeType1; }
                //if (empModel.Manager == null) { NewRow[6] = DBNull.Value; } else { NewRow[6] = empModel.Project.ProjectName; }




                _dtEmployees.Rows.Add(NewRow);

                DataView dv = _dtEmployees.DefaultView;
                dv.Sort = "EmployeeCode desc";
                _dtEmployees = dv.ToTable();

                dataGridViewEmployees.DataSource = _dtEmployees;
                _LstEmployees.Add(empModel);
                _newMode = false;
            }
            else
            {
                //UpdateData();
                empModel = FillEmployeeModelFromUI();
                empModel.CreatedBy = _currentLoadedEmployee.CreatedBy;
                empModel.CreatedDate = _currentLoadedEmployee.CreatedDate;
                empModel.UpdatedBy = LoginInfo.LoginEmployee.EmployeeCode;
                empModel.UpdateDate = DateTime.Now;
                _ec = new EmployeeController(empModel);
                _ec.UpdatenSaveModel(empModel);

                LstPed = FillProjectEmployeeDetailModel();
                _pedc = new ProjectEmployeeDetailController(LstPed);
                _pedc.SaveList(empModel.EmployeeCode);

                Employee emp = _LstEmployees.Where(x => x.EmployeeCode == empModel.EmployeeCode).FirstOrDefault();
                _LstEmployees.Remove(emp);
                _LstEmployees.Add(empModel);


                foreach (DataRow dr in _dtEmployees.Rows) // search whole table
                {
                    if (decimal.Parse(dr["EmployeeCode"].ToString()) == empModel.EmployeeCode) // if id==2
                    {
                        dr["EmployeeName"] = empModel.EmployeeName;
                        //dr["EmployeeTypeCode"] = empModel.EmployeeTypeCode;
                        //if (empModel.Manager == null){ dr["Manager"] = DBNull.Value; } else { dr["Manager"] = empModel.Manager; }
                        //if (empModel.ProjectCode == null) { dr["ProjectCode"] = DBNull.Value; } else { dr["ProjectCode"] = empModel.ProjectCode; }


                        break;
                    }
                }
            }

            MessageBox.Show("Updated Successfully");
            this.Close();
            //FillCmbManagers();
            //_LstProjects
            this.Enabled = true;

            //////////////update shared object///////////////////

            //_pc = new ProjectController();
            //CurrentOpenProject.CurrentProject = _pc.GetModelByID(_projectCode);
            ////////////

        }



        private Employee FillEmployeeModelFromUI()
        {
            Employee lObjEmp = new Employee();
            //if (_newMode == false) lObjEmp.EmployeeCode = decimal.Parse(txtEmployeeCode.Text);
            lObjEmp.EmployeeCode = decimal.Parse(txtEmployeeCode.Text);
            lObjEmp.EmployeeName = txtEmployeeName.Text;
            lObjEmp.EmployeeTypeCode = (short)cmbEmployeeType.SelectedValue;
            lObjEmp.Password = txtPassword.Text;
            lObjEmp.Active = _currentLoadedEmployee.Active;
            lObjEmp.Email = _currentLoadedEmployee.Email;
            lObjEmp.EmployeeId = _currentLoadedEmployee.EmployeeId;
            lObjEmp.SAM = _currentLoadedEmployee.SAM;

            //if ((short)cmbEmployeeType.SelectedValue == 1)
            //{
            //    lObjEmp.Manager = decimal.Parse(txtEmployeeCode.Text);//_maxEmpCode;
            //}
            //else 
            //{
            //    if (cmbMangers.SelectedValue == null)
            //    {
            //        lObjEmp.Manager = null;
            //    }
            //    else
            //    {
            //        lObjEmp.Manager = (decimal)(cmbMangers.SelectedValue);

            //    }

            //}
            ////////////////////////////////////////////////////

            //if (cmbProjects.SelectedValue == null)
            //{
            //    lObjEmp.ProjectCode = null;
            //}
            //else
            //{
            //    lObjEmp.ProjectCode = (decimal)(cmbProjects.SelectedValue);

            //}


            return lObjEmp;

        }
        private List<ProjectEmployeeDetail> FillProjectEmployeeDetailModel()
        {
            List<ProjectEmployeeDetail> LstPed = new List<ProjectEmployeeDetail>();
            ProjectEmployeeDetail ped;
            foreach (DataRow row1 in _dtProjects.Rows)
            {
                if (row1[0] != DBNull.Value && (bool)row1[0] == true)
                {
                    ped = new ProjectEmployeeDetail();
                    ped.EmployeeCode = _EmployeeCode;
                    ped.ProjectCode = (string)row1["ProjectCode"];
                    //ped.IsSelected = Convert.ToBoolean( row1[0]);
                    LstPed.Add(ped);
                }

            }
            return LstPed;
        }

        public DataTable ToDataTable<T>(List<T> items)
        {
            DataTable dataTable = new DataTable(typeof(T).Name);

            //Get all the properties
            PropertyInfo[] Props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            int adjustSize = 0;
            foreach (PropertyInfo prop in Props)
            {
                if (prop.Name == "EmployeeType" || prop.Name == "BOMs" || prop.Name == "ProjectEmployeeDetails" || prop.Name == "MRVersions") { adjustSize += 1; continue; }
                //Defining type of data column gives proper data table 
                var type = (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) ? Nullable.GetUnderlyingType(prop.PropertyType) : prop.PropertyType);
                //Setting column names as Property names
                dataTable.Columns.Add(prop.Name, type);
            }
            foreach (T item in items)
            {
                var values = new object[Props.Length - adjustSize];
                for (int i = 0; i < Props.Length; i++)
                {
                    if (Props[i].Name == "EmployeeType" || Props[i].Name == "BOMs" || Props[i].Name == "ProjectEmployeeDetails" || Props[i].Name == "MRVersions") continue;
                    //inserting property values to datatable rows
                    values[i] = Props[i].GetValue(item, null);
                }
                dataTable.Rows.Add(values);
            }
            //put a breakpoint here and check datatable
            return dataTable;
        }
        private void dataGridViewEmployees_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridViewEmployees.SelectedCells.Count > 0 && dataGridViewEmployees.SelectedCells[0].Value != DBNull.Value)
            {
                int selectedrowindex = dataGridViewEmployees.SelectedCells[0].RowIndex;
                DataGridViewRow selectedRow = dataGridViewEmployees.Rows[selectedrowindex];
                _EmployeeCode = Convert.ToDecimal(selectedRow.Cells["EmployeeCode"].Value);

                if (!string.IsNullOrEmpty(txtEmployeeCode.Text) && _EmployeeCode == decimal.Parse(txtEmployeeCode.Text)) return;

                _ec = new EmployeeController();
                _currentLoadedEmployee = _ec.GetModelByID(_EmployeeCode);
                if (_currentLoadedEmployee == null) return;
                txtEmployeeCode.Text = _currentLoadedEmployee.EmployeeCode.ToString();
                txtEmployeeName.Text = _currentLoadedEmployee.EmployeeName;
                txtPassword.Text = _currentLoadedEmployee.Password;
                cmbEmployeeType.SelectedValue = _currentLoadedEmployee.EmployeeTypeCode;
                btnShowPassword.ImageKey = "show.png";
                txtPassword.PasswordChar = '●';

                if (_currentLoadedEmployee != null && _currentLoadedEmployee.EmployeeCode == LoginInfo.LoginEmployee.EmployeeCode)
                { btnShowPassword.Visible = true; }
                else
                { btnShowPassword.Visible = false; }


                //if (employee.Manager != null)
                //{cmbManagers.SelectedValue = employee.Manager;}
                //else
                //{cmbManagers.SelectedIndex = -1;}

                if (_currentLoadedEmployee.ProjectCode != null)
                { cmbProjects.SelectedValue = _currentLoadedEmployee.ProjectCode; }
                else
                { cmbProjects.SelectedIndex = -1; }

                //foreach (DataGridViewRow sr in this.dataGridViewEmployees.SelectedRows)
                //{

                //}

                _dtProjects.Rows.OfType<DataRow>().ToList().ForEach(r => r["Select Projects"] = false);


                foreach (DataRow dr in _dtProjects.Rows) // search whole table
                {

                    if (_currentLoadedEmployee.ProjectEmployeeDetails.Where(x => x.ProjectCode == (string)dr["ProjectCode"]).FirstOrDefault() != null)
                    {
                        dr["Select Projects"] = true;
                    }
                }


                //_LstProjectEmployeeDetail = _pedc.GetModels(_EmployeeCode);

                //if (_newMode == true)
                //{
                //    var a = "123";
                //}
                //if (_newMode == false)
                //{


                //}
            }

        }
        private void btnNewEmployee_Click(object sender, EventArgs e)
        {
            txtEmployeeCode.Text = string.Empty;
            SetForNew();
            _newMode = true;
        }

        private void SetForNew()
        {


            _maxEmpCode = _ec.GetMaxCode();
            txtEmployeeCode.Text = _maxEmpCode.ToString();
            txtPassword.Text = string.Empty;
            cmbEmployeeType.SelectedIndex = -1;
            txtEmployeeName.Text = "New Employee " + _maxEmpCode;
            _dtProjects.Rows.OfType<DataRow>().ToList().ForEach(r => r["Select Projects"] = false);

        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void FrmEmployee_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (StaticClasses.NewProjectOpened.ClosePreviousProjectFormsWithOutConfirmation == true) return;
            DialogResult dialogResult = MessageBox.Show("Close this window?", "Confirmation", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No) e.Cancel = true;
        }
        private void btnResize_Click(object sender, EventArgs e)
        {
            if (splitContainer1.SplitterDistance < 201)
            {
                splitContainer1.SplitterDistance = 690;
            }
            else
            {
                splitContainer1.SplitterDistance = 199;
            }


        }

        private void dataGridViewEmployees_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                // Add this
                dataGridViewEmployees.CurrentCell = dataGridViewEmployees.Rows[e.RowIndex].Cells[e.ColumnIndex];
                // Can leave these here - doesn't hurt
                dataGridViewEmployees.Rows[e.RowIndex].Selected = true;
                dataGridViewEmployees.Focus();


            }
        }
        private void dataGridViewEmployees_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {

                MenuStripProjects.Show(Cursor.Position);
            }
        }

        private void itemDeleteProject_Click(object sender, EventArgs e)
        {
            if (dataGridViewEmployees.Rows.Count > 0 && dataGridViewEmployees.SelectedRows.Count > 0)
            {

                // int selectedrowindex = dataGridViewProjects.SelectedCells[0].RowIndex;
                // DataGridViewRow selectedRow = dataGridViewProjects.Rows[selectedrowindex];
                // _projectCode = Convert.ToDecimal(selectedRow.Cells["EmployeeCode"].Value);

                //// _dtProjects.Rows.Remove(selectedRow);
                // dataGridViewProjects.DataSource = _dtProjects;

                foreach (DataGridViewRow sr in this.dataGridViewEmployees.SelectedRows)
                {

                    //Employee project = (Employee)item.DataBoundItem;
                    decimal pc = Convert.ToDecimal(sr.Cells[0].Value);
                    Employee emp = _LstEmployees.Where(x => x.EmployeeCode == pc).FirstOrDefault();
                    if (emp != null) _LstEmployees.Remove(emp);
                    _dtEmployees.Rows.RemoveAt(sr.Index);
                    //_LstProjects.RemoveAt()
                    _ec.DeletenSaveModel(emp.EmployeeCode);

                    DataView dv = _dtEmployees.DefaultView;
                    dv.Sort = "EmployeeCode desc";
                    _dtEmployees = dv.ToTable();
                    dataGridViewEmployees.DataSource = _dtEmployees;

                    if (_dtEmployees.Rows.Count == 0)
                    {
                        SetForNew();
                        _ec.ReseedPk();
                        _newMode = true;
                    }


                    //dataGridViewProjects.Refresh();
                }
            }
            else
            {
                SetForNew();
            }
        }

        private void btnShowPassword_Click(object sender, EventArgs e)
        {
            if (btnShowPassword.ImageKey == "show.png")
            {
                btnShowPassword.ImageKey = "hide.png";
                txtPassword.PasswordChar = '\0';

            }
            else
            {
                btnShowPassword.ImageKey = "show.png";
                txtPassword.PasswordChar = '●';

            }
        }

        private void FrmEmployee_Activated(object sender, EventArgs e)
        {
            txtEmployeeName.Focus();
        }

        private void FrmEmployee_KeyDown(object sender, KeyEventArgs e)
        {
            //if (e.KeyCode == Keys.Escape)
            //{
            //    this.Close();
            //}
        }

        private void refCode()
        {
            //string Name = new System.Security.Principal.WindowsPrincipal(System.Security.Principal.WindowsIdentity.GetCurrent()).Identity.Name;
            //string Name1 = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            //string Name2 = System.DirectoryServices.AccountManagement.UserPrincipal.Current.UserPrincipalName;
            //string Name3 = System.DirectoryServices.AccountManagement.UserPrincipal.Current.EmailAddress;

            using (var context = new PrincipalContext(ContextType.Domain, "intechww.com"))
            {
                using (var searcher = new PrincipalSearcher(new UserPrincipal(context)))
                {
                    foreach (var result in searcher.FindAll())
                    {
                        DirectoryEntry de = result.GetUnderlyingObject() as DirectoryEntry;
                        Console.WriteLine("First Name: " + de.Properties["givenName"].Value);
                        Console.WriteLine("Last Name : " + de.Properties["sn"].Value);
                        Console.WriteLine("SAM account name   : " + de.Properties["samAccountName"].Value);
                        Console.WriteLine("User principal name: " + de.Properties["userPrincipalName"].Value);
                        Console.WriteLine();

                    }
                }
            }
        }

        private void btnSync_Click(object sender, EventArgs e)
        {
            this.Enabled = false;
            //string Name = new System.Security.Principal.WindowsPrincipal(System.Security.Principal.WindowsIdentity.GetCurrent()).Identity.Name;
            //string Name1 = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            //string Name2 = System.DirectoryServices.AccountManagement.UserPrincipal.Current.UserPrincipalName;
            //string Name3 = System.DirectoryServices.AccountManagement.UserPrincipal.Current.EmailAddress;

            //foreach (Employee item in _LstEmployees)
            //{
            //    if (!(string.IsNullOrEmpty(item.SAM)))
            //    {
            //        _ec.Delete(item.EmployeeCode);
            //    }

            //}
            //_ec.Save();
            progressBar1.Visible = true;
            using (var context = new PrincipalContext(ContextType.Domain, "intechww.com"))// "tenf.loc"))
            {
                
                byte empFound = 0;
                int counter = 0;
                int insertedEmp = 0;
                int UpdatedEmp = 0;
                using (var searcher = new PrincipalSearcher(new UserPrincipal(context)))
                {
                    
                    /////////////find in app database
                    
                    var AllIntechUsers = searcher.FindAll();
                    progressBar1.Maximum = AllIntechUsers.Count();

                    foreach (var result in AllIntechUsers)
                    {
                        DirectoryEntry de = result.GetUnderlyingObject() as DirectoryEntry;
                        //Console.WriteLine("First Name: " + de.Properties["givenName"].Value);
                        //Console.WriteLine("Last Name : " + de.Properties["sn"].Value);
                        //Console.WriteLine("SAM account name   : " + de.Properties["samAccountName"].Value);
                        //Console.WriteLine("User principal name: " + de.Properties["userPrincipalName"].Value);
                        //Console.WriteLine();
                        
                        counter += 1;
                        progressBar1.Value = counter;
                        if (de.Properties["userPrincipalName"].Value == null || string.IsNullOrEmpty(de.Properties["userPrincipalName"].Value.ToString().Trim()))
                        {
                            continue;
                        }
                          
                        
                        Application.DoEvents();
                        Employee oldEmp=new Employee();
                        foreach (Employee item in _LstEmployees)
                        {

                            oldEmp = item;
                            empFound = 0;
                            if (item.EmployeeName == Convert.ToString(de.Properties["userPrincipalName"].Value))
                            {

                                if (!(item.Active == IsActive(de)))
                                {
                                    empFound = 2;
                                    break;
                                }
                                empFound = 1;
                                break;
                            }


                        }
                        //0:notFound 1:Found 2:Updated
                        //incase of found not to do anything.
                        if (empFound == 0)
                        {
                            insertedEmp += 1;
                            InsertEmployee(de);
                        }
                        if (empFound == 2)
                        {
                            UpdatedEmp += 1;
                            UpdateEmployee(oldEmp, de);
                        }

                    }


                    ////////////////////////////now find in AD

                    //foreach (Employee item in _LstEmployees)
                    //{
                    //    foreach (var result in AllIntechUsers)
                    //    {
                    //        DirectoryEntry de = result.GetUnderlyingObject() as DirectoryEntry;
                    //        empFound = false;
                    //        if (item.EmployeeName == Convert.ToString(de.Properties["userPrincipalName"].Value))
                    //        {
                    //            empFound = true;
                    //            break;
                    //        }
                    //    }
                    //    if (empFound == false)
                    //    {
                    //        DeleteEmployee(item.EmployeeCode);
                    //    }
                    //}

                }



                if (insertedEmp > 0 || UpdatedEmp > 0)
                {
                    MessageBox.Show(insertedEmp + " new user(s) found." + System.Environment.NewLine + UpdatedEmp + " user(s) status updated." + System.Environment.NewLine + "Please close and reopen this form to view updated data");
                }
                else
                {
                    MessageBox.Show("Data is up-to-date");
                }
            

            }

            this.Enabled = false;
            this.Close();
        }
        private void InsertEmployee(DirectoryEntry de)
        {
            Employee emp;
            emp = new Employee();
            emp.EmployeeName = Convert.ToString(de.Properties["userPrincipalName"].Value);
            emp.SAM = Convert.ToString(de.Properties["samAccountName"].Value);
            emp.Email = Convert.ToString(de.Properties["userPrincipalName"].Value);
            emp.EmployeeId = Convert.ToDecimal(de.Properties["employeeID"].Value);
            //emp.CreatedDate = DateTime.Now;
            //emp.UpdateDate = DateTime.Now;

            emp.CreatedBy = LoginInfo.LoginEmployee.EmployeeCode;
            emp.CreatedDate = DateTime.Now;
            emp.EmployeeTypeCode = Constants.MANAGER;
            emp.Active = IsActive(de);

            _ec = new EmployeeController(emp);
            //_ec.DeleteModel(emp.EmployeeCode);
            _ec.InsertnSave();

            //emp.EmployeeCode =;
            //emp.Active 
        }
        private void UpdateEmployee(Employee oldEmp, DirectoryEntry de)
        {
            Employee emp;
            emp = new Employee();
            emp.EmployeeCode = oldEmp.EmployeeCode;
            emp.EmployeeName = Convert.ToString(de.Properties["userPrincipalName"].Value);
            emp.SAM = Convert.ToString(de.Properties["samAccountName"].Value);
            emp.Email = Convert.ToString(de.Properties["userPrincipalName"].Value);
            emp.EmployeeId = Convert.ToDecimal(de.Properties["employeeID"].Value);
            //emp.CreatedDate = DateTime.Now;
            //emp.UpdateDate = DateTime.Now;

            emp.CreatedBy = LoginInfo.LoginEmployee.EmployeeCode;
            emp.CreatedDate = DateTime.Now;
            emp.EmployeeTypeCode = oldEmp.EmployeeTypeCode;
            emp.Active = IsActive(de);

            _ec = new EmployeeController(emp);
            //_ec.DeleteModel(emp.EmployeeCode);
            _ec.UpdatenSaveModel(emp);

        }
        private void DeleteEmployee(decimal empCode)
        {
            _ec.DeletenSaveModel(empCode);
        }
        private bool IsActive(DirectoryEntry de)
        {
            if (de.NativeGuid == null) return false;

            int flags = (int)de.Properties["userAccountControl"].Value;

            return !Convert.ToBoolean(flags & 0x0002);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            string filter=string.Empty;
            BindingSource bs = new BindingSource();
            bs.DataSource = dataGridViewEmployees.DataSource;
            filter = "EmployeeName" + " like '%" + textBox1.Text + "%' ";
            filter += "OR ";
            filter += "convert(EmployeeId, 'System.String')"  + " like '%" + textBox1.Text + "%'";
            bs.Filter = filter;
            dataGridViewEmployees.DataSource = bs;
        }
    }
}

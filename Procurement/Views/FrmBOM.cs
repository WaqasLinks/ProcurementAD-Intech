using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.Data.OleDb;
using Repository.DAL;
using Procurement.Controllers;
using System.Reflection;
using StaticClasses;
using Procurement.Views;
using System.Text.RegularExpressions;
using System.Text;
using System.Drawing;
using System.IO;
using ExcelDataReader;
using Procurement.CustomClasses;
using DevExpress.XtraGrid.Views.Grid;
using System.ComponentModel;
using ClosedXML.Excel;

namespace Procurement
{
    public partial class FrmBOM : Form
    {
        ProjectController _pc;
        BOMController _bc;
        ProjectEmployeeDetailController _pedc;
        public DataTable _dtSalesBOM;
        public DataTable _dtDesignBOM;
        public DataTable _dtActualBOM;
        public DataTable _dtSummary;
        List<DataTable> _LstdtSalesBOM = new List<DataTable>();
        int _SalesBOM_UndoRedo_Idx = -1;
        decimal _projectCode;
        bool _newMode;
        Project _currentLoadedProject;
        List<string> _columnNames;
        bool IsGridView1Changed;
        bool IsGridView2Changed;
        bool IsGridView3Changed;
        bool gFlag = false;

        decimal _ExtCostTotal = 0;
        decimal _ExtCostSubTotal = 0;
        decimal _TotExtCost_CO = 0;
        //SingleTon 
        private static FrmBOM instance = null;
        private FrmBOM()
        {
            InitializeComponent();

        }
        public static FrmBOM Instance
        {
            get
            {
                if (instance == null || instance.IsDisposed)
                {

                    instance = new FrmBOM();
                }


                return instance;
            }
        }
        //End SingleTon

        private void FrmBOM_Load(object sender, EventArgs e)
        {
            //dx//dataGridView1.AllowUserToDeleteRows = false;
            //dx//dataGridView2.AllowUserToDeleteRows = false;
            //dx//dataGridView3.AllowUserToDeleteRows = false;
            try
            {
                
                //dataGridView1.footer//.IsShowRowFooters();// = true;
                if (LoginInfo.LoginEmployee.EmployeeTypeCode == Constants.EMPLOYEE)
                {

                    btnLoadBOM.Enabled = false;
                }

                ClearAll();

                _currentLoadedProject = CurrentOpenProject.CurrentProject;
                _columnNames = new List<String> {"Category1","Category2","Category3", "SORef",
                                                "Sr","ProductCategory","Product","CostHead",
                                                "CostSubHead","System","Area","Panel",
                                                "Category","Manufacturer","PartNo","Description",
                                                "Qty","UnitCost","ExtCost","UnitPrice",
                                                "ExtPrice","ChangeOrder","Column1","Column2",
                                                "Column3","Column4","Column5",
                                            };
                if (_currentLoadedProject == null)
                {
                    _newMode = true;
                    return;
                }
                else
                {
                    _newMode = false;

                    List<BOM> list1 = _currentLoadedProject.BOMs.Where(y => y.BOMTypeCode == 1).ToList();
                    _dtSalesBOM = ToDataTable<BOM>(list1);
                    _dtSalesBOM.Columns.Remove("ProjectCode");
                    _dtSalesBOM.Columns.Remove("RowAuto");
                    _dtSalesBOM.Columns.Remove("BomTypeCode");
                    _dtSalesBOM.Columns.Remove("BOMType");
                    _dtSalesBOM.Columns.Remove("Project");
                    //dx//dataGridView1.AutoGenerateColumns = false;
                    gridControl1.DataSource = _dtSalesBOM;
                    _SalesBOM_UndoRedo_Idx += 1;
                    _LstdtSalesBOM.Insert(_SalesBOM_UndoRedo_Idx, _dtSalesBOM.Copy());

                    List<BOM> list2 = _currentLoadedProject.BOMs.Where(y => y.BOMTypeCode == 2).ToList();
                    _dtDesignBOM = ToDataTable<BOM>(list2);
                    _dtDesignBOM.Columns.Remove("ProjectCode");
                    _dtDesignBOM.Columns.Remove("RowAuto");
                    _dtDesignBOM.Columns.Remove("BomTypeCode");
                    _dtDesignBOM.Columns.Remove("BOMType");
                    _dtDesignBOM.Columns.Remove("Project");
                    //dx//dataGridView2.AutoGenerateColumns = false;
                    gridControl2.DataSource = _dtDesignBOM;

                    List<BOM> list3 = _currentLoadedProject.BOMs.Where(y => y.BOMTypeCode == 3).ToList();
                    _dtActualBOM = ToDataTable<BOM>(list3);
                    _dtActualBOM.Columns.Remove("ProjectCode");
                    _dtActualBOM.Columns.Remove("RowAuto");
                    _dtActualBOM.Columns.Remove("BomTypeCode");
                    _dtActualBOM.Columns.Remove("BOMType");
                    _dtActualBOM.Columns.Remove("Project");
                    //dx//dataGridView3.AutoGenerateColumns = false;
                    gridControl3.DataSource = _dtActualBOM;


                    if (tabControl1.SelectedTab == tabControl1.TabPages["tabSaleBOM"])
                    {
                        GetSetExtCostTotal(ref _dtSalesBOM);
                    }
                    if (tabControl1.SelectedTab == tabControl1.TabPages["tabDesignBOM"])
                    {
                        GetSetExtCostTotal(ref _dtDesignBOM);
                    }
                    if (tabControl1.SelectedTab == tabControl1.TabPages["tabActualBOM"])
                    {
                        GetSetExtCostTotal(ref _dtActualBOM);
                    }

                }

            }

            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }
        private void tabControl1_Selecting(object sender, TabControlCancelEventArgs e)
        {
            txtExtCostTotal.Visible = true;
            //txtExtCostSubTotal.Visible = true;
            lblExtTotal.Visible = true;
            //lblExtTotalSub.Visible = true;
            TabPage currentTab = (sender as TabControl).SelectedTab;

            if (currentTab.Name == "tabSaleBOM")
            {
                GetSetExtCostTotal(ref _dtSalesBOM);
            }
            if (currentTab.Name == "tabDesignBOM")
            {
                GetSetExtCostTotal(ref _dtDesignBOM);
            }
            if (currentTab.Name == "tabActualBOM")
            {
                GetSetExtCostTotal(ref _dtActualBOM);
            }
            if (currentTab.Name == "tabSummary")
            {
                txtExtCostTotal.Visible = false;
                txtExtCostSubTotal.Visible = false;
                lblExtTotal.Visible = false;
                lblExtTotalSub.Visible = false;
            }
        }

        //DataTableCollection tableCollection;

        private void LoadExcel()
        {
            try
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog() { Filter = "Excel File|*.xls;*.xlsx;*.xlsm" })
                {
                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        DataSet tempDataSet;
                        txtBOMFilePath.Text = openFileDialog.FileName;
                        using (var stream = File.Open(openFileDialog.FileName, FileMode.Open, FileAccess.Read))
                        {
                            using (IExcelDataReader reader = ExcelReaderFactory.CreateReader(stream))
                            {
                                tempDataSet = reader.AsDataSet(new ExcelDataSetConfiguration()
                                {
                                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true }
                                });
                                //tableCollection = result.Tables;
                                //dataGridView1.DataSource = result.Tables[0];//tableCollection[0];
                            }
                        }

                        DataTable dtBOM = new DataTable("dtBOM");
                        //dtBOM = dtTemp.Clone();
                        dtBOM.Columns.Add(_columnNames[0], typeof(string));
                        dtBOM.Columns.Add(_columnNames[1], typeof(string));
                        dtBOM.Columns.Add(_columnNames[2], typeof(string));
                        dtBOM.Columns.Add(_columnNames[3], typeof(string));
                        dtBOM.Columns.Add(_columnNames[4], typeof(decimal));
                        dtBOM.Columns.Add(_columnNames[5], typeof(string));
                        dtBOM.Columns.Add(_columnNames[6], typeof(string));
                        dtBOM.Columns.Add(_columnNames[7], typeof(string));
                        dtBOM.Columns.Add(_columnNames[8], typeof(string));
                        dtBOM.Columns.Add(_columnNames[9], typeof(string));
                        dtBOM.Columns.Add(_columnNames[10], typeof(string));
                        dtBOM.Columns.Add(_columnNames[11], typeof(string));
                        dtBOM.Columns.Add(_columnNames[12], typeof(string));
                        dtBOM.Columns.Add(_columnNames[13], typeof(string));
                        dtBOM.Columns.Add(_columnNames[14], typeof(string));
                        dtBOM.Columns.Add(_columnNames[15], typeof(string));
                        dtBOM.Columns.Add(_columnNames[16], typeof(decimal));
                        dtBOM.Columns.Add(_columnNames[17], typeof(decimal));
                        dtBOM.Columns.Add(_columnNames[18], typeof(decimal));
                        dtBOM.Columns.Add(_columnNames[19], typeof(decimal));
                        dtBOM.Columns.Add(_columnNames[20], typeof(decimal));
                        dtBOM.Columns.Add(_columnNames[21], typeof(string));
                        dtBOM.Columns.Add(_columnNames[22], typeof(string));
                        dtBOM.Columns.Add(_columnNames[23], typeof(string));
                        dtBOM.Columns.Add(_columnNames[24], typeof(string));
                        dtBOM.Columns.Add(_columnNames[25], typeof(string));
                        dtBOM.Columns.Add(_columnNames[26], typeof(string));

                        //dx//dataGridView1.AutoGenerateColumns = false;
                        //dtBOM.Rows.Add();
                        //_TotExtCost = 0;

                        foreach (DataRow dr in tempDataSet.Tables[0].Rows)
                        {
                            //string colName=gvr.Cells[0].OwningColumn.HeaderText;

                            bool isAdd = false;
                            for (int i = 0; i < tempDataSet.Tables[0].Columns.Count; i++)
                            {
                                //if (dr[i] == null || dr[i] == DBNull.Value || String.IsNullOrWhiteSpace(dr[i].ToString()))
                                if (dr[i] == DBNull.Value)
                                {
                                    isAdd = false;
                                }
                                else
                                {
                                    isAdd = true;
                                    break;
                                }
                            }

                            if (isAdd == true)
                            {
                                //can not add like this dtBOM.Rows.Add(dr); :(  have to add new row and then add to list
                                //newDataRow = dtBOM.NewRow();
                                //for (int i = 0; i <  dtTemp.Columns.Count; i++)
                                //{
                                //    newDataRow[i] = dr[i];

                                //}
                                //dtBOM.Rows.Add(newDataRow);
                                //////////////////
                                //var varObj = dr[18];//ExtCost

                                //if (!(varObj == null || varObj == DBNull.Value) )
                                //{
                                //    _TotExtCost += Convert.ToDecimal(dr[18]);
                                //} 
                                ////////////////////
                                //else 
                                //{ 
                                //    lObjBom.ExtCost = Convert.ToDecimal(cellObj); 
                                //}



                                dtBOM.Rows.Add(dr.ItemArray);
                                //if (dtBOM.Rows.Count > 337) MessageBox.Show("338");

                            }

                        }
                        if (tabControl1.SelectedTab == tabControl1.TabPages["tabSaleBOM"])
                        {
                            //dtBOM.Rows[0][18] = _TotExtCost;
                            _dtSalesBOM = dtBOM;
                            GetSetExtCostTotal(ref _dtSalesBOM);
                            gridControl1.DataSource = _dtSalesBOM;
                            IsGridView1Changed = true;
                        }
                        if (tabControl1.SelectedTab == tabControl1.TabPages["tabDesignBOM"])
                        {
                            //dtBOM.Rows[0][18] = _TotExtCost;
                            _dtDesignBOM = dtBOM;
                            GetSetExtCostTotal(ref _dtDesignBOM);
                            gridControl2.DataSource = _dtDesignBOM;
                            IsGridView2Changed = true;
                        }
                        if (tabControl1.SelectedTab == tabControl1.TabPages["tabActualBOM"])
                        {
                            //dtBOM.Rows[0][18] = _TotExtCost;
                            _dtActualBOM = dtBOM;
                            GetSetExtCostTotal(ref _dtActualBOM);
                            gridControl3.DataSource = _dtActualBOM;
                            IsGridView3Changed = true;
                        }
                        MessageBox.Show("BOM Loaded Successfully");
                    }
                }
            }
            catch (Exception ex)
            {
                switch (ex.HResult)
                {
                    case -2147024864:
                        MessageBox.Show(ex.Message);
                        break;
                    default:
                        MessageBox.Show(ex.Message);
                        break;
                }


            }
        }
        private void loadBOMToolStripMenuItem_Click(object sender, EventArgs e)
        {

            //Method1();
            LoadExcel();

        }
        public bool IsNumeric(string value)
        {
            return value.All(char.IsNumber);
        }
        public int GetCOVersionNumber(DataTable dataTable)
        {
            int COVersion = 0;
            for (int i = dataTable.Rows.Count - 1; i >= 0; i--)
            {
                string cellValue = dataTable.Rows[i].Field<string>("ChangeOrder");
                if (!string.IsNullOrEmpty(cellValue) && cellValue.Substring(0, 2) == "CO" && IsNumeric(cellValue.Substring(2)) == true)
                {
                    COVersion = Convert.ToInt32(cellValue.Substring(2));
                    COVersion += 1;
                    break;
                }
                // ...
            }
            if (COVersion == 0) COVersion = 1;
            return COVersion;
        }
        private void loadChageOrderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog() { Filter = "Excel File|*.xls;*.xlsx;*.xlsm" })
                {
                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        DataSet tempDataSet;
                        txtBOMFilePath.Text = openFileDialog.FileName;
                        using (var stream = File.Open(openFileDialog.FileName, FileMode.Open, FileAccess.Read))
                        {
                            using (IExcelDataReader reader = ExcelReaderFactory.CreateReader(stream))
                            {
                                tempDataSet = reader.AsDataSet(new ExcelDataSetConfiguration()
                                {
                                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true }
                                });
                                //tableCollection = result.Tables;
                                //dataGridView1.DataSource = result.Tables[0];//tableCollection[0];
                            }
                        }
                        //DataTable dataTable = tempDataSet.Tables[0];
                        //dataTable.Columns.Add("ChangeOrder", typeof(System.String));
                        //DataColumn Col = tempDataSet.Tables[0].Columns.Add("ChangeOrder", typeof(System.String));

                        if (!(tempDataSet.Tables[0].Columns.Contains("ChangeOrder")))
                        {
                            DataColumn Col = tempDataSet.Tables[0].Columns.Add("ChangeOrder", typeof(System.String));
                            Col.SetOrdinal(21);// to put the column in position 0;
                        }

                        //foreach (DataRow dr in tempDataSet.Tables[0].Rows)
                        //{
                        //    dr["ChangeOrder"] = "CO1";
                        //}
                        int COVersion = 0;
                        if (tabControl1.SelectedTab == tabControl1.TabPages["tabSaleBOM"])
                        {
                            COVersion = GetCOVersionNumber(_dtSalesBOM);
                        }
                        if (tabControl1.SelectedTab == tabControl1.TabPages["tabDesignBOM"])
                        {
                            COVersion = GetCOVersionNumber(_dtDesignBOM);
                        }
                        if (tabControl1.SelectedTab == tabControl1.TabPages["tabActualBOM"])
                        {
                            COVersion = GetCOVersionNumber(_dtActualBOM);
                        }


                        //_TotExtCost_CO = 0;
                        foreach (DataRow dr in tempDataSet.Tables[0].Rows)
                        {
                            //string colName=gvr.Cells[0].OwningColumn.HeaderText;
                            dr["ChangeOrder"] = "CO" + COVersion;
                            bool isAdd = false;
                            for (int i = 0; i < tempDataSet.Tables[0].Columns.Count; i++)
                            {
                                //if (dr[i] == null || dr[i] == DBNull.Value || String.IsNullOrWhiteSpace(dr[i].ToString()))
                                if (dr[i] == DBNull.Value)
                                {
                                    isAdd = false;
                                }
                                else
                                {
                                    isAdd = true;
                                    break;
                                }
                            }

                            if (isAdd == true)
                            {
                                //can not add like this dtBOM.Rows.Add(dr); :(  have to add new row and then add to list
                                //newDataRow = dtBOM.NewRow();
                                //for (int i = 0; i <  dtTemp.Columns.Count; i++)
                                //{
                                //    newDataRow[i] = dr[i];

                                //}
                                //dtBOM.Rows.Add(newDataRow);
                                ////////////////
                                //var varObj = dr[18];//ExtCost

                                //if (!(varObj == null || varObj == DBNull.Value))
                                //{
                                //    _TotExtCost += Convert.ToDecimal(dr[18]);
                                //}
                                //////////////////

                                if (tabControl1.SelectedTab == tabControl1.TabPages["tabSaleBOM"])
                                {
                                    //_dtSalesBOM.Rows[0][18] = _TotExtCost;
                                    _dtSalesBOM.Rows.Add(dr.ItemArray);
                                    IsGridView1Changed = true;
                                }
                                if (tabControl1.SelectedTab == tabControl1.TabPages["tabDesignBOM"])
                                {
                                    //_dtDesignBOM.Rows[0][18] = _TotExtCost;
                                    _dtDesignBOM.Rows.Add(dr.ItemArray);
                                    IsGridView2Changed = true;
                                }
                                if (tabControl1.SelectedTab == tabControl1.TabPages["tabActualBOM"])
                                {
                                    //_dtActualBOM.Rows[0][18] = _TotExtCost;
                                    _dtActualBOM.Rows.Add(dr.ItemArray);
                                    IsGridView3Changed = true;
                                }

                            }

                        }
                        if (tabControl1.SelectedTab == tabControl1.TabPages["tabSaleBOM"])
                        {
                            GetSetExtCostTotal(ref _dtSalesBOM);
                        }
                        if (tabControl1.SelectedTab == tabControl1.TabPages["tabDesignBOM"])
                        {
                            GetSetExtCostTotal(ref _dtDesignBOM);
                        }
                        if (tabControl1.SelectedTab == tabControl1.TabPages["tabActualBOM"])
                        {
                            GetSetExtCostTotal(ref _dtActualBOM);
                        }
                        MessageBox.Show("Change Order Loaded Successfully");

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        #region Summary
        private void GetSummary()
        {
            try
            {


                List<Summary> LstSummary = new List<Summary>();
                Summary summary;
                //Summary summary = new Summary { A_Category= "Project Code",B_Item = CurrentOpenProject.CurrentProject.ProjectCode,C_BidCost="",D_PlanCost="",E_ActualCost="",F_CostInFuture="",G_ProjectedCost=""};
                int rowIdx = 0;
                //row 1
                summary = new Summary { A_Category = "Project Code", B_Item = CurrentOpenProject.CurrentProject.ProjectCode };
                LstSummary.Add(summary);
                rowIdx += 1;

                //row 2
                summary = new Summary { A_Category = "Project Name", B_Item = CurrentOpenProject.CurrentProject.ProjectName };
                LstSummary.Add(summary);
                rowIdx += 1;



                // row 5

                // empty line
                summary = new Summary { };
                LstSummary.Add(summary);
                rowIdx += 1;
                int SummaryInsertionPoint = rowIdx;
                // Column Names
                summary = new Summary {H_ChangeOrder="MainOrder",  A_Category = "CostHead", B_Item = "CostSubHead", C_BidCost = "BidCost", D_PlanCost = "PlanCost", G_ProjectedCost = "ProjectedCost", F_CostInFuture = "CostInFuture", E_ActualCost = "ActualCost" };
                LstSummary.Add(summary);
                rowIdx += 1;

                //------------------------------------------------------------
                var SalesGroupBy = _dtSalesBOM.AsEnumerable().Where(x => string.IsNullOrEmpty(x.Field<string>("ChangeOrder"))).GroupBy(d => new
                {
                    productCategory1 = d.Field<string>("CostHead"),
                    productCategory2 = d.Field<string>("CostSubHead"),
                })
                       .Select(x => new
                       {
                           ProductCategory1 = x.Key.productCategory1,
                           productCategory2 = x.Key.productCategory2,
                           //replace ItemArray Index with appropriate values in your code
                           //ExtCostTotal = x.Sum(y => Convert.ToDouble(string.IsNullOrEmpty(y.Field<string>("ExtCost")) ? "0" : y.Field<string>("ExtCost")))

                           //if (cellObj.Value == null || cellObj.Value == DBNull.Value) { lObjBom.Panel = null; } else { lObjBom.Panel = Convert.ToDecimal(cellObj.Value); }
                           ExtCostTotal = x.Sum(y => Convert.ToDouble(y.Field<decimal?>("ExtCost") == null ? 0 : y.Field<decimal>("ExtCost")))


                           //row.Field<int?>("Presently_Available") == null ? 0 : row.Field<int>("Presently_Available") ;
                           //ExtCostTotal = x.Sum(y => Convert.ToDouble(y.Field<double>("ExtCost") ? 0 : y.Field<double>("ExtCost")))

                       });
                //var abc= grouped.ToList();

                //DataTable dt = (DataTable)grouped;
                //grouped.Dump();
                //------------------------------------------------------------
                //$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$

                var DesignGroupBy = _dtDesignBOM.AsEnumerable().Where(x => string.IsNullOrEmpty(x.Field<string>("ChangeOrder"))).GroupBy(d => new
                {
                    productCategory1 = d.Field<string>("CostHead"),
                    productCategory2 = d.Field<string>("CostSubHead"),
                })
                       .Select(x => new
                       {
                           ProductCategory1 = x.Key.productCategory1,
                           productCategory2 = x.Key.productCategory2,

                           //replace ItemArray Index with appropriate values in your code
                           //ExtCostTotal = x.Sum(y => Convert.ToDouble(string.IsNullOrEmpty(y.Field<string>("ExtCost")) ? "0" : y.Field<string>("ExtCost")))
                           ExtCostTotal = x.Sum(y => Convert.ToDouble(y.Field<decimal?>("ExtCost") == null ? 0 : y.Field<decimal>("ExtCost")))
                       });
                ////------------------------------------------------------------
                var ActualGroupBy = _dtActualBOM.AsEnumerable().Where(x => string.IsNullOrEmpty(x.Field<string>("ChangeOrder"))).GroupBy(d => new
                {
                    productCategory1 = d.Field<string>("CostHead"),
                    productCategory2 = d.Field<string>("CostSubHead"),
                })
                       .Select(x => new
                       {
                           ProductCategory1 = x.Key.productCategory1,
                           productCategory2 = x.Key.productCategory2,

                           //replace ItemArray Index with appropriate values in your code
                           //ExtCostTotal = x.Sum(y => Convert.ToDouble(string.IsNullOrEmpty(y.Field<string>("ExtCost")) ? "0" : y.Field<string>("ExtCost")))

                           ExtCostTotal = x.Sum(y => Convert.ToDouble((y.Field<decimal?>("ExtCost") == null) ? 0 : string.IsNullOrEmpty(y.Field<string>("Column1")) ? 0 : y.Field<decimal>("ExtCost")))
                       });
                ////------------------------------------------------------------
                var ProjectedGroupBy = _dtActualBOM.AsEnumerable().Where(x => string.IsNullOrEmpty(x.Field<string>("ChangeOrder"))).GroupBy(d => new
                {
                    productCategory1 = d.Field<string>("CostHead"),
                    productCategory2 = d.Field<string>("CostSubHead"),
                })
                       .Select(x => new
                       {
                           ProductCategory1 = x.Key.productCategory1,
                           productCategory2 = x.Key.productCategory2,

                           //replace ItemArray Index with appropriate values in your code
                           //ExtCostTotal = x.Sum(y => Convert.ToDouble(string.IsNullOrEmpty(y.Field<string>("ExtCost")) ? "0" : y.Field<string>("ExtCost")))

                           ExtCostTotal = x.Sum(y => Convert.ToDouble(y.Field<decimal?>("ExtCost") == null ? 0 : y.Field<decimal>("ExtCost")))
                       });
                //$$$$$$$$$------------------------------------------------------------
                //collect name of productCategory from every bom
                List<string> Keys = new List<string>();

                foreach (var item in SalesGroupBy)
                {
                    //if (!string.IsNullOrEmpty(item.ProductCategory1.Trim()))
                    //{
                        if (!Keys.Contains(item.ProductCategory1 + "@#$" + item.productCategory2))
                        {
                            Keys.Add(item.ProductCategory1 + "@#$" + item.productCategory2);
                        }
                        //Keys.Add();
                    //}
                }
                //$$$$$$$$$
                foreach (var item in DesignGroupBy)
                {
                    //bool isHere = Keys.Any(x => x == item.ProductCategory);
                    //if (isHere == false && !string.IsNullOrEmpty(item.ProductCategory.Trim()))
                    //{
                    //    Keys.Add(item.ProductCategory.ToString());
                    //}
                    if (!Keys.Contains(item.ProductCategory1 + "@#$" + item.productCategory2))
                    {
                        Keys.Add(item.ProductCategory1 + "@#$" + item.productCategory2);
                    }
                }
                foreach (var item in ActualGroupBy)
                {
                    //bool isHere = Keys.Any(x => x == item.ProductCategory);
                    //if (isHere == false && !string.IsNullOrEmpty(item.ProductCategory.Trim()))
                    //{
                    //    Keys.Add(item.ProductCategory.ToString());
                    //}
                    if (!Keys.Contains(item.ProductCategory1 + "@#$" + item.productCategory2))
                    {
                        Keys.Add(item.ProductCategory1 + "@#$" + item.productCategory2);
                    }
                }

                foreach (var item in ProjectedGroupBy)
                {
                    //bool isHere = Keys.Any(x => x == item.ProductCategory);
                    //if (isHere == false && !string.IsNullOrEmpty(item.ProductCategory.Trim()))
                    //{
                    //    Keys.Add(item.ProductCategory.ToString());
                    //}
                    if (!Keys.Contains(item.ProductCategory1 + "@#$" + item.productCategory2))
                    {
                        Keys.Add(item.ProductCategory1 + "@#$" + item.productCategory2);
                    }
                }
                //$$$$$$$$$

                double SalesGTotal = 0;
                double DesignGTotal = 0;
                double ActualGTotal = 0;
                double ProjectedGTotal = 0;
                double FutureGTotal = 0;
                //--
                double SalesExtTotalCost=0;
                double DesignExtTotalCost=0;
                double ActualExtTotalCost=0;
                double ProjectedExtTotalCost=0;
                //making actual summary list for all BOMs
                foreach (string key in Keys)
                {

                    List<string> p1p2 = key.Split(new[] { "@#$" }, StringSplitOptions.None).ToList<string>();

                    string p1 = p1p2.First(); //key.Split(new[] { "@#$" },StringSplitOptions.None).First();
                    string p2 = p1p2.Last();//key.Split(new[] { "@#$" }, StringSplitOptions.None).Last();

                    //string p2=
                    //SalesgroupBy//DesignGroupBy//ActualGroupBy
                    //ExtCostTotal = x.Sum(y => Convert.ToDouble(string.IsNullOrEmpty(y.Field<string>("ExtCost")) ? "0" : y.Field<string>("ExtCost")))

                    //var SalesExtTotalCost = SalesgroupBy.FirstOrDefault(x => x.ProductCategory == key);
                    //SalesgroupBy.FirstOrDefault(x => x.ProductCategory == key) == null ? SalesExtTotalCost= 0 : SalesExtTotalCost=SalesgroupBy
                    SalesExtTotalCost =0;
                    var sales = SalesGroupBy.Where(x => x.ProductCategory1 == p1 && x.productCategory2==p2).FirstOrDefault();
                    if (sales == null) { SalesExtTotalCost = 0; } else { SalesExtTotalCost = sales.ExtCostTotal; }
                    SalesGTotal += SalesExtTotalCost;
                    //$$$$$
                    DesignExtTotalCost = 0;
                    var design = DesignGroupBy.Where(x => x.ProductCategory1 == p1 && x.productCategory2 == p2).FirstOrDefault();
                    if (design == null) { DesignExtTotalCost = 0; } else { DesignExtTotalCost = design.ExtCostTotal; }
                    DesignGTotal += DesignExtTotalCost;

                    ActualExtTotalCost = 0;
                    var actual = ActualGroupBy.Where(x => x.ProductCategory1 == p1 && x.productCategory2 == p2).FirstOrDefault();
                    if (actual == null) { ActualExtTotalCost = 0; } else { ActualExtTotalCost = actual.ExtCostTotal; }
                    ActualGTotal += ActualExtTotalCost;

                    ProjectedExtTotalCost = 0;
                    var projected = ProjectedGroupBy.Where(x => x.ProductCategory1 == p1 && x.productCategory2 == p2).FirstOrDefault();
                    if (projected == null) { ProjectedExtTotalCost = 0; } else { ProjectedExtTotalCost = projected.ExtCostTotal; }
                    ProjectedGTotal += ProjectedExtTotalCost;
                    //$$$$$
                    FutureGTotal += (ProjectedExtTotalCost - ActualExtTotalCost);

                    summary = new Summary { H_ChangeOrder="Main Order", A_Category = p1, B_Item = p2, C_BidCost = SalesExtTotalCost.ToString(), D_PlanCost = DesignExtTotalCost.ToString(), G_ProjectedCost = ProjectedExtTotalCost.ToString(), F_CostInFuture = (ProjectedExtTotalCost - ActualExtTotalCost).ToString(), E_ActualCost = ActualExtTotalCost.ToString() };

                    LstSummary.Add(summary);
                    rowIdx += 1;

                }
                summary = new Summary {  C_BidCost = SalesGTotal.ToString(), D_PlanCost = DesignGTotal.ToString(), G_ProjectedCost = ProjectedGTotal.ToString(), F_CostInFuture = FutureGTotal.ToString(), E_ActualCost = ActualGTotal.ToString() };
                LstSummary.Insert(SummaryInsertionPoint, summary);
                rowIdx += 1;


                //----------------new V ---------------

                summary = new Summary { A_Category = string.Empty, B_Item = string.Empty, C_BidCost = string.Empty, D_PlanCost = string.Empty, G_ProjectedCost = string.Empty, F_CostInFuture = string.Empty, E_ActualCost = string.Empty };
                LstSummary.Add(summary);
                rowIdx += 1;

                summary = new Summary { A_Category = string.Empty, B_Item = string.Empty, C_BidCost = string.Empty, D_PlanCost = string.Empty, G_ProjectedCost = string.Empty, F_CostInFuture = string.Empty, E_ActualCost = string.Empty };
                LstSummary.Add(summary);
                rowIdx += 1;

                summary = new Summary { A_Category = string.Empty, B_Item = string.Empty, C_BidCost = string.Empty, D_PlanCost = string.Empty, G_ProjectedCost = string.Empty, F_CostInFuture = string.Empty, E_ActualCost = string.Empty };
                LstSummary.Add(summary);
                rowIdx += 1;

                int ChangeOrder_SummaryInsertionPoint = rowIdx;
                summary = new Summary { A_Category = "CostHead", B_Item = "CostSubHead", C_BidCost = "BidCost", D_PlanCost = "PlanCost", G_ProjectedCost = "ProjectedCost", F_CostInFuture = "CostInFuture", E_ActualCost = "ActualCost" };
                LstSummary.Add(summary);
                rowIdx += 1;
                //------------------------------------------------------------
                var SalesGroupBy1 = _dtSalesBOM.AsEnumerable().Where(x => !string.IsNullOrEmpty(x.Field<string>("ChangeOrder"))).GroupBy(d => new
                {
                    productCategory1 = d.Field<string>("CostHead"),
                    productCategory2 = d.Field<string>("CostSubHead"),
                    productCategory3 = d.Field<string>("ChangeOrder"),
                    
                })
                       .Select(x => new
                       {
                           ProductCategory1 = x.Key.productCategory1,
                           productCategory2 = x.Key.productCategory2,
                           productCategory3 = x.Key.productCategory3,

                           //replace ItemArray Index with appropriate values in your code
                           //ExtCostTotal = x.Sum(y => Convert.ToDouble(string.IsNullOrEmpty(y.Field<string>("ExtCost")) ? "0" : y.Field<string>("ExtCost")))

                           //if (cellObj.Value == null || cellObj.Value == DBNull.Value) { lObjBom.Panel = null; } else { lObjBom.Panel = Convert.ToDecimal(cellObj.Value); }
                           ExtCostTotal = x.Sum(y => Convert.ToDouble(y.Field<decimal?>("ExtCost") == null ? 0 : y.Field<decimal>("ExtCost")))


                           //row.Field<int?>("Presently_Available") == null ? 0 : row.Field<int>("Presently_Available") ;
                           //ExtCostTotal = x.Sum(y => Convert.ToDouble(y.Field<double>("ExtCost") ? 0 : y.Field<double>("ExtCost")))

                       });
                //var abc= grouped.ToList();

                //DataTable dt = (DataTable)grouped;
                //grouped.Dump();
                //------------------------------------------------------------
                var DesignGroupBy1 = _dtDesignBOM.AsEnumerable().Where(x => !string.IsNullOrEmpty(x.Field<string>("ChangeOrder"))).GroupBy(d => new
                {
                    productCategory1 = d.Field<string>("CostHead"),
                    productCategory2 = d.Field<string>("CostSubHead"),
                    productCategory3 = d.Field<string>("ChangeOrder"),
                })
                       .Select(x => new
                       {
                           ProductCategory1 = x.Key.productCategory1,
                           productCategory2 = x.Key.productCategory2,
                           productCategory3 = x.Key.productCategory3,

                           //replace ItemArray Index with appropriate values in your code
                           //ExtCostTotal = x.Sum(y => Convert.ToDouble(string.IsNullOrEmpty(y.Field<string>("ExtCost")) ? "0" : y.Field<string>("ExtCost")))
                           ExtCostTotal = x.Sum(y => Convert.ToDouble(y.Field<decimal?>("ExtCost") == null ? 0 : y.Field<decimal>("ExtCost")))
                       });
                //------------------------------------------------------------
                var ActualGroupBy1 = _dtActualBOM.AsEnumerable().Where(x => !string.IsNullOrEmpty(x.Field<string>("ChangeOrder"))).GroupBy(d => new
                {
                    productCategory1 = d.Field<string>("CostHead"),
                    productCategory2 = d.Field<string>("CostSubHead"),
                    productCategory3 = d.Field<string>("ChangeOrder"),
                })
                       .Select(x => new
                       {
                           ProductCategory1 = x.Key.productCategory1,
                           productCategory2 = x.Key.productCategory2,
                           productCategory3 = x.Key.productCategory3,

                           //replace ItemArray Index with appropriate values in your code
                           //ExtCostTotal = x.Sum(y => Convert.ToDouble(string.IsNullOrEmpty(y.Field<string>("ExtCost")) ? "0" : y.Field<string>("ExtCost")))

                           ExtCostTotal = x.Sum(y => Convert.ToDouble((y.Field<decimal?>("ExtCost") == null) ? 0 : string.IsNullOrEmpty(y.Field<string>("Column1")) ? 0 : y.Field<decimal>("ExtCost")))
                       });
                //------------------------------------------------------------
                var ProjectedGroupBy1 = _dtActualBOM.AsEnumerable().Where(x => !string.IsNullOrEmpty(x.Field<string>("ChangeOrder"))).GroupBy(d => new
                {
                    productCategory1 = d.Field<string>("CostHead"),
                    productCategory2 = d.Field<string>("CostSubHead"),
                    productCategory3 = d.Field<string>("ChangeOrder"),
                })
                       .Select(x => new
                       {
                           ProductCategory1 = x.Key.productCategory1,
                           productCategory2 = x.Key.productCategory2,
                           productCategory3 = x.Key.productCategory3,

                           //replace ItemArray Index with appropriate values in your code
                           //ExtCostTotal = x.Sum(y => Convert.ToDouble(string.IsNullOrEmpty(y.Field<string>("ExtCost")) ? "0" : y.Field<string>("ExtCost")))

                           ExtCostTotal = x.Sum(y => Convert.ToDouble(y.Field<decimal?>("ExtCost") == null ? 0 : y.Field<decimal>("ExtCost")))
                       });
                //------------------------------------------------------------
                //collect name of productCategory from every bom
                Keys = new List<string>();

                foreach (var item in SalesGroupBy1)
                {
                    if (!Keys.Contains(item.ProductCategory1 + "@#$" + item.productCategory2 + "@#$" + item.productCategory3))
                    {
                        Keys.Add(item.ProductCategory1 + "@#$" + item.productCategory2 + "@#$" + item.productCategory3);
                    }
                }

                foreach (var item in DesignGroupBy1)
                {
                    if (!Keys.Contains(item.ProductCategory1 + "@#$" + item.productCategory2 + "@#$" + item.productCategory3))
                    {
                        Keys.Add(item.ProductCategory1 + "@#$" + item.productCategory2 + "@#$" + item.productCategory3);
                    }

                }
                foreach (var item in ActualGroupBy1)
                {
                    if (!Keys.Contains(item.ProductCategory1 + "@#$" + item.productCategory2 + "@#$" + item.productCategory3))
                    {
                        Keys.Add(item.ProductCategory1 + "@#$" + item.productCategory2 + "@#$" + item.productCategory3);
                    }
                }

                foreach (var item in ProjectedGroupBy1)
                {
                    if (!Keys.Contains(item.ProductCategory1 + "@#$" + item.productCategory2 + "@#$" + item.productCategory3))
                    {
                        Keys.Add(item.ProductCategory1 + "@#$" + item.productCategory2 + "@#$" + item.productCategory3);
                    }
                }


                double SalesGTotal1 = 0;
                double DesignGTotal1 = 0;
                double ActualGTotal1 = 0;
                double ProjectedGTotal1 = 0;
                double FutureGTotal1 = 0;
                //--
                double SalesExtTotalCost1=0;
                double DesignExtTotalCost1 = 0;
                double ActualExtTotalCost1 = 0;
                double ProjectedExtTotalCost1 = 0;
                //making actual summary list for all BOMs
                foreach (string key in Keys)
                {
                    List<string> p1p2p3 = key.Split(new[] { "@#$" }, StringSplitOptions.None).ToList<string>();

                    string p1 = p1p2p3[0]; //key.Split(new[] { "@#$" },StringSplitOptions.None).First();
                    string p2 = p1p2p3[1];//key.Split(new[] { "@#$" }, StringSplitOptions.None).Last();
                    string p3 = p1p2p3[2];

                    //SalesgroupBy//DesignGroupBy//ActualGroupBy
                    //ExtCostTotal = x.Sum(y => Convert.ToDouble(string.IsNullOrEmpty(y.Field<string>("ExtCost")) ? "0" : y.Field<string>("ExtCost")))

                    //var SalesExtTotalCost = SalesgroupBy.FirstOrDefault(x => x.ProductCategory == key);
                    //SalesgroupBy.FirstOrDefault(x => x.ProductCategory == key) == null ? SalesExtTotalCost= 0 : SalesExtTotalCost=SalesgroupBy
                    SalesExtTotalCost1 =0;
                    var sales = SalesGroupBy1.Where(x => x.ProductCategory1 == p1 && x.productCategory2 == p2 && x.productCategory3 == p3).FirstOrDefault();
                    if (sales == null) { SalesExtTotalCost = 0; } else { SalesExtTotalCost = sales.ExtCostTotal; }
                    SalesGTotal1 += SalesExtTotalCost;
                    DesignExtTotalCost1=0;
                    var design = DesignGroupBy1.Where(x => x.ProductCategory1 == p1 && x.productCategory2 == p2 && x.productCategory3 == p3).FirstOrDefault();
                    if (design == null) { DesignExtTotalCost = 0; } else { DesignExtTotalCost = design.ExtCostTotal; }
                    DesignGTotal1 += DesignExtTotalCost;

                    ActualExtTotalCost1=0;
                    var actual = ActualGroupBy1.Where(x => x.ProductCategory1 == p1 && x.productCategory2 == p2 && x.productCategory3 == p3).FirstOrDefault();
                    if (actual == null) { ActualExtTotalCost = 0; } else { ActualExtTotalCost = actual.ExtCostTotal; }
                    ActualGTotal1 += ActualExtTotalCost;

                    ProjectedExtTotalCost1 = 0;
                    var projected = ProjectedGroupBy1.Where(x => x.ProductCategory1 == p1 && x.productCategory2 == p2 && x.productCategory3 == p3).FirstOrDefault();
                    if (projected == null) { ProjectedExtTotalCost = 0; } else { ProjectedExtTotalCost = projected.ExtCostTotal; }
                    ProjectedGTotal1 += ProjectedExtTotalCost;

                    FutureGTotal1 += (ProjectedExtTotalCost - ActualExtTotalCost);

                    summary = new Summary {H_ChangeOrder=p3  , A_Category = p1, B_Item = p2, C_BidCost = SalesExtTotalCost.ToString(), D_PlanCost = DesignExtTotalCost.ToString(), G_ProjectedCost = ProjectedExtTotalCost.ToString(), F_CostInFuture = (ProjectedExtTotalCost - ActualExtTotalCost).ToString(), E_ActualCost = ActualExtTotalCost.ToString() };

                    LstSummary.Add(summary);
                    rowIdx += 1;
                }

                summary = new Summary { C_BidCost = SalesGTotal1.ToString(), D_PlanCost = DesignGTotal1.ToString(), G_ProjectedCost = ProjectedGTotal1.ToString(), F_CostInFuture = FutureGTotal1.ToString(), E_ActualCost = ActualGTotal1.ToString() };
                LstSummary.Insert(ChangeOrder_SummaryInsertionPoint, summary);
                rowIdx += 1;


                summary = new Summary { A_Category = "Savings/Loss", B_Item = ((DesignGTotal + DesignGTotal1) - (ProjectedGTotal + ProjectedGTotal1)).ToString("#.##") };
                LstSummary.Insert(2, summary);
                rowIdx += 1;
                summary = new Summary { A_Category = "Cost Diviation", B_Item = ((((DesignGTotal + DesignGTotal1) - (ProjectedGTotal + ProjectedGTotal1)) / (DesignGTotal + DesignGTotal1)).ToString("#.##") + "%") };
                LstSummary.Insert(3, summary);
                rowIdx += 1;

                //All Totals
                summary = new Summary { C_BidCost = (SalesGTotal + SalesGTotal1).ToString(), D_PlanCost = (DesignGTotal + DesignGTotal1).ToString(), G_ProjectedCost = (ProjectedGTotal + ProjectedGTotal1).ToString(), F_CostInFuture = (FutureGTotal + FutureGTotal1).ToString(), E_ActualCost = (ActualGTotal + ActualGTotal1).ToString() };
                LstSummary.Insert(5, summary);
                rowIdx += 1;

                

                
                //----------------new ^ ---------------

                _dtSummary = ToDataTable<Summary>(LstSummary);
                dataGridView4.DataSource = _dtSummary;

                //Styling
                dataGridView4.Rows[5].DefaultCellStyle.Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Bold);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Please Close and reopen this form", ex.Message);
                //throw ex;
            }
        }
        public class ProductCategoryTotal
        {
            public string ProductCategory { get; set; }
            public double ExtCostTotal { get; set; }

        }
        #endregion Summary

        private void dataGridView1_RowCellClick(object sender, DevExpress.XtraGrid.Views.Grid.RowCellClickEventArgs e)
        {
            var gv = sender as GridView;
            var rowIndex = gv.FocusedRowHandle;
            var columnIndex = gv.FocusedColumn.VisibleIndex;

            //mnuCopyAllToDesignBOM.ShowDropDown();
            //contextMenuStrip1.Show(Cursor.Position.X, Cursor.Position.Y);
            if (e.Button == System.Windows.Forms.MouseButtons.Right && rowIndex >= 0 && columnIndex == -1)
            {
                //dataGridView1.CurrentCell = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];//if we uncomment this. then it will select full row and can not get cell column index. which is necessory for paste at place
                //dxe//dataGridView1.Rows[e.RowIndex].Selected = true;
                dataGridView1.FocusedRowHandle = rowIndex;//dxf

                //dataGridView1.Focus();
                MenuStripDelete.Show(Cursor.Position);
                return;
            }
            if (e.Button == System.Windows.Forms.MouseButtons.Right && rowIndex == -1 && columnIndex >= 0)
            {
                flowLayoutPanel1.Location = new Point(Cursor.Position.X, Cursor.Position.Y - 50);
                //flowLayoutPanel1.Show();
                flowLayoutPanel1.Visible = true;
                return;
            }

            if (rowIndex < 0 || columnIndex < 0) return;

            if (e.Button == System.Windows.Forms.MouseButtons.Right && rowIndex > -1 && columnIndex > -1)
            {
                //dxe//dataGridView1.CurrentCell = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];
                MenuStripSaleBOM.Show(Cursor.Position);
            }
        }
        private void dataGridView2_RowCellClick(object sender, RowCellClickEventArgs e)
        {
            var gv = sender as GridView;
            var rowIndex = gv.FocusedRowHandle;
            var columnIndex = gv.FocusedColumn.VisibleIndex;

            //mnuCopyAllToDesignBOM.ShowDropDown();
            //contextMenuStrip1.Show(Cursor.Position.X, Cursor.Position.Y);
            if (e.Button == System.Windows.Forms.MouseButtons.Right && rowIndex >= 0 && columnIndex == -1)
            {
                //dataGridView1.CurrentCell = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];//if we uncomment this. then it will select full row and can not get cell column index. which is necessory for paste at place
                //dxe//dataGridView1.Rows[e.RowIndex].Selected = true;
                dataGridView1.FocusedRowHandle = rowIndex;//dxf

                //dataGridView1.Focus();
                MenuStripDelete.Show(Cursor.Position);
                return;
            }
            if (e.Button == System.Windows.Forms.MouseButtons.Right && rowIndex == -1 && columnIndex >= 0)
            {
                flowLayoutPanel1.Location = new Point(Cursor.Position.X, Cursor.Position.Y - 50);
                //flowLayoutPanel1.Show();
                flowLayoutPanel1.Visible = true;
                return;
            }

            if (rowIndex < 0 || columnIndex < 0) return;

            if (e.Button == System.Windows.Forms.MouseButtons.Right && rowIndex > -1 && columnIndex > -1)
            {
                //dxe//dataGridView1.CurrentCell = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];
                MenuStripDesignBOM.Show(Cursor.Position);
            }
        }
        private void dataGridView3_RowCellClick(object sender, RowCellClickEventArgs e)
        {
            var gv = sender as GridView;
            var rowIndex = gv.FocusedRowHandle;
            var columnIndex = gv.FocusedColumn.VisibleIndex;

            //mnuCopyAllToDesignBOM.ShowDropDown();
            //contextMenuStrip1.Show(Cursor.Position.X, Cursor.Position.Y);
            if (e.Button == System.Windows.Forms.MouseButtons.Right && rowIndex >= 0 && columnIndex == -1)
            {
                //dataGridView1.CurrentCell = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];//if we uncomment this. then it will select full row and can not get cell column index. which is necessory for paste at place
                //dxe//dataGridView1.Rows[e.RowIndex].Selected = true;
                dataGridView1.FocusedRowHandle = rowIndex;//dxf

                //dataGridView1.Focus();
                MenuStripDelete.Show(Cursor.Position);
                return;
            }
            if (e.Button == System.Windows.Forms.MouseButtons.Right && rowIndex == -1 && columnIndex >= 0)
            {
                flowLayoutPanel1.Location = new Point(Cursor.Position.X, Cursor.Position.Y - 50);
                //flowLayoutPanel1.Show();
                flowLayoutPanel1.Visible = true;
                return;
            }

            if (rowIndex < 0 || columnIndex < 0) return;

            if (e.Button == System.Windows.Forms.MouseButtons.Right && rowIndex > -1 && columnIndex > -1)
            {
                //dxe//dataGridView1.CurrentCell = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];
                MenuStripActualBOM.Show(Cursor.Position);
            }
        }
        private void itmCopyAllToDesignBOM_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("All data from Bid BOM will copy to Planned BOM... Sure?", "Confirmation", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                _dtDesignBOM = new DataTable();
                _dtDesignBOM = _dtSalesBOM.Copy();

                //dataGridView2.DataSource =dataGridView1.DataSource;
                gridControl2.DataSource = _dtDesignBOM;
                tabControl1.SelectedTab = tabDesignBOM;
                IsGridView2Changed = true;
            }
        }

        private void itmCopyAllToActualBOM_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("All data from Planned BOM will copy to Actual BOM... Sure?", "Confirmation", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                _dtActualBOM = new DataTable();
                _dtActualBOM = _dtDesignBOM.Copy();

                //dataGridView2.DataSource =dataGridView1.DataSource;
                gridControl3.DataSource = _dtActualBOM;
                tabControl1.SelectedTab = tabActualBOM;
                IsGridView3Changed = true;
            }
        }
        private Project FillProjectModel()
        {
            Project lObjProj = new Project();
            //if (_newMode == false) lObjProj.ProjectCode = decimal.Parse(txtProjectCode.Text);
            lObjProj.ProjectCode = _currentLoadedProject.ProjectCode;
            lObjProj.ProjectName = _currentLoadedProject.ProjectName;
            lObjProj.EndUser = _currentLoadedProject.EndUser;
            lObjProj.Customer = _currentLoadedProject.Customer;
            return lObjProj;

        }
        private void btnSave_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Do you want to Save?", "Confirmation", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No) return;

            this.Enabled = false;
            Project projModel;
            ProjectEmployeeDetail ped;
            if (_newMode == true)
            {
                //SaveData();
                projModel = FillProjectModel();
                projModel.CreatedBy = LoginInfo.LoginEmployee.EmployeeCode;
                projModel.CreatedDate = DateTime.Now;
                _pc = new ProjectController(projModel);
                _pc.Save();

                ped = new ProjectEmployeeDetail();
                ped.EmployeeCode = LoginInfo.LoginEmployee.EmployeeCode;//_EmployeeCode;
                ped.ProjectCode = projModel.ProjectCode;//(decimal)row1["ProjectCode"];

                _pedc = new ProjectEmployeeDetailController(ped);
                _pedc.Save();
                //-------------------


                _newMode = false;
            }
            else
            {
                //UpdateData();
                projModel = FillProjectModel();
                projModel.CreatedBy = _currentLoadedProject.CreatedBy;
                projModel.CreatedDate = _currentLoadedProject.CreatedDate;
                projModel.UpdatedBy = LoginInfo.LoginEmployee.EmployeeCode;
                projModel.UpdateDate = DateTime.Now;
                _pc = new ProjectController(projModel);
                _pc.UpdateModel(projModel);

            }
            progressBar1.Visible = true;
            progressBar1.Value = 10;
            Application.DoEvents();
            List<BOM> LstObjBom;
            //--
            if (IsGridView1Changed == true)
            {
                LstObjBom = FillBOMModel1(ref projModel);
                _bc = new BOMController(LstObjBom);
                _bc.SaveList(projModel.ProjectCode, 1);
                IsGridView1Changed = false;
            }
            progressBar1.Value = 33;
            Application.DoEvents();
            //---
            if (IsGridView2Changed == true)
            {
                LstObjBom = FillBOMModel2(ref projModel);
                _bc = new BOMController(LstObjBom);
                _bc.SaveList(projModel.ProjectCode, 2);
                IsGridView2Changed = false;
            }
            progressBar1.Value = 66;
            Application.DoEvents();
            //---
            if (IsGridView3Changed == true)
            {
                LstObjBom = FillBOMModel3(ref projModel);
                _bc = new BOMController(LstObjBom);
                _bc.SaveList(projModel.ProjectCode, 3);
                IsGridView3Changed = false;
            }
            progressBar1.Value = 100;

            Application.DoEvents();
            //Project proj = _LstProjects.Where(x => x.ProjectCode == projModel.ProjectCode).FirstOrDefault();

            //if (proj == null)
            //{
            //    _LstProjects.Add(projModel);
            //}
            //else
            //{
            //    _LstProjects.Remove(proj);
            //    _LstProjects.Add(projModel);
            //}


            //_LstProjects
            this.Enabled = true;

            //////////////update shared object///////////////////

            _pc = new ProjectController();
            CurrentOpenProject.CurrentProject = _pc.GetModelByID(projModel.ProjectCode);
            ////////////
            MessageBox.Show("BOM Saved Successfully");
            this.Close();
        }

        private List<BOM> FillBOMModel1(ref Project pProjectModel)
        {
            List<BOM> LstObjBom = new List<BOM>();
            //dxe//
            //foreach (DataGridViewRow gvr in dataGridView1.Rows)
            //{
            //    FillBOMModelSub(ref pProjectModel, ref LstObjBom, gvr, 1);
            //}

            for (int i = 0; i < dataGridView1.DataRowCount; i++)
            {
                DataRow row = dataGridView1.GetDataRow(i);
                FillBOMModelSub(ref pProjectModel, ref LstObjBom, row, 1);
            }


            return LstObjBom;

        }

        private List<BOM> FillBOMModel2(ref Project pProjectModel)
        {
            List<BOM> LstObjBom = new List<BOM>();
            //foreach (DataGridViewRow gvr in dataGridView2.Rows)
            //{
            //    FillBOMModelSub(ref pProjectModel, ref LstObjBom, gvr, 2);
            //}

            for (int i = 0; i < dataGridView2.DataRowCount; i++)
            {
                DataRow row = dataGridView2.GetDataRow(i);
                FillBOMModelSub(ref pProjectModel, ref LstObjBom, row, 2);
            }
            return LstObjBom;

        }
        private List<BOM> FillBOMModel3(ref Project pProjectModel)
        {
            List<BOM> LstObjBom = new List<BOM>();
            //foreach (DataGridViewRow gvr in dataGridView3.Rows)
            //{
            //    FillBOMModelSub(ref pProjectModel, ref LstObjBom, gvr, 3);
            //}
            for (int i = 0; i < dataGridView3.DataRowCount; i++)
            {
                DataRow row = dataGridView3.GetDataRow(i);
                FillBOMModelSub(ref pProjectModel, ref LstObjBom, row, 3);
            }
            return LstObjBom;

        }
        //int cntr = -1;
        private void FillBOMModelSub(ref Project pProjectModel, ref List<BOM> pLstObjBom, DataRow pGvr, short pBOMTypeCode)
        {
            //string colName=pGvr.Cells[0].OwningColumn.HeaderText;

            bool isAdd = false;
            for (int i = 0; i < pGvr.ItemArray.Count(); i++)
            {
                //if (pGvr.Cells[i].Value == null || pGvr.Cells[i].Value == DBNull.Value || String.IsNullOrWhiteSpace(pGvr.Cells[i].Value.ToString()))
                if (pGvr[i] == null)
                {
                    isAdd = false;
                }
                else
                {
                    isAdd = true;
                    break;
                }
            }
            if (isAdd == true)
            {
                BOM lObjBom = new BOM();
                //lObjBom.BOMCode = (string)pGvr.Cells[0].Value;
                lObjBom.BOMTypeCode = pBOMTypeCode;//(string)pGvr.Cells[0].Value;
                lObjBom.ProjectCode = pProjectModel.ProjectCode;

                //for (int i = 0; i < 18; i++)
                //{
                //    MessageBox.Show(dataGridView1.Columns[i].Name);
                //}
                //cntr += 1;
                //MessageBox.Show(dataGridView1.Columns[cntr].Name);
                //string columnName = dataGridView1.Columns[cntr].Name;
                var cellObj = pGvr[0];
                lObjBom.Category1 = (cellObj == null) ? string.Empty : cellObj.ToString();

                cellObj = pGvr[1];
                lObjBom.Category2 = (cellObj == null) ? string.Empty : cellObj.ToString();

                cellObj = pGvr[2];
                lObjBom.Category3 = (cellObj == null) ? string.Empty : cellObj.ToString();

                cellObj = pGvr[3];
                lObjBom.SORef = (cellObj == null) ? string.Empty : cellObj.ToString();

                cellObj = pGvr[4];
                //lObjBom.Sr = (cellObj == null) ? (decimal?)null : Convert.ToDecimal (cellObj);
                if (cellObj == null || cellObj == DBNull.Value) { lObjBom.Sr = null; } else { lObjBom.Sr = Convert.ToDecimal(cellObj); }

                cellObj = pGvr[5];
                lObjBom.ProductCategory = (cellObj == null) ? string.Empty : cellObj.ToString();

                cellObj = pGvr[6];
                lObjBom.Product = (cellObj == null) ? string.Empty : cellObj.ToString();

                cellObj = pGvr[7];
                lObjBom.CostHead = (cellObj == null) ? string.Empty : cellObj.ToString();

                cellObj = pGvr[8];
                lObjBom.CostSubHead = (cellObj == null) ? string.Empty : cellObj.ToString();

                cellObj = pGvr[9];
                lObjBom.System = (cellObj == null) ? string.Empty : cellObj.ToString();

                cellObj = pGvr[10];
                lObjBom.Area = (cellObj == null) ? string.Empty : cellObj.ToString();

                cellObj = pGvr[11];
                lObjBom.Panel = (cellObj == null) ? string.Empty : cellObj.ToString();
                //lObjBom.Panel = (cellObj == null) ? (decimal?)null : Convert.ToDecimal(cellObj);
                //if (cellObj == null || cellObj == DBNull.Value) { lObjBom.Panel = null; } else { lObjBom.Panel = Convert.ToDecimal(cellObj); }

                cellObj = pGvr[12];
                lObjBom.Category = (cellObj == null) ? string.Empty : cellObj.ToString();

                cellObj = pGvr[13];
                lObjBom.Manufacturer = (cellObj == null) ? string.Empty : cellObj.ToString();

                cellObj = pGvr[14];
                lObjBom.PartNo = (cellObj == null) ? string.Empty : cellObj.ToString();

                cellObj = pGvr[15];
                lObjBom.Description = (cellObj == null) ? string.Empty : cellObj.ToString();

                cellObj = pGvr[16];
                //lObjBom.Qty = (cellObj == null) ? (decimal?)null : Convert.ToDecimal(cellObj);
                if (cellObj == null || cellObj == DBNull.Value) { lObjBom.Qty = null; } else { lObjBom.Qty = Convert.ToDecimal(cellObj); }



                cellObj = pGvr[17];
                //lObjBom.UnitCost = (cellObj == null) ? Convert.ToInt64(null) : Convert.ToDecimal(cellObj);
                //lObjBom.UnitCost = (cellObj == null) ? (decimal?)null : Convert.ToDecimal(cellObj);
                if (cellObj == null || cellObj == DBNull.Value) { lObjBom.UnitCost = null; } else { lObjBom.UnitCost = Convert.ToDecimal(cellObj); }




                cellObj = pGvr[18];
                //lObjBom.ExtCost = (cellObj == null) ? (decimal?)null : Convert.ToDecimal(cellObj);
                if (cellObj == null || cellObj == DBNull.Value) { lObjBom.ExtCost = null; } else { lObjBom.ExtCost = Convert.ToDecimal(cellObj); }



                cellObj = pGvr[19];
                //lObjBom.UnitPrice = (cellObj == null) ? (decimal?)null : Convert.ToDecimal(cellObj);
                if (cellObj == null || cellObj == DBNull.Value) { lObjBom.UnitPrice = null; } else { lObjBom.UnitPrice = Convert.ToDecimal(cellObj); }


                cellObj = pGvr[20];
                //lObjBom.ExtPrice = (cellObj == null) ? (decimal?)null : Convert.ToDecimal(cellObj);
                if (cellObj == null || cellObj == DBNull.Value) { lObjBom.ExtPrice = null; } else { lObjBom.ExtPrice = Convert.ToDecimal(cellObj); }


                cellObj = pGvr[21];
                lObjBom.ChangeOrder = (cellObj == null) ? string.Empty : cellObj.ToString();

                cellObj = pGvr[22];
                lObjBom.Column1 = (cellObj == null) ? string.Empty : cellObj.ToString();

                cellObj = pGvr[23];
                lObjBom.Column2 = (cellObj == null) ? string.Empty : cellObj.ToString();

                cellObj = pGvr[24];
                lObjBom.Column3 = (cellObj == null) ? string.Empty : cellObj.ToString();

                cellObj = pGvr[25];
                lObjBom.Column4 = (cellObj == null) ? string.Empty : cellObj.ToString();

                cellObj = pGvr[26];
                lObjBom.Column5 = (cellObj == null) ? string.Empty : cellObj.ToString();

                pLstObjBom.Add(lObjBom);
                pProjectModel.BOMs.Add(lObjBom);

            }

            //return null;
        }

        public DataTable ToDataTable<T>(List<T> items)
        {
            DataTable dataTable = new DataTable(typeof(T).Name);

            //Get all the properties
            PropertyInfo[] Props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo prop in Props)
            {
                //Defining type of data column gives proper data table 
                var type = (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) ? Nullable.GetUnderlyingType(prop.PropertyType) : prop.PropertyType);
                //Setting column names as Property names
                dataTable.Columns.Add(prop.Name, type);
            }
            foreach (T item in items)
            {
                var values = new object[Props.Length];
                for (int i = 0; i < Props.Length; i++)
                {
                    //inserting property values to datatable rows
                    values[i] = Props[i].GetValue(item, null);
                }
                dataTable.Rows.Add(values);
            }
            //put a breakpoint here and check datatable
            return dataTable;
        }



        private void ClearAll()
        {
            if (_dtSalesBOM != null) _dtSalesBOM.Rows.Clear();
            if (_dtDesignBOM != null) _dtDesignBOM.Rows.Clear();
            if (_dtActualBOM != null) _dtActualBOM.Rows.Clear();

        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void FrmBOM_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (StaticClasses.NewProjectOpened.ClosePreviousProjectFormsWithOutConfirmation == true) return;
            DialogResult dialogResult = MessageBox.Show("Close this window?", "Confirmation", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No) e.Cancel = true;
            progressBar1.Visible = false;
        }
        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {

        }
        private void dataGridView1_CustomRowCellEdit(object sender, CustomRowCellEditEventArgs e)
        {

        }
        private void dataGridView1_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            IsGridView1Changed = true;
        }

        private void dataGridView2_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            IsGridView2Changed = true;
            //dxe//decimal quantity = ReturnAppropriateValue(dataGridView2.Rows[e.RowIndex].Cells["Qty2"].Value);
            //dxe//decimal rate = ReturnAppropriateValue(dataGridView2.Rows[e.RowIndex].Cells["UnitCost2"].Value);

            //if (decimal.TryParse(dataGridView2.Rows[e.RowIndex].Cells["Qty"].Value.ToString(), out quantity) && decimal.TryParse(dataGridView2.Rows[e.RowIndex].Cells["UnitCost"].Value.ToString(), out rate))
            //{
            //dxe//decimal price = quantity * rate;
            //dataGridView2.Rows[e.RowIndex].Cells[15].Value = price.ToString();
            //dxe//dataGridView2.Rows[e.RowIndex].Cells["ExtCost2"].Value = price.ToString();

            //}
        }

        private void dataGridView2_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            if (gFlag == false)
            {
                gFlag = true;
                IsGridView2Changed = true;
                var gv = sender as GridView;
                var rowIndex = gv.FocusedRowHandle;
                var columnIndex = gv.FocusedColumn.VisibleIndex;

                //decimal quantity = ReturnAppropriateValue(dataGridView2.Rows[e.RowIndex].Cells["Qty2"].Value);
                //decimal rate = ReturnAppropriateValue(dataGridView2.Rows[e.RowIndex].Cells["UnitCost2"].Value);

                //decimal quantity = ReturnAppropriateValue(gv.GetRowCellDisplayText(rowIndex, dataGridView2.Columns[columnIndex]));
                var abc = gv.GetRowCellValue(rowIndex, "Qty");
                decimal quantity = ReturnAppropriateValue(gv.GetRowCellDisplayText(rowIndex, "Qty"));
                decimal rate = ReturnAppropriateValue(gv.GetRowCellDisplayText(rowIndex, "UnitCost"));

                //if (decimal.TryParse(dataGridView2.Rows[e.RowIndex].Cells["Qty"].Value.ToString(), out quantity) && decimal.TryParse(dataGridView2.Rows[e.RowIndex].Cells["UnitCost"].Value.ToString(), out rate))
                //{
                decimal price = quantity * rate;
                //dataGridView2.Rows[e.RowIndex].Cells[15].Value = price.ToString();
                //dataGridView2.Rows[e.RowIndex].Cells["ExtCost2"].Value = price.ToString();
                gv.SetFocusedRowCellValue("ExtCost", price);
                gFlag = false;

                //v--------cal total and show on top-------------------
                //_TotExtCost = 0;
                //for (int i = 0; i < dataGridView2.DataRowCount; i++)
                //{
                //    if (i == 0) continue;
                //    DataRow row = dataGridView2.GetDataRow(i);
                //    //FillBOMModelSub(ref pProjectModel, ref LstObjBom, row, 3);
                //    var varObj = row[18];//ExtCost

                //    if (!(varObj == null || varObj == DBNull.Value))
                //    {
                //        _TotExtCost += Convert.ToDecimal(row[18]);
                //    }
                //}
                //_dtDesignBOM.Rows[0][18] = _TotExtCost;

                //^--------cal total and show on top------------------
                GetSetExtCostTotal(ref _dtDesignBOM);


            }
        }
        private void GetSetExtCostTotal(ref DataTable dataTable)
        {
            _ExtCostTotal = 0;
            _ExtCostSubTotal = 0;
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                if (i == 0) continue;
                DataRow row = dataTable.Rows[i];
                var varObj = row[18];//ExtCost
                if (!(varObj == null || varObj == DBNull.Value))
                {
                    _ExtCostTotal += Convert.ToDecimal(row[18]);
                }
            }
            //dataTable.Rows[0][18] = _TotExtCost;
            txtExtCostTotal.Text = _ExtCostTotal.ToString();

            //still working on sub total. this code not summing correct. IsRowVisible is not returning correct rows. need some other handle row something.
            //for (int i = 0; i < dataGridView1.DataRowCount; i++)
            //{
            //    var isRowVisible = dataGridView1.IsRowVisible(i);
            //    if (isRowVisible == RowVisibleState.Visible)
            //    {
            //        DataRow row = dataGridView1.GetDataRow(i);
            //        var varObj = row[18];//ExtCost
            //        if (!(varObj == null || varObj == DBNull.Value))
            //        {
            //            _ExtCostSubTotal += Convert.ToDecimal(row[18]);
            //        }
            //    }
            //}
            //txtExtCostSubTotal.Text = _ExtCostSubTotal.ToString();

            //var abccc= dataGridView2.Columns["ExtCost"].Summary.Add(DevExpress.Data.SummaryItemType.Sum, "TotalPrice", "Grand Total = {0:c2}");
            //var abccc = dataGridView2.Columns["ExtCost"].Summary;

        }
        private void dataGridView3_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            if (gFlag == false)
            {
                gFlag = true;
                IsGridView3Changed = true;
                var gv = sender as GridView;
                var rowIndex = gv.FocusedRowHandle;
                var columnIndex = gv.FocusedColumn.VisibleIndex;

                //decimal quantity = ReturnAppropriateValue(dataGridView3.Rows[e.RowIndex].Cells["Qty2"].Value);
                //decimal rate = ReturnAppropriateValue(dataGridView3.Rows[e.RowIndex].Cells["UnitCost2"].Value);

                //decimal quantity = ReturnAppropriateValue(gv.GetRowCellDisplayText(rowIndex, dataGridView3.Columns[columnIndex]));
                var abc = gv.GetRowCellValue(rowIndex, "Qty");
                decimal quantity = ReturnAppropriateValue(gv.GetRowCellDisplayText(rowIndex, "Qty"));
                decimal rate = ReturnAppropriateValue(gv.GetRowCellDisplayText(rowIndex, "UnitCost"));

                //if (decimal.TryParse(dataGridView3.Rows[e.RowIndex].Cells["Qty"].Value.ToString(), out quantity) && decimal.TryParse(dataGridView3.Rows[e.RowIndex].Cells["UnitCost"].Value.ToString(), out rate))
                //{
                decimal price = quantity * rate;
                //dataGridView3.Rows[e.RowIndex].Cells[15].Value = price.ToString();
                //dataGridView3.Rows[e.RowIndex].Cells["ExtCost2"].Value = price.ToString();
                gv.SetFocusedRowCellValue("ExtCost", price);
                gFlag = false;

                //v--------cal total and show on top-------------------
                //_TotExtCost = 0;
                //for (int i = 0; i < dataGridView3.DataRowCount; i++)
                //{
                //    if (i == 0) continue;
                //    DataRow row = dataGridView3.GetDataRow(i);
                //    //FillBOMModelSub(ref pProjectModel, ref LstObjBom, row, 3);
                //    var varObj = row[18];//ExtCost

                //    if (!(varObj == null || varObj == DBNull.Value))
                //    {
                //        _TotExtCost += Convert.ToDecimal(row[18]);
                //    }
                //}
                //_dtActualBOM.Rows[0][18] = _TotExtCost;

                //^--------cal total and show on top------------------
                GetSetExtCostTotal(ref _dtActualBOM);
            }
        }

        private void dataGridView2_ValidatingEditor(object sender, DevExpress.XtraEditors.Controls.BaseContainerValidateEditorEventArgs e)
        {
            GridView view = sender as GridView;
            if (view.FocusedColumn.FieldName == "Sr" || view.FocusedColumn.FieldName == "Qty" || view.FocusedColumn.FieldName == "UnitCost" ||
                view.FocusedColumn.FieldName == "UnitPrice" || view.FocusedColumn.FieldName == "ExtPrice")
            {
                double price = 0;
                if (!String.IsNullOrEmpty(e.Value.ToString()) && !Double.TryParse(e.Value as String, out price))
                {
                    e.Valid = false;
                    e.ErrorText = "Only numeric values are accepted.";
                }
            }

        }
        private void dataGridView3_ValidatingEditor(object sender, DevExpress.XtraEditors.Controls.BaseContainerValidateEditorEventArgs e)
        {
            GridView view = sender as GridView;
            if (view.FocusedColumn.FieldName == "Sr" || view.FocusedColumn.FieldName == "Qty" || view.FocusedColumn.FieldName == "UnitCost" ||
                view.FocusedColumn.FieldName == "UnitPrice" || view.FocusedColumn.FieldName == "ExtPrice")
            {
                double price = 0;
                if (!String.IsNullOrEmpty(e.Value.ToString()) && !Double.TryParse(e.Value as String, out price))
                {
                    e.Valid = false;
                    e.ErrorText = "Only numeric values are accepted.";
                }
            }
        }

        private void dataGridView3_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (dataGridView3.Columns[e.ColumnIndex].Name == "Sr3" ||
            dataGridView3.Columns[e.ColumnIndex].Name == "Qty3" ||
            dataGridView3.Columns[e.ColumnIndex].Name == "UnitCost3" ||
            dataGridView3.Columns[e.ColumnIndex].Name == "UnitPrice3" ||
            dataGridView3.Columns[e.ColumnIndex].Name == "ExtPrice3")
            {
                decimal parsedValue;
                if (!String.IsNullOrEmpty(e.FormattedValue.ToString()) && decimal.TryParse(e.FormattedValue.ToString(), out parsedValue) == false)
                {
                    MessageBox.Show("Only numeric values are allowed");
                    e.Cancel = true;
                }
            }
        }

        private decimal ReturnAppropriateValue(object ObjValue)
        {
            //lObjBom.PartNo = (cellObj.Value == null) ? string.Empty : cellObj.Value.ToString();

            //decimal decimalValue = Convert.ToDecimal(string.IsNullOrEmpty(ObjValue.ToString()) ? "0" : ObjValue);

            if (ObjValue == null || ObjValue == DBNull.Value || string.IsNullOrEmpty(ObjValue.ToString().Trim()))
            {
                return 0;
            }
            else
            {
                return Convert.ToDecimal(ObjValue);
            }

            //decimal decimalValue = Convert.ToDecimal((ObjValue == null) ? "0" : ObjValue);

            //return decimalValue;

            //if (decimal.TryParse(ObjValue.ToString(), out returnDecimalValue))

            //if (Convert.ToDecimal(string.IsNullOrEmpty(ObjValue.ToString()) ? "0" : ObjValue))
            //{
            //    return returnDecimalValue;
            //}
            //else
            //{
            //    return 0;
            //}
        }

        private void FrmBOM_Activated(object sender, EventArgs e)
        {
            txtBOMFilePath.Focus();
        }

        private void splitContainer1_SplitterMoved(object sender, SplitterEventArgs e)
        {

        }

        private void FrmBOM_KeyDown(object sender, KeyEventArgs e)
        {
            //if (e.KeyCode == Keys.Escape)
            //{
            //    this.Close();
            //}
        }
        private void LoadBOM_Click(object sender, EventArgs e)
        {
            MenuStripLoad.Show(Cursor.Position);
        }
        private void btnLoadBOM_Enter(object sender, EventArgs e)
        {
            MenuStripLoad.Show(Cursor.Position);
        }
        private void btnLoadBOM_MouseEnter(object sender, EventArgs e)
        {
            MenuStripLoad.Show(Cursor.Position);
        }

        private void copyFromExcelToSaleBOMToolStripMenuItem_Click(object sender, EventArgs e)
        {

            PasteFromExcel(ref _dtSalesBOM, 1);
        }
        private void copyFromExcelToDesignBOMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PasteFromExcel(ref _dtDesignBOM, 2);
        }
        private void copyFromExcelToActualBOM_Click(object sender, EventArgs e)
        {
            PasteFromExcel(ref _dtActualBOM, 3);
        }

        private void PasteFromExcel(ref DataTable dtRef, int gridviewNumber)
        {

            string excelData = Clipboard.GetText();
            List<string> Rows = excelData.Split(new[] { "\r\n" }, StringSplitOptions.None).ToList<string>();

            //mytab.Split(new[] { "\r\n" }, StringSplitOptions.None);
            //string[] lines = excelData.Replace("\n", "").Split('\r');
            //string[] lines = Regex.Split(s.TrimEnd("\r\n".ToCharArray()), "\r\n");
            string[] fields;


            int rowCounter = 0;
            bool IsPasteData = true;
            foreach (string row in Rows.ToList<string>())
            {
                rowCounter += 1;
                fields = row.Split('\t');

                if (fields.Count() == 1 && fields[0] == string.Empty)
                {
                    Rows.Remove(row);
                    continue;

                }

                if (fields.Count() < 18)
                {
                    MessageBox.Show("Data not copied. There is a 'Enter' in Cell. Please remove 'Enter' after '" + fields.Last() + "' in column " + _columnNames[fields.Count() - 1] + " from Row number " + rowCounter);
                    IsPasteData = false;
                    break;
                }
            }
            if (IsPasteData == false) return;
            foreach (string row in Rows)
            {
                fields = row.Split('\t');
                DataRow newRow = dtRef.NewRow();
                newRow["Category1"] = fields[0];
                newRow["Category2"] = fields[1];
                newRow["Category3"] = fields[2];
                newRow["SORef"] = fields[3];
                newRow["Sr"] = fields[4];
                newRow["ProductCategory"] = fields[5];
                newRow["Product"] = fields[6];
                newRow["CostHead"] = fields[7];
                newRow["CostSubHead"] = fields[8];
                newRow["System"] = fields[9];
                newRow["Area"] = fields[10];
                newRow["Panel"] = fields[11];
                newRow["Category"] = fields[12];
                newRow["Manufacturer"] = fields[13];
                newRow["PartNo"] = fields[14];
                newRow["Description"] = fields[15];
                newRow["Qty"] = fields[16];
                newRow["UnitCost"] = fields[17];
                newRow["ExtCost"] = fields[18];
                newRow["UnitPrice"] = fields[19];
                newRow["ExtPrice"] = fields[20];
                newRow["ChangeOrder"] = fields[21];
                newRow["Column1"] = fields[22];
                newRow["Column2"] = fields[23];
                newRow["Column3"] = fields[24];
                newRow["Column4"] = fields[25];
                newRow["Column5"] = fields[26];


                dtRef.Rows.Add(newRow);

            }
            switch (gridviewNumber)
            {
                case 1:
                    gridControl1.DataSource = dtRef;
                    IsGridView1Changed = true;
                    break;
                case 2:
                    gridControl2.DataSource = dtRef;
                    IsGridView2Changed = true;
                    break;
                case 3:
                    gridControl3.DataSource = dtRef;
                    IsGridView3Changed = true;
                    break;
            }


            //foreach (string stringRow in stringRows)
            //{
            //    dtRef.Rows.Add(new DataRow(stringRow));
            //}


            //DataRow newRow = dtRef.NewRow();
            //newRow["Sr"] = gvr.Cells["Sr2"].Value;
            //newRow["PartNo"] = gvr.Cells["PartNo2"].Value;
            //newRow["Description"] = gvr.Cells["Description2"].Value;
            //newRow["Qty"] = gvr.Cells["Qty2"].Value;
            //newRow["UnitCost"] = gvr.Cells["UnitCost2"].Value;
            //newRow["ExtCost"] = gvr.Cells["ExtCost2"].Value;
            //newRow["UnitPrice"] = gvr.Cells["UnitPrice2"].Value;
            //newRow["ExtPrice"] = gvr.Cells["ExtPrice2"].Value;
            //dtMR.Rows.Add(newRow);


            //dtRef.Rows.Add(myRows.Count - 1);
            //string[] fields;
            //int row = 0;
            //int col = 0;

            //foreach (string item in myRows)
            //{
            //    fields = item.Split('\t');
            //    foreach (string f in fields)
            //    {
            //        //Console.WriteLine(f);
            //        dtRef[col, row].Value = f;
            //        col++;
            //    }
            //    row++;
            //    col = 0;
            //}
        }
        private void PasteColumnsAtPlaceFromExcelToSaleBOMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PasteColumnsAtPlaceFromExcel(ref _dtSalesBOM, 1);
        }
        private void PasteColumnsAtPlaceFromExcelToDesignBOMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PasteColumnsAtPlaceFromExcel(ref _dtDesignBOM, 2);
        }
        private void PasteColumnsAtPlaceFromExcelToActualBOMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PasteColumnsAtPlaceFromExcel(ref _dtActualBOM, 3);
        }
        private void PasteColumnsAtPlaceFromExcel(ref DataTable dtRef, int gridviewNumber)
        {
            try
            {


                string excelData = Clipboard.GetText();
                List<string> Rows = excelData.Split(new[] { "\r\n" }, StringSplitOptions.None).ToList<string>();

                //mytab.Split(new[] { "\r\n" }, StringSplitOptions.None);
                //string[] lines = excelData.Replace("\n", "").Split('\r');
                //string[] lines = Regex.Split(s.TrimEnd("\r\n".ToCharArray()), "\r\n");
                string[] fields;

                
                int rowCounter = 0;
                bool IsPasteData = true;
                foreach (string row in Rows.ToList<string>())
                {
                    rowCounter += 1;
                    fields = row.Split('\t');

                    if (fields.Count() == 1 && fields[0] == string.Empty)
                    {
                        Rows.Remove(row);
                        continue;

                    }
                    //If there is enter in row. then coloumn count in certain row will be less than 18
                    //if (fields.Count() < 3)
                    //{
                    //    MessageBox.Show("Data not copied. There is a 'Enter' in Cell. Please remove 'Enter' after '" + fields.Last() + "' in column " + _columnNames[fields.Count() - 1] + " from Row number " + rowCounter);
                    //    IsPasteData = false;
                    //    break;
                    //}
                }
                if (IsPasteData == false) return;

                switch (gridviewNumber)
                {


                    case 1:

                        int selectedrowIndex1 = dataGridView1.FocusedRowHandle; //dxe//= dataGridView1.SelectedCells[0].RowIndex;
                        int selectedColumnIndex1 = dataGridView1.FocusedColumn.VisibleIndex; //dxe//= dataGridView1.SelectedCells[0].ColumnIndex;
                        int oringalcolumnIndex1 = dataGridView1.FocusedColumn.VisibleIndex;
                        int[] SelectedRowHandles = dataGridView1.GetSelectedRows();
                        //string B = dataGridView1.GetRowCellValue(SelectedRowHandles[0], dataGridView1.Columns[0]);


                        //DataGridViewRow selectedRow = dataGridView1.Rows[selectedrowindex];
                        ////string a = Convert.ToString(selectedRow.Cells["enter column name"].Value);
                        //DataRowView currentDataRowView = (DataRowView)dataGridView1.CurrentRow.DataBoundItem;
                        //DataRow row = currentDataRowView.Row;
                        ////selectedRow.row
                        ///

                        //dataGridView1.Rows[selectedrowindex].Cells[0].Value = price.ToString();

                        ////////dxe////////////////////////////

                        foreach (string row in Rows)
                        {
                            fields = row.Split('\t');

                            for (int i = 0; i <= fields.Count() - 1; i++)
                            {
                                if ((selectedColumnIndex1 >= 0 && selectedColumnIndex1 <= 2) || (selectedColumnIndex1 >= 21 && selectedColumnIndex1 <= 25))
                                {
                                    //dataGridView1.Rows[selectedrowIndex1].Cells[selectedColumnIndex1].Value = fields[i]; //price.ToString();

                                    string theValue = fields[i].Replace(" ", string.Empty).Replace("$", "").Replace(",", "");
                                    string ValueWithOutDot = fields[i].Replace(" ", string.Empty).Replace("$", "").Replace(".", "").Replace(",", "");
                                    if (IsNumeric(ValueWithOutDot))
                                    {
                                        dataGridView1.SetRowCellValue(selectedrowIndex1, dataGridView1.Columns[selectedColumnIndex1], theValue);
                                    }
                                    else
                                    {
                                        dataGridView1.SetRowCellValue(selectedrowIndex1, dataGridView1.Columns[selectedColumnIndex1], fields[i]);
                                    }

                                }
                                selectedColumnIndex1 += 1;

                            }
                            selectedColumnIndex1 = oringalcolumnIndex1;//dataGridView1.SelectedCells[0].ColumnIndex;
                            selectedrowIndex1 += 1;

                        }

                        /////////////////////////////

                        gridControl1.DataSource = dtRef;
                        IsGridView1Changed = true;
                        break;
                    case 2:

                        ////////dxe////////////////////////////
                        int selectedrowIndex2 = dataGridView2.FocusedRowHandle;//dataGridView2.SelectedCells[0].RowIndex;
                        int selectedColumnIndex2 = dataGridView2.FocusedColumn.VisibleIndex;//dataGridView2.SelectedCells[0].ColumnIndex;
                        int oringalcolumnIndex2 = dataGridView2.FocusedColumn.VisibleIndex;
                        foreach (string row in Rows)
                        {
                            fields = row.Split('\t');

                            for (int i = 0; i <= fields.Count() - 1; i++)
                            {
                                string theValue = fields[i].Replace(" ", string.Empty).Replace("$", "").Replace(",", "");
                                string ValueWithOutDot = fields[i].Replace(" ", string.Empty).Replace("$", "").Replace(".", "").Replace(",", "");
                                if (IsNumeric(ValueWithOutDot))
                                {
                                    //dataGridView2.Rows[selectedrowIndex2].Cells[selectedColumnIndex2].Value = fields[i]; //price.ToString();
                                    dataGridView2.SetRowCellValue(selectedrowIndex2, dataGridView1.Columns[selectedColumnIndex2], theValue);
                                }
                                else
                                {
                                    //dataGridView2.Rows[selectedrowIndex2].Cells[selectedColumnIndex2].Value = fields[i]; //price.ToString();
                                    dataGridView2.SetRowCellValue(selectedrowIndex2, dataGridView1.Columns[selectedColumnIndex2], fields[i]);
                                }

                                
                                selectedColumnIndex2 += 1;
                            }
                            selectedColumnIndex2 = oringalcolumnIndex2;//dataGridView2.SelectedCells[0].ColumnIndex;
                            selectedrowIndex2 += 1;

                        }

                        /////////////////////////////
                        gridControl2.DataSource = dtRef;
                        IsGridView2Changed = true;
                        break;
                    case 3:
                        ////////dxe////////////////////////////
                        int selectedrowIndex3 = dataGridView3.FocusedRowHandle;//dataGridView3.SelectedCells[0].RowIndex;
                        int selectedColumnIndex3 = dataGridView3.FocusedColumn.VisibleIndex;//dataGridView3.SelectedCells[0].ColumnIndex;
                        int oringalcolumnIndex3 = dataGridView3.FocusedColumn.VisibleIndex;
                        foreach (string row in Rows)
                        {
                            fields = row.Split('\t');

                            for (int i = 0; i <= fields.Count() - 1; i++)
                            {
                                
                                string theValue = fields[i].Replace(" ", string.Empty).Replace("$", "").Replace(",","");
                                string ValueWithOutDot= fields[i].Replace(" ", string.Empty).Replace("$", "").Replace(".","").Replace(",","");
                                if (IsNumeric(ValueWithOutDot))
                                {
                                    //dataGridView2.Rows[selectedrowIndex2].Cells[selectedColumnIndex2].Value = fields[i]; //price.ToString();
                                    dataGridView3.SetRowCellValue(selectedrowIndex3, dataGridView3.Columns[selectedColumnIndex3], theValue);
                                }
                                else
                                {
                                    //dataGridView2.Rows[selectedrowIndex2].Cells[selectedColumnIndex2].Value = fields[i]; //price.ToString();
                                    dataGridView3.SetRowCellValue(selectedrowIndex3, dataGridView3.Columns[selectedColumnIndex3], fields[i]);
                                }
                                selectedColumnIndex3 += 1;
                            }
                            selectedColumnIndex3 = oringalcolumnIndex3;//dataGridView3.SelectedCells[0].ColumnIndex;
                            selectedrowIndex3 += 1;

                        }

                        /////////////////////////////
                        gridControl3.DataSource = dtRef;
                        IsGridView3Changed = true;
                        break;
                }

            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }
        private void toolStripMenuItem_Insert_SalesBOM_Click(object sender, EventArgs e)
        {
            InsertColumnsAtPlaceFromExcel(ref _dtSalesBOM, 1);
        }
        private void toolStripMenuItem_Insert_DesignBOM_Click(object sender, EventArgs e)
        {
            InsertColumnsAtPlaceFromExcel(ref _dtDesignBOM, 2);
        }
        private void toolStripMenuItem_Insert_ActualBOM_Click(object sender, EventArgs e)
        {
            InsertColumnsAtPlaceFromExcel(ref _dtActualBOM, 3);
        }
        private void InsertColumnsAtPlaceFromExcel(ref DataTable dtRef, int gridviewNumber)
        {
            try
            {


                string excelData = Clipboard.GetText();
                List<string> Rows = excelData.Split(new[] { "\r\n" }, StringSplitOptions.None).ToList<string>();

                //mytab.Split(new[] { "\r\n" }, StringSplitOptions.None);
                //string[] lines = excelData.Replace("\n", "").Split('\r');
                //string[] lines = Regex.Split(s.TrimEnd("\r\n".ToCharArray()), "\r\n");
                string[] fields;


                int rowCounter = 0;
                bool IsPasteData = true;
                foreach (string row in Rows.ToList<string>())
                {
                    rowCounter += 1;
                    fields = row.Split('\t');

                    if (fields.Count() == 1 && fields[0] == string.Empty)
                    {
                        Rows.Remove(row);
                        continue;

                    }
                    //If there is enter in row. then coloumn count in certain row will be less than 18
                    //if (fields.Count() < 3)
                    //{
                    //    MessageBox.Show("Data not copied. There is a 'Enter' in Cell. Please remove 'Enter' after '" + fields.Last() + "' in column " + _columnNames[fields.Count() - 1] + " from Row number " + rowCounter);
                    //    IsPasteData = false;
                    //    break;
                    //}
                }
                if (IsPasteData == false) return;
                List<string> LSTdataColStr;
                switch (gridviewNumber)
                {


                    case 1:
                        
                        int selectedrowIndex1 = dataGridView1.FocusedRowHandle;
                        foreach (string row in Rows)
                        {
                            LSTdataColStr = new List<string>();
                            fields = row.Split('\t');

                            for (int i = 0; i <= fields.Count() - 1; i++)
                            {
                                string theValue = fields[i].Replace(" ", string.Empty).Replace("$", "").Replace(",", "");
                                string ValueWithOutDot = fields[i].Replace(" ", string.Empty).Replace("$", "").Replace(".", "").Replace(",", "");
                                if (IsNumeric(ValueWithOutDot))
                                {
                                    LSTdataColStr.Add(theValue);
                                }
                                else
                                {
                                    LSTdataColStr.Add(fields[i]);
                                }

                            }
                            DataRow newRow = dtRef.NewRow();
                            newRow["Category1"] = LSTdataColStr[0];
                            newRow["Category2"] = LSTdataColStr[1];
                            newRow["Category3"] = LSTdataColStr[2];
                            newRow["SORef"] = LSTdataColStr[3];
                            newRow["Sr"] = LSTdataColStr[4];
                            newRow["ProductCategory"] = LSTdataColStr[5];
                            newRow["Product"] = LSTdataColStr[6];
                            newRow["CostHead"] = LSTdataColStr[7];
                            newRow["CostSubHead"] = LSTdataColStr[8];
                            newRow["System"] = LSTdataColStr[9];
                            newRow["Area"] = LSTdataColStr[10];
                            newRow["Panel"] = LSTdataColStr[11];
                            newRow["Category"] = LSTdataColStr[12];
                            newRow["Manufacturer"] = LSTdataColStr[13];
                            newRow["PartNo"] = LSTdataColStr[14];
                            newRow["Description"] = LSTdataColStr[15];
                            newRow["Qty"] = LSTdataColStr[16];
                            newRow["UnitCost"] = LSTdataColStr[17];
                            newRow["ExtCost"] = LSTdataColStr[18];
                            newRow["UnitPrice"] = LSTdataColStr[19];
                            newRow["ExtPrice"] = LSTdataColStr[20];
                            newRow["ChangeOrder"] = LSTdataColStr[21];
                            newRow["Column1"] = LSTdataColStr[22];
                            newRow["Column2"] = LSTdataColStr[23];
                            newRow["Column3"] = LSTdataColStr[24];
                            newRow["Column4"] = LSTdataColStr[25];
                            newRow["Column5"] = LSTdataColStr[26];
                            dtRef.Rows.InsertAt(newRow, selectedrowIndex1);
                            selectedrowIndex1 += 1;
                        }

                        /////////////////////////////
                        //gridControl1.DataSource = dtRef;
                        IsGridView1Changed = true;
                        break;
                    case 2:
                        
                        int selectedrowIndex2 = dataGridView2.FocusedRowHandle;
                        foreach (string row in Rows)
                        {
                            LSTdataColStr = new List<string>();
                            fields = row.Split('\t');

                            for (int i = 0; i <= fields.Count() - 1; i++)
                            {
                                string theValue = fields[i].Replace(" ", string.Empty).Replace("$", "").Replace(",", "");
                                string ValueWithOutDot = fields[i].Replace(" ", string.Empty).Replace("$", "").Replace(".", "").Replace(",", "");
                                if (IsNumeric(ValueWithOutDot))
                                {
                                    LSTdataColStr.Add(theValue);
                                }
                                else
                                {
                                    LSTdataColStr.Add(fields[i]);
                                }
                                
                            }
                            DataRow newRow = dtRef.NewRow();
                            newRow["Category1"] = LSTdataColStr[0];
                            newRow["Category2"] = LSTdataColStr[1];
                            newRow["Category3"] = LSTdataColStr[2];
                            newRow["SORef"] = LSTdataColStr[3];
                            newRow["Sr"] = LSTdataColStr[4];
                            newRow["ProductCategory"] = LSTdataColStr[5];
                            newRow["Product"] = LSTdataColStr[6];
                            newRow["CostHead"] = LSTdataColStr[7];
                            newRow["CostSubHead"] = LSTdataColStr[8];
                            newRow["System"] = LSTdataColStr[9];
                            newRow["Area"] = LSTdataColStr[10];
                            newRow["Panel"] = LSTdataColStr[11];
                            newRow["Category"] = LSTdataColStr[12];
                            newRow["Manufacturer"] = LSTdataColStr[13];
                            newRow["PartNo"] = LSTdataColStr[14];
                            newRow["Description"] = LSTdataColStr[15];
                            newRow["Qty"] = LSTdataColStr[16];
                            newRow["UnitCost"] = LSTdataColStr[17];
                            newRow["ExtCost"] = LSTdataColStr[18];
                            newRow["UnitPrice"] = LSTdataColStr[19];
                            newRow["ExtPrice"] = LSTdataColStr[20];
                            newRow["ChangeOrder"] = LSTdataColStr[21];
                            newRow["Column1"] = LSTdataColStr[22];
                            newRow["Column2"] = LSTdataColStr[23];
                            newRow["Column3"] = LSTdataColStr[24];
                            newRow["Column4"] = LSTdataColStr[25];
                            newRow["Column5"] = LSTdataColStr[26];
                            dtRef.Rows.InsertAt(newRow, selectedrowIndex2);
                            selectedrowIndex2 += 1;
                        }

                        /////////////////////////////
                        //gridControl2.DataSource = dtRef;
                        IsGridView2Changed = true;
                        break;
                    case 3:
                        int selectedrowIndex3 = dataGridView3.FocusedRowHandle;
                        foreach (string row in Rows)
                        {
                            LSTdataColStr = new List<string>();
                            fields = row.Split('\t');

                            for (int i = 0; i <= fields.Count() - 1; i++)
                            {
                                string theValue = fields[i].Replace(" ", string.Empty).Replace("$", "").Replace(",", "");
                                string ValueWithOutDot = fields[i].Replace(" ", string.Empty).Replace("$", "").Replace(".", "").Replace(",", "");
                                if (IsNumeric(ValueWithOutDot))
                                {
                                    LSTdataColStr.Add(theValue);
                                }
                                else
                                {
                                    LSTdataColStr.Add(fields[i]);
                                }

                            }
                            DataRow newRow = dtRef.NewRow();
                            newRow["Category1"] = LSTdataColStr[0];
                            newRow["Category2"] = LSTdataColStr[1];
                            newRow["Category3"] = LSTdataColStr[2];
                            newRow["SORef"] = LSTdataColStr[3];
                            newRow["Sr"] = LSTdataColStr[4];
                            newRow["ProductCategory"] = LSTdataColStr[5];
                            newRow["Product"] = LSTdataColStr[6];
                            newRow["CostHead"] = LSTdataColStr[7];
                            newRow["CostSubHead"] = LSTdataColStr[8];
                            newRow["System"] = LSTdataColStr[9];
                            newRow["Area"] = LSTdataColStr[10];
                            newRow["Panel"] = LSTdataColStr[11];
                            newRow["Category"] = LSTdataColStr[12];
                            newRow["Manufacturer"] = LSTdataColStr[13];
                            newRow["PartNo"] = LSTdataColStr[14];
                            newRow["Description"] = LSTdataColStr[15];
                            newRow["Qty"] = LSTdataColStr[16];
                            newRow["UnitCost"] = LSTdataColStr[17];
                            newRow["ExtCost"] = LSTdataColStr[18];
                            newRow["UnitPrice"] = LSTdataColStr[19];
                            newRow["ExtPrice"] = LSTdataColStr[20];
                            newRow["ChangeOrder"] = LSTdataColStr[21];
                            newRow["Column1"] = LSTdataColStr[22];
                            newRow["Column2"] = LSTdataColStr[23];
                            newRow["Column3"] = LSTdataColStr[24];
                            newRow["Column4"] = LSTdataColStr[25];
                            newRow["Column5"] = LSTdataColStr[26];
                            dtRef.Rows.InsertAt(newRow, selectedrowIndex3);
                            selectedrowIndex3 += 1;
                        }

                        /////////////////////////////
                        //gridControl3.DataSource = dtRef;
                        IsGridView3Changed = true;
                        break;
                }

            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }
        private void toolStripMenuSales_InsertEmptyRow_Click(object sender, EventArgs e)
        {
            int selectedrowIndex = dataGridView1.FocusedRowHandle;
            DataRow newRow = _dtSalesBOM.NewRow();
            _dtSalesBOM.Rows.InsertAt(newRow, selectedrowIndex);
        }
        private void toolStripMenuDesign_InsertEmptyRow_Click(object sender, EventArgs e)
        {
            int selectedrowIndex = dataGridView2.FocusedRowHandle;
            DataRow newRow = _dtDesignBOM.NewRow();
            _dtDesignBOM.Rows.InsertAt(newRow, selectedrowIndex);
        }
        private void toolStripMenuActual_InsertEmptyRow_Click(object sender, EventArgs e)
        {
            int selectedrowIndex = dataGridView3.FocusedRowHandle;
            DataRow newRow = _dtActualBOM.NewRow();
            _dtActualBOM.Rows.InsertAt(newRow, selectedrowIndex);
        }
        private void pasteMe_Click(object sender, EventArgs e)
        {
            ////////dxe////////////////////////////

            //string s = Clipboard.GetText();

            //string[] lines = s.Replace("ss\n", "").Split('\r');

            //dataGridView2.Rows.Add(lines.Length - 1);
            //string[] fields;
            //int row = 0;
            //int col = 0;

            //foreach (string item in lines)
            //{
            //    fields = item.Split('\t');
            //    foreach (string f in fields)
            //    {
            //        Console.WriteLine(f);
            //        dataGridView2[col, row].Value = f;
            //        col++;
            //    }
            //    row++;
            //    col = 0;
            //}

        }

        private void button1_Click(object sender, EventArgs e)
        {
            flowLayoutPanel1.Visible = false;
        }
        #region "Show Hide Columns"
        private void cbArea_CheckedChanged(object sender, EventArgs e)
        {
            if (cbArea.Checked == true)
            {
                Area1.Visible = true;
                Area2.Visible = true;
                Area3.Visible = true;
            }
            else
            {
                Area1.Visible = false;
                Area2.Visible = false;
                Area3.Visible = false;

            }
        }

        private void cbCategory_CheckedChanged(object sender, EventArgs e)
        {
            if (cbCategory.Checked == true)
            {
                Category1.Visible = true;
                Category2.Visible = true;
                Category3.Visible = true;
            }
            else
            {
                Category1.Visible = false;
                Category2.Visible = false;
                Category3.Visible = false;

            }
        }

        private void cbCategory1_CheckedChanged(object sender, EventArgs e)
        {
            if (cbCategory1.Checked == true)
            {
                Category1_1.Visible = true;
                Category1_2.Visible = true;
                Category1_3.Visible = true;
            }
            else
            {
                Category1_1.Visible = false;
                Category1_2.Visible = false;
                Category1_3.Visible = false;

            }
        }

        private void cbCategory2_CheckedChanged(object sender, EventArgs e)
        {
            if (cbCategory2.Checked == true)
            {
                Category2_1.Visible = true;
                Category2_2.Visible = true;
                Category2_3.Visible = true;
            }
            else
            {
                Category2_1.Visible = false;
                Category2_2.Visible = false;
                Category2_3.Visible = false;

            }
        }

        private void cbCategory3_CheckedChanged(object sender, EventArgs e)
        {
            if (cbCategory3.Checked == true)
            {
                Category3_1.Visible = true;
                Category3_2.Visible = true;
                Category3_3.Visible = true;
            }
            else
            {
                Category3_1.Visible = false;
                Category3_2.Visible = false;
                Category3_3.Visible = false;

            }
        }

        private void cbColumn1_CheckedChanged(object sender, EventArgs e)
        {
            if (cbColumn1.Checked == true)
            {
                Column1_1.Visible = true;
                Column1_2.Visible = true;
                Column1_3.Visible = true;
            }
            else
            {
                Column1_1.Visible = false;
                Column1_2.Visible = false;
                Column1_3.Visible = false;

            }
        }

        private void cbColumn2_CheckedChanged(object sender, EventArgs e)
        {
            if (cbColumn2.Checked == true)
            {
                Column2_1.Visible = true;
                Column2_2.Visible = true;
                Column2_3.Visible = true;
            }
            else
            {
                Column2_1.Visible = false;
                Column2_2.Visible = false;
                Column2_3.Visible = false;

            }
        }

        private void cbColumn3_CheckedChanged(object sender, EventArgs e)
        {
            if (cbColumn3.Checked == true)
            {
                Column3_1.Visible = true;
                Column3_2.Visible = true;
                Column3_3.Visible = true;
            }
            else
            {
                Column3_1.Visible = false;
                Column3_2.Visible = false;
                Column3_3.Visible = false;

            }
        }

        private void cbColumn4_CheckedChanged(object sender, EventArgs e)
        {
            if (cbColumn4.Checked == true)
            {
                Column4_1.Visible = true;
                Column4_2.Visible = true;
                Column4_3.Visible = true;
            }
            else
            {
                Column4_1.Visible = false;
                Column4_2.Visible = false;
                Column4_3.Visible = false;

            }
        }

        private void cbColumn5_CheckedChanged(object sender, EventArgs e)
        {
            if (cbColumn5.Checked == true)
            {
                Column5_1.Visible = true;
                Column5_2.Visible = true;
                Column5_3.Visible = true;
            }
            else
            {
                Column5_1.Visible = false;
                Column5_2.Visible = false;
                Column5_3.Visible = false;

            }
        }

        private void cbCostHead_CheckedChanged(object sender, EventArgs e)
        {
            if (cbCostHead.Checked == true)
            {
                CostHead1.Visible = true;
                CostHead2.Visible = true;
                CostHead3.Visible = true;
            }
            else
            {
                CostHead1.Visible = false;
                CostHead2.Visible = false;
                CostHead3.Visible = false;

            }
        }

        private void cbCostSubHead_CheckedChanged(object sender, EventArgs e)
        {
            if (cbCostSubHead.Checked == true)
            {
                CostSubHead1.Visible = true;
                CostSubHead2.Visible = true;
                CostSubHead3.Visible = true;
            }
            else
            {
                CostSubHead1.Visible = false;
                CostSubHead2.Visible = false;
                CostSubHead3.Visible = false;

            }
        }

        private void cbDescription_CheckedChanged(object sender, EventArgs e)
        {
            if (cbDescription.Checked == true)
            {
                Description1.Visible = true;
                Description2.Visible = true;
                Description3.Visible = true;
            }
            else
            {
                Description1.Visible = false;
                Description2.Visible = false;
                Description3.Visible = false;

            }
        }

        private void cbExtCost_CheckedChanged(object sender, EventArgs e)
        {
            if (cbExtCost.Checked == true)
            {
                ExtCost1.Visible = true;
                ExtCost2.Visible = true;
                ExtCost3.Visible = true;
            }
            else
            {
                ExtCost1.Visible = false;
                ExtCost2.Visible = false;
                ExtCost3.Visible = false;

            }
        }

        private void cbExtPrice_CheckedChanged(object sender, EventArgs e)
        {
            if (cbExtPrice.Checked == true)
            {
                ExtPrice1.Visible = true;
                ExtPrice2.Visible = true;
                ExtPrice3.Visible = true;
            }
            else
            {
                ExtPrice1.Visible = false;
                ExtPrice2.Visible = false;
                ExtPrice3.Visible = false;

            }
        }

        private void cbManufacturer_CheckedChanged(object sender, EventArgs e)
        {
            if (cbManufacturer.Checked == true)
            {
                Manufacturer1.Visible = true;
                Manufacturer2.Visible = true;
                Manufacturer3.Visible = true;
            }
            else
            {
                Manufacturer1.Visible = false;
                Manufacturer2.Visible = false;
                Manufacturer3.Visible = false;

            }
        }

        private void cbPanel_CheckedChanged(object sender, EventArgs e)
        {
            if (cbPanel.Checked == true)
            {
                Panel1.Visible = true;
                Panel2.Visible = true;
                Panel3.Visible = true;
            }
            else
            {
                Panel1.Visible = false;
                Panel2.Visible = false;
                Panel3.Visible = false;

            }
        }

        private void cbPartNo_CheckedChanged(object sender, EventArgs e)
        {
            if (cbPartNo.Checked == true)
            {
                PartNo1.Visible = true;
                PartNo2.Visible = true;
                PartNo3.Visible = true;
            }
            else
            {
                PartNo1.Visible = false;
                PartNo2.Visible = false;
                PartNo3.Visible = false;

            }
        }

        private void cbProduct_CheckedChanged(object sender, EventArgs e)
        {
            if (cbProduct.Checked == true)
            {
                Product1.Visible = true;
                Product2.Visible = true;
                Product3.Visible = true;
            }
            else
            {
                Product1.Visible = false;
                Product2.Visible = false;
                Product3.Visible = false;

            }
        }

        private void cbProductCategory_CheckedChanged(object sender, EventArgs e)
        {
            if (cbProductCategory.Checked == true)
            {
                ProductCategory1.Visible = true;
                ProductCategory2.Visible = true;
                ProductCategory3.Visible = true;
            }
            else
            {
                ProductCategory1.Visible = false;
                ProductCategory2.Visible = false;
                ProductCategory3.Visible = false;

            }
        }

        private void cbQty_CheckedChanged(object sender, EventArgs e)
        {
            if (cbQty.Checked == true)
            {
                Qty1.Visible = true;
                Qty2.Visible = true;
                Qty3.Visible = true;
            }
            else
            {
                Qty1.Visible = false;
                Qty2.Visible = false;
                Qty3.Visible = false;

            }
        }

        private void cbSORef_CheckedChanged(object sender, EventArgs e)
        {
            if (cbSORef.Checked == true)
            {
                SORef1.Visible = true;
                SORef2.Visible = true;
                SORef3.Visible = true;
            }
            else
            {
                SORef1.Visible = false;
                SORef2.Visible = false;
                SORef3.Visible = false;

            }
        }

        private void cbSr_CheckedChanged(object sender, EventArgs e)
        {
            if (cbSr.Checked == true)
            {
                Sr1.Visible = true;
                Sr2.Visible = true;
                Sr3.Visible = true;
            }
            else
            {
                Sr1.Visible = false;
                Sr2.Visible = false;
                Sr3.Visible = false;

            }
        }

        private void cbSystem_CheckedChanged(object sender, EventArgs e)
        {
            if (cbSystem.Checked == true)
            {
                System1.Visible = true;
                System2.Visible = true;
                System3.Visible = true;
            }
            else
            {
                System1.Visible = false;
                System2.Visible = false;
                System3.Visible = false;

            }
        }

        private void cbUnitCost_CheckedChanged(object sender, EventArgs e)
        {
            if (cbUnitCost.Checked == true)
            {
                UnitCost1.Visible = true;
                UnitCost2.Visible = true;
                UnitCost3.Visible = true;
            }
            else
            {
                UnitCost1.Visible = false;
                UnitCost2.Visible = false;
                UnitCost3.Visible = false;

            }
        }

        private void cbUnitPrice_CheckedChanged(object sender, EventArgs e)
        {
            if (cbUnitPrice.Checked == true)
            {
                UnitPrice1.Visible = true;
                UnitPrice2.Visible = true;
                UnitPrice3.Visible = true;
            }
            else
            {
                UnitPrice1.Visible = false;
                UnitPrice2.Visible = false;
                UnitPrice3.Visible = false;

            }
        }

        #endregion "Show Hide Columns"

        //private void dataGridView1_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        //{
        //    if (showDeleteConfirmation == true)
        //    {
        //        DialogResult dialogResult = MessageBox.Show("Do you want to delete row(s)?", "Confirmation", MessageBoxButtons.YesNo);
        //        if (dialogResult == DialogResult.No)
        //        {
        //            e.Cancel = true;
        //            answervalue = true;
        //            showDeleteConfirmation = false;

        //        }

        //    }
        //    else
        //    {
        //        e.Cancel = answervalue;
        //    }

        //}
        //bool answervalue = false;
        //bool showDeleteConfirmation = true;
        private void deleteRowSaleBOM_Click(object sender, EventArgs e)
        {
            DeleteRow();
        }
        private void deleteRowDesignBOM_Click(object sender, EventArgs e)
        {
            DeleteRow();
        }
        private void deleteRowActualBOM_Click(object sender, EventArgs e)
        {
            DeleteRow();
        }
        public void DeleteRow()
        {
            
            //showDeleteConfirmation = false;
            DialogResult dialogResult = MessageBox.Show("Do you want to delete row(s)?", "Confirmation", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No) return;

            if (tabControl1.SelectedTab == tabControl1.TabPages["tabSaleBOM"])
            {
                //dx//

                //foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                //{
                //    dataGridView1.Rows.RemoveAt(row.Index);
                //}


                dataGridView1.DeleteSelectedRows();
                GetSetExtCostTotal(ref _dtSalesBOM);
                IsGridView1Changed = true;
                _SalesBOM_UndoRedo_Idx += 1;
                _LstdtSalesBOM.Insert(_SalesBOM_UndoRedo_Idx,_dtSalesBOM.Copy());
            }
            if (tabControl1.SelectedTab == tabControl1.TabPages["tabDesignBOM"])
            {
                ////////dxe////////////////////////////
                //foreach (DataGridViewRow row in dataGridView2.SelectedRows)
                //{
                //    dataGridView2.Rows.RemoveAt(row.Index);
                //}
                dataGridView2.DeleteSelectedRows();
                GetSetExtCostTotal(ref _dtDesignBOM);
                IsGridView2Changed = true;
            }
            if (tabControl1.SelectedTab == tabControl1.TabPages["tabActualBOM"])
            {
                ////////dxe////////////////////////////
                //foreach (DataGridViewRow row in dataGridView3.SelectedRows)
                //{
                //    dataGridView3.Rows.RemoveAt(row.Index);
                //}
                dataGridView3.DeleteSelectedRows();
                GetSetExtCostTotal(ref _dtActualBOM);
                IsGridView3Changed = true;
            }

        }
        private void itemDeleteRow_Click(object sender, EventArgs e)
        {
            //showDeleteConfirmation = false;
            DialogResult dialogResult = MessageBox.Show("Do you want to delete row(s)?", "Confirmation", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No) return;

            if (tabControl1.SelectedTab == tabControl1.TabPages["tabSaleBOM"])
            {
                //dx//

                //foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                //{
                //    dataGridView1.Rows.RemoveAt(row.Index);
                //}
                IsGridView1Changed = true;
            }
            if (tabControl1.SelectedTab == tabControl1.TabPages["tabDesignBOM"])
            {
                ////////dxe////////////////////////////
                //foreach (DataGridViewRow row in dataGridView2.SelectedRows)
                //{
                //    dataGridView2.Rows.RemoveAt(row.Index);
                //}
                IsGridView1Changed = true;
            }
            if (tabControl1.SelectedTab == tabControl1.TabPages["tabActualBOM"])
            {
                ////////dxe////////////////////////////
                //foreach (DataGridViewRow row in dataGridView3.SelectedRows)
                //{
                //    dataGridView3.Rows.RemoveAt(row.Index);
                //}
                IsGridView1Changed = true;
            }

            //showDeleteConfirmation = true;
        }



        //private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        //{
        //    if (e.RowIndex > -1)
        //    {
        //        IsGridView1Changed = true;
        //    }


        //}

        //private void dataGridView2_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        //{
        //    if (e.RowIndex > -1)
        //    {
        //        IsGridView2Changed = true;
        //    }

        //}

        //private void dataGridView3_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        //{
        //    if (e.RowIndex > -1)
        //    {
        //        IsGridView3Changed = true;
        //    }

        //}
        private void LoadExcelOldMethod()
        {
            try
            {
                OpenFileDialog dlg_im = new OpenFileDialog();
                dlg_im.Filter = "Excel File|*.xls;*.xlsx;*.xlsm";
                //dlg_im.Filter = "Excel File|*.xlsx";

                if (dlg_im.ShowDialog() == DialogResult.OK)
                {
                    //dataGridView1.Rows.Clear();
                    txtBOMFilePath.Text = dlg_im.FileName;


                    string constr = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + txtBOMFilePath.Text + ";Extended Properties='Excel 12.0 XML;HDR=YES;';";

                    OleDbConnection Con = new OleDbConnection(constr);

                    Con.Open();

                    // Get the name of the first worksheet:
                    DataTable dbSchema = Con.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                    if (dbSchema == null || dbSchema.Rows.Count < 1)
                    {
                        throw new Exception("Error: Could not determine the name of the first worksheet.");
                    }

                    //foreach (DataRow item in dbSchema.Rows)
                    //{
                    //    listBoxWorkSheets.Items.Add(item["TABLE_NAME"].ToString());
                    //}

                    //return;

                    string firstSheetName = dbSchema.Rows[0]["TABLE_NAME"].ToString();

                    //firstSheetName = "BOM$";

                    OleDbCommand cmd = new OleDbCommand("SELECT * FROM [" + firstSheetName + "]", Con);



                    OleDbDataAdapter sda = new OleDbDataAdapter(cmd);
                    DataTable dtTemp = new DataTable();

                    sda.Fill(dtTemp);
                    Con.Close();

                    //DataRow newDataRow;
                    //////////////////////////////////////////////////////////////
                    //if (!(tabControl1.SelectedTab == tabControl1.TabPages["tabSaleBOM"]))
                    //if (tabControl1.SelectedTab == tabControl1.TabPages["tabDesignBOM"])
                    //{
                    //    DataColumn Col = dtTemp.Columns.Add("Select2", System.Type.GetType("System.Boolean"));
                    //    //Col.SetOrdinal(0);// to put the column in position 0;

                    //}
                    DataTable dtBOM = new DataTable("dtBOM");
                    //dtBOM = dtTemp.Clone();
                    dtBOM.Columns.Add(_columnNames[0], typeof(string));
                    dtBOM.Columns.Add(_columnNames[1], typeof(string));
                    dtBOM.Columns.Add(_columnNames[2], typeof(string));
                    dtBOM.Columns.Add(_columnNames[3], typeof(string));
                    dtBOM.Columns.Add(_columnNames[4], typeof(decimal));
                    dtBOM.Columns.Add(_columnNames[5], typeof(string));
                    dtBOM.Columns.Add(_columnNames[6], typeof(string));
                    dtBOM.Columns.Add(_columnNames[7], typeof(string));
                    dtBOM.Columns.Add(_columnNames[8], typeof(string));
                    dtBOM.Columns.Add(_columnNames[9], typeof(string));
                    dtBOM.Columns.Add(_columnNames[10], typeof(string));
                    dtBOM.Columns.Add(_columnNames[11], typeof(string));
                    dtBOM.Columns.Add(_columnNames[12], typeof(string));
                    dtBOM.Columns.Add(_columnNames[13], typeof(string));
                    dtBOM.Columns.Add(_columnNames[14], typeof(string));
                    dtBOM.Columns.Add(_columnNames[15], typeof(string));
                    dtBOM.Columns.Add(_columnNames[16], typeof(decimal));
                    dtBOM.Columns.Add(_columnNames[17], typeof(decimal));
                    dtBOM.Columns.Add(_columnNames[18], typeof(decimal));
                    dtBOM.Columns.Add(_columnNames[19], typeof(decimal));
                    dtBOM.Columns.Add(_columnNames[20], typeof(decimal));
                    dtBOM.Columns.Add(_columnNames[21], typeof(string));
                    dtBOM.Columns.Add(_columnNames[22], typeof(string));
                    dtBOM.Columns.Add(_columnNames[23], typeof(string));
                    dtBOM.Columns.Add(_columnNames[24], typeof(string));
                    dtBOM.Columns.Add(_columnNames[25], typeof(string));
                    dtBOM.Columns.Add(_columnNames[26], typeof(string));
                    //dx//dataGridView1.AutoGenerateColumns = false;

                    //}
                    //for (int j = 0; j < dtBOM.Columns.Count; j++)
                    //{
                    //    MessageBox.Show(dtBOM.Columns[j].ColumnName +"----" + dtBOM.Columns[j].DataType.Name.ToString());
                    //}
                    foreach (DataRow dr in dtTemp.Rows)
                    {
                        //string colName=gvr.Cells[0].OwningColumn.HeaderText;

                        bool isAdd = false;
                        for (int i = 0; i < dtTemp.Columns.Count; i++)
                        {
                            //if (dr[i] == null || dr[i] == DBNull.Value || String.IsNullOrWhiteSpace(dr[i].ToString()))
                            if (dr[i] == DBNull.Value)
                            {
                                isAdd = false;
                            }
                            else
                            {
                                isAdd = true;
                                break;
                            }
                        }

                        if (isAdd == true)
                        {
                            //can not add like this dtBOM.Rows.Add(dr); :(  have to add new row and then add to list
                            //newDataRow = dtBOM.NewRow();
                            //for (int i = 0; i <  dtTemp.Columns.Count; i++)
                            //{
                            //    newDataRow[i] = dr[i];

                            //}
                            //dtBOM.Rows.Add(newDataRow);

                            dtBOM.Rows.Add(dr.ItemArray);
                        }

                    }
                    if (tabControl1.SelectedTab == tabControl1.TabPages["tabSaleBOM"])
                    {
                        _dtSalesBOM = dtBOM;
                        gridControl1.DataSource = _dtSalesBOM;
                    }
                    if (tabControl1.SelectedTab == tabControl1.TabPages["tabDesignBOM"])
                    {
                        _dtDesignBOM = dtBOM;
                        gridControl2.DataSource = _dtDesignBOM;
                    }
                    if (tabControl1.SelectedTab == tabControl1.TabPages["tabActualBOM"])
                    {
                        _dtActualBOM = dtBOM;
                        gridControl3.DataSource = _dtActualBOM;
                    }




                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private void dataGridView1_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            //if (e.Button == MouseButtons.Right && e.RowIndex > -1 && e.ColumnIndex > -1)
            //{
            //    // Add this
            //    dataGridView1.CurrentCell = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];
            //    // Can leave these here - doesn't hurt
            //    //dataGridView1.Rows[e.RowIndex].Selected = true;//if we uncomment this. then it will select full row and can not get cell column index. which is necessory for paste at place
            //    dataGridView1.Focus();
            //}
        }
        private void dataGridView2_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            //if (e.Button == MouseButtons.Right && e.RowIndex > -1 && e.ColumnIndex > -1)
            //{
            //    // Add this
            //    dataGridView2.CurrentCell = dataGridView2.Rows[e.RowIndex].Cells[e.ColumnIndex];
            //    // Can leave these here - doesn't hurt
            //    //dataGridView2.Rows[e.RowIndex].Selected = true;//if we uncomment this. then it will select full row and can not get cell column index. which is necessory for paste at place
            //    dataGridView2.Focus();
            //}
        }

        private void dataGridView3_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            //if (e.Button == MouseButtons.Right && e.RowIndex > -1 && e.ColumnIndex > -1)
            //{
            //    // Add this
            //    dataGridView3.CurrentCell = dataGridView3.Rows[e.RowIndex].Cells[e.ColumnIndex];
            //    // Can leave these here - doesn't hurt
            //    //dataGridView3.Rows[e.RowIndex].Selected = true;//if we uncomment this. then it will select full row and can not get cell column index. which is necessory for paste at place
            //    dataGridView3.Focus();
            //}
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //GetSummary();
            //List<DataTable> LstdataTables = new List<DataTable>();
            //for (int i=1; i<200; i++)
            //{
            //    LstdataTables.Add(_dtDesignBOM.Copy());

            //}
            //MessageBox.Show("200");
            //undo
            
            
            if (_SalesBOM_UndoRedo_Idx == 0) return;
            _SalesBOM_UndoRedo_Idx -= 1;
            gridControl1.DataSource = _LstdtSalesBOM[_SalesBOM_UndoRedo_Idx].Copy();
            //_LstdtSalesBOM.RemoveAt(_SalesBOM_UndoRedo_Idx);

        }
        private void button3_Click(object sender, EventArgs e)
        {
            //redo
            if (_SalesBOM_UndoRedo_Idx == _LstdtSalesBOM.Count - 1) return;
            _SalesBOM_UndoRedo_Idx += 1;
            gridControl1.DataSource = _LstdtSalesBOM[_SalesBOM_UndoRedo_Idx].Copy();
            //_LstdtSalesBOM.RemoveAt(_SalesBOM_UndoRedo_Idx);
        }
        private void tabSummary_Click(object sender, EventArgs e)
        {
            //GetSummary();
        }

        private void tabSummary_Enter(object sender, EventArgs e)
        {
            GetSummary();
        }
        DataTable _exportDT;
        SaveFileDialog _savefile;
        private void btnExportXLS_Click(object sender, EventArgs e)
        {
            //ExportXLS_Method1(); export single datatable only;
            ExportXLS();

        }
        
        public void ExportXLS()
        {
            _savefile = new SaveFileDialog();
            // set a default file name
            string datetime = DateTime.Now.ToString("yyyyMMddHHmmss");
            datetime = datetime.Replace(":", "");

            _savefile.FileName = "Control Sheet" + " " + datetime + ".xlsx";
            // set filters - this can be done in properties as well
            _savefile.Filter = "Excel Workbook (*.xlsx)|*.xlsx|All files (*.*)|*.*";

            if (_savefile.ShowDialog() == DialogResult.OK)
            {
                tabControl1.SelectedTab  = tabControl1.TabPages["tabSummary"];
                XLWorkbook wb = new XLWorkbook();

                wb.Worksheets.Add(_dtSalesBOM, "Bid");
                wb.Worksheets.Add(_dtDesignBOM, "Planned");
                wb.Worksheets.Add(_dtActualBOM, "Actual");
                wb.Worksheets.Add(_dtSummary, "Summary");
                //wb.SaveAs(@"C:\test\control sheet.xlsx");
                wb.SaveAs(_savefile.FileName);

                MessageBox.Show("Exported successfully");
            }
        }
        private void ExportXLS_Method_old()
        {

            progressBar1.Visible = true;
            progressBar1.Step = 1;
            progressBar1.Value = 0;

            _exportDT = new DataTable();
            if (tabControl1.SelectedTab == tabControl1.TabPages["tabSaleBOM"])
            {
                _exportDT = _dtSalesBOM;
            }
            if (tabControl1.SelectedTab == tabControl1.TabPages["tabDesignBOM"])
            {
                _exportDT = _dtDesignBOM;
            }
            if (tabControl1.SelectedTab == tabControl1.TabPages["tabActualBOM"])
            {
                _exportDT = _dtActualBOM;
            }
            if (tabControl1.SelectedTab == tabControl1.TabPages["tabSummary"])
            {
                _exportDT = _dtSummary;
            }

            progressBar1.Maximum = _exportDT.Rows.Count - 1;

            //--------------
            _savefile = new SaveFileDialog();
            // set a default file name
            string datetime = DateTime.Now.ToString("yyyyMMddHHmmss");
            datetime = datetime.Replace(":", "");

            _savefile.FileName = _exportDT.TableName + " " + datetime + ".xlsx";
            // set filters - this can be done in properties as well
            _savefile.Filter = "Excel Workbook (*.xlsx)|*.xlsx|All files (*.*)|*.*";

            if (_savefile.ShowDialog() == DialogResult.OK)
            {
                backgroundWorker1.RunWorkerAsync();
            }

        }
        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            //var backgroundWorker = sender as BackgroundWorker;
            ExportExl_BkGroundWorker();
        }
        private void ExportExl_BkGroundWorker()
        {
                        
                ///////////////export to excel/////////////////
                // Here is main process
                Microsoft.Office.Interop.Excel.Application objexcelapp = new Microsoft.Office.Interop.Excel.Application();
                objexcelapp.Application.Workbooks.Add(Type.Missing);
                objexcelapp.Columns.AutoFit();


                for (int i = 1; i < _exportDT.Columns.Count + 1; i++)
                {
                    Microsoft.Office.Interop.Excel.Range xlRange = (Microsoft.Office.Interop.Excel.Range)objexcelapp.Cells[1, i];
                    xlRange.Font.Bold = -1;
                    xlRange.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    xlRange.Borders.Weight = 1d;
                    xlRange.HorizontalAlignment = Microsoft.Office.Interop.Excel.XlHAlign.xlHAlignCenter;
                    objexcelapp.Cells[1, i] = _exportDT.Columns[i - 1].Caption;

                }
                //progressBar1.Maximum = exportDT.Rows.Count;
                /*For storing Each row and column value to excel sheet*/
                for (int i = 0; i < _exportDT.Rows.Count; i++)
                {
                    backgroundWorker1.ReportProgress(i);
                    for (int j = 0; j < _exportDT.Columns.Count; j++)
                    {
                        //if (exportDT.Rows[i][j] != null)
                        if (!string.IsNullOrEmpty(_exportDT.Rows[i][j].ToString()))
                        {
                            Microsoft.Office.Interop.Excel.Range xlRange = (Microsoft.Office.Interop.Excel.Range)objexcelapp.Cells[i + 2, j + 1];
                            xlRange.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                            xlRange.Borders.Weight = 1d;
                            objexcelapp.Cells[i + 2, j + 1] = _exportDT.Rows[i][j].ToString();
                        }

                    }
                }
                objexcelapp.Columns.AutoFit(); // Auto fix the columns size
                //System.Windows.Forms.Application.DoEvents();
                
                //if (Directory.Exists("C:\\CTR_Data\\")) // Folder dic
                //{
                //    objexcelapp.ActiveWorkbook.SaveCopyAs("C:\\CTR_Data\\" + "excelFilename" + ".xlsx");
                //}
                //else
                //{
                //    Directory.CreateDirectory("C:\\CTR_Data\\");
                //    objexcelapp.ActiveWorkbook.SaveCopyAs("C:\\CTR_Data\\" + "excelFilename" + ".xlsx");
                //}

                objexcelapp.ActiveWorkbook.SaveCopyAs(_savefile.FileName);
                objexcelapp.ActiveWorkbook.Saved = true;
                //System.Windows.Forms.Application.DoEvents();
            MessageBox.Show("Exported successfully");

        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        
    }
}

//     DataTable dtSummary = _dtSalesBOM.AsEnumerable()
//.GroupBy(r => new { Col1 = r["ProductCategory"] })
//.Select(g => g.OrderBy(r => r["ProductCategory"]).First())
//.CopyToDataTable();

//var grouped = _dtSalesBOM.AsEnumerable().GroupBy(d => new
//{
//    block = d.Field<string>("Block"),
//    transplant = d.Field<string>("Transplant"),
//    variety = d.Field<string>("Variety")
//})
//       .Select(x => new {
//           Block = x.Key.block,
//           Transplant = x.Key.transplant,
//           Variety = x.Key.variety,
//      //replace ItemArray Index with appropriate values in your code
//      Q1 = x.Sum(y => int.Parse(y.ItemArray[3].ToString())),
//           Q2 = x.Sum(y => int.Parse(y.ItemArray[4].ToString())),
//       });
//grouped.Dump();




//var result = _dtSalesBOM.AsEnumerable()
//.GroupBy(row => new
//{
//    Value = row.Field<string>("ProductCategory")/*,*/
//    //Description = row.Field<string>("Description")
//})
//.Select(g =>
//{
//    var row = g.First();
//    row.SetField("ExtCost", g.Sum(r => Convert.ToDouble(string.IsNullOrEmpty(r.Field<string>("ExtCost")) ? "0" : r.Field<string>("ExtCost"))));
//    return row;

//    //new
//    //{
//    //    Block = x.Key.Value,

//    //    //replace ItemArray Index with appropriate values in your code
//    //    Q1 = x.Sum(y => int.Parse(y.ItemArray[18].ToString()))

//});
//DataTable resultTable = result.CopyToDataTable();



// var dt = _dtSalesBOM.AsEnumerable()
//.GroupBy(r => new { Col1 = r["ProductCategory"] })
//.Select(g => g.Sum(r => Convert.ToDouble(string.IsNullOrEmpty(r.Field<string>("ExtCost")) ? "0" : r.Field<string>("ExtCost"))));

using Repository.DAL;
using StaticClasses;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Procurement.Controllers
{
    public class ProjectEmployeeDetailController
    {
        private _IProcurementRepository<ProjectEmployeeDetail> interfaceObj;
        private ProjectEmployeeDetail _gPedModel;
        private List<ProjectEmployeeDetail> _gLstPedModelRunning;
        public ProjectEmployeeDetailController()
        {
            interfaceObj = new ProcurementRepository<ProjectEmployeeDetail>();

        }
        public ProjectEmployeeDetailController(ProjectEmployeeDetail _pPedModel)
        {
            interfaceObj = new ProcurementRepository<ProjectEmployeeDetail>();
            _gPedModel = _pPedModel;
        }
        public ProjectEmployeeDetailController(List<ProjectEmployeeDetail> pLstPedModel)
        {
            interfaceObj = new ProcurementRepository<ProjectEmployeeDetail>();
            _gLstPedModelRunning = pLstPedModel;
        }
        //public Object Create()
        //{
        //    FrmProject frmProject = new FrmProject();
        //    return frmProject;
        //}
        public void Save()
        {
            interfaceObj.InsertModel(_gPedModel);
            interfaceObj.Save();
        }
        public void SaveList(decimal pEmployeeCode)
            {
            List<ProjectEmployeeDetail> LstDbPed = GetModels(pEmployeeCode); //.Products.where(x => x.StoreId == store.StoreId)
                                                                             //if (LstDbPed.Count > 0)
                                                                             //{
                                                                             //    foreach (ProjectEmployeeDetail pedDb in LstDbPed)
                                                                             //    {
                                                                             //        ProjectEmployeeDetail projectEmployeeDetail = _gLstPedModel.FirstOrDefault(x => x.EmployeeCode == pedDb.EmployeeCode && x.ProjectCode == pedDb.ProjectCode);
                                                                             //        if (projectEmployeeDetail != null && projectEmployeeDetail.IsSelected == false) interfaceObj.DeleteModel(pedDb.Id);
                                                                             //    }

            //}

            //foreach (ProjectEmployeeDetail pedModel in _gLstPedModel)
            //{


            //    interfaceObj.InsertModel(pedModel);
            //}

            //--------------------------
            ProjectController projectController = new ProjectController();
            List<Project> LstProjectsOwnedbyLoginedEmpDb;

            if (LoginInfo.LoginEmployee.EmployeeTypeCode == Constants.ADMIN)
            {
                //get all
                LstProjectsOwnedbyLoginedEmpDb = projectController.GetModels();
            }
            else
            {
                //get only logined manager
                LstProjectsOwnedbyLoginedEmpDb = projectController.GetModelsByCreatedByLoginedEmp();
            }




            //bool Found = false;
            foreach (Project prjDb in LstProjectsOwnedbyLoginedEmpDb)
            {
                ProjectEmployeeDetail pedRunningObj = _gLstPedModelRunning.FirstOrDefault(x => x.ProjectCode == prjDb.ProjectCode && x.EmployeeCode == pEmployeeCode);
                if (pedRunningObj == null)
                {
                    ProjectEmployeeDetail pedDb = LstDbPed.FirstOrDefault(x => x.ProjectCode == prjDb.ProjectCode && x.EmployeeCode == pEmployeeCode);
                    if (pedDb != null)
                    {
                        //delete
                        interfaceObj.DeleteModel(pedDb.Id);
                    }
                }
                else
                {

                    ProjectEmployeeDetail pedDb = LstDbPed.FirstOrDefault(x => x.ProjectCode == pedRunningObj.ProjectCode && x.EmployeeCode == pEmployeeCode);
                    if (pedDb == null)
                    {
                        //insert    
                        interfaceObj.InsertModel(pedRunningObj);
                    }

                }
            }

            interfaceObj.Save();

        }

        public List<ProjectEmployeeDetail> GetModels(decimal pEmployeeCode)
        {
            return interfaceObj.GetModels().Where(x => x.EmployeeCode == pEmployeeCode).AsQueryable().ToList<ProjectEmployeeDetail>();
        }
        //public ProjectEmployeeDetail GetModelByID(decimal projectCode)
        //{
        //    return interfaceObj.GetModelByID(projectCode);
        //}
    }
}

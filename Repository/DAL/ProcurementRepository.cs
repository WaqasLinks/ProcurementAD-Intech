﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.DAL
{
    public class ProcurementRepository<T> : _IProcurementRepository<T> where T : class
    {
        ProcurementEntities _context;
        private IDbSet<T> dbEntity;

        public ProcurementRepository()
        {
            _context = new ProcurementEntities();
            _context.Configuration.ProxyCreationEnabled = true;
            _context.Configuration.LazyLoadingEnabled = true;

            dbEntity = _context.Set<T>();
        }
        public void DeleteModel(Object modelID)
        {
            T model = dbEntity.Find(modelID);
            dbEntity.Remove(model);
        }

        public IEnumerable<T> GetModels()
        {
            return dbEntity.ToList();
        }

        //public IEnumerable<T> GetModelsByID(decimal modelID)
        //{
        //    return dbEntity.AsQueryable().Where(x=>x.).ToList();
        //}
        public T GetModelByID(Object modelId)
        {
            return dbEntity.Find(modelId);
        }
        //public List<T> GetBOMModelsByProjID(decimal modelId)
        //{
        //    return dbEntity.ToList();
             
        //}

        public void InsertModel(T model)
        {
            dbEntity.Add(model);
            //_context.BOMs.Add(model);
        }

        public void Save()
        {
            _context.SaveChanges();
        }

        public void UpdateModel(T model)
        {
            _context.Entry(model).State = System.Data.Entity.EntityState.Modified;
        }
        public void ReseedPK(string TableName)
        {
            _context.Database.ExecuteSqlCommand("DBCC CHECKIDENT('" + TableName +  "', RESEED, 0)");
        }
    }
}
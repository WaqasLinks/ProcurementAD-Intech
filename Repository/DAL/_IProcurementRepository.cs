﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.DAL
{
    public interface _IProcurementRepository<T> where T : class
    {
        IEnumerable<T> GetModels();
        T GetModelByID(Object modelId);
        void InsertModel(T model);
        void DeleteModel(Object modelID);
        void UpdateModel(T model);
        void Save();
        void ReseedPK(String TableName);
    }
}

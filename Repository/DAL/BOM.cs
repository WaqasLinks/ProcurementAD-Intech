//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Repository.DAL
{
    using System;
    using System.Collections.Generic;
    
    public partial class BOM
    {
        public decimal RowAuto { get; set; }
        public string ProjectCode { get; set; }
        public Nullable<short> BOMTypeCode { get; set; }
        public string Category1 { get; set; }
        public string Category2 { get; set; }
        public string Category3 { get; set; }
        public string SORef { get; set; }
        public Nullable<decimal> Sr { get; set; }
        public string ProductCategory { get; set; }
        public string Product { get; set; }
        public string CostHead { get; set; }
        public string CostSubHead { get; set; }
        public string System { get; set; }
        public string Area { get; set; }
        public string Panel { get; set; }
        public string Category { get; set; }
        public string Manufacturer { get; set; }
        public string PartNo { get; set; }
        public string Description { get; set; }
        public Nullable<decimal> Qty { get; set; }
        public Nullable<decimal> UnitCost { get; set; }
        public Nullable<decimal> ExtCost { get; set; }
        public Nullable<decimal> UnitPrice { get; set; }
        public Nullable<decimal> ExtPrice { get; set; }
        public string ChangeOrder { get; set; }
        public string Column1 { get; set; }
        public string Column2 { get; set; }
        public string Column3 { get; set; }
        public string Column4 { get; set; }
        public string Column5 { get; set; }
    
        public virtual BOMType BOMType { get; set; }
        public virtual Project Project { get; set; }
    }
}

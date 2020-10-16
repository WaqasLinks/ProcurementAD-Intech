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
    
    public partial class Employee
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Employee()
        {
            this.ProjectEmployeeDetails = new HashSet<ProjectEmployeeDetail>();
        }
    
        public decimal EmployeeCode { get; set; }
        public Nullable<decimal> EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string Password { get; set; }
        public string SAM { get; set; }
        public string Email { get; set; }
        public Nullable<bool> Active { get; set; }
        public Nullable<short> EmployeeTypeCode { get; set; }
        public Nullable<decimal> Manager { get; set; }
        public Nullable<decimal> ProjectCode { get; set; }
        public Nullable<decimal> CreatedBy { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public Nullable<decimal> UpdatedBy { get; set; }
        public Nullable<System.DateTime> UpdateDate { get; set; }
    
        public virtual EmployeeType EmployeeType { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ProjectEmployeeDetail> ProjectEmployeeDetails { get; set; }
    }
}

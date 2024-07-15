//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace POS.APP_Data
{
    using System;
    using System.Collections.Generic;
    
    public partial class ConsignmentSettlement
    {
        public int Id { get; set; }
        public System.DateTime SettlementDate { get; set; }
        public int ConsignorId { get; set; }
        public Nullable<decimal> TotalSettlementPrice { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public Nullable<int> CreatedBy { get; set; }
        public bool IsDelete { get; set; }
        public string ConsignmentNo { get; set; }
        public System.DateTime FromTransactionDate { get; set; }
        public System.DateTime ToTransactionDate { get; set; }
        public string Comment { get; set; }
        public Nullable<int> count { get; set; }
    
        public virtual ConsignmentSettlement ConsignmentSettlement1 { get; set; }
        public virtual ConsignmentSettlement ConsignmentSettlement2 { get; set; }
    }
}

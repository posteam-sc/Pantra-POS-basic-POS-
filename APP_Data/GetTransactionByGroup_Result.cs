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
    
    public partial class GetTransactionByGroup_Result
    {
        public string Id { get; set; }
        public Nullable<System.DateTime> DateTime { get; set; }
        public Nullable<int> UserId { get; set; }
        public Nullable<int> CounterId { get; set; }
        public string Type { get; set; }
        public Nullable<bool> IsPaid { get; set; }
        public Nullable<bool> IsComplete { get; set; }
        public Nullable<bool> IsActive { get; set; }
        public Nullable<bool> IsDeleted { get; set; }
        public Nullable<int> PaymentTypeId { get; set; }
        public Nullable<int> TaxAmount { get; set; }
        public Nullable<int> DiscountAmount { get; set; }
        public Nullable<long> TotalAmount { get; set; }
        public Nullable<long> RecieveAmount { get; set; }
        public string ParentId { get; set; }
        public Nullable<int> GiftCardId { get; set; }
        public Nullable<int> CustomerId { get; set; }
        public Nullable<decimal> MCDiscountAmt { get; set; }
        public Nullable<decimal> BDDiscountAmt { get; set; }
        public Nullable<int> MemberTypeId { get; set; }
        public Nullable<decimal> MCDiscountPercent { get; set; }
        public Nullable<int> ReceivedCurrencyId { get; set; }
        public Nullable<bool> IsSettlement { get; set; }
        public string TranVouNos { get; set; }
        public Nullable<bool> IsWholeSale { get; set; }
        public Nullable<decimal> GiftCardAmount { get; set; }
        public Nullable<int> ShopId { get; set; }
        public Nullable<System.DateTime> UpdatedDate { get; set; }
        public string Note { get; set; }
        public Nullable<int> TableIdOrQue { get; set; }
        public Nullable<int> ServiceFee { get; set; }
    }
}

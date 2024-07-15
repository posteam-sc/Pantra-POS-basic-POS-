﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Reporting.WinForms;
using POS.APP_Data;

namespace POS
{
    public partial class PaidByFOC : Form
    {
        #region Variables

        public List<TransactionDetail> DetailList = new List<TransactionDetail>();

        public int Discount { get; set; }

        public int Tax { get; set; }

        public int ExtraDiscount { get; set; }

        public int ExtraTax { get; set; }

        public Boolean isDraft { get; set; }        

        public string DraftId { get; set; }

        public Boolean IsWholeSale { get; set; }

        public int Type { get; set; }
        
        private POSEntities entity = new POSEntities();

        public decimal BDDiscount { get; set; }

        public decimal MCDiscount { get; set; }

        public int CustomerId { get; set; }

        public int? MemberTypeId { get; set; }

        public decimal? MCDiscountPercent { get; set; }

        int Qty = 0;

        List<Stock_Transaction> productList = new List<Stock_Transaction>();
        public Boolean IsPrint = false;
        public string Note = "";
        public int tableId = 0;
        public int servicefee = 0;
        public long GiftDiscountAmt { get; set; }
        public List<GiftSystem> GiftList = new List<GiftSystem>();
        #endregion
        public PaidByFOC()
        {
            InitializeComponent();
        }

        private void PaidByFOC_Load(object sender, EventArgs e)
        {
            long totalCost = (long)DetailList.Sum(x => x.TotalAmount)  - ExtraDiscount - (long)BDDiscount - (long)MCDiscount-GiftDiscountAmt;
            long unitpriceTotalCost = (long)DetailList.Sum(x => x.UnitPrice * x.Qty);//Edit ZMH


            //System.Data.Objects.ObjectResult<String> Id = entity.InsertTransaction(DateTime.Now, MemberShip.UserId, MemberShip.CounterId, TransactionType.Sale, true, true, Type, ExtraTax + Tax, ExtraDiscount + Discount, totalCost, 0, null, CustomerId,MCDiscount,BDDiscount,MemberTypeId,MCDiscountPercent,false,"",IsWholeSale);
            System.Data.Objects.ObjectResult<String> Id = entity.InsertTransaction(DateTime.Now, MemberShip.UserId, MemberShip.CounterId, TransactionType.Sale, true, true, Type, ExtraTax + Tax, 0, totalCost, 0, null, CustomerId, MCDiscount, BDDiscount, MemberTypeId, MCDiscountPercent, false, "", IsWholeSale, 0, SettingController.DefaultShop.Id, SettingController.DefaultShop.ShortCode,Note,tableId,servicefee,null);
            entity = new POSEntities();
            string resultId = Id.FirstOrDefault().ToString();
            Transaction insertedTransaction = (from trans in entity.Transactions where trans.Id == resultId select trans).FirstOrDefault<Transaction>();
            foreach (TransactionDetail detail in DetailList)
            {
                detail.Product = (from prod in entity.Products where prod.Id == (long)detail.ProductId select prod).FirstOrDefault();
                detail.Product.Qty = detail.Product.Qty - detail.Qty;
                detail.IsFOC = true;
                detail.UnitPrice = 0;
                detail.TotalAmount = 0;
                detail.DiscountRate = 0;
                detail.TaxRate = 0;
                //save in stocktransaction
             
                Stock_Transaction st = new Stock_Transaction();
                st.ProductId = detail.Product.Id;
                Qty = Convert.ToInt32(detail.Qty);
                st.Sale = Qty;
                productList.Add(st);
                Qty = 0;


                if (detail.ConsignmentPrice == null)
                {
                    detail.ConsignmentPrice = 0;
                }


                detail.IsDeleted = false;
                Boolean? IsConsignmentPaid = Utility.IsConsignmentPaid(detail.Product);
                var detailID = entity.InsertTransactionDetail(insertedTransaction.Id, Convert.ToInt32(detail.ProductId), Convert.ToInt32(detail.Qty), Convert.ToInt32(detail.UnitPrice), Convert.ToDouble(detail.DiscountRate), Convert.ToDouble(detail.TaxRate), Convert.ToInt32(detail.TotalAmount), detail.IsDeleted, detail.ConsignmentPrice, IsConsignmentPaid, detail.IsFOC, Convert.ToInt32(detail.SellingPrice)).SingleOrDefault();
                if (detail.Product.IsWrapper == true)
                {
                    List<WrapperItem> wList = detail.Product.WrapperItems.ToList();
                    if (wList.Count > 0)
                    {
                        foreach (WrapperItem w in wList)
                        {
                            Product wpObj = (from p in entity.Products where p.Id == w.ChildProductId select p).FirstOrDefault();
                            wpObj.Qty = wpObj.Qty - (w.ChildQty * detail.Qty);


                            SPDetail spDetail = new SPDetail();
                            spDetail.TransactionDetailID = Convert.ToInt32(detailID);
                            spDetail.DiscountRate = detail.DiscountRate;
                            spDetail.ParentProductID = w.ParentProductId;
                            spDetail.ChildProductID = w.ChildProductId;
                            spDetail.ChildQty = w.ChildQty * detail.Qty;
                            spDetail.Price = wpObj.Price;
                            entity.insertSPDetail(spDetail.TransactionDetailID, spDetail.ParentProductID, spDetail.ChildProductID, spDetail.Price, spDetail.DiscountRate, "PC", spDetail.ChildQty);
                            //entity.SPDetails.Add(spDetail);

                            Stock_Transaction stwp = new Stock_Transaction();
                            stwp.ProductId = w.ChildProductId;
                            Qty = Convert.ToInt32(w.ChildQty * detail.Qty);
                            stwp.Sale = Qty;
                            productList.Add(stwp);


                            Qty = 0;
                        }
                    }
                }
               
               // insertedTransaction.TransactionDetails.Add(detail);
            }
            //Add promotion gift records for this transaction
            if (GiftList.Count > 0) {
                foreach (GiftSystem gs in GiftList) {
                    GiftSystemInTransaction gft = new GiftSystemInTransaction();
                    gft.GiftSystemId = gs.Id;
                    gft.TransactionId = insertedTransaction.Id;
                    entity.GiftSystemInTransactions.Add(gft);
                    }
                }
            //saving to db gift item lists
            entity.SaveChanges();
            //save in stock transaction
            Save_SaleQty_ToStockTransaction(productList);
            productList.Clear();

            insertedTransaction.IsDeleted = false;
            entity.SaveChanges();

            #region purchase
            // for Purchase Detail and PurchaseDetailInTransacton.

            foreach (TransactionDetail detail in insertedTransaction.TransactionDetails)
            {


                int pId = Convert.ToInt32(detail.ProductId);
                //modify purchaseintransaction zp
                if (detail.Product.IsWrapper == true)
                {
                    List<WrapperItem> wList = detail.Product.WrapperItems.ToList();
                    foreach (WrapperItem w in wList)
                    {
                        int Qty = Convert.ToInt32(w.ChildQty) * Convert.ToInt32(detail.Qty);
                        Product wpObj = (from p in entity.Products where p.Id == w.ChildProductId select p).FirstOrDefault();

                        List<APP_Data.PurchaseDetail> pulist = Utility.InventoryByControlMethod(wpObj.Id,entity);

                        if (pulist.Count > 0)
                        {
                            int TotalQty = Convert.ToInt32(pulist.Sum(x => x.CurrentQy));

                            if (TotalQty >= Qty)
                            {
                                foreach (PurchaseDetail p in pulist)
                                {
                                    if (Qty > 0)
                                    {
                                        if (p.CurrentQy >= Qty)
                                        {
                                            PurchaseDetailInTransaction pdObjInTran = new PurchaseDetailInTransaction();
                                            pdObjInTran.ProductId = w.ChildProductId;
                                            pdObjInTran.TransactionDetailId = detail.Id;
                                            pdObjInTran.PurchaseDetailId = p.Id;
                                            pdObjInTran.Date = detail.Transaction.DateTime;
                                            pdObjInTran.IsSpecialChild = true;
                                            pdObjInTran.Qty = Qty;
                                            p.CurrentQy = p.CurrentQy - Qty;
                                            Qty = 0;

                                            entity.PurchaseDetailInTransactions.Add(pdObjInTran);
                                            entity.Entry(p).State = EntityState.Modified;
                                            entity.SaveChanges();
                                            break;
                                        }
                                        else if (p.CurrentQy <= Qty)
                                        {
                                            PurchaseDetailInTransaction pdObjInTran = new PurchaseDetailInTransaction();
                                            pdObjInTran.ProductId = w.ChildProductId;
                                            pdObjInTran.TransactionDetailId = detail.Id;
                                            pdObjInTran.PurchaseDetailId = p.Id;
                                            pdObjInTran.Date = detail.Transaction.DateTime;
                                            pdObjInTran.IsSpecialChild = true;
                                            pdObjInTran.Qty = p.CurrentQy;

                                            Qty = Convert.ToInt32(Qty - p.CurrentQy);
                                            p.CurrentQy = 0;
                                            entity.PurchaseDetailInTransactions.Add(pdObjInTran);
                                            entity.Entry(p).State = EntityState.Modified;
                                            entity.SaveChanges();

                                        }
                                    }
                                }
                            }
                        }

                    }

                }
                else
                {
                    int Qty = Convert.ToInt32(detail.Qty);

                    // Get purchase detail with same product Id and order by purchase date ascending
                    List<APP_Data.PurchaseDetail> pulist = Utility.InventoryByControlMethod(pId,entity);

                    if (pulist.Count > 0)
                    {
                        int TotalQty = Convert.ToInt32(pulist.Sum(x => x.CurrentQy));
                        if (TotalQty >= Qty)
                        {
                            foreach (PurchaseDetail p in pulist)
                            {
                                if (Qty > 0)
                                {
                                    if (p.CurrentQy >= Qty)
                                    {
                                        PurchaseDetailInTransaction pdObjInTran = new PurchaseDetailInTransaction();
                                        pdObjInTran.ProductId = pId;
                                        pdObjInTran.TransactionDetailId = detail.Id;
                                        pdObjInTran.PurchaseDetailId = p.Id;
                                        pdObjInTran.Date = p.Date;
                                        pdObjInTran.IsSpecialChild = false;
                                        pdObjInTran.Qty = Qty;
                                        p.CurrentQy = p.CurrentQy - Qty;
                                        Qty = 0;

                                        entity.PurchaseDetailInTransactions.Add(pdObjInTran);
                                        entity.Entry(p).State = EntityState.Modified;
                                        entity.SaveChanges();
                                        break;
                                    }
                                    else if (p.CurrentQy <= Qty)
                                    {
                                        PurchaseDetailInTransaction pdObjInTran = new PurchaseDetailInTransaction();
                                        pdObjInTran.ProductId = pId;
                                        pdObjInTran.TransactionDetailId = detail.Id;
                                        pdObjInTran.PurchaseDetailId = p.Id;
                                        pdObjInTran.Date = p.Date;
                                        pdObjInTran.Qty = p.CurrentQy;
                                        pdObjInTran.IsSpecialChild = false;
                                        Qty = Convert.ToInt32(Qty - p.CurrentQy);
                                        p.CurrentQy = 0;
                                        entity.PurchaseDetailInTransactions.Add(pdObjInTran);
                                        entity.Entry(p).State = EntityState.Modified;
                                        entity.SaveChanges();
                                    }
                                }
                            }
                        }

                    }
                }
            }
            #endregion

          


            //Print Invoice

            if (Type == 4 || Type == 6)
            {
                #region [ Print ]
                if (IsPrint)
                {
                
                dsReportTemp dsReport = new dsReportTemp();
                dsReportTemp.ItemListDataTable dtReport = (dsReportTemp.ItemListDataTable)dsReport.Tables["ItemList"];

                int _tAmt = 0;
            
                foreach (TransactionDetail transaction in DetailList)
                {
                    dsReportTemp.ItemListRow newRow = dtReport.NewItemListRow();
                    newRow.Name = transaction.Product.Name;
                    newRow.Qty = transaction.Qty.ToString();
                    newRow.DiscountPercent = transaction.DiscountRate.ToString();
                    newRow.PhotoPath = string.IsNullOrEmpty(transaction.Product.PhotoPath) ? "" : Application.StartupPath + transaction.Product.PhotoPath.Remove(0, 1);
                    //newRow.TotalAmount = (int)transaction.TotalAmount; //Edit By ZMH
                    newRow.TotalAmount = (int)transaction.UnitPrice * (int)transaction.Qty; //Edit By ZMH
                    switch (Utility.GetDefaultPrinter())
                    {
                        case "A4 Printer":
                            newRow.UnitPrice = transaction.UnitPrice.ToString();
                            break;
                        case "Slip Printer":
                            newRow.UnitPrice = "1@" + transaction.UnitPrice.ToString();
                            break;
                    }
                    _tAmt += newRow.TotalAmount;
                    dtReport.AddItemListRow(newRow);
                }

                string reportPath = "";
                ReportViewer rv = new ReportViewer();
                ReportDataSource rds = new ReportDataSource("DataSet1", dsReport.Tables["ItemList"]);
                reportPath = Application.StartupPath + Utility.GetReportPath("FOC");
                rv.Reset();
                rv.LocalReport.ReportPath = reportPath;
                rv.LocalReport.DataSources.Add(rds);

                Utility.Slip_Log(rv);
         
                Utility.Slip_A4_Footer(rv);
                APP_Data.Customer cus = entity.Customers.Where(x => x.Id == CustomerId).FirstOrDefault();


                if (Type == 4)
                {
                    ReportParameter Title = new ReportParameter("Title", "FOC SALE INVOICE");
                    rv.LocalReport.SetParameters(Title);
                }
                else if (Type == 6)
                {
                    ReportParameter Title = new ReportParameter("Title", "TESTER SALE INVOICE");
                    rv.LocalReport.SetParameters(Title);
                }

                ReportParameter CustomerName = new ReportParameter("CustomerName", cus.Name);
                rv.LocalReport.SetParameters(CustomerName);

                if (Convert.ToInt32(insertedTransaction.MCDiscountAmt) != 0)
                {
                    Int64 _mcDiscountAmt = Convert.ToInt64(insertedTransaction.MCDiscountAmt);
                    ReportParameter MCDiscountAmt = new ReportParameter("MCDiscount", "-" + _mcDiscountAmt.ToString());
                    rv.LocalReport.SetParameters(MCDiscountAmt);
                }

                else if (Convert.ToInt32(insertedTransaction.BDDiscountAmt) != 0)
                {
                    Int64 _bcDiscountAmt = Convert.ToInt64(insertedTransaction.BDDiscountAmt);
                    ReportParameter BCDiscountAmt = new ReportParameter("MCDiscount", "-" + _bcDiscountAmt.ToString());
                    rv.LocalReport.SetParameters(BCDiscountAmt);
                }
                else
                {
                    ReportParameter BCDiscountAmt = new ReportParameter("MCDiscount", "0");
                    rv.LocalReport.SetParameters(BCDiscountAmt);
                }
                if (insertedTransaction.Note.ToString() == "")
                {
                    ReportParameter Notes = new ReportParameter("Notes", " ");
                    rv.LocalReport.SetParameters(Notes);
                }
                else
                {
                    ReportParameter Notes = new ReportParameter("Notes", insertedTransaction.Note.ToString());
                    rv.LocalReport.SetParameters(Notes);
                }
                if (SettingController.SelectDefaultPrinter != "Slip Printer")
                {
                    ReportParameter PrintProductImage = new ReportParameter("PrintImage", SettingController.ShowProductImageIn_A4Reports.ToString());
                    rv.LocalReport.SetParameters(PrintProductImage);
                }
                ReportParameter TAmt = new ReportParameter("TAmt", _tAmt.ToString());
                rv.LocalReport.SetParameters(TAmt);

                ReportParameter ShopName = new ReportParameter("ShopName", SettingController.ShopName);
                rv.LocalReport.SetParameters(ShopName);


                ReportParameter BranchName = new ReportParameter("BranchName", SettingController.BranchName);
                rv.LocalReport.SetParameters(BranchName);

                    ReportParameter TotalGiftDiscount = new ReportParameter("TotalGiftDiscount", GiftDiscountAmt.ToString());
                    rv.LocalReport.SetParameters(TotalGiftDiscount);

                    ReportParameter Phone = new ReportParameter("Phone", SettingController.PhoneNo);
                rv.LocalReport.SetParameters(Phone);

                ReportParameter OpeningHours = new ReportParameter("OpeningHours", SettingController.OpeningHours);
                rv.LocalReport.SetParameters(OpeningHours);

                ReportParameter TransactionId = new ReportParameter("TransactionId", resultId.ToString());
                rv.LocalReport.SetParameters(TransactionId);

                APP_Data.Counter c = entity.Counters.FirstOrDefault(x => x.Id == MemberShip.CounterId);

                ReportParameter CounterName = new ReportParameter("CounterName", c.Name);
                rv.LocalReport.SetParameters(CounterName);

                ReportParameter PrintDateTime = new ReportParameter();
                switch (Utility.GetDefaultPrinter())
                {
                    case "A4 Printer":
                        PrintDateTime = new ReportParameter("PrintDateTime", DateTime.Now.ToString("dd-MMM-yyyy"));
                        rv.LocalReport.SetParameters(PrintDateTime);
                        break;
                    case "Slip Printer":
                        PrintDateTime = new ReportParameter("PrintDateTime", DateTime.Now.ToString("dd/MM/yyyy hh:mm"));
                        rv.LocalReport.SetParameters(PrintDateTime);
                        break;
                }

                ReportParameter CasherName = new ReportParameter("CasherName", MemberShip.UserName);
                rv.LocalReport.SetParameters(CasherName);

                Int64 totalAmountRep = insertedTransaction.TotalAmount == null ? 0 : Convert.ToInt64(insertedTransaction.TotalAmount)-GiftDiscountAmt; 
                ReportParameter TotalAmount = new ReportParameter("TotalAmount", totalAmountRep.ToString());
                rv.LocalReport.SetParameters(TotalAmount);

                ReportParameter TaxAmount = new ReportParameter("TaxAmount", insertedTransaction.TaxAmount.ToString());
                rv.LocalReport.SetParameters(TaxAmount);


                if (Convert.ToInt32(insertedTransaction.DiscountAmount) == 0)
                {
                    ReportParameter DiscountAmount = new ReportParameter("DiscountAmount", insertedTransaction.DiscountAmount.ToString());
                    rv.LocalReport.SetParameters(DiscountAmount);
                }
                else
                {
                    ReportParameter DiscountAmount = new ReportParameter("DiscountAmount", "-" + insertedTransaction.DiscountAmount.ToString());
                    rv.LocalReport.SetParameters(DiscountAmount);
                }

                ReportParameter PaidAmount = new ReportParameter("PaidAmount", "0");
                rv.LocalReport.SetParameters(PaidAmount);

                ReportParameter Change = new ReportParameter("Change", "0");
                rv.LocalReport.SetParameters(Change);


                if (Utility.GetDefaultPrinter() == "A4 Printer")
                {
                    ReportParameter CusAddress = new ReportParameter("CusAddress", cus.Address);
                    rv.LocalReport.SetParameters(CusAddress);
                }

            

                if (Type == 4 || Type == 6)
              ////      PrintDoc.PrintReport(rv, Utility.GetDefaultPrinter());
                    Utility.Get_Print(rv);
                #endregion
            }
            }


            //MessageBox.Show("Payment Completed");
            
                if (System.Windows.Forms.Application.OpenForms["Sales"] != null)
                {
                    Sales newForm = (Sales)System.Windows.Forms.Application.OpenForms["Sales"];
                    newForm.note = "";
                    newForm.Clear();
                }
                Note = "";
            this.Dispose();
        }

        #region for saving Sale Qty in Stock Transaction table
        private void Save_SaleQty_ToStockTransaction(List<Stock_Transaction> productList)
        {
            int _year, _month;

            _year = System.DateTime.Now.Year;
            _month = System.DateTime.Now.Month;
            Utility.Sale_Run_Process(_year, _month, productList);
        }
        #endregion
    }
}

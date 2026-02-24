
namespace AvinyaAICRM.Application.DTOs.Order
{
  
    public class OrderResponseDto
    {
        public Guid OrderID { get; set; }
        public string? OrderNo { get; set; }
        public Guid ClientID { get; set; }

        public string? ClientName { get; set; }
        public string? CompanyName { get; set; }
        public string? Email { get; set; }
        public string? mobile { get; set; }
        public string? GstNo { get; set; }
        public string? BillAddress { get; set; }

        public bool IsUseBillingAddress { get; set; }
        public string? ShippingAddress { get; set; }

        public int? StateID { get; set; }
        public int? CityID { get; set; }
        public string? StateName { get; set; }
        public string? CityName { get; set; }

        public Guid? QuotationID { get; set; }
        public string? QuotationNo { get; set; }

        public DateTime OrderDate { get; set; }

        public bool IsDesignByUs { get; set; }
        public decimal? DesigningCharge { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }

        public int Status { get; set; }
        public string? StatusName { get; set; }

        public int DesignStatus { get; set; }
        public string? DesignStatusName { get; set; }

        public string? CreatedBy { get; set; }
        public string? CreatedByName { get; set; }

        public string? AssignedDesignTo { get; set; }
        public string? AssignedDesignToName { get; set; }

        public DateTime CreatedDate { get; set; }

        public bool EnableTax { get; set; }

        public decimal? TotalAmount { get; set; }
        public decimal? Taxes { get; set; }
        public decimal? GrandTotal { get; set; }

        public bool IsAssign { get; set; }

        public int FirmID { get; set; }
        public string FirmName { get; set; }
        public string? FirmGSTNo { get; set; }
        public string? FirmAddress { get; set; }
        public string FirmMobile { get; set; }

        public Guid? BillID { get; set; }

        public List<OrderItemReponceDto>? OrderItems { get; set; }
        public List<WorkOrderData>? WorkOrder { get; set; }

        public List<BillData?> Bill { get; set; }
    }
    public class WorkOrderData
    {
        public Guid WorkOrderID { get; set; }

        public string WorkOrderNo { get; set; }

        public Guid VendorID { get; set; }

        public List<VendordataDTO> Vendors { get; set; }
        public DateTime DueDate { get; set; }

        public int? Status { get; set; }
        public string WorkOrderStatus { get; set; }
        public string? Remarks { get; set; }
        public DateTime? CreatedDate { get; set; }
        public decimal TotalAmount { get; set; }

        public List<WorkOrderItemData> WorkOrderItems { get; set; }
    }
    public class VendordataDTO
    {
        public Guid VendorID { get; set; }
        public string VendorName { get; set; }
        public string? ContactPerson { get; set; }
        public string? GSTNo { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public int? CityID { get; set; }

        public int? StateID { get; set; }
        public string? Pincode { get; set; }
        public string? PaymentTerms { get; set; }
        public string? PreferredPrintingTypes { get; set; }
    }

    public class WorkOrderItemData
    {
        public Guid WorkOrderItemID { get; set; }
        public Guid WorkOrderID { get; set; }
        public string ProductDescription { get; set; }
        public string? Description { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public int WorkTypeID { get; set; }
        public string WorkTypeName { get; set; }

        public int ProcessStage { get; set; }
        public string ProcessStageName { get; set; }
    }

    public class BillData
    {
        public Guid BillID { get; set; }
        public string BillNo { get; set; }
        public Guid? OrderID { get; set; }
        public Guid? ClientID { get; set; }
        public DateTime BillDate { get; set; }
        public bool IsGST { get; set; }
        public int? FirmID { get; set; }
        public string? FirmName { get; set; }
        public decimal? SubTotal { get; set; }
        public decimal? Taxes { get; set; }
        public decimal Discount { get; set; }
        public decimal? GrandTotal { get; set; }
        public decimal? RemainingPayment { get; set; }
        public decimal? PaidAmount { get; set; }
        public string? PlaceOfSupply { get; set; }
        public bool? ReverseCharge { get; set; }
        public string? GRRRNo { get; set; }
        public DateTime? DueDate { get; set; }
        public string? Transport { get; set; }
        public string? VehicleNo { get; set; }
        public string? Station { get; set; }
        public string? EWayBillNo { get; set; }
        public decimal? OutstandingAmount { get; set; }
        public BankDetailsDto? Bank1 { get; set; }
        public BankDetailsDto? Bank2 { get; set; }
        public int? Status { get; set; }
        public string? StatusName { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class BankDetailsDto
    {
        public Guid? BankAccountId { get; set; }
        public string? BankName { get; set; }
        public string? AccountHolderName { get; set; }
        public string? AccountNumber { get; set; }
        public string? IFSCCode { get; set; }
        public string? BranchName { get; set; }
        public bool? IsActive { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.DTOs.Reports
{
    public class VendorReport
    {
        public Guid VendorID { get; set; }
        public string VendorName { get; set; }
        public string ContactPerson { get; set; }
        public string Mobile { get; set; }
        public string AlternateContact { get; set; }
        public string Email { get; set; }
        public string GSTNo { get; set; }
        public string Address { get; set; }
        public int? CityID { get; set; }
        public string? CityName { get; set; }     
        public int? StateID { get; set; }
        public string? StateName { get; set; }
        public string Pincode { get; set; }
        public string PaymentTerms { get; set; }
        public string PreferredPrintingTypes { get; set; }

        public bool Status { get; set; }

        public string CreatedBy { get; set; }
        public string CreatedByName { get; set; }
        public DateTime? CreatedDate { get; set; }

        public int WorkOrderCount { get; set; }
        public int InwardCount { get; set; }
        public int VendorPerformanceCount { get; set; }

        public decimal TotalWorkOrderAmount { get; set; }
        public int TotalReceivedQuantity { get; set; }
        public int TotalPendingQuantity { get; set; }

    }

}

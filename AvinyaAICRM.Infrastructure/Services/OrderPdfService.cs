using AvinyaAICRM.Application.DTOs.Order;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Orders;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace AvinyaAICRM.Infrastructure.Services
{
    public class OrderPdfService : IOrderPdfService
    {
        static OrderPdfService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GenerateOrderBillPdf(OrderResponseDto order, Guid billId)
        {
            var bill = order.Bill.FirstOrDefault(b => b.BillID == billId);
            if (bill == null) return null;

            var document = new OrderBillDocument(order, bill);
            return document.GeneratePdf();
        }
    }

    public class OrderBillDocument : IDocument
    {
        public OrderResponseDto Order { get; }
        public BillData Bill { get; }

        public OrderBillDocument(OrderResponseDto order, BillData bill)
        {
            Order = order;
            Bill = bill;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(20);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.Arial));

                page.Content().Border(1).Column(column =>
                {
                    // 1. Header
                    column.Item().Element(ComposeHeader);
                    
                    // 2. Invoice Details
                    column.Item().Element(ComposeInvoiceDetails);
                    
                    // 3. Shipping / Billing Details
                    column.Item().Element(ComposePartyDetails);

                    // 4. Table
                    column.Item().Element(ComposeTable);

                    // 5. Total Section
                    column.Item().Element(ComposeTotalSection);

                    // 6. Bank Details Section
                    column.Item().Element(ComposeBankDetails);

                    // 7. Footer (Terms & Condition, QR, Signature)
                    column.Item().Element(ComposeFooter);
                });
            });
        }

        private void ComposeHeader(IContainer container)
        {
            container.BorderBottom(1).Padding(5).Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text($"GSTIN : {Bill.FirmName ?? "-"}").FontSize(8).Bold();
                    row.RelativeItem().AlignCenter().Text("TAX INVOICE").FontSize(9).Bold();
                    row.RelativeItem().AlignRight().Text("Original Copy").FontSize(7.5f);
                });

                column.Item().AlignCenter().PaddingTop(5).Text(Bill.FirmName?.ToUpper() ?? "AVINYA AI").FontSize(14).Bold();
            });
        }

        private void ComposeInvoiceDetails(IContainer container)
        {
            container.BorderBottom(1).Row(row =>
            {
                // Left - Invoice info
                row.RelativeItem().BorderRight(1).Padding(5).Column(col =>
                {
                    col.Item().Row(r => { r.ConstantItem(90).Text("Invoice No."); r.RelativeItem().Text($": {Bill.BillNo}").Bold(); });
                    col.Item().Row(r => { r.ConstantItem(90).Text("Date"); r.RelativeItem().Text($": {Bill.BillDate:dd/MM/yyyy}"); });
                    col.Item().Row(r => { r.ConstantItem(90).Text("Place of Supply"); r.RelativeItem().Text($": {Bill.PlaceOfSupply ?? Order.StateName ?? "-"}"); });
                    col.Item().Row(r => { r.ConstantItem(90).Text("Reverse Charge"); r.RelativeItem().Text($": {(Bill.ReverseCharge == true ? "Yes" : "No")}"); });
                    col.Item().Row(r => { r.ConstantItem(90).Text("GR/RR No."); r.RelativeItem().Text($": {Bill.GRRRNo ?? "-"}"); });
                    col.Item().Row(r => { r.ConstantItem(90).Text("Due Date").Bold(); r.RelativeItem().Text($": {Bill.DueDate:dd/MM/yyyy}").Bold(); });
                });

                // Right - Transport info
                row.RelativeItem().Padding(5).Column(col =>
                {
                    col.Item().Row(r => { r.ConstantItem(90).Text("Transport"); r.RelativeItem().Text($": {Bill.Transport ?? "-"}"); });
                    col.Item().Row(r => { r.ConstantItem(90).Text("Vehicle No."); r.RelativeItem().Text($": {Bill.VehicleNo ?? "-"}"); });
                    col.Item().Row(r => { r.ConstantItem(90).Text("Station"); r.RelativeItem().Text($": {Bill.Station ?? "-"}"); });
                    col.Item().Row(r => { r.ConstantItem(90).Text("E-Way Bill No."); r.RelativeItem().Text($": {Bill.EWayBillNo ?? "-"}"); });
                    col.Item().PaddingTop(5).Text($"Outstanding Amt: {Bill.OutstandingAmount?.ToString("F2") ?? "0.00"}").Bold();
                });
            });
        }

        private void ComposePartyDetails(IContainer container)
        {
            container.BorderBottom(1).Row(row =>
            {
                // Billed To
                row.RelativeItem().BorderRight(1).Padding(5).Column(col =>
                {
                    col.Item().Text("Billed to :").Bold().FontSize(9);
                    col.Item().Text(Order.CompanyName ?? Order.ClientName ?? "-").Bold();
                    col.Item().Text(Order.BillAddress ?? "-");
                    col.Item().Text($"{Order.CityName ?? ""} {Order.StateName ?? ""}");
                    col.Item().Text($"GSTIN / UIN : {Order.GstNo ?? "-"}");
                });

                // Shipped To
                row.RelativeItem().Padding(5).Column(col =>
                {
                    col.Item().Text("Shipped to :").Bold().FontSize(9);
                    col.Item().Text(Order.CompanyName ?? Order.ClientName ?? "-").Bold();
                    col.Item().Text(Order.ShippingAddress ?? Order.BillAddress ?? "-");
                    col.Item().Text($"{Order.CityName ?? ""} {Order.StateName ?? ""}");
                    col.Item().Text($"GSTIN / UIN : {Order.GstNo ?? "-"}");
                });
            });
        }

        private void ComposeTable(IContainer container)
        {
            container.MinHeight(300).Layers(layers =>
            {
                layers.Layer().Row(row =>
                {
                    row.ConstantItem(30).BorderRight(1);  // S.N.
                    row.RelativeItem().BorderRight(1);    // Description
                    row.ConstantItem(70).BorderRight(1);  // HSN
                    row.ConstantItem(45).BorderRight(1);  // Qty
                    row.ConstantItem(45).BorderRight(1);  // Unit
                    row.ConstantItem(60).BorderRight(1);  // Price
                    row.ConstantItem(45).BorderRight(1);  // Tax %
                    row.ConstantItem(80);                  // Amount
                });

                layers.PrimaryLayer().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);
                        columns.RelativeColumn();
                        columns.ConstantColumn(70);
                        columns.ConstantColumn(45);
                        columns.ConstantColumn(45);
                        columns.ConstantColumn(60);
                        columns.ConstantColumn(45);
                        columns.ConstantColumn(80);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderStyle).Text("S.N.");
                        header.Cell().Element(HeaderStyle).Text("Description of Goods");
                        header.Cell().Element(HeaderStyle).Text("HSN/SAC Code");
                        header.Cell().Element(HeaderStyle).Text("Qty.");
                        header.Cell().Element(HeaderStyle).Text("Price");
                        header.Cell().Element(HeaderStyle).Text("Tax %");
                        header.Cell().Element(HeaderStyle).Text("Amount(`)");

                        static IContainer HeaderStyle(IContainer sub) => sub.DefaultTextStyle(x => x.Bold()).BorderBottom(1).PaddingVertical(3).AlignCenter();
                    });

                    var items = Order.OrderItems ?? new List<OrderItemReponceDto>();
                    for (int i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        table.Cell().Element(ItemStyle).Text($"{i + 1}.");
                        table.Cell().Element(ItemStyle).AlignLeft().Column(c =>
                        {
                            c.Item().Text(item.ProductName).Bold();
                            if (!string.IsNullOrEmpty(item.Description))
                                c.Item().Text(item.Description).FontSize(7.5f);
                        });
                        table.Cell().Element(ItemStyle).Text(item.HsnCode ?? "-");
                        table.Cell().Element(ItemStyle).Text(item.Quantity.ToString("F2"));
                        table.Cell().Element(ItemStyle).Text(item.UnitPrice.ToString("F2"));
                        table.Cell().Element(ItemStyle).Text(Order.EnableTax ? "18%" : "-");
                        table.Cell().Element(ItemStyle).Text(item.LineTotal.ToString("F2"));

                        static IContainer ItemStyle(IContainer sub) => sub.PaddingHorizontal(5).PaddingVertical(3).AlignCenter();
                    }
                });
            });
        }

        private void ComposeTotalSection(IContainer container)
        {
            container.BorderTop(1).Row(row =>
            {
                // Amount in words
                row.RelativeItem().Padding(5).Column(col =>
                {
                    col.Item().Text(NumberToWords((double)(Bill.GrandTotal ?? 0))).Bold();
                });

                // Quantities and Totals
                row.ConstantItem(230).BorderLeft(1).Padding(5).Column(col =>
                {
                    col.Item().Row(r => { r.RelativeItem().Text("Sub Total"); r.RelativeItem().AlignRight().Text(Bill.SubTotal?.ToString("F2") ?? "0.00"); });
                    if (Bill.Taxes > 0)
                        col.Item().Row(r => { r.RelativeItem().Text("Taxes"); r.RelativeItem().AlignRight().Text(Bill.Taxes?.ToString("F2") ?? "0.00"); });
                    if (Bill.Discount > 0)
                        col.Item().Row(r => { r.RelativeItem().Text("Discount"); r.RelativeItem().AlignRight().Text($"-{Bill.Discount:F2}"); });
                    if (Order.DesigningCharge > 0)
                        col.Item().Row(r => { r.RelativeItem().Text("Design Charge"); r.RelativeItem().AlignRight().Text(Order.DesigningCharge?.ToString("F2") ?? "0.00"); });

                    col.Item().Row(r => { r.RelativeItem().Text("Grand Total").Bold().FontSize(10); r.RelativeItem().AlignRight().Text(Bill.GrandTotal?.ToString("F2") ?? "0.00").Bold().FontSize(10); });
                });
            });
        }

        private void ComposeBankDetails(IContainer container)
        {
            if (Bill.Bank1 == null && Bill.Bank2 == null) return;

            container.BorderTop(1).Row(row =>
            {
                if (Bill.Bank1 != null)
                {
                    row.RelativeItem().Padding(5).Column(col =>
                    {
                        col.Item().Text("Bank Details :").Bold();
                        col.Item().Text($"BANK NAME : {Bill.Bank1.BankName ?? "-"}");
                        col.Item().Text($"A/C NO : {Bill.Bank1.AccountNumber ?? "-"}");
                        col.Item().Text($"IFSC CODE : {Bill.Bank1.IFSCCode ?? "-"}");
                    });
                }
                if (Bill.Bank2 != null)
                {
                    row.RelativeItem().BorderLeft(1).Padding(5).Column(col =>
                    {
                        col.Item().Text("Bank Details :").Bold();
                        col.Item().Text($"BANK NAME : {Bill.Bank2.BankName ?? "-"}");
                        col.Item().Text($"A/C NO : {Bill.Bank2.AccountNumber ?? "-"}");
                        col.Item().Text($"IFSC CODE : {Bill.Bank2.IFSCCode ?? "-"}");
                    });
                }
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.BorderTop(1).Padding(5).Row(row =>
            {
                // Terms
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Terms & Conditions").Bold().FontSize(8.5f);
                    col.Item().Text("E. & O. E.").FontSize(7);
                    col.Item().PaddingTop(2).Text("1. Goods once sold will not be taken back.");
                    col.Item().Text("2. Interest @18% p.a. will be charged if payment is not made in time.");
                    col.Item().Text("3. Subject to 'SURAT' jurisdiction only.");
                });

                // QR Code Placeholder (if possible to add QR logic here later)
                if (false) // Dummy for QR box as per JS reference
                {
                    row.ConstantItem(80).AlignCenter().Column(col => { col.Item().Text("Payment QR").FontSize(8); });
                }

                // Signature
                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text("Receiver's Signature :").FontSize(9);
                    col.Item().PaddingTop(5).Text($"For {Bill.FirmName?.ToUpper() ?? "AVINYA AI"}").Bold();
                    col.Item().PaddingTop(25).Text("Authorised Signatory").FontSize(8.5f);
                });
            });
        }

        private string NumberToWords(double number)
        {
            if (number == 0) return "Zero Only";
            if (number < 0) return "Minus " + NumberToWords(Math.Abs(number));

            long intPart = (long)number;
            int decimalPart = (int)Math.Round((number - intPart) * 100);

            string words = NumberToWordsHelper(intPart);
            if (decimalPart > 0)
            {
                words += " and " + NumberToWordsHelper(decimalPart) + " Paise";
            }

            return words + " Only";
        }

        private string NumberToWordsHelper(long number)
        {
            if (number == 0) return "";
            if (number < 20) return new[] { "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" }[number];
            if (number < 100) return new[] { "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" }[number / 10] + ((number % 10 > 0) ? " " + NumberToWordsHelper(number % 10) : "");
            if (number < 1000) return NumberToWordsHelper(number / 100) + " Hundred" + ((number % 100 > 0) ? " " + NumberToWordsHelper(number % 100) : "");
            if (number < 100000) return NumberToWordsHelper(number / 1000) + " Thousand" + ((number % 1000 > 0) ? " " + NumberToWordsHelper(number % 1000) : "");
            if (number < 10000000) return NumberToWordsHelper(number / 100000) + " Lakh" + ((number % 100000 > 0) ? " " + NumberToWordsHelper(number % 100000) : "");
            return NumberToWordsHelper(number / 10000000) + " Crore" + ((number % 10000000 > 0) ? " " + NumberToWordsHelper(number % 10000000) : "");
        }
    }
}

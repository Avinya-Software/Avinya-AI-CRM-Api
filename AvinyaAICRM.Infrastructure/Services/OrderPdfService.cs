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

        public byte[] GenerateOrderPdf(OrderResponseDto order)
        {
            var document = new OrderDocument(order);
            return document.GeneratePdf();
        }
    }

    public class OrderDocument : IDocument
    {
        public OrderResponseDto Order { get; }

        public OrderDocument(OrderResponseDto order)
        {
            Order = order;
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
                    
                    // 2. Order Details
                    column.Item().Element(ComposeOrderDetails);
                    
                    // 3. Shipping / Billing Details
                    column.Item().Element(ComposePartyDetails);

                    // 4. Table
                    column.Item().Element(ComposeTable);

                    // 5. Total Section
                    column.Item().Element(ComposeTotalSection);

                    // 6. Footer (Terms, QR Placeholder, Signature)
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
                    row.RelativeItem().Text($"GSTIN : {Order.FirmGSTNo ?? "-"}").FontSize(8).Bold();
                    row.RelativeItem().AlignCenter().Text("ORDER CONFIRMATION").FontSize(9).Bold();
                    row.RelativeItem().AlignRight().Text("Client Copy").FontSize(7.5f);
                });

                column.Item().AlignCenter().PaddingTop(5).Text(Order.FirmName?.ToUpper() ?? " ").FontSize(14).Bold();
                column.Item().AlignCenter().Text(Order.FirmAddress?.ToUpper() ?? "-").FontSize(7.5f);
                column.Item().AlignCenter().Text($"Tel : {Order.FirmMobile ?? "-"}").FontSize(8);
            });
        }

        private void ComposeOrderDetails(IContainer container)
        {
            container.BorderBottom(1).Row(row =>
            {
                // Left - Order Details
                row.RelativeItem().BorderRight(1).Padding(5).Column(col =>
                {
                    col.Item().Row(r => { r.ConstantItem(90).Text("Order No."); r.RelativeItem().Text($": {Order.OrderNo}").Bold(); });
                    col.Item().Row(r => { r.ConstantItem(90).Text("Order Date"); r.RelativeItem().Text($": {Order.OrderDate:dd/MM/yyyy}"); });
                    col.Item().Row(r => { r.ConstantItem(90).Text("State Name"); r.RelativeItem().Text($": {Order.StateName ?? "-"}"); });
                    col.Item().Row(r => { r.ConstantItem(90).Text("Quotation Ref."); r.RelativeItem().Text($": {Order.QuotationNo ?? "-"}"); });
                });

                // Right - Delivery Info
                row.RelativeItem().Padding(5).Column(col =>
                {
                    col.Item().Row(r => { r.ConstantItem(120).Text("Expected Delivery"); r.RelativeItem().Text($": {Order.ExpectedDeliveryDate?.ToString("dd/MM/yyyy") ?? "-"}"); });
                    col.Item().Row(r => { r.ConstantItem(120).Text("Design Status"); r.RelativeItem().Text($": {Order.DesignStatusName ?? "-"}"); });
                    col.Item().Row(r => { r.ConstantItem(120).Text("Order Status"); r.RelativeItem().Text($": {Order.StatusName ?? "-"}"); });
                    col.Item().PaddingTop(5).Text($"Created By: {Order.CreatedByName ?? "-"}").FontSize(8);
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
            container.MinHeight(500).Layers(layers =>
            {
                layers.Layer().Row(row =>
                {
                    row.ConstantItem(30).BorderRight(1);  // S.N.
                    row.RelativeItem().BorderRight(1);    // Description
                    row.ConstantItem(70).BorderRight(1);  // HSN
                    row.ConstantItem(50).BorderRight(1);  // Qty
                    row.ConstantItem(70).BorderRight(1);  // Price
                    row.ConstantItem(50).BorderRight(1);  // Tax %
                    row.ConstantItem(80);                  // Amount
                });

                layers.PrimaryLayer().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);
                        columns.RelativeColumn();
                        columns.ConstantColumn(70);
                        columns.ConstantColumn(50);
                        columns.ConstantColumn(70);
                        columns.ConstantColumn(50);
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
                        table.Cell().Element(ItemStyle).Text((item.Quantity * item.UnitPrice).ToString("F2"));

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
                    col.Item().Text(NumberToWords((double)(Order.GrandTotal ?? 0))).Bold();
                });

                // Totals
                row.ConstantItem(230).BorderLeft(1).Padding(5).Column(col =>
                {
                    col.Item().Row(r => { r.RelativeItem().Text("Sub Total"); r.RelativeItem().AlignRight().Text(Order.TotalAmount?.ToString("F2") ?? "0.00"); });
                    if (Order.Taxes > 0)
                        col.Item().Row(r => { r.RelativeItem().Text("Taxes"); r.RelativeItem().AlignRight().Text(Order.Taxes?.ToString("F2") ?? "0.00"); });
                    if (Order.DesigningCharge > 0)
                        col.Item().Row(r => { r.RelativeItem().Text("Design Charge"); r.RelativeItem().AlignRight().Text(Order.DesigningCharge?.ToString("F2") ?? "0.00"); });

                    col.Item().Row(r => { r.RelativeItem().Text("Grand Total").Bold().FontSize(10); r.RelativeItem().AlignRight().Text(Order.GrandTotal?.ToString("F2") ?? "0.00").Bold().FontSize(10); });
                });
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
                    col.Item().Text("1. Goods once sold will be as per order confirmation.");
                    col.Item().Text("2. Expected delivery dates are subject to material availability.");
                });

                // Signature
                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().PaddingTop(5).Text($"For {Order.FirmName?.ToUpper() ?? " "}").Bold();
                    col.Item().PaddingTop(50).Text("Authorised Signatory").FontSize(8.5f);
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

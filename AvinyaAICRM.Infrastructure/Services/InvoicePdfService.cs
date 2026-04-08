using AvinyaAICRM.Application.DTOs.Invoice;
using AvinyaAICRM.Application.DTOs.Order;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Invoice;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace AvinyaAICRM.Infrastructure.Services
{
    public class InvoicePdfService : IInvoicePdfService
    {
        static InvoicePdfService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GenerateInvoicePdf(InvoiceDto invoice, OrderResponseDto order)
        {
            var document = new InvoiceDocument(invoice, order);
            return document.GeneratePdf();
        }
    }

    public class InvoiceDocument : IDocument
    {
        public InvoiceDto Invoice { get; }
        public OrderResponseDto Order { get; }

        public InvoiceDocument(InvoiceDto invoice, OrderResponseDto order)
        {
            Invoice = invoice;
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

                // ✅ CONTENT
                page.Content().Border(1).Column(column =>
                {
                    column.Item().Element(ComposeHeader);
                    column.Item().Element(ComposeInvoiceDetails);
                    column.Item().Element(ComposePartyDetails);

                    // 👇 CRITICAL 1: .ExtendVertical() is required here! 
                    // This forces the table block to expand and fill all empty space down to the footer.
                    column.Item().ExtendVertical().Element(ComposeTable);
                });

                // ✅ FIXED FOOTER
                page.Footer().Element(ComposeTotalAndFooter);
            });
        }

        private void ComposeHeader(IContainer container)
        {
            container.BorderBottom(1).Padding(5).Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text($"GSTIN : {Order.FirmGSTNo ?? "-"}").FontSize(8).Bold();
                    row.RelativeItem().AlignCenter().Text("TAX INVOICE").FontSize(9).Bold();
                    row.RelativeItem().AlignRight().Text("Original Copy").FontSize(7.5f);
                });

                column.Item().AlignCenter().PaddingTop(5).Text(Order.FirmName?.ToUpper() ?? " ").FontSize(14).Bold();
                column.Item().AlignCenter().Text(Order.FirmAddress?.ToUpper() ?? "-").FontSize(7.5f);
                column.Item().AlignCenter().Text($"Tel : {Order.FirmMobile ?? "-"}").FontSize(8);
            });
        }

        private void ComposeInvoiceDetails(IContainer container)
        {
            container.BorderBottom(1).Row(row =>
            {
                // Left - Invoice Details
                row.RelativeItem().BorderRight(1).Padding(5).Column(col =>
                {
                    col.Item().Row(r => { r.ConstantItem(90).Text("Invoice No."); r.RelativeItem().Text($": {Invoice.InvoiceNo}").Bold(); });
                    col.Item().Row(r => { r.ConstantItem(90).Text("Invoice Date"); r.RelativeItem().Text($": {Invoice.InvoiceDate:dd/MM/yyyy}"); });
                    col.Item().Row(r => { r.ConstantItem(90).Text("Place of Supply"); r.RelativeItem().Text($": {Invoice.PlaceOfSupply ?? "-"}"); });
                    col.Item().Row(r => { r.ConstantItem(90).Text("Reverse Charge"); r.RelativeItem().Text($": {(Invoice.ReverseCharge ? "Yes" : "No")}"); });
                });

                // Right - Transport Info
                row.RelativeItem().Padding(5).Column(col =>
                {
                    col.Item().Row(r => { r.ConstantItem(90).Text("Transport"); r.RelativeItem().Text($": {Invoice.Transport ?? "-"}"); });
                    col.Item().Row(r => { r.ConstantItem(90).Text("Vehicle No."); r.RelativeItem().Text($": {Invoice.VehicleNo ?? "-"}"); });
                    col.Item().Row(r => { r.ConstantItem(90).Text("Station"); r.RelativeItem().Text($": {Invoice.Station ?? "-"}"); });
                    col.Item().Row(r => { r.ConstantItem(90).Text("GR/RR No."); r.RelativeItem().Text($": {Invoice.GRRRNo ?? "-"}"); });
                    col.Item().Row(r => { r.ConstantItem(90).Text("E-Way Bill No."); r.RelativeItem().Text($": {Invoice.EWayBillNo ?? "-"}"); });
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
            container.Layers(layers =>
            {
                // This layer draws the vertical column borders spanning the full height
                layers.Layer().Row(row =>
                {
                    // 👇 CRITICAL 2: .ExtendVertical() on EVERY item.
                    // This tells the vertical lines to stretch all the way to the bottom of the table block.
                    row.ConstantItem(30).BorderRight(1).ExtendVertical();  // S.N.
                    row.RelativeItem().BorderRight(1).ExtendVertical();    // Description
                    row.ConstantItem(70).BorderRight(1).ExtendVertical();  // HSN
                    row.ConstantItem(50).BorderRight(1).ExtendVertical();  // Qty
                    row.ConstantItem(70).BorderRight(1).ExtendVertical();  // Price
                    row.ConstantItem(50).BorderRight(1).ExtendVertical();  // Tax %
                    row.ConstantItem(80).ExtendVertical();                 // Amount
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

                        static IContainer HeaderStyle(IContainer sub) =>
                            sub.DefaultTextStyle(x => x.Bold()).BorderBottom(1).PaddingVertical(3).AlignCenter();
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

                        static IContainer ItemStyle(IContainer sub) =>
                            sub.PaddingHorizontal(5).PaddingVertical(3).AlignCenter();
                    }
                });
            });
        }

        /// <summary>
        /// Total summary row + full footer combined into ONE element so QuestPDF
        /// never splits them across pages. They always land together at the bottom
        /// of whichever page the table ends on.
        /// </summary>
        private void ComposeTotalAndFooter(IContainer container)
        {
            container.BorderLeft(1).BorderRight(1).BorderBottom(1).Column(column =>
            {
                // ── Total Summary Row ─────────────────────────────────────────
                column.Item().BorderTop(1).Row(row =>
                {
                    // Left — Amount in words
                    row.RelativeItem().Padding(5).Column(col =>
                    {
                        col.Item().Text(NumberToWords((double)Invoice.OutstandingAmount)).Bold();
                    });

                    // Right — Numeric totals
                    row.ConstantItem(230).BorderLeft(1).Padding(5).Column(col =>
                    {
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Sub Total");
                            r.RelativeItem().AlignRight().Text(Invoice.SubTotal.ToString("F2"));
                        });

                        if (Invoice.Taxes > 0)
                            col.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Taxes");
                                r.RelativeItem().AlignRight().Text(Invoice.Taxes.ToString("F2"));
                            });

                        if (Invoice.Discount > 0)
                            col.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Discount");
                                r.RelativeItem().AlignRight().Text($"- {Invoice.Discount.ToString("F2")}");
                            });

                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Grand Total").Bold().FontSize(10);
                            r.RelativeItem().AlignRight().Text(Invoice.OutstandingAmount.ToString("F2")).Bold().FontSize(10);
                        });

                        col.Item().PaddingTop(5).BorderTop(0.5f).Row(r =>
                        {
                            r.RelativeItem().Text("Amount Paid").FontSize(8);
                            r.RelativeItem().AlignRight().Text(Invoice.PaidAmount.ToString("F2")).FontSize(8);
                        });

                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Balance Due").Bold().FontSize(9);
                            r.RelativeItem().AlignRight().Text(Invoice.RemainingPayment.ToString("F2")).Bold().FontSize(9);
                        });
                    });
                });

                // ── Bank Details Row ──────────────────────────────────────────
                if (Order.Bank1 != null || Order.Bank2 != null)
                {
                    column.Item().BorderTop(1).BorderBottom(1).Row(row =>
                    {
                        if (Order.Bank1 != null)
                        {
                            row.RelativeItem().Padding(5).Column(col =>
                            {
                                col.Item().Text(text =>
                                {
                                    text.Span("Bank Details : BANK NAME : ").Bold().FontSize(8.5f);
                                    text.Span(Order.Bank1.BankName ?? "-").FontSize(8.5f);
                                });
                                col.Item().PaddingLeft(60).Text($"A/C NO : {Order.Bank1.AccountNumber ?? "-"}").FontSize(8.5f);
                                col.Item().PaddingLeft(60).Text($"IFSC CODE : {Order.Bank1.IFSCCode ?? "-"}").FontSize(8.5f);
                            });
                        }

                        if (Order.Bank2 != null)
                        {
                            row.RelativeItem()
                               .BorderLeft(Order.Bank1 != null ? 1 : 0)
                               .Padding(5)
                               .Column(col =>
                               {
                                   col.Item().Text(text =>
                                   {
                                       text.Span("Bank Details : BANK NAME : ").Bold().FontSize(8.5f);
                                       text.Span(Order.Bank2.BankName ?? "-").FontSize(8.5f);
                                   });
                                   col.Item().PaddingLeft(60).Text($"A/C NO : {Order.Bank2.AccountNumber ?? "-"}").FontSize(8.5f);
                                   col.Item().PaddingLeft(60).Text($"IFSC CODE : {Order.Bank2.IFSCCode ?? "-"}").FontSize(8.5f);
                               });
                        }
                    });
                }

                // ── Terms / QR / Signature Row ────────────────────────────────
                column.Item().BorderTop(1).Layers(layers =>
                {
                    // Border layer — vertical dividers span the full row height.
                    layers.Layer().Row(row =>
                    {
                        row.RelativeItem();                   // Terms  (no left border)
                        row.RelativeItem().BorderLeft(1);     // QR Code
                        row.RelativeItem().BorderLeft(1);     // Signature
                    });

                    // Content layer
                    layers.PrimaryLayer().Row(row =>
                    {
                        // ── Terms ──
                        row.RelativeItem().Padding(5).Column(col =>
                        {
                            col.Item().Text("Terms & Conditions").Bold().FontSize(8.5f);
                            col.Item().PaddingTop(3).Text("E. & O. E.").FontSize(7.5f);
                            col.Item().PaddingTop(3).Text("1. Goods once sold will not be taken back.").FontSize(7.5f);
                            col.Item().PaddingTop(3).Text("2. Interest @18% p.a. will be charged").FontSize(7.5f);
                            col.Item().PaddingTop(1).PaddingLeft(8).Text("if the payment is not made in time.").FontSize(7.5f);
                            col.Item().PaddingTop(3).Text("3. Subject to 'SURAT' jurisdiction only.").FontSize(7.5f);
                        });

                        // ── QR Code ──
                        row.RelativeItem().Padding(5).AlignCenter().Column(col =>
                        {
                            col.Item().AlignCenter().Text("Payment QR Code").Bold().FontSize(8.5f);

                            if (Order.ShowPaymentQR && !string.IsNullOrWhiteSpace(Order.PaymentUPIId))
                            {
                                try
                                {
                                    string upiString = $"upi://pay?pa={Order.PaymentUPIId}" +
                                                       $"&pn={Order.FirmName?.Replace(" ", "%20")}" +
                                                       $"&am={Invoice.RemainingPayment}&cu=INR";

                                    using var qrGenerator = new QRCoder.QRCodeGenerator();
                                    using var qrCodeData = qrGenerator.CreateQrCode(upiString, QRCoder.QRCodeGenerator.ECCLevel.Q);
                                    using var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
                                    byte[] qrCodeImage = qrCode.GetGraphic(5);

                                    col.Item().PaddingTop(5).AlignCenter().Width(60).Height(60).Image(qrCodeImage);
                                }
                                catch
                                {
                                    col.Item().PaddingTop(5).Text("QR Error").FontSize(7f);
                                }
                            }
                            else
                            {
                                col.Item().PaddingTop(5).Text(" ").FontSize(8f);
                            }
                        });

                        // ── Signature ──
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().BorderBottom(1).Padding(5)
                               .Text("Receiver's Signature :").Bold().FontSize(8.5f);

                            col.Item().Padding(5).AlignRight()
                               .Text($"For {Order.FirmName?.ToUpper() ?? " "}").Bold().FontSize(8.5f);

                            col.Item().Padding(5).PaddingTop(30).AlignRight()
                               .Text("Authorised Signatory").Bold().FontSize(8.5f);
                        });
                    });
                });
            });
        }

        // ── Number → Words (Indian system) ───────────────────────────────────

        private string NumberToWords(double number)
        {
            if (number == 0) return "Zero Only";
            if (number < 0) return "Minus " + NumberToWords(Math.Abs(number));

            long intPart = (long)number;
            int decimalPart = (int)Math.Round((number - intPart) * 100);

            string words = NumberToWordsHelper(intPart);
            if (decimalPart > 0)
                words += " and " + NumberToWordsHelper(decimalPart) + " Paise";

            return words + " Only";
        }

        private string NumberToWordsHelper(long number)
        {
            if (number == 0) return "";
            if (number < 20)
                return new[] { "", "One", "Two", "Three", "Four", "Five", "Six", "Seven",
                                "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen",
                                "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" }[number];
            if (number < 100)
                return new[] { "", "", "Twenty", "Thirty", "Forty", "Fifty",
                                "Sixty", "Seventy", "Eighty", "Ninety" }[number / 10]
                       + (number % 10 > 0 ? " " + NumberToWordsHelper(number % 10) : "");
            if (number < 1000)
                return NumberToWordsHelper(number / 100) + " Hundred"
                       + (number % 100 > 0 ? " " + NumberToWordsHelper(number % 100) : "");
            if (number < 100000)
                return NumberToWordsHelper(number / 1000) + " Thousand"
                       + (number % 1000 > 0 ? " " + NumberToWordsHelper(number % 1000) : "");
            if (number < 10000000)
                return NumberToWordsHelper(number / 100000) + " Lakh"
                       + (number % 100000 > 0 ? " " + NumberToWordsHelper(number % 100000) : "");
            return NumberToWordsHelper(number / 10000000) + " Crore"
                   + (number % 10000000 > 0 ? " " + NumberToWordsHelper(number % 10000000) : "");
        }
    }
}
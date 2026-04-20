using AvinyaAICRM.Application.DTOs.Quotation;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Quotations;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace AvinyaAICRM.Infrastructure.Services
{
    public class QuotationPdfService : IQuotationPdfService
    {
        static QuotationPdfService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GenerateQuotationPdf(QuotationResponseDto quotation)
        {
            var document = new QuotationDocument(quotation);
            return document.GeneratePdf();
        }
    }

    public class QuotationDocument : IDocument
    {
        public QuotationResponseDto Model { get; }

        public QuotationDocument(QuotationResponseDto model)
        {
            Model = model;
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
                    column.Item().Element(ComposeHeader);
                    column.Item().Element(ComposePartyDetails);
                    column.Item().Element(ComposeTable);
                    column.Item().Element(ComposeTotalSection);
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
                    row.RelativeItem().Text($"GSTIN : {Model.FirmGSTNo ?? "-"}").FontSize(8).Bold();
                });

                column.Item().AlignCenter().Text("SALES QUOTATION").FontSize(11).Bold();
                column.Item().AlignCenter().Text(Model.FirmName?.ToUpper() ?? "AVINYA AI").FontSize(14).Bold();
                column.Item().AlignCenter().Text(Model.FirmAddress?.ToUpper() ?? "-").FontSize(7.5f);
                column.Item().AlignCenter().Text($"Tel : {Model.FirmMobile ?? "-"}").FontSize(8);
            });
        }

        private void ComposePartyDetails(IContainer container)
        {
            container.BorderBottom(1).Row(row =>
            {
                // Left - Party Details
                row.RelativeItem().BorderRight(1).Padding(5).Column(col =>
                {
                    col.Item().Text("Party Details :").Bold().FontSize(10);
                    col.Item().Text(Model.CompanyName ?? Model.ClientName ?? "-").Bold();
                    col.Item().Text(Model.BillAddress ?? "-");
                    col.Item().Text($"GSTIN / UIN : {Model.GstNo ?? "-"}");
                });

                // Right - Quotation Details
                row.RelativeItem().Padding(5).Column(col =>
                {
                    col.Item().Row(r =>
                    {
                        r.ConstantItem(85).Text("Quotation No.");
                        r.RelativeItem().Text($": {Model.QuotationNo}").Bold();
                    });
                    col.Item().Row(r =>
                    {
                        r.ConstantItem(85).Text("Dated");
                        r.RelativeItem().Text($": {Model.QuotationDate.ToString("dd/MM/yyyy")}");
                    });
                });
            });
        }

        private void ComposeTable(IContainer container)
        {
            container.MinHeight(550).Layers(layers =>
            {
                // Background layer for vertical lines
                layers.Layer().Row(row =>
                {
                    row.ConstantItem(30).BorderRight(1);  // S.N.
                    row.RelativeItem().BorderRight(1);    // Description
                    row.ConstantItem(80).BorderRight(1);  // HSN
                    row.ConstantItem(50).BorderRight(1);  // Qty
                    row.ConstantItem(50).BorderRight(1);  // Unit
                    row.ConstantItem(70).BorderRight(1);  // Rate
                    row.ConstantItem(80);                  // Amount (Last column has no border-right)
                });

                // Foreground layer for actual content
                layers.PrimaryLayer().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);
                        columns.RelativeColumn();
                        columns.ConstantColumn(80);
                        columns.ConstantColumn(50);
                        columns.ConstantColumn(50);
                        columns.ConstantColumn(50);
                        columns.ConstantColumn(70);
                        columns.ConstantColumn(80);
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCellStyle).Text("S.N.");
                        header.Cell().Element(HeaderCellStyle).Text("Description of Goods");
                        header.Cell().Element(HeaderCellStyle).Text("HSN/SAC Code");
                        header.Cell().Element(HeaderCellStyle).Text("Qty.");
                        header.Cell().Element(HeaderCellStyle).Text("Unit");
                        header.Cell().Element(HeaderCellStyle).Text("Tax");
                        header.Cell().Element(HeaderCellStyle).Text("Rate");
                        header.Cell().Element(HeaderCellStyle).Text("Amount(`)");

                        static IContainer HeaderCellStyle(IContainer container)
                        {
                            return container.DefaultTextStyle(x => x.Bold()).BorderBottom(1).PaddingVertical(3).AlignCenter();
                        }
                    });

                    // Items
                    foreach (var item in Model.Items.Select((value, index) => new { value, index }))
                    {
                        table.Cell().Element(ItemCellStyle).Text((item.index + 1).ToString() + ".");
                        table.Cell().Element(ItemCellStyle).AlignLeft().Column(c => {
                            c.Item().Text(item.value.ProductName).Bold();
                            if (!string.IsNullOrEmpty(item.value.Description))
                                c.Item().Text(item.value.Description).FontSize(8);
                        });
                        table.Cell().Element(ItemCellStyle).Text(item.value.HsnCode ?? "-");
                        table.Cell().Element(ItemCellStyle).Text(item.value.Quantity.ToString("F2"));
                        table.Cell().Element(ItemCellStyle).Text(item.value.UnitName ?? "Pcs");
                        table.Cell().Element(ItemCellStyle).Text(item.value.TaxCategoryName ?? "-");
                        table.Cell().Element(ItemCellStyle).Text(item.value.UnitPrice.ToString("F2"));
                        table.Cell().Element(ItemCellStyle).Text(item.value.LineTotal.ToString("F2"));

                        static IContainer ItemCellStyle(IContainer container)
                        {
                            return container.PaddingHorizontal(5).PaddingVertical(3).AlignCenter();
                        }
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
                    col.Item().Text(NumberToWords((double)Model.GrandTotal)).Bold();
                });

                // Totals
                row.ConstantItem(200).BorderLeft(1).Padding(5).Column(col =>
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Sub Total");
                        r.RelativeItem().AlignRight().Text(Model.TotalAmount.ToString("F2"));
                    });
                    if (Model.Taxes > 0)
                    {
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Total Tax");
                            r.RelativeItem().AlignRight().Text(Model.Taxes.ToString("F2"));
                        });
                    }
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Grand Total").Bold();
                        r.RelativeItem().AlignRight().Text(Model.GrandTotal.ToString("F2")).Bold();
                    });
                });
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.BorderTop(1).PaddingHorizontal(5).PaddingVertical(1).Row(row =>
            {
                // Terms
                row.RelativeItem().Column(col =>
                {
                    col.Item().PaddingTop(10).Text("Terms & Conditions:").Bold().FontSize(8.5f);
                    var terms = Model.TermsAndConditions?.Split('\n') ?? new string[0];
                    int index = 1;
                    foreach (var term in terms)
                    {
                        if (!string.IsNullOrWhiteSpace(term))
                            col.Item().Text($"{index++}. {term.Trim()}").FontSize(7.5f);
                    }
                });

                // Signature
                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().PaddingTop(10).Text($"For {Model.FirmName?.ToUpper() ?? "AVINYA AI"}").Bold();
                    col.Item().PaddingTop(50).Text("Authorised Signatory").FontSize(8.5f);
                });
            });
        }

        private string NumberToWords(double number)
        {
            if (number == 0) return "Zero";
            if (number < 0) return "Minus " + NumberToWords(Math.Abs(number));

            long intPart = (long)number;
            int decimalPart = (int)Math.Round((number - intPart) * 100);

            string words = NumberToWordsHelper(intPart);
            if (decimalPart > 0)
            {
                words += " and " + NumberToWordsHelper(decimalPart) + " Paise";
            }

            return words;
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

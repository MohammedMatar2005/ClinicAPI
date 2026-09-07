    using System;
    using System.Collections.Generic;
    using ClinicAPIBusiness.DTO.InvoicesDTOs;
    using QuestPDF.Fluent;
    using QuestPDF.Helpers;
    using QuestPDF.Infrastructure;

    namespace ClinicAPIBusiness.Utils
    {
        public class ExportPDF
        {
            private const string ArabicFont = "Segoe UI";

            // =========================================================================
            // 1. تصدير فاتورة واحدة تفصيلية (Single Invoice Report)
            // =========================================================================
            public static void GenerateSingleInvoicePDF(InvoiceViewDTO invoice, string savePath)
            {
                if (invoice == null || string.IsNullOrEmpty(savePath)) return;

                // تفعيل رخصة المجتمع المجانية لـ QuestPDF
                QuestPDF.Settings.License = LicenseType.Community;

                QuestPDF.Fluent.Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);

                        // دعم اللغة العربية والاتجاه من اليمين لليسار
                        page.ContentFromRightToLeft();

                        // --- الترويسة (Header) ---
                        page.Header().Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("نظام إدارة العيادة الطبية").FontFamily(ArabicFont).FontSize(18).Bold().FontColor(Colors.Blue.Darken3);
                                col.Item().Text($"رقم الفاتورة: {invoice.InvoiceNumber}").FontFamily(ArabicFont).FontSize(11);
                                col.Item().Text($"تاريخ الإصدار: {invoice.InvoiceDate:yyyy/MM/dd}").FontFamily(ArabicFont).FontSize(11);
                            });

                            row.ConstantItem(100).AlignMiddle().Text("").FontFamily(ArabicFont).FontSize(12).Bold().FontColor(Colors.Grey.Medium);
                        });

                        // --- المحتوى (Content) ---
                        page.Content().PaddingVertical(1, Unit.Centimetre).Column(column =>
                        {
                            // بيانات المريض الأساسية
                            column.Item().Background(Colors.Grey.Lighten3).Padding(10).Row(row =>
                            {
                                row.RelativeItem().Text($"السيد/ة: {invoice.PatientFullName}").FontFamily(ArabicFont).FontSize(12).Bold();
                                row.RelativeItem().Text($"تاريخ الاستحقاق: {invoice.DueDate:yyyy/MM/dd}").FontFamily(ArabicFont).FontSize(11).AlignLeft();
                            });

                            column.Item().PaddingTop(20);

                            // جدول بنود الرسوم المالية للفاتورة المحددة
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("الخدمة / البيان").FontFamily(ArabicFont).FontColor(Colors.White).Bold();
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("المبلغ").FontFamily(ArabicFont).FontColor(Colors.White).Bold().AlignLeft();
                                });

                                table.Cell().BorderBottom(1, Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("رسوم الكشفية / الاستشارة").FontFamily(ArabicFont);
                                table.Cell().BorderBottom(1, Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{invoice.ConsultationFee:N2}$").FontFamily(ArabicFont).AlignLeft();

                                table.Cell().BorderBottom(1, Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("رسوم الفحوصات المخبرية").FontFamily(ArabicFont);
                                table.Cell().BorderBottom(1, Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{invoice.LabTestFee:N2}$").FontFamily(ArabicFont).AlignLeft();

                                table.Cell().BorderBottom(1, Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("رسوم العمليات / الإجراءات الطبية").FontFamily(ArabicFont);
                                table.Cell().BorderBottom(1, Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{invoice.ProcedureFee:N2}$").FontFamily(ArabicFont).AlignLeft();

                                table.Cell().BorderBottom(1, Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("مصاريف أخرى").FontFamily(ArabicFont);
                                table.Cell().BorderBottom(1, Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{invoice.OtherCharges:N2}$").FontFamily(ArabicFont).AlignLeft();
                            });

                            column.Item().PaddingTop(20);

                            // الحسابات المالية الإجمالية والخصم
                            column.Item().AlignLeft().Width(200).Column(summaryCol =>
                            {
                                summaryCol.Item().Row(r => { r.RelativeItem().Text("المجموع الفرعي:").FontFamily(ArabicFont); r.ConstantItem(70).Text($"{invoice.SubTotal:N2}$").FontFamily(ArabicFont).AlignLeft(); });
                                summaryCol.Item().Row(r => { r.RelativeItem().Text($"الخصم ({invoice.DiscountPercentage}%):").FontFamily(ArabicFont).FontColor(Colors.Red.Medium); r.ConstantItem(70).Text($"-{invoice.DiscountAmount:N2}$").FontFamily(ArabicFont).FontColor(Colors.Red.Medium).AlignLeft(); });
                                summaryCol.Item().Row(r => { r.RelativeItem().Text($"الضريبة ({invoice.TaxPercentage}%):").FontFamily(ArabicFont); r.ConstantItem(70).Text($"+{invoice.TaxAmount:N2}$").FontFamily(ArabicFont).AlignLeft(); });

                                summaryCol.Item().PaddingVertical(5).LineHorizontal(1);

                                summaryCol.Item().Row(r => { r.RelativeItem().Text("المبلغ الإجمالي:").FontFamily(ArabicFont).Bold().FontSize(14); r.ConstantItem(70).Text($"{invoice.FinalAmount:N2}$").FontFamily(ArabicFont).Bold().FontSize(14).FontColor(Colors.Green.Darken2).AlignLeft(); });
                            });

                            column.Item().PaddingTop(30).Text($"حالة الفاتورة: {invoice.StatusName}").FontFamily(ArabicFont).Bold().FontColor(invoice.InvoiceStatusId == 1 ? Colors.Green.Medium : Colors.Red.Medium);
                        });

                        // --- التذييل (Footer) ---
                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.CurrentPageNumber().FontFamily(ArabicFont);
                            x.Span(" / ").FontFamily(ArabicFont);
                            x.TotalPages().FontFamily(ArabicFont);
                        });
                    });
                }).GeneratePdf(savePath);
            }

            // =========================================================================
        // 2. تصدير كافة الفواتير كجدول شامل (All Invoices Grid Report)
        // =========================================================================
            public static void GenerateAllInvoicesTablePDF(List<InvoiceViewDTO> invoicesList, string savePath)
        {
            if (invoicesList == null || invoicesList.Count == 0 || string.IsNullOrEmpty(savePath)) return;

            // تصحيح نوع الرخصة إلى الإصدار المعتمد مجتمعياً
            QuestPDF.Settings.License = LicenseType.Community;

            QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape()); // وضعية أفقية لتناسب التمدد العرضي
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.ContentFromRightToLeft(); // ضبط اتجاه القراءة العربي الصحيح

                    // --- الترويسة الشقراء (Header) ---
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("تقرير الفواتير الشامل").FontFamily(ArabicFont).FontSize(20).Bold().FontColor(Colors.Blue.Darken3);
                            col.Item().PaddingTop(4).Text($"تاريخ استخراج التقرير: {DateTime.Now:yyyy/MM/dd}").FontFamily(ArabicFont).FontSize(10).FontColor(Colors.Grey.Darken1);
                            col.Item().Text($"إجمالي عدد الفواتير: {invoicesList.Count}").FontFamily(ArabicFont).FontSize(10).FontColor(Colors.Grey.Darken1);
                        });
                    });

                    // --- محتوى الجدول المنظم (Content) ---
                    page.Content().PaddingVertical(1, Unit.Centimetre).Table(table =>
                    {
                        // 1. تعريف مساحات الـ 6 أعمدة (يجب أن تطابق عدد الخلايا بالأسفل تماماً)
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.0f); // معرف الفاتورة
                            columns.RelativeColumn(3.0f); // اسم المريض
                            //columns.RelativeColumn(2.0f); // الرقم الوطني
                            columns.RelativeColumn(1.5f); // المبلغ
                            columns.RelativeColumn(2.0f); // تاريخ الفاتورة
                            columns.RelativeColumn(1.5f); // رقم الفاتورة
                        });

                        // 2. تصميم عناوين رأس الجدول
                        table.Header(header =>
                        {
                            var headerStyle = TextStyle.Default.FontFamily(ArabicFont).FontColor(Colors.White).Bold().FontSize(11);
                            var headerCellBackground = Colors.Blue.Darken3;

                            header.Cell().Background(headerCellBackground).Padding(8).Text("معرف الفاتورة").Style(headerStyle);
                            header.Cell().Background(headerCellBackground).Padding(8).Text("اسم المريض").Style(headerStyle);
                           // header.Cell().Background(headerCellBackground).Padding(8).Text("الرقم الوطني").Style(headerStyle);
                            header.Cell().Background(headerCellBackground).Padding(8).Text("المبلغ").Style(headerStyle);
                            header.Cell().Background(headerCellBackground).Padding(8).Text("تاريخ الفاتورة").Style(headerStyle);
                            header.Cell().Background(headerCellBackground).Padding(8).Text("رقم الفاتورة").Style(headerStyle);
                        });

                        // 3. ضخ الخلايا بالترتيب الدقيق الحذر لمنع الانزياح العشوائي
                        foreach (var inv in invoicesList)
                        {
                            var cellStyle = TextStyle.Default.FontFamily(ArabicFont).FontSize(10);
                            var borderColor = Colors.Grey.Lighten2;
                            var paddingAmount = 8; // زيادة البادينج قليلاً لراحة العين عند القراءة

                            // العمود 1: معرف الفاتورة
                            table.Cell().BorderBottom(1, Unit.Point).BorderColor(borderColor).Padding(paddingAmount).Text($"{inv.InvoiceId}").Style(cellStyle);

                            // العمود 2: اسم المريض
                            table.Cell().BorderBottom(1, Unit.Point).BorderColor(borderColor).Padding(paddingAmount).Text(inv.PatientFullName ?? "غير معرف").Style(cellStyle).Bold();

                            // العمود 4: المبلغ المالي النهائي
                            table.Cell().BorderBottom(1, Unit.Point).BorderColor(borderColor).Padding(paddingAmount).Text($"{inv.FinalAmount:N2}$").Style(cellStyle);

                            // العمود 5: تاريخ الفاتورة
                            table.Cell().BorderBottom(1, Unit.Point).BorderColor(borderColor).Padding(paddingAmount).Text($"{inv.InvoiceDate:yyyy/MM/dd}").Style(cellStyle);

                            // العمود 6: رقم الفاتورة الخارجي
                            table.Cell().BorderBottom(1, Unit.Point).BorderColor(borderColor).Padding(paddingAmount).Text(inv.InvoiceNumber ?? "-").Style(cellStyle);
                        }
                    });

                    // --- التذييل التلقائي (Footer) ---
                    page.Footer().AlignCenter().PaddingTop(1, Unit.Centimetre).Text(x =>
                    {
                        x.CurrentPageNumber().FontFamily(ArabicFont).FontSize(10);
                        x.Span(" / ").FontFamily(ArabicFont).FontSize(10);
                        x.TotalPages().FontFamily(ArabicFont).FontSize(10);
                    });
                });
            }).GeneratePdf(savePath);
        }
    }
    }
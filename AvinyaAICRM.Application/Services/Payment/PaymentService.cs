using AvinyaAICRM.Application.DTOs.Payment;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Invoices;
using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Payment;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Payment;
using AvinyaAICRM.Domain.Entities.Invoice;
using AvinyaAICRM.Shared.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Services.Payment
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IInvoiceRepository _invoiceRepository;

        public PaymentService(IPaymentRepository paymentRepository, IInvoiceRepository invoiceRepository)
        {
            _paymentRepository = paymentRepository;
            _invoiceRepository = invoiceRepository;
        }

        public async Task<ResponseModel> GetPaymentByIdAsync(Guid paymentId)
        {
            try
            {
                var payment = await _paymentRepository.GetPaymentByIdAsync(paymentId);
                if (payment == null)
                    return new ResponseModel(404, "Payment not found.");

                return new ResponseModel
                {
                    StatusCode = 200,
                    StatusMessage = "Success",
                    Data = MapToDto(payment)
                };
            }
            catch (Exception ex)
            {
                return new ResponseModel(500, ex.Message);
            }
        }

        public async Task<ResponseModel> GetPaymentsByInvoiceIdAsync(Guid invoiceId)
        {
            try
            {
                var allPayments = await _paymentRepository.GetPaymentsByInvoiceIdAsync(invoiceId);
                var paymentDtos = allPayments.Select(MapToDto).ToList();

                return new ResponseModel
                {
                    StatusCode = 200,
                    StatusMessage = "Success",
                    Data = paymentDtos
                };
            }
            catch (Exception ex)
            {
                return new ResponseModel(500, ex.Message);
            }
        }

        public async Task<ResponseModel> CreatePaymentAsync(CreatePaymentDto dto, string tenantId, string userId)
        {
            try
            {
                var invoice = await _invoiceRepository.GetInvoiceByIdAsync(dto.InvoiceID, tenantId);
                if (invoice == null)
                    return new ResponseModel(404, "Invoice not found or access denied.");

                var payment = new Domain.Entities.Payments.Payment
                {
                    PaymentID = Guid.NewGuid(),
                    InvoiceID = dto.InvoiceID,
                    PaymentDate = dto.PaymentDate,
                    Amount = dto.Amount,
                    PaymentMode = dto.PaymentMode,
                    TransactionRef = dto.TransactionRef,
                    ReceivedBy = userId,
                };

                var created = await _paymentRepository.AddPaymentAsync(payment);

                // Recalculate Invoice Amounts from sum of all payments
                await UpdateInvoiceTotalsAsync(dto.InvoiceID, tenantId, invoice);

                return new ResponseModel
                {
                    StatusCode = 200,
                    StatusMessage = "Payment recorded successfully",
                    Data = MapToDto(created)
                };
            }
            catch (Exception ex)
            {
                return new ResponseModel(500, ex.Message);
            }
        }

        public async Task<ResponseModel> UpdatePaymentAsync(UpdatePaymentDto dto, string tenantId)
        {
            try
            {
                var existing = await _paymentRepository.GetPaymentByIdAsync(dto.PaymentID);
                if (existing == null)
                    return new ResponseModel(404, "Payment not found or access denied.");

                existing.PaymentDate = dto.PaymentDate;
                existing.Amount = dto.Amount;
                existing.PaymentMode = dto.PaymentMode;
                existing.TransactionRef = dto.TransactionRef;

                var updated = await _paymentRepository.UpdatePaymentAsync(existing);

                // Recalculate Invoice Amounts
                var invoice = await _invoiceRepository.GetInvoiceByIdAsync(existing.InvoiceID, tenantId);
                if (invoice != null)
                {
                    await UpdateInvoiceTotalsAsync(invoice.InvoiceID, tenantId, invoice);
                }

                return new ResponseModel
                {
                    StatusCode = 200,
                    StatusMessage = "Payment updated successfully",
                    Data = MapToDto(updated)
                };
            }
            catch (Exception ex)
            {
                return new ResponseModel(500, ex.Message);
            }
        }

        private async Task UpdateInvoiceTotalsAsync(Guid invoiceId, string tenantId, Domain.Entities.Invoice.Invoice invoice)
        {
            var allPayments = await _paymentRepository.GetPaymentsByInvoiceIdAsync(invoiceId);
            invoice.PaidAmount = allPayments.Sum(p => p.Amount);
            invoice.AmountAfterDiscount = invoice.GrandTotal - invoice.Discount;
            invoice.RemainingPayment = invoice.AmountAfterDiscount - invoice.PaidAmount;

            // Optional: Auto-update status based on balance
            if (invoice.RemainingPayment <= 0) invoice.InvoiceStatusID = 3; // Assuming 3 is 'Paid'
            else if (invoice.PaidAmount > 0) invoice.InvoiceStatusID = 2; // Assuming 2 is 'Partially Paid'

            await _invoiceRepository.UpdateInvoiceAsync(invoice);
        }

        private PaymentDto MapToDto(Domain.Entities.Payments.Payment payment)
        {
            return new PaymentDto
            {
                PaymentID = payment.PaymentID,
                InvoiceID = payment.InvoiceID,
                PaymentDate = payment.PaymentDate,
                Amount = payment.Amount,
                PaymentMode = payment.PaymentMode,
                TransactionRef = payment.TransactionRef,
                ReceivedBy = payment.ReceivedBy
            };
        }
    }
}

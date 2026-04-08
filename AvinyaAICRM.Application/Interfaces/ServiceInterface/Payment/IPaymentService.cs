using AvinyaAICRM.Application.DTOs.Payment;
using AvinyaAICRM.Shared.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.Payment
{
    public interface IPaymentService
    {
        Task<ResponseModel> GetPaymentsByInvoiceIdAsync(Guid invoiceId);
        Task<ResponseModel> GetPaymentByIdAsync(Guid paymentId);
        Task<ResponseModel> CreatePaymentAsync(CreatePaymentDto dto, string tenantId, string userId);
        Task<ResponseModel> UpdatePaymentAsync(UpdatePaymentDto dto, string tenantId);
    }
}

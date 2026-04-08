using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.Payment
{
    public interface IPaymentRepository
    {
        Task<IEnumerable<Domain.Entities.Payments.Payment>> GetPaymentsByInvoiceIdAsync(Guid invoiceId);
        Task<Domain.Entities.Payments.Payment?> GetPaymentByIdAsync(Guid paymentId);
        Task<Domain.Entities.Payments.Payment> AddPaymentAsync(Domain.Entities.Payments.Payment payment);
        Task<Domain.Entities.Payments.Payment> UpdatePaymentAsync(Domain.Entities.Payments.Payment payment);
    }
}

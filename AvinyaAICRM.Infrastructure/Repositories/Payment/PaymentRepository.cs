using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Payment;
using AvinyaAICRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AvinyaAICRM.Infrastructure.Repositories.Payment
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly AppDbContext _context;

        public PaymentRepository(AppDbContext context)
        {
            _context = context;
        }

       
        public async Task<IEnumerable<Domain.Entities.Payments.Payment>> GetPaymentsByInvoiceIdAsync(Guid invoiceId)
        {
            return await _context.Payments
                .Where(p => p.InvoiceID == invoiceId)
                .ToListAsync();
        }

        public async Task<Domain.Entities.Payments.Payment?> GetPaymentByIdAsync(Guid paymentId)
        {
            return await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentID == paymentId );
        }

        public async Task<Domain.Entities.Payments.Payment> AddPaymentAsync(Domain.Entities.Payments.Payment payment)
        {
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<Domain.Entities.Payments.Payment> UpdatePaymentAsync(Domain.Entities.Payments.Payment payment)
        {
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

       
    }
}

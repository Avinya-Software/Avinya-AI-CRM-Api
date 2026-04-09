using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Invoice;
using AvinyaAICRM.Domain.Entities.Invoice;
using AvinyaAICRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AvinyaAICRM.Infrastructure.Repositories.Invoice
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly AppDbContext _context;

        public InvoiceRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Domain.Entities.Invoice.Invoice>> GetAllInvoicesAsync(string tenantId)
        {
            return await _context.Invoices
                .Where(i => i.TenantId == tenantId && !i.IsDeleted)
                .ToListAsync();
        }

        public async Task<Domain.Entities.Invoice.Invoice?> GetInvoiceByIdAsync(Guid invoiceId, string tenantId)
        {
            return await _context.Invoices
                .FirstOrDefaultAsync(i => i.InvoiceID == invoiceId && i.TenantId == tenantId && !i.IsDeleted);
        }

        public async Task<Domain.Entities.Invoice.Invoice> AddInvoiceAsync(Domain.Entities.Invoice.Invoice invoice)
        {
            _context.Invoices.Add(invoice);

            // ✅ Convert string → Guid
            if (Guid.TryParse(invoice.OrderID.ToString(), out Guid orderId))
            {
                var order = await _context.Orders.FindAsync(orderId);

                if (order != null)
                {
                    order.isInvoiceCreated = true;
                }
            }
            else
            {
                throw new Exception("Invalid OrderID format");
            }

            await _context.SaveChangesAsync();
            return invoice;
        }

        public async Task<Domain.Entities.Invoice.Invoice> UpdateInvoiceAsync(Domain.Entities.Invoice.Invoice invoice)
        {
            _context.Invoices.Update(invoice);
            await _context.SaveChangesAsync();
            return invoice;
        }

        public async Task<bool> DeleteInvoiceAsync(Guid invoiceId, string tenantId)
        {
            var invoice = await GetInvoiceByIdAsync(invoiceId, tenantId);
            if (invoice == null) return false;

            invoice.IsDeleted = true;
            invoice.DeletedDate = DateTime.Now;
            _context.Invoices.Update(invoice);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<AvinyaAICRM.Shared.Model.PagedResult<AvinyaAICRM.Application.DTOs.Invoice.InvoiceDto>> GetFilteredAsync(
            string? search,
            string? statusFilter,
            DateTime? startDate,
            DateTime? endDate,
            int pageNumber,
            int pageSize,
            string userId)
        {
            var userData = await _context.Users.FindAsync(userId);
            if (userData == null) throw new Exception("User not found");

            var query = _context.Invoices
                .Where(i => !i.IsDeleted && i.TenantId == userData.TenantId.ToString())
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var likeSearch = $"%{search}%";
                query = from i in query
                        join c in _context.Clients on i.ClientID equals c.ClientID.ToString() into invoiceClient
                        from client in invoiceClient.DefaultIfEmpty()
                        where EF.Functions.Like(i.InvoiceNo, likeSearch) ||
                              EF.Functions.Like(client.CompanyName ?? "", likeSearch) ||
                              EF.Functions.Like(client.ContactPerson ?? "", likeSearch)
                        select i;
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                if (int.TryParse(statusFilter, out int statusId))
                {
                    query = query.Where(i => i.InvoiceStatusID == statusId);
                }
                else
                {
                    var statuses = await _context.InvoiceStatuses.ToListAsync();
                    var matchedIds = statuses
                        .Where(s => s.InvoiceStatusName.Contains(statusFilter, StringComparison.OrdinalIgnoreCase))
                        .Select(s => s.InvoiceStatusID)
                        .ToList();
                    query = query.Where(i => matchedIds.Contains(i.InvoiceStatusID));
                }
            }

            if (startDate.HasValue)
                query = query.Where(i => i.InvoiceDate >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(i => i.InvoiceDate <= endDate.Value);

            var totalRecords = await query.CountAsync();
            
            var invoicesPaged = await query
                .OrderByDescending(i => i.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Mapping to DTO with Client and Status details
            var clients = await _context.Clients.ToListAsync();
            var statusesList = await _context.InvoiceStatuses.ToListAsync();

            DateTime ConvertUtcToLocal(DateTime utc) =>
                   TimeZoneInfo.ConvertTimeFromUtc(
                       utc,
                       TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

            var dataItems = invoicesPaged.Select(i => new AvinyaAICRM.Application.DTOs.Invoice.InvoiceDto
            {
                InvoiceID = i.InvoiceID,
                InvoiceNo = i.InvoiceNo,
                OrderID = i.OrderID,
                ClientID = i.ClientID,
                InvoiceDate = i.InvoiceDate,
                SubTotal = i.SubTotal,
                Taxes = i.Taxes,
                Discount = i.Discount,
                GrandTotal = i.GrandTotal,
                InvoiceStatusID = i.InvoiceStatusID,
                CreatedDate = ConvertUtcToLocal(i.CreatedDate),
                RemainingPayment = i.RemainingPayment,
                PaidAmount = i.PaidAmount,
                PlaceOfSupply = i.PlaceOfSupply,
                ReverseCharge = i.ReverseCharge,
                GRRRNo = i.GRRRNo,
                DueDate = i.DueDate,
                Transport = i.Transport,
                VehicleNo = i.VehicleNo,
                Station = i.Station,
                EWayBillNo = i.EWayBillNo,
                OutstandingAmount = i.OutstandingAmount,
                CompanyName = clients.FirstOrDefault(c => c.ClientID.ToString() == i.ClientID)?.CompanyName,
                ContactPerson = clients.FirstOrDefault(c => c.ClientID.ToString() == i.ClientID)?.ContactPerson,
                StatusName = statusesList.FirstOrDefault(s => s.InvoiceStatusID == i.InvoiceStatusID)?.InvoiceStatusName
            }).ToList();

            return new AvinyaAICRM.Shared.Model.PagedResult<AvinyaAICRM.Application.DTOs.Invoice.InvoiceDto>
            {
                Data = dataItems,
                TotalRecords = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<IEnumerable<InvoiceStatus>> GetAllInvoiceStatusesAsync()
        {
            return await _context.InvoiceStatuses.ToListAsync();
        }
    }
}

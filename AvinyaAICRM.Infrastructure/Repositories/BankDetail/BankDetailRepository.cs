using AvinyaAICRM.Application.Interfaces.RepositoryInterface.BankDetail;
using AvinyaAICRM.Domain.Entities.BankDetail;
using AvinyaAICRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;


namespace AvinyaAICRM.Infrastructure.Repositories.BankDetail
{
    public class BankDetailRepository : IBankDetailRepository
    {
        private readonly AppDbContext _context;

        public BankDetailRepository(AppDbContext context)
        {
            _context = context;
        }
     
        public async Task<BankDetails> createbankdetail(BankDetails bankDetails)
        {
                await _context.BankDetails.AddAsync(bankDetails);
                await _context.SaveChangesAsync();
                return bankDetails;
        }

        public async Task<bool> DeleteBankDetail(Guid bankAccountId)
        {
            try
            {
                var bankDetail = await _context.BankDetails
                    .FirstOrDefaultAsync(b => b.BankAccountId == bankAccountId);

                if (bankDetail == null)
                    return false;

                _context.BankDetails.Remove(bankDetail);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<IEnumerable<BankDetails>> GetBankDetails(string TenantId)
        {
            return await _context.BankDetails
                   .Where(x => x.TenantId == TenantId)
                   .ToListAsync();
        }

        public async Task<BankDetails> Updatebankdatail(BankDetails bankDetails)
        {
            _context.BankDetails.Update(bankDetails);
            await _context.SaveChangesAsync();
            return bankDetails;
        }
    }
}

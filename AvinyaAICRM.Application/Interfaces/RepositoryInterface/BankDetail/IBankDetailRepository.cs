using AvinyaAICRM.Domain.Entities.BankDetail;

namespace AvinyaAICRM.Application.Interfaces.RepositoryInterface.BankDetail
{
    public interface IBankDetailRepository
    {
         Task<BankDetails> createbankdetail(BankDetails bankDetails);
         Task<BankDetails> Updatebankdatail(BankDetails bankDetails);
         Task<bool> DeleteBankDetail(Guid bankAccountId);
         Task<IEnumerable<BankDetails>> GetBankDetails(string TenantId);
    }
}

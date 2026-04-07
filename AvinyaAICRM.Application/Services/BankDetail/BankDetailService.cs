using AvinyaAICRM.Application.Interfaces.RepositoryInterface.BankDetail;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.BankDetail;
using AvinyaAICRM.Domain.Entities.BankDetail;
using AvinyaAICRM.Shared.Helper;
using AvinyaAICRM.Shared.Model;


namespace AvinyaAICRM.Application.Services.BankDetail
{
    public class BankDetailService : IBankDetailService
    {
        private readonly IBankDetailRepository _bankDetailRepository;
        public BankDetailService(IBankDetailRepository bankDetailRepository)
        {
          _bankDetailRepository = bankDetailRepository;   
        }

        public async Task<ResponseModel> createbankdetail(BankDetails bankDetails)
        {
            var result = await _bankDetailRepository.createbankdetail(bankDetails);
            return CommonHelper.GetResponseMessage(result);
        }

        public async Task<ResponseModel> DeleteBankDetail(Guid bankAccountId)
        {
            var result = await _bankDetailRepository.DeleteBankDetail(bankAccountId);
            return CommonHelper.GetResponseMessage(result);
        }

        public async Task<ResponseModel> GetBankDetails(string TenantId)
        {
            var result = await _bankDetailRepository.GetBankDetails(TenantId);
            return CommonHelper.GetResponseMessage(result);
        }

        public async Task<ResponseModel> Updatebankdatail(BankDetails bankDetails)
        {
            var result = await _bankDetailRepository.Updatebankdatail(bankDetails);
            return CommonHelper.GetResponseMessage(result);
        }
    }
}

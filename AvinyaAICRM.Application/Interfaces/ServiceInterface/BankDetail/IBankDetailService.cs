using AvinyaAICRM.Domain.Entities.BankDetail;
using AvinyaAICRM.Shared.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvinyaAICRM.Application.Interfaces.ServiceInterface.BankDetail
{
    public interface IBankDetailService
    {
        Task<ResponseModel> createbankdetail(BankDetails bankDetails);
        Task<ResponseModel> Updatebankdatail(BankDetails bankDetails);
        Task<ResponseModel> DeleteBankDetail(Guid bankAccountId);
        Task<ResponseModel> GetBankDetails(string TenantId);
    }
}

using AvinyaAICRM.Application.Interfaces.ServiceInterface.BankDetail;
using AvinyaAICRM.Domain.Entities.BankDetail;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvinyaAICRM.API.Controllers.BankDetail
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class BankDetailController : ControllerBase
    {
        private readonly IBankDetailService _bankDetailService;
        public BankDetailController(IBankDetailService bankDetailService)
        {
            _bankDetailService = bankDetailService;
        }

        [HttpPost("add")]
        public async Task<IActionResult> CreateBankDetails(BankDetails bankDetails) 
        {
            var result = await _bankDetailService.createbankdetail(bankDetails);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("update")]
        public async Task<IActionResult> Updatebankdatail(BankDetails bankDetails)
        {
            var result = await _bankDetailService.Updatebankdatail(bankDetails);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("deleted")]
        public async Task<IActionResult> DeleteBankDetail(Guid bankAccountId) 
        {
            var result = await _bankDetailService.DeleteBankDetail(bankAccountId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("getbankdatail")]
        public async Task<IActionResult> GetBankDetails(string TenantId) 
        {
            var result = await _bankDetailService.GetBankDetails(TenantId);
            return StatusCode(result.StatusCode, result);
        }
    }
}

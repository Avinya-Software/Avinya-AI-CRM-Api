using AvinyaAICRM.Application.Interfaces.RepositoryInterface.Quotations;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.Quotations;
using AvinyaAICRM.Domain.Entities.Quotations;
using AvinyaAICRM.Shared.Helper;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Services.Quotations
{
    public class QuotationItemService : IQuotationItemService
    {
        private readonly IQuotationItemRepository _repository;

        public QuotationItemService(IQuotationItemRepository repository)
        {
            _repository = repository;
        }

        // ✅ Get All
        public async Task<ResponseModel> GetAllAsync(Guid? quotationId = null)
        {
            try
            {
                var data = await _repository.GetAllAsync(quotationId);

                if (quotationId.HasValue &&
                    quotationId != Guid.Empty &&
                    !data.Any())
                {
                    return new ResponseModel(
                        404,
                        "No quotation items found for the given quotation ID.");
                }

                return CommonHelper.GetResponseMessage(data);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }

        // ✅ Get By Id
        public async Task<ResponseModel> GetByIdAsync(Guid id)
        {
            try
            {
                var data = await _repository.GetByIdAsync(id);

                if (data == null)
                    return new ResponseModel(404, "Quotation item not found");

                return CommonHelper.GetResponseMessage(data);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }

        // ✅ Create
        public async Task<ResponseModel> CreateAsync(QuotationItem dto)
        {
            try
            {
                await _repository.AddAsync(dto);

                return CommonHelper.SuccessResponseMessage(
                    "Quotation item created successfully",
                    dto);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }

        // ✅ Update
        public async Task<ResponseModel> UpdateAsync(Guid id, QuotationItem dto)
        {
            try
            {
                if (id == Guid.Empty)
                    return new ResponseModel(400, "Invalid Quotation Item ID");

                dto.QuotationItemID = id;

                var updatedEntity = await _repository.UpdateAsync(dto);

                if (updatedEntity == null)
                    return new ResponseModel(404, "Quotation item not found");

                var detailed = await _repository.GetByIdAsync(id);

                return CommonHelper.SuccessResponseMessage(
                    "Quotation item updated successfully",
                    detailed);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }

        // ✅ Delete
        public async Task<ResponseModel> DeleteAsync(Guid id)
        {
            try
            {
                var deleted = await _repository.DeleteAsync(id);

                if (!deleted)
                    return new ResponseModel(404, "Quotation item not found");

                return CommonHelper.SuccessResponseMessage(
                    "Quotation item deleted successfully",
                    null);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }

        // ✅ Filter + Pagination
        public async Task<ResponseModel> GetFilteredAsync(
            string? search,
            Guid? statusId,
            int page,
            int pageSize)
        {
            try
            {
                var result =
                    await _repository.GetFilteredAsync(search, statusId, page, pageSize);

                return CommonHelper.GetResponseMessage(result);
            }
            catch (Exception ex)
            {
                return CommonHelper.ExceptionMessage(ex);
            }
        }
    }
}
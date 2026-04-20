using AvinyaAICRM.Application.Interfaces.RepositoryInterface.TaxCategories;
using AvinyaAICRM.Application.Interfaces.ServiceInterface.TaxCategories;
using AvinyaAICRM.Shared.Model;

namespace AvinyaAICRM.Application.Services.TaxCategories
{
    public class TaxCategoryService : ITaxCategoryService
    {
        private readonly ITaxCategoryRepository _taxCategoryRepository;

        public TaxCategoryService(ITaxCategoryRepository taxCategoryRepository)
        {
            _taxCategoryRepository = taxCategoryRepository;
        }

        public async Task<ResponseModel> GetAllAsync()
        {
            var data = await _taxCategoryRepository.GetAllAsync();
            return new ResponseModel
            {
                StatusCode = 200,
                StatusMessage = "Tax categories fetched successfully.",
                Data = data
            };
        }

        public async Task<ResponseModel> GetByIdAsync(Guid id)
        {
            var data = await _taxCategoryRepository.GetByIdAsync(id);
            if (data == null)
            {
                return new ResponseModel
                {
                    StatusCode = 404,
                    StatusMessage = "Tax category not found."
                };
            }

            return new ResponseModel
            {
                StatusCode = 200,
                StatusMessage = "Tax category fetched successfully.",
                Data = data
            };
        }
    }
}

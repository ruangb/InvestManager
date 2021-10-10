using System.Collections.Generic;
using System.Threading.Tasks;
using InvestManager.Models;

namespace InvestManager.Manager.Repositories
{
    public interface IParameterRepository
    {
        Task<List<Parameter>> FindAllAsync();

        Task InsertAsync(Parameter obj);

        Task<Parameter> FindByIdAsync(int id);

        Task RemoveAsync(int id);

        Task UpdateAsync(Parameter obj);
    }
}

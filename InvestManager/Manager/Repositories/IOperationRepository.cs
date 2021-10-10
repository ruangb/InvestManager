using InvestManager.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InvestManager.Manager.Repositories
{
    public interface IOperationRepository
    {
        Task<List<Operation>> FindAllAsync();

        Task<int> InsertAsync(Operation obj);

        Task<bool> InsertAsync(IList<Operation> obj);

        Task<Operation> FindByIdAsync(int id);

        Task RemoveAsync(int id);

        Task UpdateAsync(Operation obj);

        void Keep(Operation obj);

        IList<Operation> GetRentabilityPerPeriod(Operation operation, List<Operation> operations, List<Parameter> parameters);

        IList<Operation> WalletProcess(List<Operation> operations);
    }
}

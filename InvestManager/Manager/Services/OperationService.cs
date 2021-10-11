using InvestManager.Manager.Repositories;
using InvestManager.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace InvestManager.Services
{
    public class OperationService : IOperationRepository
    {
        private readonly InvestManagerContext _context;

        public OperationService(InvestManagerContext context)
        {
            _context = context;
        }

        public async Task<List<Operation>> FindAllAsync()
        {
            var listOperation =  await _context.Operation.ToListAsync();

            foreach (var item in listOperation)
            {
                item.InvestValue = item.Quantity * item.Price;
            }

            listOperation = listOperation.OrderBy(x => x.Date).ThenBy(x => x.Asset).ThenByDescending(x => x.Price).ToList();

            return listOperation;
        }

        public async Task<int> InsertAsync(Operation obj)
        {
            _context.Add(obj);
            return await _context.SaveChangesAsync();
        }

        public async Task<bool> InsertAsync(IList<Operation> obj)
        {
            foreach (var item in obj)
            {
                _context.Add(item);
            }
            
            try
            {
                var affectedRows = await _context.SaveChangesAsync();

                return (obj.Count == affectedRows);
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<Operation> FindByIdAsync(int id)
        {
            return await _context.Operation.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task RemoveAsync(int id)
        {
            try
            {
                _context.Operation.Remove(await _context.Operation.FindAsync(id));
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException e)
            {
                // implementar erro
            }
        }

        public async Task UpdateAsync(Operation obj)
        {
            bool hasAny = await _context.Operation.AnyAsync(x => x.Id == obj.Id);
            if (!hasAny)
                throw new NotFoundException("Id not found");

            try
            {
                _context.Update(obj);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException e)
            {
                throw new DBConcurrencyException(e.Message);
            }
        }

        public void Keep(Operation obj)
        {
            if (_context.Operation.Any(x => x.Id == 0))
            {
                _context.Operation.Remove(_context.Operation.Where(x => x.Id == 0).First());
            }

            _context.Add(obj);
        }

        public IList<Operation> GetRentabilityPerPeriod(Operation operation, List<Operation> operations, List<Parameter> parameters)
        {
            Operation operationReturn = new Operation();

            DateTime startDate;
            DateTime endDate;

            if (operation.ReferenceMonth != null)
            {
                string monthIndex = Enums.GetIndexByDescription(Enums.Month.None, operation.ReferenceMonth).ToString();

                if (monthIndex.Length == 1)
                    monthIndex = "0" + monthIndex;

                startDate = Convert.ToDateTime($"{"01/"}{monthIndex}{"/"}{operation.ReferenceYear}");
                endDate = startDate.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);
            }
            else
            {
                startDate = Convert.ToDateTime($"{"01/01/"}{operation.ReferenceYear}");
                endDate = startDate.AddYears(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);
            }

            operations = operations.Where(x => x.Date <= endDate).ToList();

            IList<Operation> listWallet    = WalletProcess(operations);
            IList<Operation> listOperation = new List<Operation>();

            decimal rentabilityTotalValue = 0;
            decimal rentabilityTotalPercentage = 0;
            int registerSoldQuantity = 0;

            var listGroup = operations.GroupBy(x => x.Asset);

            foreach (var item in listGroup)
            {
                int soldQuantity = operations.Where(x => x.Asset == item.Key
                                                && x.Type == Enums.OperationType.Sale.GetDescription()
                                                && x.Date >= startDate
                                                && x.Date <= endDate).Sum(x => x.Quantity);

                if (soldQuantity > 0)
                {
                    int count = 0;
                    decimal purchaseTotalPrice = 0;
                    decimal soldTotalPrice = 0;
                    int purchaseQuantity = 0;
                    soldQuantity = 0;

                    IList<Operation> listByAsset = operations.Where(x => x.Asset == item.Key
                                                                && ((x.Type == Enums.OperationType.Purchase.GetDescription()
                                                                && x.Date <= endDate)
                                                                    || (x.Type == Enums.OperationType.Sale.GetDescription()
                                                                    && x.Date < startDate))).OrderBy(x => x.Date).ToList();

                    foreach (var op in listByAsset)
                    {
                        if (count == 0 && op.Type == Enums.OperationType.Purchase.GetDescription())
                        {
                            purchaseTotalPrice = op.Price * op.Quantity;
                            purchaseQuantity = op.Quantity;

                            count++;
                        }
                        else if (op.Type == Enums.OperationType.Sale.GetDescription())
                        {
                            soldTotalPrice += (op.Price * op.Quantity);
                            soldQuantity += op.Quantity;

                            purchaseTotalPrice -= (purchaseTotalPrice / purchaseQuantity) * op.Quantity;
                            purchaseQuantity -= op.Quantity;
                        }
                        else
                        {
                            purchaseTotalPrice += (op.Price * op.Quantity);
                            purchaseQuantity += op.Quantity;
                        }
                    }

                    decimal deadlineSalesTotalPrice = operations.Where(x => x.Asset == item.Key 
                                                            && x.Type == Enums.OperationType.Sale.GetDescription()
                                                            && x.Date >= startDate
                                                            && x.Date <= endDate).Sum(x => x.Price * x.Quantity);

                    int deadlineSalesQuantity = operations.Where(x => x.Asset == item.Key
                                        && x.Type == Enums.OperationType.Sale.GetDescription()
                                        && x.Date >= startDate
                                        && x.Date <= endDate).Sum(x => x.Quantity);


                    if (purchaseQuantity > deadlineSalesQuantity)
                    {
                        int posDateSoldQuantity = 0;
                        int inWalletQuantity = listWallet.Where(x => x.Asset == item.Key).Select(x => x.Quantity).Sum();
                        decimal inWalletValue = listWallet.Where(x => x.Asset == item.Key).Select(x => x.Price * x.Quantity).Sum();

                        posDateSoldQuantity = operations.Where(x => x.Asset == item.Key && x.Type == Enums.OperationType.Sale.GetDescription() && x.Date > endDate).Sum(x => x.Quantity);

                        decimal posDateSoldValue = (purchaseTotalPrice / purchaseQuantity) * posDateSoldQuantity;

                        purchaseTotalPrice -= inWalletValue;
                        purchaseTotalPrice -= posDateSoldValue;
                    }

                    if (parameters.Count > 0)
                    {
                        purchaseTotalPrice      -= ((purchaseTotalPrice) * (parameters[0].TradingFee / 100));
                        deadlineSalesTotalPrice -= ((deadlineSalesTotalPrice) * (parameters[0].LiquidityFee / 100));
                    }

                    operationReturn.Quantity = deadlineSalesQuantity;

                    operationReturn.RentabilityValue = deadlineSalesTotalPrice - purchaseTotalPrice;
                    operationReturn.RentabilityPercentage = operationReturn.RentabilityValue / purchaseTotalPrice;

                    rentabilityTotalValue += operationReturn.RentabilityValue;
                    rentabilityTotalPercentage += operationReturn.RentabilityPercentage;

                    operationReturn.Asset = item.Key;

                    listOperation.Add(operationReturn);

                    operationReturn = new Operation();

                    registerSoldQuantity++;
                }
            }

            listOperation = listOperation.OrderBy(x => x.Asset).ToList();

            return listOperation;
        }

        public IList<Operation> WalletProcess(List<Operation> operations)
        {
            IList<Operation> listOperation = new List<Operation>();

            var listGroup = operations.GroupBy(x => x.Asset);

            foreach (var item in listGroup)
            {
                Operation operation = new Operation();

                int quantityPurchased;
                int quantitySold;
                decimal pricePurchased;
                decimal priceSold;

                int count = 0;
                decimal averagePrice = 0;
                decimal quantity = 0;

                quantityPurchased = operations.Where(x => x.Asset == item.Key && x.Type == Enums.OperationType.Purchase.GetDescription()).Sum(x => x.Quantity);
                quantitySold = operations.Where(x => x.Asset == item.Key && x.Type == Enums.OperationType.Sale.GetDescription()).Sum(x => x.Quantity);

                operation.Quantity = quantityPurchased - quantitySold;

                if (operation.Quantity > 0)
                {
                    pricePurchased = operations.Where(x => x.Asset == item.Key && x.Type == Enums.OperationType.Purchase.GetDescription()).Select(x => x.Price * x.Quantity).Sum();
                    priceSold = operations.Where(x => x.Asset == item.Key && x.Type == Enums.OperationType.Sale.GetDescription()).Select(x => x.Price * x.Quantity).Sum();

                    IList<Operation> listByAsset = operations.Where(x => x.Asset == item.Key).OrderBy(x => x.Date).ToList();

                    foreach (var op in listByAsset)
                    {
                        if (count == 0 && op.Type == Enums.OperationType.Purchase.GetDescription())
                        {
                            averagePrice = op.Price;
                            quantity = op.Quantity;

                            count++;
                        }
                        else if (op.Type == Enums.OperationType.Sale.GetDescription())
                            quantity -= op.Quantity;
                        else
                        {
                            averagePrice = ((averagePrice * quantity) + (op.Price * op.Quantity)) / (quantity + op.Quantity);
                            quantity += op.Quantity;
                        }
                    }

                    operation.Price = averagePrice;
                    operation.Asset = item.Key;
                    operation.InvestValue = operation.Price * operation.Quantity;

                    listOperation.Add(operation);
                }
            }

            return listOperation.OrderByDescending(x => x.InvestValue).ThenBy(x => x.Asset).ToList();
        }

        public async Task<IList<Operation>> GetLiquidation(IList<Operation> listOperation, IList<Operation> listWallet, IList<Parameter> listParameter)
        {
            IList<Operation> operations = new List<Operation>();

            decimal rentabilityTotalValue = 0;
            decimal rentabilityTotalPercentage = 0;
            int registerSoldQuantity = 0;

            var listGroup = listOperation.Where(x => x.Type == Enums.OperationType.Purchase.GetDescription()).GroupBy(x => x.Asset);

            foreach (var item in listGroup)
            {
                Operation operation = new Operation();

                int soldQuantity = listOperation.Where(x => x.Asset == item.Key && x.Type == Enums.OperationType.Sale.GetDescription()).Sum(x => x.Quantity);

                if (soldQuantity > 0)
                {
                    int count = 0;
                    decimal purchaseTotalPrice = 0;
                    decimal soldTotalPrice = 0;
                    int purchaseQuantity = 0;
                    soldQuantity = 0;

                    IList<Operation> listByAsset = listOperation.Where(x => x.Asset == item.Key).OrderBy(x => x.Date).ToList();

                    foreach (var op in listByAsset)
                    {
                        if (count == 0 && op.Type == Enums.OperationType.Purchase.GetDescription())
                        {
                            purchaseTotalPrice = op.Price * op.Quantity;
                            purchaseQuantity = op.Quantity;

                            count++;
                        }
                        else if (op.Type == Enums.OperationType.Sale.GetDescription())
                        {
                            soldTotalPrice += (op.Price * op.Quantity);
                            soldQuantity += op.Quantity;
                        }
                        else
                        {
                            purchaseTotalPrice += (op.Price * op.Quantity);
                            purchaseQuantity += op.Quantity;
                        }
                    }

                    if (purchaseQuantity > soldQuantity)
                        purchaseTotalPrice -= listWallet.Where(x => x.Asset == item.Key).Select(x => x.Price * x.Quantity).Sum();

                    purchaseTotalPrice -= ((purchaseTotalPrice) * (listParameter[0].TradingFee / 100));
                    soldTotalPrice -= ((soldTotalPrice) * (listParameter[0].LiquidityFee / 100));

                    operation.Quantity = soldQuantity;

                    operation.RentabilityValue = soldTotalPrice - purchaseTotalPrice;
                    operation.RentabilityPercentage = operation.RentabilityValue / purchaseTotalPrice;

                    rentabilityTotalValue += operation.RentabilityValue;
                    rentabilityTotalPercentage += operation.RentabilityPercentage;

                    operation.Asset = item.Key;

                    operations.Add(operation);

                    registerSoldQuantity++;
                }
            }

            return await Task.Run(() => operations);
        }
    }

    [Serializable]
    internal class NotFoundException : Exception
    {
        public NotFoundException()
        {
        }

        public NotFoundException(string message) : base(message)
        {
        }

        public NotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    internal class IntegrityException : Exception
    {
        public IntegrityException()
        {
        }

        public IntegrityException(string message) : base(message)
        {
        }

        public IntegrityException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected IntegrityException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}

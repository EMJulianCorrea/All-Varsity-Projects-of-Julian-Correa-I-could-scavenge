using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoffeeNChill.Functions.Models;

namespace CoffeeNChill.Functions.Repository;

public interface IOrderRepository
{
    Task<OrderEntity> CreateAsync(OrderEntity entity, CancellationToken cancellationToken = default);
    Task<OrderEntity> GetAsync(string orderDate, string orderId, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(string orderDate, string orderId, string status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderEntity>> GetActiveOrdersAsync(CancellationToken cancellationToken = default);
    Task<OrderEntity?> FindByOrderIdAsync(string orderId, CancellationToken cancellationToken = default);
}
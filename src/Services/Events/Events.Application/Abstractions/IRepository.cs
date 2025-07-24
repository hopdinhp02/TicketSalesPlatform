﻿using SharedKernel;

namespace TicketFlow.Events.Application.Abstractions
{
    public interface IRepository<T>
        where T : AggregateRoot<Guid>
    {
        Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        void Add(T entity);
    }
}

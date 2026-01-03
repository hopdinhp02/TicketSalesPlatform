using MediatR;
using TicketSalesPlatform.Events.Application.Abstractions;
using TicketSalesPlatform.Events.Domain.Aggregates;

namespace TicketSalesPlatform.Events.Application.PublishEvent
{
    public class PublishEventCommandHandler : IRequestHandler<PublishEventCommand>
    {
        private readonly IRepository<Event> _repository;
        private readonly IUnitOfWork _unitOfWork;

        public PublishEventCommandHandler(IRepository<Event> repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(PublishEventCommand request, CancellationToken cancellationToken)
        {
            var @event = await _repository.GetByIdAsync(request.EventId, cancellationToken);

            if (@event is null)
                throw new KeyNotFoundException($"Event {request.EventId} not found");

            @event.Publish();

            _repository.Update(@event);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}

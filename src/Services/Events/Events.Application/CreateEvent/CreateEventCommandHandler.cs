using TicketFlow.Events.Application.Abstractions;
using TicketFlow.Events.Domain.Aggregates;
using MediatR;

namespace TicketFlow.Events.Application.CreateEvent
{
    public sealed class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, Guid>
    {
        private readonly IRepository<Event> _eventRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateEventCommandHandler(IRepository<Event> eventRepository, IUnitOfWork unitOfWork)
        {
            _eventRepository = eventRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(
            CreateEventCommand request,
            CancellationToken cancellationToken
        )
        {
            var newEvent = Event.Create(request.Title, request.Description, request.Date);

            _eventRepository.Add(newEvent);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return newEvent.Id;
        }
    }
}

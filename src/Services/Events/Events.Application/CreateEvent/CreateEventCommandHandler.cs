using MediatR;
using TicketSalesPlatform.Events.Application.Abstractions;
using TicketSalesPlatform.Events.Domain.Aggregates;

namespace TicketSalesPlatform.Events.Application.CreateEvent
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
            if (request.TicketTypes != null && request.TicketTypes.Any())
            {
                foreach (var tt in request.TicketTypes)
                {
                    newEvent.AddTicketType(tt.Name, tt.Price, tt.Quantity);
                }
            }
            _eventRepository.Add(newEvent);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return newEvent.Id;
        }
    }
}

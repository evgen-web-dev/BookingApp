using BookingApp.Application.DTOs;
using BookingApp.Application.DTOs.Booking;
using BookingApp.Application.Errors;
using BookingApp.Application.Interfaces;
using BookingApp.Domain.Entities;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace BookingApp.Application.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IApartmentRepository _apartmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BookingService> _logger;
    private readonly IMapper _mapper;
    private readonly TimeProvider _timeProvider;

    public BookingService(
        IBookingRepository bookingRepository, 
        IApartmentRepository apartmentRepository, 
        IUnitOfWork unitOfWork, 
        ILogger<BookingService> logger,
        IMapper mapper,
        TimeProvider timeProvider)
    {
        _bookingRepository = bookingRepository;
        _apartmentRepository = apartmentRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mapper = mapper;
        _timeProvider = timeProvider;
    }
    
    public async Task<OperationResult<BookingResponse>> CreateBookingAsync(CreateBookingRequest request, int clientId, CancellationToken cancellationToken)
    {
        if (request.CheckIn.Date < _timeProvider.GetUtcNow().Date)
        {
            return OperationResult<BookingResponse>.Failure([BookingErrorCodes.InvalidCheckInDate]);
        }
        
        if (!await _apartmentRepository.ExistsAsync(request.ApartmentId, cancellationToken))
        {
            return OperationResult<BookingResponse>.Failure([BookingErrorCodes.ApartmentNotFound]);
        }
        
        if (!await _bookingRepository.HasOverlappingBookingAsync(request.ApartmentId, request.CheckIn, request.CheckOut, cancellationToken))
        {
            return OperationResult<BookingResponse>.Failure([BookingErrorCodes.ApartmentNotAvailable]);
        }

        var bookingInstance = new Booking
        {
            ClientId = clientId,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
        };
        _mapper.Map(request, bookingInstance);
        _bookingRepository.Add(bookingInstance);
        
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            await _unitOfWork.CommitAsync(cancellationToken);
            return OperationResult<BookingResponse>.Success(_mapper.Map<BookingResponse>(bookingInstance));
        }
        catch
        {
            if (_unitOfWork.HasActiveTransaction)
            {
                // SafeRollbackAsync will catch and "silence" exception if it was thrown when rolling back a transaction
                await SafeRollbackAsync(cancellationToken);
            }

            throw;
        }
    }

    public async Task<OperationResult<BookingResponse>> GetBookingAsync(int bookingId, int callerId, CancellationToken cancellationToken)
    {
        var bookingInstance = await _bookingRepository.FindByIdAsync(bookingId, cancellationToken);

        if (bookingInstance == null || bookingInstance.ClientId != callerId)
        {
            return OperationResult<BookingResponse>.Failure([BookingErrorCodes.BookingNotFound]);
        }
        
        return OperationResult<BookingResponse>.Success(_mapper.Map<BookingResponse>(bookingInstance));
    }
    
    private async Task SafeRollbackAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed transaction rollback in {ClassName}.{Method}",
                nameof(BookingService),
                nameof(SafeRollbackAsync));
        }
    }
}
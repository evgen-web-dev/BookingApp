using BookingApp.Application.DTOs.Apartment;
using BookingApp.Application.DTOs.Common;
using BookingApp.Application.Interfaces;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;

namespace BookingApp.API.Controllers;

[ApiController]
[Route("api/apartments")]
public class ApartmentsController : ControllerBase
{
    private readonly IApartmentRepository _apartmentRepository;
    private readonly IMapper _mapper;

    public ApartmentsController(IApartmentRepository apartmentRepository, IMapper mapper)
    {
        _apartmentRepository = apartmentRepository;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ApartmentDetailsResponse>>> GetAvailableApartments(
        [FromQuery] AvailableApartmentsPaginatedRequest request, CancellationToken cancellationToken)
    {
        var pagesQueryParams = new PageQueryParams { PageSize = request.PageSize, PageNumber = request.PageNumber };
        
        var availableApartments = await _apartmentRepository.FindAvailableAsync(
            request.AvailableFrom?.Date, 
            request.AvailableTo?.Date, 
            pagesQueryParams,
            cancellationToken);
        
        return Ok(
            PaginatedResponse<ApartmentDetailsResponse>
            .Create(
                _mapper.Map<List<ApartmentDetailsResponse>>(availableApartments.Items),
                pagesQueryParams,
                availableApartments.TotalCount)
        );
    }
}
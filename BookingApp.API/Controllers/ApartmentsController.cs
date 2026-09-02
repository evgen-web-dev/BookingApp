using BookingApp.Application.DTOs.Apartment;
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
    public async Task<ActionResult<AvailableApartmentsResponse>> GetAvailableApartments([FromQuery] AvailableApartmentsRequest request)
    {
        var availableApartments = await _apartmentRepository.FindAvailableAsync(
            request.AvailableFrom?.Date, request.AvailableTo?.Date);
        
        return Ok(new AvailableApartmentsResponse
        {
            Apartments = _mapper.Map<List<ApartmentDetailsResponse>>(availableApartments)
        });
    }
}
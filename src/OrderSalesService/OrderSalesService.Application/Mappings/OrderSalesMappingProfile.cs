using AutoMapper;
using OrderSalesService.Application.DTOs;
using OrderSalesService.Application.Models;

namespace OrderSalesService.Application.Mappings;

public class OrderSalesMappingProfile : Profile
{
    public OrderSalesMappingProfile()
    {
        CreateMap<Order, OrderDto>();
        CreateMap<OrderDetail, OrderDetailDto>();

        CreateMap<Customer, CustomerDto>()
            .ForMember(d => d.CustomerGroupName,
                opt => opt.MapFrom(s => s.CustomerGroup != null ? s.CustomerGroup.Name : null));

        CreateMap<Supplier, SupplierDto>();
        CreateMap<CustomerGroup, CustomerGroupDto>();
    }
}

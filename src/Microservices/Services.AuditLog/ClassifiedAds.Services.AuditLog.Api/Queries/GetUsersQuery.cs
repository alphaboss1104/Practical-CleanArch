﻿using ClassifiedAds.Application;
using ClassifiedAds.Infrastructure.Grpc;
using ClassifiedAds.Services.AuditLog.DTOs;
using ClassifiedAds.Services.Identity.Grpc;
using Grpc.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassifiedAds.Services.AuditLog.Queries
{
    public class GetUsersQuery : IQuery<List<UserDTO>>
    {
        public bool IncludeClaims { get; set; }
        public bool IncludeUserRoles { get; set; }
        public bool IncludeRoles { get; set; }
        public bool AsNoTracking { get; set; }
    }

    public class GetUsersQueryHandler : IQueryHandler<GetUsersQuery, List<UserDTO>>
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GetUsersQueryHandler(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public List<UserDTO> Handle(GetUsersQuery query)
        {
            var token = _httpContextAccessor.HttpContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).GetAwaiter().GetResult();
            var headers = new Metadata
            {
                { "Authorization", $"Bearer {token}" },
            };

            var client = new User.UserClient(ChannelFactory.Create(_configuration["Services:Identity:Grpc"]));
            var response = client.GetUsersAsync(new GetUsersRequest(), headers).GetAwaiter().GetResult();

            return response.Users.Select(x => new UserDTO
            {
                Id = Guid.Parse(x.Id),
                UserName = x.UserName,
                Email = x.Email,
            }).ToList();
        }
    }
}

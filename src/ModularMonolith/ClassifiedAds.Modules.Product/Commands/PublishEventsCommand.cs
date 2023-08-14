﻿using ClassifiedAds.Application;
using ClassifiedAds.Contracts.AuditLog.Services;
using ClassifiedAds.CrossCuttingConcerns.OS;
using ClassifiedAds.Domain.Repositories;
using ClassifiedAds.Modules.Product.Entities;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ClassifiedAds.Modules.Product.Commands;

public class PublishEventsCommand : ICommand
{
    public int SentEventsCount { get; set; }
}

public class PublishEventsCommandHandler : ICommandHandler<PublishEventsCommand>
{
    private readonly ILogger<PublishEventsCommandHandler> _logger;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRepository<OutboxEvent, long> _outboxEventRepository;
    private readonly IAuditLogService _externalAuditLogService;

    public PublishEventsCommandHandler(ILogger<PublishEventsCommandHandler> logger,
        IDateTimeProvider dateTimeProvider,
        IRepository<OutboxEvent, long> outboxEventRepository,
        IAuditLogService externalAuditLogService)
    {
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
        _outboxEventRepository = outboxEventRepository;
        _externalAuditLogService = externalAuditLogService;
    }

    public async Task HandleAsync(PublishEventsCommand command, CancellationToken cancellationToken = default)
    {
        var events = _outboxEventRepository.GetQueryableSet()
            .Where(x => !x.Published)
            .OrderBy(x => x.CreatedDateTime)
            .Take(50)
            .ToList();

        foreach (var eventLog in events)
        {
            if (eventLog.EventType == "AUDIT_LOG_ENTRY_CREATED")
            {
                var logEntry = JsonSerializer.Deserialize<Contracts.AuditLog.DTOs.AuditLogEntryDTO>(eventLog.Message);
                await _externalAuditLogService.AddAsync(logEntry);
            }
            else
            {
                // TODO: Take Note
            }

            eventLog.Published = true;
            eventLog.UpdatedDateTime = _dateTimeProvider.OffsetNow;
            await _outboxEventRepository.UnitOfWork.SaveChangesAsync();
        }

        command.SentEventsCount = events.Count;
    }
}
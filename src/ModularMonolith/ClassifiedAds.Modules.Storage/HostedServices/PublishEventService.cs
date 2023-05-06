﻿using ClassifiedAds.CrossCuttingConcerns.OS;
using ClassifiedAds.Domain.Infrastructure.MessageBrokers;
using ClassifiedAds.Domain.Repositories;
using ClassifiedAds.Modules.AuditLog.Contracts.Services;
using ClassifiedAds.Modules.Storage.DTOs;
using ClassifiedAds.Modules.Storage.Entities;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ClassifiedAds.Modules.Storage.HostedServices;

public class PublishEventService
{
    private readonly ILogger<PublishEventService> _logger;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRepository<OutboxEvent, long> _outboxEventRepository;
    private readonly IMessageSender<FileUploadedEvent> _fileUploadedEventSender;
    private readonly IMessageSender<FileDeletedEvent> _fileDeletedEventSender;
    private readonly IAuditLogService _externalAuditLogService;

    public PublishEventService(ILogger<PublishEventService> logger,
        IDateTimeProvider dateTimeProvider,
        IRepository<OutboxEvent, long> outboxEventRepository,
        IMessageSender<FileUploadedEvent> fileUploadedEventSender,
        IMessageSender<FileDeletedEvent> fileDeletedEventSender,
        IAuditLogService externalAuditLogService)
    {
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
        _outboxEventRepository = outboxEventRepository;
        _fileUploadedEventSender = fileUploadedEventSender;
        _fileDeletedEventSender = fileDeletedEventSender;
        _externalAuditLogService = externalAuditLogService;
    }

    public async Task<int> PublishEvents()
    {
        var events = _outboxEventRepository.GetAll()
            .Where(x => !x.Published)
            .OrderBy(x => x.CreatedDateTime)
            .Take(50)
            .ToList();

        foreach (var eventLog in events)
        {
            if (eventLog.EventType == "FILEENTRY_CREATED")
            {
                await _fileUploadedEventSender.SendAsync(new FileUploadedEvent { FileEntry = JsonSerializer.Deserialize<FileEntry>(eventLog.Message) });
            }
            else if (eventLog.EventType == "FILEENTRY_DELETED")
            {
                await _fileDeletedEventSender.SendAsync(new FileDeletedEvent { FileEntry = JsonSerializer.Deserialize<FileEntry>(eventLog.Message) });
            }
            else if (eventLog.EventType == "AUDIT_LOG_ENTRY_CREATED")
            {
                var logEntry = JsonSerializer.Deserialize<AuditLog.Contracts.DTOs.AuditLogEntryDTO>(eventLog.Message);
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

        return events.Count;
    }
}

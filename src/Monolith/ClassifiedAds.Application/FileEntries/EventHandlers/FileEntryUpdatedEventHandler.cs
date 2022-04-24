﻿using ClassifiedAds.CrossCuttingConcerns.ExtensionMethods;
using ClassifiedAds.Domain.Entities;
using ClassifiedAds.Domain.Events;
using ClassifiedAds.Domain.Identity;
using ClassifiedAds.Domain.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ClassifiedAds.Application.FileEntries.EventHandlers
{
    public class FileEntryUpdatedEventHandler : IDomainEventHandler<EntityUpdatedEvent<FileEntry>>
    {
        private readonly ICrudService<AuditLogEntry> _auditSerivce;
        private readonly ICurrentUser _currentUser;
        private readonly IRepository<EventLog, long> _eventLogRepository;

        public FileEntryUpdatedEventHandler(ICrudService<AuditLogEntry> auditSerivce,
            ICurrentUser currentUser,
            IRepository<EventLog, long> eventLogRepository)
        {
            _auditSerivce = auditSerivce;
            _currentUser = currentUser;
            _eventLogRepository = eventLogRepository;
        }

        public async Task HandleAsync(EntityUpdatedEvent<FileEntry> domainEvent, CancellationToken cancellationToken = default)
        {
            await _auditSerivce.AddOrUpdateAsync(new AuditLogEntry
            {
                UserId = _currentUser.IsAuthenticated ? _currentUser.UserId : Guid.Empty,
                CreatedDateTime = domainEvent.EventDateTime,
                Action = "UPDATED_FILEENTRY",
                ObjectId = domainEvent.Entity.Id.ToString(),
                Log = domainEvent.Entity.AsJsonString(),
            });

            await _eventLogRepository.AddOrUpdateAsync(new EventLog
            {
                EventType = "FILEENTRY_UPDATED",
                TriggeredById = _currentUser.UserId,
                CreatedDateTime = domainEvent.EventDateTime,
                ObjectId = domainEvent.Entity.Id.ToString(),
                Message = domainEvent.Entity.AsJsonString(),
                Published = false,
            }, cancellationToken);

            await _eventLogRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}

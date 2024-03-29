﻿using ClassifiedAds.Domain.Repositories;
using ClassifiedAds.Persistence.Interceptors;
using ClassifiedAds.Persistence.Locks;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ClassifiedAds.Persistence;

public class AdsDbContext : DbContext, IUnitOfWork, IDataProtectionKeyContext
{
    private readonly ILogger<AdsDbContext> _logger;

    private IDbContextTransaction _dbContextTransaction;

    public AdsDbContext(DbContextOptions<AdsDbContext> options,
        ILogger<AdsDbContext> logger)
        : base(options)
    {
        _logger = logger;
    }

    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

    public async Task<IDisposable> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default)
    {
        _dbContextTransaction = await Database.BeginTransactionAsync(isolationLevel, cancellationToken);
        return _dbContextTransaction;
    }

    public async Task<IDisposable> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, string lockName = null, CancellationToken cancellationToken = default)
    {
        _dbContextTransaction = await Database.BeginTransactionAsync(isolationLevel, cancellationToken);

        var sqlLock = new SqlDistributedLock(_dbContextTransaction.GetDbTransaction() as SqlTransaction);
        var lockScope = sqlLock.Acquire(lockName);
        if (lockScope == null)
        {
            throw new Exception($"Could not acquire lock: {lockName}");
        }

        return _dbContextTransaction;
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _dbContextTransaction.CommitAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(new SelectWithoutWhereCommandInterceptor(_logger));
        optionsBuilder.AddInterceptors(new SelectWhereInCommandInterceptor(_logger));
    }
}

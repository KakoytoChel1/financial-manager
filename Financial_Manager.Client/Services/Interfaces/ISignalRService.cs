using AccountingTool;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Financial_Manager.Client.Services.Interfaces
{
    public interface ISignalRService
    {
        public event Action<string, User>? OnConfirmCodeReceived;
        public event Action<string>? OnConfirmErrorReceived;

        public event Action<List<FinancialOperation>, List<OperationCategory>>? OnDataCollectionsReceived;

        public event Action? OnAccessErrorReceived;
        public event Action? OnSyncWarningReceived;

        public event Action? OnConnected;
        public event Action? OnDisconnected;

        public Task StartAsync();

        public Task StopAsync();

        public Task SendUserConfirmationCode(long userId);

        public Task RequestDataCollections(long userId);

        public Task SyncLocalDataWithServer(long userId, List<FinancialOperation> financialOperations, List<OperationCategory> operationCategories);

        public Task AddNewItemToDataCollections<TEntity>(TEntity item, long userId) where TEntity : class, new();

        public Task RemoveItemsFromDataCollections<TEntity>(List<TEntity> items, long userId) where TEntity : class, new();

        public Task UpdateItemInDataCollections<TEntity>(TEntity item, long userId) where TEntity : class, new();

        public void InitializeConnection();

        public HubConnectionState? GetConnectionState();
    }
}

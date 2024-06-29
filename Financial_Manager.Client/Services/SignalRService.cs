using Financial_Manager.Client.Services.Interfaces;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;
using AccountingTool;
using System.Collections.Generic;
using SQLitePCL;

namespace Financial_Manager.Client.Services
{
    public class SignalRService : ISignalRService
    {
        private HubConnection? _connection;
        private readonly IConfigurationService _configurationService;

        public event Action<string, User>? OnConfirmCodeReceived;
        public event Action<string>? OnConfirmErrorReceived;

        public event Action<List<FinancialOperation>, List<OperationCategory>>? OnDataCollectionsReceived;

        public event Action? OnConnected;
        public event Action? OnDisconnected;

        public event Action? OnAccessErrorReceived;
        public event Action? OnSyncWarningReceived;

        public SignalRService(IConfigurationService configurationService)
        {
            _configurationService = configurationService;
        }

        public void InitializeConnection()
        {
            var config = _configurationService.GetConfigurationSettings();

            if (config == null)
                throw new ArgumentNullException("Configuration Error: The configuration file is invalid or could not be found.");

            var connectionString = config.ServerConnectionString;

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException("Connection string error: The connection string could not be found.");

            _connection = new HubConnectionBuilder()
                .WithUrl(connectionString)
                .Build();

            _connection.Closed += _connection_Closed;

            _connection.On("ReceiveUserAccessError", () =>
            {
                OnAccessErrorReceived?.Invoke();
            });
            _connection.On("ReceiveSyncWarning", () =>
            {
                OnSyncWarningReceived?.Invoke();
            });
            _connection.On<string, User>("ReceiveConfirmationCode", (confirmCode, user) =>
            {
                OnConfirmCodeReceived?.Invoke(confirmCode, user);
            });
            _connection.On<string>("ReceiveConfirmationError", (errorMessage) =>
            {
                OnConfirmErrorReceived?.Invoke(errorMessage);
            });
            _connection.On<List<FinancialOperation>, List<OperationCategory>>("ReceiveDataCollections", (financialOperations, operationCategories) =>
            {
                OnDataCollectionsReceived?.Invoke(financialOperations, operationCategories);
            });
        }

        private Task _connection_Closed(Exception? arg)
        {
            OnDisconnected?.Invoke();
            return Task.CompletedTask;
        }

        public async Task StartAsync()
        {
            if(_connection != null)
            {
                await _connection.StartAsync();
                OnConnected?.Invoke();
            }
        }

        public async Task SendUserConfirmationCode(long userId)
        {
            if(_connection != null)
            {
                await _connection.SendAsync("SendConfirmationRequestToUser", userId);
            }
        }

        public async Task RequestDataCollections(long userId)
        {
            if(_connection != null)
            {
                await _connection.SendAsync("SendDataCollections", userId);
            }
        }

        public async Task SyncLocalDataWithServer(long userId, List<FinancialOperation> financialOperations, 
            List<OperationCategory> operationCategories)
        {
            if(_connection != null)
            {
                await _connection.SendAsync("UpdateDataCollection", userId, financialOperations, operationCategories);
            }
        }

        public async Task AddNewItemToDataCollections<TEntity>(TEntity item, long userId) where TEntity : class, new()
        {
            if (_connection != null)
            {
                if (typeof(TEntity) == typeof(FinancialOperation))
                {
                    await _connection.SendAsync("AddReceivedNewItem", userId, item, null);
                }
                else if (typeof(TEntity) == typeof(OperationCategory))
                {
                    await _connection.SendAsync("AddReceivedNewItem", userId, null, item);
                }
            }
        }

        public async Task RemoveItemsFromDataCollections<TEntity>(List<TEntity> items, long userId) where TEntity : class, new()
        {
            if (_connection != null)
            {
                if (typeof(TEntity) == typeof(FinancialOperation))
                {
                    await _connection.SendAsync("RemoveReceivedItems", userId, items, null);
                }
                else if (typeof(TEntity) == typeof(OperationCategory))
                {
                    await _connection.SendAsync("RemoveReceivedItems", userId, null, items);
                }
            }
        }

        public async Task UpdateItemInDataCollections<TEntity>(TEntity item, long userId) where TEntity : class, new()
        {
            if (_connection != null)
            {
                if (typeof(TEntity) == typeof(FinancialOperation))
                {
                    await _connection.SendAsync("UpdateReceivedItem", userId, item, null);
                }
                else if (typeof(TEntity) == typeof(OperationCategory))
                {
                    await _connection.SendAsync("UpdateReceivedItem", userId, null, item);
                }
            }
        }

        public async Task StopAsync()
        {
            if(_connection != null)
            {
                await _connection.StopAsync();
                await _connection.DisposeAsync();
            }
        }

        public HubConnectionState? GetConnectionState()
        {
            if (_connection != null)
                return _connection.State;
            return null;
        }
    }
}

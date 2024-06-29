using AccountingTool;
using DataBaseAccess;
using Financial_Manager.Server.Model;
using Financial_Manager.Server.Services;
using Microsoft.AspNetCore.SignalR;

namespace Financial_Manager.Server.Hubs
{
    public class MainHub : Hub
    {
        private readonly ApplicationContext _dbContext;
        private readonly DbAccess _dbAccess;
        private readonly TelegramBotService _botService;

        public MainHub(ApplicationContext dbContext, TelegramBotService botService)
        {
            _dbContext = dbContext;
            _dbAccess = new DbAccess(dbContext);
            _botService = botService;
        }

        private bool IsUserHasAccess(long userId)
        {
            var user = _dbAccess.Get<User>(u => u.UserId == userId);

            if (user != null && user.UserStatus == User.UserStates.Logged)
                return true;

            return false;
        }

        private bool IsUserHasAccess(long userId, ref User user_)
        {
            var user = _dbAccess.Get<User>(u => u.UserId == userId);

            if (user != null && user.UserStatus == User.UserStates.Logged)
            {
                user_ = user;
                return true;
            }

            return false;
        }

        public async Task SendConfirmationRequestToUser(long userId)
        {
            try
            {
                string result;
                User user = new User();

                if (IsUserHasAccess(userId, ref user))
                {
                    result = await _botService.SendConfirmationRequestToUserAsync(userId);
                    await Clients.Caller.SendAsync("ReceiveConfirmationCode", result, user);
                }
                else
                {
                    result = $"User '{userId}' doesn't exist or has no access.";
                    await Clients.Caller.SendAsync("ReceiveConfirmationError", result);
                }
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ReceiveConfirmationError", ex.Message);
            }
        }

        public async Task SendDataCollections(long userId)
        {
            try
            {
                if (IsUserHasAccess(userId))
                {
                    var financialOperations = _dbAccess.GetAll<FinancialOperation>().ToList();
                    var operationCategories = _dbAccess.GetAll<OperationCategory>().ToList();

                    await Clients.Caller.SendAsync("ReceiveDataCollections", financialOperations, operationCategories);
                }
                else
                {
                    await Clients.Caller.SendAsync("ReceiveUserAccessError");
                }
            }
            catch(Exception ex)
            {
                await Clients.Caller.SendAsync("ReceiveDataSyncError", ex.Message);
            }
        }

        public async Task UpdateDataCollection(long userId,
            List<FinancialOperation> financialOperations, List<OperationCategory> operationCategories)
        {
            try
            {
                if (IsUserHasAccess(userId))
                {
                    await _dbAccess.UpdateEntitiesAsync(operationCategories);
                    await _dbAccess.UpdateEntitiesAsync(financialOperations);

                    await Clients.Others.SendAsync("ReceiveSyncWarning");
                }
                else
                {
                    await Clients.Caller.SendAsync("ReceiveUserAccessError");
                }
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ReceiveDataUpdatingError", ex);
            }
        }

        // Adding
        #region ...
        public async Task AddReceivedNewItem(long userId,
            FinancialOperation? financialOperation = null, OperationCategory? operationCategory = null)
        {
            try
            {
                if (IsUserHasAccess(userId))
                {
                    if (operationCategory != null)
                        _dbAccess.Add(operationCategory);
                    else if (financialOperation != null)
                        _dbAccess.Add(financialOperation);

                    await Clients.Others.SendAsync("ReceiveSyncWarning");
                }
                else
                {
                    await Clients.Caller.SendAsync("ReceiveUserAccessError");
                }
            }
            catch(Exception ex)
            {
                await Clients.Caller.SendAsync("ReceiveDataSyncError", ex.Message);
            }
        }
        #endregion

        // Deleting
        #region ...
        public async Task RemoveReceivedItems(long userId,
            List<FinancialOperation>? financialOperations = null, List<OperationCategory>? operationCategories = null)
        {
            try
            {
                if (IsUserHasAccess(userId))
                {
                    if (operationCategories != null)
                        _dbAccess.RemoveRange(operationCategories);
                    else if (financialOperations != null)
                        _dbAccess.RemoveRange(financialOperations);

                    await Clients.Others.SendAsync("ReceiveSyncWarning");
                }
                else
                {
                    await Clients.Caller.SendAsync("ReceiveUserAccessError");
                }
            }
            catch(Exception ex)
            {
                await Clients.Caller.SendAsync("ReceiveDataSyncError", ex.Message);
            }
        }
        #endregion

        // Updating
        #region ...
        public async Task UpdateReceivedItem(long userId,
            FinancialOperation? financialOperation = null, OperationCategory? operationCategory = null)
        {
            try
            {
                if (IsUserHasAccess(userId))
                {
                    if (operationCategory != null)
                        _dbAccess.Update(operationCategory);
                    else if (financialOperation != null)
                        _dbAccess.Update(financialOperation);

                    await Clients.Others.SendAsync("ReceiveSyncWarning");
                }
                else
                {
                    await Clients.Caller.SendAsync("ReceiveUserAccessError");
                }
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ReceiveDataSyncError", ex.Message);
            }
        }
        #endregion
    }
}

using AccountingTool;
using DataBaseAccess;
using Financial_Manager.Server.Model;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace Financial_Manager.Server.Services
{
    public class TelegramBotService
    {
        private readonly string _greetingMessage = "👋 Welcome! \nIt looks like you don't have permission, click the button below to request a login.";

        private readonly string _botToken;
        private readonly IServiceScopeFactory _scopeFactory;
        private int _financialPageSize = 10;
        private int _categoryPageSize = 5;

        private enum Elements
        {
            Previous,
            Next
        }

        private enum KeyboardUpdateTypes
        {
            MainMenu,
            CategorySelectorMenu
        }

        public TelegramBotService(IServiceScopeFactory scopeFactory, IOptions<TelegramBotSettings> botSettings)
        {
            _botToken = botSettings.Value.BotToken;
            _scopeFactory = scopeFactory;
        }

        public void StartReceivingUpdates()
        {
            try
            {
                // bot client initialization
                TelegramBotClient botClient = new TelegramBotClient(_botToken);

                botClient.StartReceiving(
                    HandleUpdatesAsync,
                    HandleErrorAsync
                );

                Console.WriteLine("Bot start receiving updates.");
            }
            catch (Exception ex) { Console.WriteLine($"Error when starting to receive updates: {ex.Message}"); }
        }

        // updates handler
        private async Task HandleUpdatesAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Type == UpdateType.Message && update.Message?.Text != null && IsInWhiteList(botClient, update.Message.From!.Id).Result)
                {
                    await ProcessMessageAsync(botClient, update.Message);
                }
                else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
                {
                    await ProcessCallbackQueryAsync(botClient, update.CallbackQuery);
                    await botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
                }
            }
            catch (Exception ex) { Console.WriteLine($"Error during update processing: {ex.Message}"); }
        }

        // errors handler
        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Bot client error during update/receive processing: {exception.Message}");
            return Task.CompletedTask;
        }

        // Processing methods
        #region ...

        // processing possible received commands
        private async Task ProcessMessageAsync(ITelegramBotClient botClient, Message message)
        {
            if (message.Text == "/me")
            {
                await ShowUserInfoAsync(botClient, message.From!.Id);
            }
            else if (message.Text == "/financial_list")
            {
                // send viewer menu
                await SetFinancialMainViewerAsync(botClient, message.From!.Id, false);
            }
        }

        //_dataBaseAccess.GetAll

        // processing callback queries (buttons clicks)
        private async Task ProcessCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

                var callbackData = callbackQuery.Data;
                var user = context.Users.FirstOrDefault(u => u.UserId == callbackQuery.From.Id);
                var messageId = callbackQuery.Message!.MessageId;
                var chatSenderId = callbackQuery.Message.Chat.Id;

                if (callbackData == null)
                    return;


                if (callbackData == "send_registration_request")
                {
                    var userExists = false;

                    if (user != null)
                    {
                        if (user.UserStatus == AccountingTool.User.UserStates.Logged)
                            return;

                        userExists = true;
                    }

                    await SendRegistrationRequest(botClient, callbackQuery.From, userExists);
                }

                else if (IsInWhiteList(botClient, callbackQuery.From.Id).Result)
                {
                    // director's feedback to a new user
                    if (callbackData.StartsWith("accept_login_request_"))
                    {
                        await ProccesUserRequest(botClient, callbackData, chatSenderId, messageId, true);
                    }

                    else if (callbackData.StartsWith("decline_login_request_"))
                    {
                        await ProccesUserRequest(botClient, callbackData, chatSenderId, messageId, false);
                    }

                    // category selector page changing / back to main menu
                    else if (callbackData.StartsWith("category_page_previous_"))
                    {
                        int requestedPage = int.Parse(ExtractAdditionalData(callbackData)!);
                        await SetCategorySelectorViewerKeyboard(botClient, chatSenderId, requestedPage, messageId);
                    }

                    else if (callbackData.StartsWith("category_page_next_"))
                    {
                        int requestedPage = int.Parse(ExtractAdditionalData(callbackData)!);
                        await SetCategorySelectorViewerKeyboard(botClient, chatSenderId, requestedPage, messageId);
                    }

                    else if (callbackData == "back_to_menu")
                    {
                        await SetFinancialMainViewerAsync(botClient, user!.UserId, true, messageId);
                    }

                    // open category selector (setting an another keyboard)
                    else if (callbackData == "open_category_selector")
                    {
                        await ChangeKeyboardAsync(botClient, GenerateSortByCategoryKeyboard(0), chatSenderId, messageId);
                    }

                    // financial viewer page changing
                    else if (callbackData == "previous_viewer_page")
                    {
                        await ProccessViewerPageChanging(botClient, user, messageId, Elements.Previous);
                    }

                    else if (callbackData == "next_viewer_page")
                    {
                        await ProccessViewerPageChanging(botClient, user, messageId, Elements.Next);
                    }

                    // sort by category
                    else if (callbackData.StartsWith("sortby_category_"))
                    {
                        int categoryId = int.Parse(ExtractAdditionalData(callbackData)!);

                        await ProccesChangingUserSortingType(botClient, user,
                            AccountingTool.User.SortingTypes.ByCategory, messageId, categoryId);
                    }

                    else if (callbackData == "sortby_title_asc")
                    {
                        await ProccesChangingUserSortingType(botClient,
                            user, AccountingTool.User.SortingTypes.ByTitleAsc, messageId);
                    }

                    else if (callbackData == "sortby_title_desc")
                    {
                        await ProccesChangingUserSortingType(botClient,
                            user, AccountingTool.User.SortingTypes.ByTitleDesc, messageId);
                    }

                    else if (callbackData == "sortby_amount_asc")
                    {
                        await ProccesChangingUserSortingType(botClient,
                            user, AccountingTool.User.SortingTypes.ByAmountAsc, messageId);
                    }

                    else if (callbackData == "sortby_amount_desc")
                    {
                        await ProccesChangingUserSortingType(botClient,
                            user, AccountingTool.User.SortingTypes.ByAmountDesc, messageId);
                    }

                    else if (callbackData == "sortby_date_asc")
                    {
                        await ProccesChangingUserSortingType(botClient,
                            user, AccountingTool.User.SortingTypes.ByDateAsc, messageId);
                    }

                    else if (callbackData == "sortby_date_desc")
                    {
                        await ProccesChangingUserSortingType(botClient,
                            user, AccountingTool.User.SortingTypes.ByDateDesc, messageId);
                    }

                    else if (callbackData == "sortby_type_income")
                    {
                        await ProccesChangingUserSortingType(botClient,
                            user, AccountingTool.User.SortingTypes.ByTypeIncome, messageId);
                    }

                    else if (callbackData == "sortby_type_expense")
                    {
                        await ProccesChangingUserSortingType(botClient,
                            user, AccountingTool.User.SortingTypes.ByTypeExpense, messageId);
                    }
                }
            }
        }

        private async Task ChangeKeyboardAsync(ITelegramBotClient botClient, InlineKeyboardMarkup keyboard, long userId, int messageId)
        {
            await botClient.EditMessageReplyMarkupAsync(userId, messageId, keyboard);
        }

        private async Task ProccesUserRequest(ITelegramBotClient botClient, string callbackData, long directorId,
            int messageId, bool requestStatus)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

                long userId;

                if (!long.TryParse(ExtractAdditionalData(callbackData), out userId))
                    return;

                var user = context.Users.FirstOrDefault(u => u.UserId == userId)!;

                await NotifyUserAndChangeStatus(botClient, requestStatus, user); // requestStatus - accepted or declined

                await botClient.DeleteMessageAsync(directorId, messageId);

                await botClient.SendTextMessageAsync(directorId,
                    $"Запит користувача:\n" +
                    $"ID - {user.UserId}\n" +
                    $"Ім'я - {user.FirstName} {user.LastName}\n" +
                    $"UserName - {user.TelegramUserName}\n" +
                    $"*було {(requestStatus == true ? "одобрено" : "відхилено")}*",
                    parseMode: ParseMode.Markdown);
            }
        }

        // changing users data when changed the sorting type
        private async Task ProccesChangingUserSortingType(ITelegramBotClient botClient, AccountingTool.User? user,
            AccountingTool.User.SortingTypes sortingType, int messageId, int? categoryId = null)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

                if (user != null)
                {
                    user.SelectedSortingType = sortingType;
                    user.SelectedFinancialViewerIndex = 0;
                    context.Update(user);
                    context.SaveChanges();

                    await SetFinancialMainViewerAsync(botClient, user.UserId, false, messageId, categoryId);
                }
            }
        }

        // changing financial viewer page
        private async Task ProccessViewerPageChanging(ITelegramBotClient botClient,
            AccountingTool.User? user, int messageId, Elements element)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

                if (user != null)
                {
                    var count = context.FinancialOperations.Count();

                    if (element == Elements.Previous)
                        user.SelectedFinancialViewerIndex = Math.Max(0, user.SelectedFinancialViewerIndex - _financialPageSize);

                    else if (element == Elements.Next)
                        user.SelectedFinancialViewerIndex = Math.Min(user.SelectedFinancialViewerIndex + _financialPageSize, count - 1);

                    context.Update(user);
                    context.SaveChanges();
                    await SetFinancialMainViewerAsync(botClient, user.UserId, false, messageId);
                }
            }
        }
        #endregion

        // Sending/updating methods
        #region ...

        // sending information to user about themself
        private async Task ShowUserInfoAsync(ITelegramBotClient botClient, long userId)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

                var user = context.Users.FirstOrDefault(u => u.UserId == userId)!;

                string message =
                    $"ID - `{user.UserId}`\n" +
                    $"Name - {user.FirstName} {user.LastName}\n" +
                    $"Telegram username - {user.TelegramUserName}\n" +
                    $"Role - {user.UserRole}";

                await botClient.SendTextMessageAsync(userId, message, parseMode: ParseMode.Markdown);
            }
        }

        // sending financial viewer
        private async Task SetFinancialMainViewerAsync(ITelegramBotClient botClient, long userId, bool isOnlyKeyboard,
            int? messageId = null, int? categoryId = null)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

                var user = context.Users.FirstOrDefault(u => u.UserId == userId)!;

                int count;

                string message = GenerateFinancialOperationsPageTextMessage(user.SelectedFinancialViewerIndex,
                    out count, user.SelectedSortingType, categoryId);

                if (isOnlyKeyboard && messageId != null)
                {
                    await botClient.EditMessageReplyMarkupAsync(user.UserId, messageId.Value,
                            GenerateViewerKeyboard(user.SelectedFinancialViewerIndex, count, user.SelectedSortingType));
                    return;
                }

                if (messageId != null)
                {
                    await botClient.EditMessageTextAsync(
                    userId,
                    messageId.Value,
                    message,
                    parseMode:
                    ParseMode.Markdown,
                    replyMarkup: GenerateViewerKeyboard(user.SelectedFinancialViewerIndex, count, user.SelectedSortingType));
                }
                else
                {
                    await botClient.SendTextMessageAsync(
                    userId,
                    message,
                    parseMode: ParseMode.Markdown,
                    replyMarkup: GenerateViewerKeyboard(user.SelectedFinancialViewerIndex, count, user.SelectedSortingType));
                }
            }
        }

        // adding new user to the data base, sending registration request to the director
        private async Task SendRegistrationRequest(ITelegramBotClient botClient, Telegram.Bot.Types.User telegramUser, bool userExists)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

                var director = context.Users.FirstOrDefault();

                if (director == null || director.UserRole != AccountingTool.User.UserRoles.Director)
                {
                    await botClient.SendTextMessageAsync(telegramUser.Id, "At the moment, the director of the company is not in the system, please make a request later.");
                    return;
                }

                if (!userExists)
                {
                    context.Add(
                    new AccountingTool.User(telegramUser.Id, telegramUser.FirstName,
                    telegramUser.LastName, telegramUser.Username, AccountingTool.User.UserRoles.Accounter,
                    AccountingTool.User.UserStates.NoAccess, AccountingTool.User.SortingTypes.ByDateDesc));
                    context.SaveChanges();
                }

                // sending request to the director
                await ShowRequestAcceptMessageAsync(botClient, director.UserId, telegramUser);

                await botClient.SendTextMessageAsync(telegramUser.Id, "Your request has been sent, please wait for confirmation from the company's director...");
            }
        }

        // Notifying user about director's decision
        private async Task NotifyUserAndChangeStatus(ITelegramBotClient botClient, bool status, AccountingTool.User user)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

                if (status)
                {
                    user.UpdateData(userStatus: AccountingTool.User.UserStates.Logged);

                    context.Update(user);
                    context.SaveChanges();

                    await botClient.SendTextMessageAsync(user.UserId, "Congratulations, you have been granted access!");
                }
                else
                {
                    await botClient.SendTextMessageAsync(user.UserId, "Unfortunately, your request has been rejected. If there was an error, please call the following number (...)");
                }
            }
        }

        // showing message confirm/decline in the director's chat with bot
        private async Task ShowRequestAcceptMessageAsync(ITelegramBotClient botClient, long directorId, Telegram.Bot.Types.User user)
        {
            var acceptInlineButton = new InlineKeyboardButton("Confirm")
            {
                CallbackData = $"accept_login_request_{user.Id}"
            };

            var declineInlineButton = new InlineKeyboardButton("Decline")
            {
                CallbackData = $"decline_login_request_{user.Id}"
            };

            await botClient.SendTextMessageAsync(
                directorId,
                $"*Login request*\n" +
                $"Id: *{user.Id}*\n" +
                $"Name: *{user.LastName} {user.FirstName}*\n" +
                $"Telegram username: *{user.Username ?? "\"Missing\""}*\n",
                parseMode: ParseMode.Markdown,
                replyMarkup: new InlineKeyboardMarkup(new[] { new[] { acceptInlineButton }, new[] { declineInlineButton } }));
        }

        private async Task SetCategorySelectorViewerKeyboard(ITelegramBotClient botClient, long chatId, int page, int messageId)
        {
            var keyboardRows = GenerateSortByCategoryKeyboard(page);

            await botClient.EditMessageReplyMarkupAsync(
                chatId,
                messageId: messageId,
                replyMarkup: keyboardRows
            );
        }
        #endregion

        // Generating methods
        #region

        // generating of a text message from all financial elements
        private string GenerateFinancialOperationsPageTextMessage(int selectedUserFinancialIndex, out int count,
            AccountingTool.User.SortingTypes currentUserSortingType, int? categoryId = null)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

                var list = GetSortedFinancialList(context.FinancialOperations, currentUserSortingType, categoryId);

                count = list.Count;

                int endIndex = Math.Min(selectedUserFinancialIndex + _financialPageSize, count);

                var message = $"List of financial elements:\n";
                for (int i = selectedUserFinancialIndex; i < endIndex; i++)
                {
                    var financialItem = list[i];
                    var category = context.OperationCategories.FirstOrDefault(op => op.Id == financialItem.CategoryId);

                    message += $"{i + 1}. " +
                        $"`{financialItem.Title}`: " +
                        $"*{(financialItem.Type == OperationType.Income ? '+' : '-')}" +
                        $"{financialItem.Amount}" +
                        $"{financialItem.Currency}* " +
                        $"{financialItem.CreatedOperationDate}\n" +
                        $"Category: `{(category != null ? category.Title : "Without category")}`\n";
                }

                return message;
            }
        }

        // generating keyboard for financial viewer
        private InlineKeyboardMarkup GenerateViewerKeyboard(int currentFinancialElementIndex,
            int count, AccountingTool.User.SortingTypes currentSortingMethod)
        {
            var navigationBtnsRow = new List<InlineKeyboardButton>();

            if (currentFinancialElementIndex > 0)
                navigationBtnsRow.Add(new InlineKeyboardButton("⬅️")
                {
                    CallbackData = "previous_viewer_page"
                });

            if (currentFinancialElementIndex + _financialPageSize < count)
                navigationBtnsRow.Add(new InlineKeyboardButton("➡️")
                {
                    CallbackData = "next_viewer_page"
                });

            var nameSortRow = new List<InlineKeyboardButton>();

            if (currentSortingMethod != AccountingTool.User.SortingTypes.ByTitleAsc)
                nameSortRow.Add(
                new InlineKeyboardButton("📈 sort by title")
                {
                    CallbackData = "sortby_title_asc"
                });

            if (currentSortingMethod != AccountingTool.User.SortingTypes.ByTitleDesc)
                nameSortRow.Add(new InlineKeyboardButton("📉 sort by title")
                {
                    CallbackData = "sortby_title_desc"
                });

            var amountSortRow = new List<InlineKeyboardButton>();

            if (currentSortingMethod != AccountingTool.User.SortingTypes.ByAmountAsc)
                amountSortRow.Add(new InlineKeyboardButton("📈 sort by amount")
                {
                    CallbackData = "sortby_amount_asc"
                });

            if (currentSortingMethod != AccountingTool.User.SortingTypes.ByAmountDesc)
                amountSortRow.Add(new InlineKeyboardButton("📉 sort by amount")
                {
                    CallbackData = "sortby_amount_desc"
                });

            var dateSortRow = new List<InlineKeyboardButton>();
            if (currentSortingMethod != AccountingTool.User.SortingTypes.ByDateAsc)
                dateSortRow.Add(new InlineKeyboardButton("📈 sort by date")
                {
                    CallbackData = "sortby_date_asc"
                });

            if (currentSortingMethod != AccountingTool.User.SortingTypes.ByDateDesc)
                dateSortRow.Add(new InlineKeyboardButton("📉 sort by date")
                {
                    CallbackData = "sortby_date_desc"
                });

            var categorySortRow = new List<InlineKeyboardButton>();

            categorySortRow.Add(new InlineKeyboardButton("📊 sort by category")
            {
                CallbackData = "open_category_selector"
            });

            var typeSortRow = new List<InlineKeyboardButton>();

            if (currentSortingMethod != AccountingTool.User.SortingTypes.ByTypeIncome)
                typeSortRow.Add(new InlineKeyboardButton("📈💲by incomes")
                {
                    CallbackData = "sortby_type_income"
                });

            if (currentSortingMethod != AccountingTool.User.SortingTypes.ByTypeExpense)
                typeSortRow.Add(new InlineKeyboardButton("📉💲by expenses")
                {
                    CallbackData = "sortby_type_expense"
                });

            var inlineRows = new List<List<InlineKeyboardButton>>
            {
                navigationBtnsRow,
                nameSortRow,
                amountSortRow,
                dateSortRow,
                categorySortRow,
                typeSortRow
            };

            return new InlineKeyboardMarkup(inlineRows);
        }

        private InlineKeyboardMarkup GenerateSortByCategoryKeyboard(int currentPage)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

                var categories = context.OperationCategories.ToList();

                int pageCount = (categories.Count + _categoryPageSize - 1) / _categoryPageSize;

                var currentPageCategories = categories.Skip(currentPage * _categoryPageSize).Take(_categoryPageSize).ToList();

                var keyboardRows = currentPageCategories
                    .Select(category => new InlineKeyboardButton(category.Title)
                    {
                        CallbackData = $"sortby_category_{category.Id}"
                    })
                    .Select(button => new List<InlineKeyboardButton> { button })
                    .ToList();

                if (pageCount > 1)
                {
                    var navigationButtons = new List<InlineKeyboardButton>();

                    if (currentPage > 0)
                    {
                        navigationButtons.Add(new InlineKeyboardButton("◀️")
                        {
                            CallbackData = $"category_page_previous_{currentPage - 1}"
                        });
                    }

                    if (currentPage < pageCount - 1)
                    {
                        navigationButtons.Add(new InlineKeyboardButton("▶️")
                        {
                            CallbackData = $"category_page_next_{currentPage + 1}"
                        });
                    }

                    keyboardRows.Add(navigationButtons);
                }

                keyboardRows.Add(new List<InlineKeyboardButton>()
                {
                    new InlineKeyboardButton("<< Back")
                    {
                        CallbackData = "back_to_menu"
                    }
                });

                return new InlineKeyboardMarkup(keyboardRows);
            }
        }
        #endregion

        // Other
        #region ...

        // sorting financial list
        private List<FinancialOperation> GetSortedFinancialList(IEnumerable<FinancialOperation> financialOperations,
            AccountingTool.User.SortingTypes sortingType, int? categoryId = null)
        {
            if (sortingType == AccountingTool.User.SortingTypes.ByCategory && categoryId != null)
                return SortingTool.SortByCategory(financialOperations, categoryId.Value).ToList();

            switch (sortingType)
            {
                case AccountingTool.User.SortingTypes.ByTitleAsc:
                    return SortingTool.SortFinancialsByTitle(financialOperations, SortingTool.SortTypes.Ascending).ToList();

                case AccountingTool.User.SortingTypes.ByTitleDesc:
                    return SortingTool.SortFinancialsByTitle(financialOperations, SortingTool.SortTypes.Descending).ToList();

                case AccountingTool.User.SortingTypes.ByAmountAsc:
                    return SortingTool.SortByAmount(financialOperations, SortingTool.SortTypes.Ascending).ToList();

                case AccountingTool.User.SortingTypes.ByAmountDesc:
                    return SortingTool.SortByAmount(financialOperations, SortingTool.SortTypes.Descending).ToList();

                case AccountingTool.User.SortingTypes.ByDateAsc:
                    return SortingTool.SortByCreatedDate(financialOperations, SortingTool.SortTypes.Ascending).ToList();

                case AccountingTool.User.SortingTypes.ByTypeIncome:
                    return SortingTool.SortByIncome(financialOperations).ToList();

                case AccountingTool.User.SortingTypes.ByTypeExpense:
                    return SortingTool.SortByExpense(financialOperations).ToList();

                default:
                    return SortingTool.SortByCreatedDate(financialOperations, SortingTool.SortTypes.Descending).ToList();
            }
        }

        // extracts the data that is at the end of the callbackquery data string
        private string? ExtractAdditionalData(string inputString)
        {
            string[] parts = inputString.Split('_');
            if (parts.Length > 0)
            {
                return parts[parts.Length - 1];
            }
            return null;
        }

        // checking whether the user has access to use the bot functionality
        private async Task<bool> IsInWhiteList(ITelegramBotClient botClient, long userId)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

                var user = context.Users.FirstOrDefault(u => u.UserId == userId);

                if (user != null && user.UserStatus == AccountingTool.User.UserStates.Logged)
                    return true;

                var inlineButton = new InlineKeyboardButton("Make a request")
                {
                    CallbackData = "send_registration_request"
                };

                await botClient.SendTextMessageAsync(
                    userId,
                    _greetingMessage,
                    parseMode: ParseMode.Markdown,
                    replyMarkup: new InlineKeyboardMarkup(new[] { inlineButton }));

                return false;
            }
        }
        #endregion

        public string GenerateAuthorizationCode()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                int lenght = 10;

                byte[] randomNumber = new byte[lenght];
                rng.GetBytes(randomNumber);

                char[] digits = new char[lenght];
                for (int i = 0; i < lenght; i++)
                {
                    digits[i] = (randomNumber[i] % 10).ToString()[0];
                }

                return new string(digits);
            }
        }

        public async Task<string> SendConfirmationRequestToUserAsync(long userId)
        {
            try
            {
                var code = GenerateAuthorizationCode();

                TelegramBotClient client = new TelegramBotClient(_botToken);

                await client.SendTextMessageAsync(userId, $"An attempt has been made to log in with your Id, you have been provided with a verification code\n" +
                    $"`{code}`.\n" +
                    $"If this is not you, do not share the received code with anyone, and simply ignore this message.",
                    parseMode: ParseMode.Markdown);

                return code;

            }
            catch (Exception) { return "Something went wrong."; }
        }
    }
}

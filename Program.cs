
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Otus.TelegramBot
{
    internal class Program
    {
        //using var cts = new CancellationTokenSource();

        private const string _botKey = "7752213003:AAFid9Mp9tHkvHHo-1GhEzI-N0LW8B6af44";
        private static List<Photo> _photos = new List<Photo>();
        private static string _fileName = "photos.json";

        private static async Task Main()
        {
            var bot = new TelegramBotClient(_botKey);

            var me = await bot.GetMeAsync();
            Console.Title = me.Username ?? "My Bot";
            Console.WriteLine($"My username: {me.Username} ");

            bot.StartReceiving(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandleErrorAsync,
                receiverOptions: new ReceiverOptions()
                {
                    AllowedUpdates = Array.Empty<Telegram.Bot.Types.Enums.UpdateType>()
                },
                cancellationToken: default);

            Console.WriteLine($"Start listening...");

            if (System.IO.File.Exists(_fileName))
            {
                var json = System.IO.File.ReadAllText(_fileName);
                _photos = JsonSerializer.Deserialize<List<Photo>>(json);
            }
            else {
                System.IO.File.Create(_fileName);
            }

            Console.ReadKey();
        }

        private static async Task HandleUpdateAsync(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token)
        {
            try
            {
                switch (update.Type)
                {
                    case Telegram.Bot.Types.Enums.UpdateType.Message:
                        await BotOnMessageReceived(client, update.Message!);
                        break;
                }

            }
            catch (Exception exception)
            {
                await HandleErrorAsync(client, exception, HandleErrorSource.PollingError, token);
            }
        }

        private static async Task BotOnMessageReceived(ITelegramBotClient client, Message message)
        {
            Console.WriteLine($"Receive message type: {message.Type}");

            if (message.Type == MessageType.Photo)
            {
                await StorePhoto(client, message);
                return;
            }


            if (message.Type != MessageType.Text)
                return;

            var action = message.Text!.Split(' ')[0];
            switch (action)
            {
                case "/start":
                    await StartMessage(client, message);
                    break;
                    break;
                case "/find":
                    await FindPhoto(client, message);
                    break;
                default:
                    await Echo(client, message);
                    break;
            }            
        }

        private static async Task StorePhoto(ITelegramBotClient client, Message message)
        {
            if (string.IsNullOrEmpty(message.Caption))
            {
                await client.SendTextMessageAsync(chatId: message.Chat.Id, text: $"Caption is empty!'");
                return;
            }
            if (message?.Photo?.Length == 0)
            {
                await client.SendTextMessageAsync(chatId: message.Chat.Id, text: $"Photo is empty!'");
                return;
            }

            var photo = new Photo()
            {
                Caption = message.Caption,
                FileId = message.Photo[0].FileId
            };
            _photos.Add(photo);

            var json = JsonSerializer.Serialize(_photos);
            System.IO.File.WriteAllText(_fileName, json);
        }

        static async Task StartMessage(ITelegramBotClient client, Message message)
        {
            var userName = $"{message.From.LastName} {message.From.FirstName}";
            await client.SendTextMessageAsync(chatId: message.Chat.Id, text: $"Hello, {userName}!");
        }

        private static async Task Echo(ITelegramBotClient client, Message message)
        {
            await client.SendTextMessageAsync(chatId: message.Chat.Id, text: $"Not found command '{message.Text}'");
        }
                
        private static async Task FindPhoto(ITelegramBotClient client, Telegram.Bot.Types.Message message)
        {
            var caption = message.Text.Replace("/find ", "").Trim();
            var photo = _photos.Find(x => x.Caption == caption);
            if (photo == null)
            {
                await client.SendTextMessageAsync(chatId: message.Chat.Id, text: $"Not found '{caption}'");
                return;
            }

            var file = InputFile.FromFileId(photo.FileId);

            await client.SendChatActionAsync(message.Chat.Id, ChatAction.UploadPhoto);
            await client.SendPhotoAsync(
                chatId: message.Chat.Id, 
                photo: file,
                caption: caption
                );
        }

        private static async Task HandleErrorAsync(ITelegramBotClient client, Exception exception, HandleErrorSource source, CancellationToken token)
        {
            var errorMessage = exception.ToString();

            Console.WriteLine(errorMessage);
        }


    }
}
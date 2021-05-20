using MetalServer.Modules.UniChat.Server.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using static MetalServer.Modules.UniChat.Server.Message.Message.Query;
using static MetalServer.Modules.UniChat.Server.Message.Message.Query.Chat;

namespace MetalServer.Modules.UniChat.Server.Chats {

	public class ChatRoom {

		public Chat Chat { private set; get; }

		public List<User> ConnectedUsers { private set; get; }

		public bool IsPersistent { private set; get; }

		public ChatRoom(Chat chat, bool isPersistent = false) {

			Chat = chat;

			ConnectedUsers = new List<User>();

			IsPersistent = isPersistent;

		}

		public bool AddUser(User user) {

			if(!ConnectedUsers.Contains(user)) {

				AddMessage($"{user.Name} joined the chat", ChatMessage.ChatMessageTypes.META);

				ConnectedUsers.Add(user);

				return true;

			}

			return false;

		}

		public void RemoveUser(User user) {

			if(ConnectedUsers.Contains(user)) {

				ConnectedUsers.Remove(user);

				AddMessage($"{user.Name} left the chat", ChatMessage.ChatMessageTypes.META);

			}

		}

		public void AddMessage(string text, string type, string userName = null, List<ChatFile> files = null) {

			ChatMessage message = new ChatMessage(UniChatModule.GenerateKey(ChatRoomsController.CHAT_KEY_LENGTH)) {

				text = text,
				type = type,
				creator = userName,
				files = files?.Select(file => new ChatFile() {
				
					key = file.key,
					chatKey = file.chatKey
				
				})?.ToList(),
				timestamp = (long) DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds

			};

			Chat.messages.Add(message);

			ConnectedUsers.ForEach(user => user.NewMessage(message, Chat.key));

		}

		public void AddFile(ChatFile file) {

			Chat.files.Add(file);

			ConnectedUsers.ForEach(user => user.NewFile(file, Chat.key));

		}

	}

}